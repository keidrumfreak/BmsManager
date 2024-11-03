using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BmsManager.Entity;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.Model
{
    class FolderLoader : ObservableObject
    {
        string loadingPath;
        public string LoadingPath
        {
            get => loadingPath;
            set => SetProperty(ref loadingPath, value);
        }

        static readonly string[] previewExt = ["wav", "ogg", "mp3", "flac"];

        public async Task LoadAsync(RootDirectory entity, Func<RootDirectory, Task> rootFunc = null, Action<BmsFolder> folderAction = null)
        {
            LoadingPath = entity.Path;
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
                    LoadingPath = folder;
                    await loadBmsFolderAsync(entity.ID, folder, files, bmsFileDatas, folderAction).ConfigureAwait(false);
                }
                else
                {
                    LoadingPath = folder;
                    var child = await loadRootDirectoryAsync(entity.ID, folder).ConfigureAwait(false);
                    if (rootFunc == null)
                    {
                        await LoadAsync(child).ConfigureAwait(false);
                    }
                    else
                    {
                        await rootFunc(child).ConfigureAwait(false);
                    }
                }
            }
        }

        private static async Task<RootDirectory> loadRootDirectoryAsync(int rootID, string path)
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
                        ParentRootID = rootID,
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
            return root;
        }

        private static async Task loadBmsFolderAsync(int rootID, string path, IEnumerable<string> files, IEnumerable<(string file, byte[] data)> bmsFileDatas, Action<BmsFolder> folderAction)
        {
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
                    RootID = rootID
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
            folderAction?.Invoke(bmsFolder);
            //Folders.Add(bmsFolder);
        }
    }
}
