using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BmsManager.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.Model
{
    internal class RootTreeModel : ObservableObject
    {
        ObservableCollection<RootDirectoryModel> rootTree;
        public ObservableCollection<RootDirectoryModel> RootTree
        {
            get => rootTree;
            set => SetProperty(ref rootTree, value);
        }

        string loadingPath;
        public string LoadingPath
        {
            get => loadingPath;
            set => SetProperty(ref loadingPath, value);
        }

        readonly string[] previewExt = ["wav", "ogg", "mp3", "flac"];

        public async Task LoadRootTreeAsync()
        {
            RootTree = [new RootDirectoryModel()];

            var con = new BmsManagerContext();

            var roots = await con.RootDirectories.Where(r => r.ParentRootID == null)
                .Include(r => r.Children)
                .Include(r => r.Folders)
                .ThenInclude(r => r.Files)
                .AsNoTracking().ToArrayAsync().ConfigureAwait(false);

            RootTree = new ObservableCollection<RootDirectoryModel>(roots.Select(r => new RootDirectoryModel(r, true)).ToArray());

            foreach (var root in RootTree)
            {
                await root.LoadChildAsync(Task.CompletedTask).ConfigureAwait(false);
            }
        }

        public async Task AddRootAsync(string targetDirectory)
        {
            if (string.IsNullOrWhiteSpace(targetDirectory))
                return;

            using var con = new BmsManagerContext();

            if (con.RootDirectories.Any(f => f.Path == targetDirectory))
            {
                MessageBox.Show("既に登録済のフォルダは登録できません。");
                return;
            }

            var root = new RootDirectory
            {
                Path = targetDirectory,
                FolderUpdateDate = SystemProvider.FileSystem.DirectoryInfo.New(targetDirectory).LastWriteTimeUtc
            };

            var parent = rootTree.SelectMany(r => r.Descendants()).FirstOrDefault(r => r.Root.Path == Path.GetDirectoryName(targetDirectory));
            if (parent != default)
                root.ParentRootID = parent.ID;

            con.RootDirectories.Add(root);
            await con.SaveChangesAsync().ConfigureAwait(false);
            Application.Current.Dispatcher.Invoke(() => (parent?.Children ?? RootTree).Add(new RootDirectoryModel(root)));
        }

        public async Task LoadFromFileSystemAsync(RootDirectoryModel root)
        {
            await root.LoadFromFileSystemAsync(this).ConfigureAwait(false);
        }

        public async Task LoadFromFileSystemAsync(RootDirectory root)
        {
            LoadingPath = root.Path;

            var folders = SystemProvider.FileSystem.Directory.EnumerateDirectories(root.Path);

            using (var con = new BmsManagerContext())
            {
                var delRoot = await con.RootDirectories.Where(c => c.ParentRootID == root.ID && !folders.Contains(c.Path)).ToArrayAsync().ConfigureAwait(false);
                if (delRoot.Length != 0)
                {
                    con.ChangeTracker.AutoDetectChangesEnabled = false;
                    con.RootDirectories.RemoveRange(delRoot);
                    await con.SaveChangesAsync().ConfigureAwait(false);
                }
                var delFol = await con.BmsFolders.Where(c => c.RootID == root.ParentRootID && !folders.Contains(c.Path)).ToArrayAsync().ConfigureAwait(false);
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
                LoadingPath = folder;

                var updateDate = SystemProvider.FileSystem.DirectoryInfo.New(folder).LastWriteTimeUtc;
                var files = SystemProvider.FileSystem.Directory.EnumerateFiles(folder)
                    .Where(f =>
                    extentions.Concat(["txt"]).Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant())
                    || f.StartsWith("preview", StringComparison.CurrentCultureIgnoreCase) && previewExt.Contains(Path.GetExtension(f).Trim('.').ToLowerInvariant())).ToArray();

                var bmsFileDatas = files.Where(f => extentions.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant()))
                    .Select(file => (file, SystemProvider.FileSystem.File.ReadAllBytes(file)));

                if (bmsFileDatas.Any())
                {
                    await loadBmsFolderAsync(folder, files, bmsFileDatas, root).ConfigureAwait(false);
                }
                else
                {
                    await loadRootDirectoryAsync(folder, root).ConfigureAwait(false);
                }
            }

            LoadingPath = "読込完了";
        }

        private async Task loadRootDirectoryAsync(string path, RootDirectory parent)
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
                {
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
            }
            await LoadFromFileSystemAsync(root).ConfigureAwait(false);
        }

        private async Task loadBmsFolderAsync(string path, IEnumerable<string> files, IEnumerable<(string file, byte[] data)> bmsFileDatas, RootDirectory parent)
        {
            LoadingPath = path;

            using var con = new BmsManagerContext();
            var bmsFolder = con.BmsFolders.FirstOrDefault(f => f.Path == path);

            // 読込済データの解析なので並列化
            var bmsFiles = bmsFileDatas.AsParallel().Select(d => new BmsFile(d.file, d.data)).Where(f => !string.IsNullOrEmpty(f.Path)).ToArray();
            if (bmsFiles.Length == 0)
            {
                if (bmsFolder != default)
                {
                    con.BmsFolders.Remove(bmsFolder);
                }
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
        }
    }
}
