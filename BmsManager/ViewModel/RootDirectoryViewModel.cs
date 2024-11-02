using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Entity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.ViewModel
{
    internal class RootDirectoryViewModel : ObservableObject
    {
        public string Text
        {
            get => entity == null ? string.Empty
                : entity.ParentRootID == null ? entity.Path
                : Path.GetFileName(entity.Path);
        }

        bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set
            {
                // 下位フォルダのロード状況を優先する
                if (Children.Any() && Children.Any(c => c.Descendants().Any(c => c.IsLoading)) != value)
                    return;
                SetProperty(ref isLoading, value);
                if (parent != null && parent.IsLoading != value)
                    parent.IsLoading = value;
            }
        }

        bool isError = false;
        public bool IsError
        {
            get => isError;
            set
            {
                // 下位フォルダのエラー状況を優先する
                if (Children.Any() && Children.Any(c => c.Descendants().Any(c => c.IsError)) != value)
                    return;
                SetProperty(ref isError, value);
                if (parent != null && parent.IsError != value)
                    parent.IsError = value;
            }
        }

        public string FullPath => entity.Path;

        ObservableCollection<BmsFolder> folders = [];
        public ObservableCollection<BmsFolder> Folders
        {
            get => folders;
            set => SetProperty(ref folders, value);
        }

        ObservableCollection<RootDirectoryViewModel> children = [];
        public ObservableCollection<RootDirectoryViewModel> Children
        {
            get => children;
            set => SetProperty(ref children, value);
        }

        public BmsFolder[] DescendantFolders => Descendants().Where(r => r.Folders?.Any() ?? false).SelectMany(r => r.Folders).ToArray();

        public ICommand LoadFromFileSystem { get; }

        public ICommand LoadFromDB { get; }

        public ICommand Remove { get; }

        public int ID => entity.ID;

        public RootDirectory Root => entity;

        readonly RootDirectory entity;

        readonly RootDirectoryViewModel parent;

        readonly RootTreeViewModel tree;

        static readonly object lockObj = new();

        static readonly string[] previewExt = ["wav", "ogg", "mp3", "flac"];

        public RootDirectoryViewModel(RootTreeViewModel tree) : this(tree, new RootDirectory(), true) { }

        public RootDirectoryViewModel(RootTreeViewModel tree, RootDirectory entity, bool isLoading = false, RootDirectoryViewModel parent = null)
        {
            this.tree = tree;
            this.entity = entity;
            this.parent = parent;
            LoadFromFileSystem = new AsyncRelayCommand(loadFromFileSystemAsync);
            Remove = new AsyncRelayCommand(removeAsync);
            LoadFromDB = new AsyncRelayCommand(loadFromDB);
            IsLoading = isLoading;
        }

        public async Task LoadChildAsync(Task loadRootTask)
        {
            try
            {
                if (entity.Children.Count != 0)
                {
                    using var con = new BmsManagerContext();
                    {
                        var childrenEntity = await con.RootDirectories.Where(r => r.ParentRootID == entity.ID)
                            .Include(r => r.Children)
                            .Include(r => r.Folders)
                            .AsNoTracking().ToArrayAsync().ConfigureAwait(false);
                        Folders = [];
                        Children = new ObservableCollection<RootDirectoryViewModel>(childrenEntity.Select(e => new RootDirectoryViewModel(tree, e, true, this)).ToArray());
                    }

                    foreach (var child in children)
                        loadRootTask = loadRootTask.ContinueWith((t) => child.LoadChildAsync(loadRootTask));

                }
                await loadRootTask.ConfigureAwait(false);
                if (entity.Folders.Count != 0)
                    lock (lockObj)
                    {
                        using var con = new BmsManagerContext();
                        var folderEntity = con.BmsFolders.Where(r => r.RootID == entity.ID)
                            .Include(f => f.Files)
                            .AsNoTracking().ToArray();
                        Folders = new ObservableCollection<BmsFolder>(folderEntity);
                    }

                IsLoading = false;
            }
            catch (Exception e)
            {
                SystemProvider.Logger.TraceExceptionLog(e);
                IsError = true;
                IsLoading = false;
            }
        }

        private async Task removeAsync()
        {
            using (var con = new BmsManagerContext())
            {
                inner(FullPath);
                void inner(string path)
                {
                    foreach (var root in con.RootDirectories.Include(r => r.Children).Include(r => r.Folders).ThenInclude(f => f.Files).Where(r => r.Path == path).ToArray())
                    {
                        foreach (var child in root.Children)
                            inner(child.Path);

                        foreach (var folder in root.Folders)
                        {
                            foreach (var file in folder.Files)
                                con.Files.Remove(file);
                            con.BmsFolders.Remove(folder);
                        }
                        con.RootDirectories.Remove(root);
                    }
                }
                await con.SaveChangesAsync().ConfigureAwait(false);
            }
            if (entity.Parent == null)
                tree.RootTree.Remove(this);
            else
                tree.RootTree.SelectMany(r => r.Descendants()).FirstOrDefault(r => r.FullPath == entity.Parent.Path)?.Root.LoadFromDB();
        }

        private async Task loadFromDB()
        {
            await Task.Run(() => entity.LoadFromDB()).ConfigureAwait(false);
        }

        private async Task loadFromFileSystemAsync()
        {
            await loadFromFileSystemAsync(this, tree).ConfigureAwait(false);
        }

        private async Task loadFromFileSystemAsync(RootDirectoryViewModel root, RootTreeViewModel tree)
        {
            root.IsLoading = true;
            if (tree != null)
                tree.LoadingPath = entity.Path;

            var folders = SystemProvider.FileSystem.Directory.EnumerateDirectories(entity.Path);

            using (var con = new BmsManagerContext())
            {
                var delRoot = await con.RootDirectories.Where(c => c.ParentRootID == entity.ID && !folders.Contains(c.Path)).ToArrayAsync().ConfigureAwait(false);
                if (delRoot.Length != 0)
                {
                    con.ChangeTracker.AutoDetectChangesEnabled = false;
                    con.RootDirectories.RemoveRange(delRoot);
                    await con.SaveChangesAsync().ConfigureAwait(false);
                }
                var delFol = await con.BmsFolders.Where(c => c.RootID == entity.ParentRootID && !folders.Contains(c.Path)).ToArrayAsync().ConfigureAwait(false);
                if (delFol.Length != 0)
                {
                    con.ChangeTracker.AutoDetectChangesEnabled = false;
                    con.BmsFolders.RemoveRange(delFol);
                    await con.SaveChangesAsync().ConfigureAwait(false);
                }
            }

            var extentions = Settings.Default.Extentions;

            foreach (var folder in folders)
            {
                if (tree != null)
                    tree.LoadingPath = folder;

                var updateDate = SystemProvider.FileSystem.DirectoryInfo.New(folder).LastWriteTimeUtc;
                var files = SystemProvider.FileSystem.Directory.EnumerateFiles(folder)
                    .Where(f =>
                    extentions.Concat(["txt"]).Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant())
                    || f.StartsWith("preview", StringComparison.CurrentCultureIgnoreCase) && previewExt.Contains(Path.GetExtension(f).Trim('.').ToLowerInvariant())).ToArray();

                var bmsFileDatas = files.Where(f => extentions.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant()))
                    .Select(file => (file, SystemProvider.FileSystem.File.ReadAllBytes(file)));

                if (bmsFileDatas.Any())
                    await loadBmsFolderAsync(folder, files, bmsFileDatas, root).ConfigureAwait(false);
                else
                    await loadRootDirectoryAsync(folder, root, tree).ConfigureAwait(false);
            }

            root.IsLoading = false;
            if (tree != null)
                tree.LoadingPath = "読込完了";
        }

        private async Task loadRootDirectoryAsync(string path, RootDirectoryViewModel parent, RootTreeViewModel tree)
        {
            var updateDate = SystemProvider.FileSystem.DirectoryInfo.New(path).LastWriteTimeUtc;
            RootDirectory root;
            using (var con = new BmsManagerContext())
            {
                root = await con.RootDirectories.FirstOrDefaultAsync(c => c.Path == path).ConfigureAwait(false);
                if (root == default)
                {
                    root = new RootDirectory
                    {
                        Path = path,
                        FolderUpdateDate = updateDate,
                        ParentRootID = parent.ID,
                    };
                    con.RootDirectories.Add(root);
                    await con.SaveChangesAsync().ConfigureAwait(false);
                }
                else
                    // 既存Root
                    if (root.FolderUpdateDate.Date != updateDate.Date
                        && root.FolderUpdateDate.Hour != updateDate.Hour
                        && root.FolderUpdateDate.Minute != updateDate.Minute
                        && root.FolderUpdateDate.Second != updateDate.Second) // 何故かミリ秒がずれるのでミリ秒以外で比較
                {
                    con.RootDirectories.Find(root.ID).FolderUpdateDate = updateDate;
                    await con.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            var model = parent.Children.FirstOrDefault(c => c.FullPath == root.Path);
            if (model == default)
            {
                model = new RootDirectoryViewModel(tree, root, true, parent);
                Application.Current.Dispatcher.Invoke(() => parent.Children.Add(model));
            }
            else
                model.Folders = [];
            await model.loadFromFileSystemAsync(this, tree).ConfigureAwait(false);
        }

        private static async Task loadBmsFolderAsync(string path, IEnumerable<string> files, IEnumerable<(string file, byte[] data)> bmsFileDatas, RootDirectoryViewModel parent, RootTreeViewModel tree = null)
        {
            if (tree != null)
                tree.LoadingPath = path;

            using var con = new BmsManagerContext();
            var bmsFolder = con.BmsFolders.FirstOrDefault(f => f.Path == path);

            // 読込済データの解析なので並列化
            var bmsFiles = bmsFileDatas.AsParallel().Select(d => new BmsFile(d.file, d.data)).Where(f => !string.IsNullOrEmpty(f.Path)).ToArray();
            if (bmsFiles.Length == 0)
            {
                if (bmsFolder != default)
                    con.BmsFolders.Remove(bmsFolder);
                return;
            }

            var updateDate = SystemProvider.FileSystem.DirectoryInfo.New(path).LastWriteTimeUtc;

            var (title, artist) = bmsFiles.GetMetaFromFiles();
            var hasText = files.Any(f => f.ToLower().EndsWith("txt"));
            var preview = files.FirstOrDefault(f => f.StartsWith("preview", StringComparison.CurrentCultureIgnoreCase) && previewExt.Contains(Path.GetExtension(f).Trim('.').ToLowerInvariant()));
            if (bmsFolder == default)
            {
                // 新規Folder
                bmsFolder = new BmsFolder
                {
                    Path = path,
                    Title = title,
                    Artist = artist,
                    FolderUpdateDate = updateDate,
                    HasText = hasText,
                    Preview = preview,
                    Files = bmsFiles,
                    RootID = parent.ID
                };
                await bmsFolder.RegisterAsync().ConfigureAwait(false);
            }
            else
            {
                var childrenFiles = await con.Files.Where(f => f.FolderID == bmsFolder.ID).ToArrayAsync().ConfigureAwait(false);
                var del = childrenFiles.Where(f => !bmsFiles.Any(e => f.MD5 == e.MD5));
                if (del.Any())
                {
                    con.ChangeTracker.AutoDetectChangesEnabled = false;
                    con.Files.RemoveRange(del);
                    await con.SaveChangesAsync().ConfigureAwait(false);
                }
                var upd = bmsFiles.Where(f => !childrenFiles.Any(e => e.MD5 == f.MD5));
                if (upd.Any())
                {
                    foreach (var file in upd)
                    {
                        file.FolderID = bmsFolder.ID;
                        con.Files.Add(file);
                    }
                    await con.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            parent.Folders.Add(bmsFolder);
        }

        public IEnumerable<RootDirectoryViewModel> Descendants()
        {
            yield return this;
            if (Children == null || !Children.Any())
                yield break;
            foreach (var child in Children.SelectMany(c => c.Descendants()))
                yield return child;
        }
    }
}
