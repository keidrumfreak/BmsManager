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
        ObservableCollection<RootDirectory> rootTree;
        public ObservableCollection<RootDirectory> RootTree
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

        readonly string[] previewExt = new[] { "wav", "ogg", "mp3", "flac" };

        public async Task LoadRootTreeAsync()
        {
            RootTree = new ObservableCollection<RootDirectory> { new RootDirectory { Path = "loading..." } };

            var con = new BmsManagerContext();
            var roots = await con.RootDirectories
                .Include(r => r.Folders)
                .ThenInclude(f => f.Files)
                .AsNoTracking().ToArrayAsync().ConfigureAwait(false);

            foreach (var parent in roots)
            {
                parent.Children = roots.Where(r => r.ParentRootID == parent.ID).ToList();
            }
            RootTree = new ObservableCollection<RootDirectory>(roots.Where(r => r.ParentRootID == null).ToArray());
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

            var parent = rootTree.SelectMany(r => r.Descendants()).FirstOrDefault(r => r.Path == Path.GetDirectoryName(targetDirectory));
            if (parent != default)
                root.ParentRootID = parent.ID;

            con.RootDirectories.Add(root);
            await con.SaveChangesAsync().ConfigureAwait(false);
            await LoadRootTreeAsync().ConfigureAwait(false);
        }

        public async Task LoadFromFileSystemAsync(RootDirectory root)
        {
            using var con = new BmsManagerContext();
            var roots = await con.RootDirectories.AsNoTracking().ToArrayAsync().ConfigureAwait(false);
            var folders = await con.BmsFolders.AsNoTracking().ToArrayAsync().ConfigureAwait(false);
            var files = await con.Files.AsNoTracking().ToArrayAsync().ConfigureAwait(false);
            await loadFromFileSystemAsync(root, roots, folders, files).ConfigureAwait(false);
        }

        private async Task loadFromFileSystemAsync(RootDirectory root, ICollection<RootDirectory> cacheRoots, IEnumerable<BmsFolder> cacheFolders, IEnumerable<BmsFile> cacheFiles)
        {
            LoadingPath = root.Path;

            var folders = SystemProvider.FileSystem.Directory.EnumerateDirectories(root.Path);

            var delRoot = cacheRoots.Where(c => c.ParentRootID == root.ID && !folders.Contains(c.Path));
            if (delRoot.Any())
            {
                using var con = new BmsManagerContext();
                con.ChangeTracker.AutoDetectChangesEnabled = false;
                con.RootDirectories.RemoveRange(delRoot);
                await con.SaveChangesAsync().ConfigureAwait(false);
            }
            var delFol = cacheFolders.Where(c => c.RootID == root.ParentRootID && !folders.Contains(c.Path));
            if (delFol.Any())
            {
                using var con = new BmsManagerContext();
                con.ChangeTracker.AutoDetectChangesEnabled = false;
                con.BmsFolders.RemoveRange(delFol);
                await con.SaveChangesAsync().ConfigureAwait(false);
            }

            var extentions = Settings.Default.Extentions;

            foreach (var folder in folders)
            {
                LoadingPath = folder;

                var updateDate = SystemProvider.FileSystem.DirectoryInfo.New(folder).LastWriteTimeUtc;
                var files = SystemProvider.FileSystem.Directory.EnumerateFiles(folder)
                    .Where(f =>
                    extentions.Concat(["txt"]).Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant())
                    || f.ToLower().StartsWith("preview") && previewExt.Contains(Path.GetExtension(f).Trim('.').ToLowerInvariant())).ToArray();

                var bmsFileDatas = files.Where(f => extentions.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant()))
                    .Select(file => (file, SystemProvider.FileSystem.File.ReadAllBytes(file)));

                if (bmsFileDatas.Any())
                {
                    await loadBmsFolderAsync(folder, files, bmsFileDatas, root, cacheFolders, cacheFiles).ConfigureAwait(false);
                }
                else
                {
                    await loadRootDirectoryAsync(folder, root, cacheRoots, cacheFolders, cacheFiles).ConfigureAwait(false);
                }
            }

            LoadingPath = "読込完了";
        }

        private async Task loadRootDirectoryAsync(string path, RootDirectory parent, ICollection<RootDirectory> cacheRoots, IEnumerable<BmsFolder> cacheFolders, IEnumerable<BmsFile> cacheFiles)
        {
            var updateDate = SystemProvider.FileSystem.DirectoryInfo.New(path).LastWriteTimeUtc;
            var root = cacheRoots.FirstOrDefault(c => c.Path == path);
            if (root == default)
            {
                root = new RootDirectory
                {
                    Path = path,
                    FolderUpdateDate = updateDate,
                    ParentRootID = parent.ID,
                };
                using var con = new BmsManagerContext();
                con.RootDirectories.Add(root);
                await con.SaveChangesAsync().ConfigureAwait(false);
                cacheRoots.Add(root);
            }
            else
            {
                // 既存Root
                if (root.FolderUpdateDate.Date != updateDate.Date
                    && root.FolderUpdateDate.Hour != updateDate.Hour
                    && root.FolderUpdateDate.Minute != updateDate.Minute
                    && root.FolderUpdateDate.Second != updateDate.Second) // 何故かミリ秒がずれるのでミリ秒以外で比較
                {
                    using var con = new BmsManagerContext();
                    con.RootDirectories.Find(root.ID).FolderUpdateDate = updateDate;
                    await con.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            await loadFromFileSystemAsync(root, cacheRoots, cacheFolders, cacheFiles).ConfigureAwait(false);
        }

        private async Task loadBmsFolderAsync(string path, IEnumerable<string> files, IEnumerable<(string file, byte[] data)> bmsFileDatas, RootDirectory parent, IEnumerable<BmsFolder> cacheFolders, IEnumerable<BmsFile> cacheFiles)
        {
            LoadingPath = path;

            var bmsFolder = cacheFolders.FirstOrDefault(f => f.Path == path);

            // 読込済データの解析なので並列化
            var bmsFiles = bmsFileDatas.AsParallel().Select(d => new BmsFile(d.file, d.data)).Where(f => !string.IsNullOrEmpty(f.Path)).ToArray();
            if (!bmsFiles.Any())
            {
                if (bmsFolder != default)
                {
                    using var con = new BmsManagerContext();
                    con.BmsFolders.Remove(bmsFolder);
                }
                return;
            }

            var updateDate = SystemProvider.FileSystem.DirectoryInfo.New(path).LastWriteTimeUtc;

            var meta = bmsFiles.GetMetaFromFiles();
            var hasText = files.Any(f => f.ToLower().EndsWith("txt"));
            var preview = files.FirstOrDefault(f => f.ToLower().StartsWith("preview") && previewExt.Contains(Path.GetExtension(f).Trim('.').ToLowerInvariant()));
            if (bmsFolder == default)
            {
                // 新規Folder
                bmsFolder = new BmsFolder
                {
                    Path = path,
                    Title = meta.Title,
                    Artist = meta.Artist,
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
                var childrenFiles = cacheFiles.Where(f => f.FolderID == bmsFolder.ID);
                var del = childrenFiles.Where(f => !bmsFiles.Any(e => f.MD5 == e.MD5));
                if (del.Any())
                {
                    using var con = new BmsManagerContext();
                    con.ChangeTracker.AutoDetectChangesEnabled = false;
                    con.Files.RemoveRange(del);
                    await con.SaveChangesAsync().ConfigureAwait(false);
                }
                var upd = bmsFiles.Where(f => !childrenFiles.Any(e => e.MD5 == f.MD5));
                if (upd.Any())
                {
                    using var con = new BmsManagerContext();
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
