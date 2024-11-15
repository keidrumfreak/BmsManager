using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BmsManager.Entity;
using BmsParser;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace BmsManager.Model
{
    class FolderLoader : ObservableObject
    {
        string loadingPath = string.Empty;
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
            RootDirectory? root;
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
                // 既存Root
                else if (root.FolderUpdateDate.Date != updateDate.Date
                        && root.FolderUpdateDate.Hour != updateDate.Hour
                        && root.FolderUpdateDate.Minute != updateDate.Minute
                        && root.FolderUpdateDate.Second != updateDate.Second) // 何故かミリ秒がずれるのでミリ秒以外で比較
                {
                    con.RootDirectories.First(r => r.ID == root.ID).FolderUpdateDate = updateDate;
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
            var bmsFiles = bmsFileDatas.AsParallel().Select(d => BmsModel.Decode(d.file, d.data)).Where(f => f != null && !string.IsNullOrEmpty(f.Path)).Select(f => f!.ToEntity()).ToArray();
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
                await registerAsync(bmsFolder).ConfigureAwait(false);
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
        }

        private static async Task registerAsync(BmsFolder folder)
        {
            using var context = new BmsManagerContext();
            context.BmsFolders.Add(folder);
            await context.SaveChangesAsync().ConfigureAwait(false);

            foreach (var files in folder.Files.Chunk(50))
            {
                using var con = context.Database.GetDbConnection();
                con.Open();
                using var cmd = con.CreateCommand();
                var sql = new StringBuilder(@"INSERT INTO BmsFile
    (FolderID
    ,Path
    ,Title
    ,Subtitle
    ,Genre
    ,Artist
    ,SubArtist
    ,MD5
    ,Sha256
    ,Banner
    ,StageFile
    ,BackBmp
    ,Preview
    ,PlayLevel
    ,Mode
    ,Difficulty
    ,Judge
    ,MinBpm
    ,MaxBpm
    ,Length
    ,Notes
    ,Feature
    ,HasBga
    ,IsNoKeySound
    ,ChartHash
    ,N
    ,LN
    ,S
    ,LS
    ,Total
    ,Density
    ,PeakDensity
    ,EndDensity
    ,Distribution
    ,MainBpm
    ,SpeedChange
    ,LaneNotes)
     VALUES");
                foreach (var (file, index) in files.Select((f, i) => (f, i)))
                {
                    sql.AppendLine(@$"(@{nameof(BmsFile.FolderID)}{index}
    ,@{nameof(BmsFile.Path)}{index}
    ,@{nameof(BmsFile.Title)}{index}
    ,@{nameof(BmsFile.Subtitle)}{index}
    ,@{nameof(BmsFile.Genre)}{index}
    ,@{nameof(BmsFile.Artist)}{index}
    ,@{nameof(BmsFile.SubArtist)}{index}
    ,@{nameof(BmsFile.MD5)}{index}
    ,@{nameof(BmsFile.Sha256)}{index}
    ,@{nameof(BmsFile.Banner)}{index}
    ,@{nameof(BmsFile.StageFile)}{index}
    ,@{nameof(BmsFile.BackBmp)}{index}
    ,@{nameof(BmsFile.Preview)}{index}
    ,@{nameof(BmsFile.PlayLevel)}{index}
    ,@{nameof(BmsFile.Mode)}{index}
    ,@{nameof(BmsFile.Difficulty)}{index}
    ,@{nameof(BmsFile.Judge)}{index}
    ,@{nameof(BmsFile.MinBpm)}{index}
    ,@{nameof(BmsFile.MaxBpm)}{index}
    ,@{nameof(BmsFile.Length)}{index}
    ,@{nameof(BmsFile.Notes)}{index}
    ,@{nameof(BmsFile.Feature)}{index}
    ,@{nameof(BmsFile.HasBga)}{index}
    ,@{nameof(BmsFile.IsNoKeySound)}{index}
    ,@{nameof(BmsFile.ChartHash)}{index}
    ,@{nameof(BmsFile.N)}{index}
    ,@{nameof(BmsFile.LN)}{index}
    ,@{nameof(BmsFile.S)}{index}
    ,@{nameof(BmsFile.LS)}{index}
    ,@{nameof(BmsFile.Total)}{index}
    ,@{nameof(BmsFile.Density)}{index}
    ,@{nameof(BmsFile.PeakDensity)}{index}
    ,@{nameof(BmsFile.EndDensity)}{index}
    ,@{nameof(BmsFile.Distribution)}{index}
    ,@{nameof(BmsFile.MainBpm)}{index}
    ,@{nameof(BmsFile.SpeedChange)}{index}
    ,@{nameof(BmsFile.LaneNotes)}{index}),");
                    cmd.AddParameter($"@{nameof(BmsFile.FolderID)}{index}", folder.ID, DbType.Int32);
                    cmd.AddParameter($"@{nameof(BmsFile.Path)}{index}", file.Path, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.Title)}{index}", file.Title, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.Subtitle)}{index}", file.Subtitle, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.Genre)}{index}", file.Genre, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.Artist)}{index}", file.Artist, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.SubArtist)}{index}", file.SubArtist, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.MD5)}{index}", file.MD5 ?? (object)DBNull.Value, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.Sha256)}{index}", file.Sha256, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.Banner)}{index}", file.Banner ?? (object)DBNull.Value, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.StageFile)}{index}", file.StageFile ?? (object)DBNull.Value, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.BackBmp)}{index}", file.BackBmp ?? (object)DBNull.Value, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.Preview)}{index}", file.Preview ?? (object)DBNull.Value, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.PlayLevel)}{index}", file.PlayLevel, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.Mode)}{index}", file.Mode, DbType.Int32);
                    cmd.AddParameter($"@{nameof(BmsFile.Difficulty)}{index}", file.Difficulty, DbType.Int32);
                    cmd.AddParameter($"@{nameof(BmsFile.Judge)}{index}", file.Judge, DbType.Int32);
                    cmd.AddParameter($"@{nameof(BmsFile.MinBpm)}{index}", file.MinBpm, DbType.Double);
                    cmd.AddParameter($"@{nameof(BmsFile.MaxBpm)}{index}", file.MaxBpm, DbType.Double);
                    cmd.AddParameter($"@{nameof(BmsFile.Length)}{index}", file.Length, DbType.Int32);
                    cmd.AddParameter($"@{nameof(BmsFile.Notes)}{index}", file.Notes, DbType.Int32);
                    cmd.AddParameter($"@{nameof(BmsFile.Feature)}{index}", file.Feature, DbType.Int32);
                    cmd.AddParameter($"@{nameof(BmsFile.HasBga)}{index}", file.HasBga, DbType.Boolean);
                    cmd.AddParameter($"@{nameof(BmsFile.IsNoKeySound)}{index}", file.IsNoKeySound, DbType.Boolean);
                    cmd.AddParameter($"@{nameof(BmsFile.ChartHash)}{index}", file.ChartHash, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.N)}{index}", file.N, DbType.Int32);
                    cmd.AddParameter($"@{nameof(BmsFile.LN)}{index}", file.LN, DbType.Int32);
                    cmd.AddParameter($"@{nameof(BmsFile.S)}{index}", file.S, DbType.Int32);
                    cmd.AddParameter($"@{nameof(BmsFile.LS)}{index}", file.LS, DbType.Int32);
                    cmd.AddParameter($"@{nameof(BmsFile.Total)}{index}", file.Total, DbType.Double);
                    cmd.AddParameter($"@{nameof(BmsFile.Density)}{index}", file.Density, DbType.Double);
                    cmd.AddParameter($"@{nameof(BmsFile.PeakDensity)}{index}", file.PeakDensity, DbType.Double);
                    cmd.AddParameter($"@{nameof(BmsFile.EndDensity)}{index}", file.EndDensity, DbType.Double);
                    cmd.AddParameter($"@{nameof(BmsFile.Distribution)}{index}", file.Distribution, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.MainBpm)}{index}", file.MainBpm, DbType.Double);
                    cmd.AddParameter($"@{nameof(BmsFile.SpeedChange)}{index}", file.SpeedChange, DbType.String);
                    cmd.AddParameter($"@{nameof(BmsFile.LaneNotes)}{index}", file.LaneNotes, DbType.String);
                }
                sql.Remove(sql.Length - 3, 3);
                cmd.CommandText = sql.ToString();
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }
}
