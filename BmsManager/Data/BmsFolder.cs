using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BmsParser;
using CommonLib.IO;
using CommonLib.Linq;
using CommonLib.Wpf;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SysPath = System.IO.Path;

namespace BmsManager.Data
{
    [Table("BmsFolder")]
    class BmsFolder : BindableBase
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int RootID { get; set; }

        public string Path { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        public bool HasText { get; set; }

        public string Preview { get; set; }

        public DateTime FolderUpdateDate { get; set; }

        [InverseProperty(nameof(BmsFile.Folder))]
        public virtual ICollection<BmsFile> Files { get; set; }

        [ForeignKey(nameof(RootID))]
        public virtual RootDirectory Root { get; set; }

        IEnumerable<BmsFolder> duplicates;
        /// <summary>
        /// 重複フォルダ
        /// </summary>
        [NotMapped]
        public IEnumerable<BmsFolder> Duplicates
        {
            get => duplicates ?? [];
            set => SetProperty(ref duplicates, value);
        }

        public void Rename()
        {
            var rename = $"[{Utility.ToFileNameString(Artist)}]{Utility.ToFileNameString(Title)}";

            var dst = PathUtil.Combine(SysPath.GetDirectoryName(Path), rename);
            var tmp = PathUtil.Combine(SysPath.GetDirectoryName(Path), "tmp");

            if (SysPath.GetFileName(Path) == rename)
                return;

            try
            {
                SystemProvider.FileSystem.Directory.Move(Path, tmp);

                var i = 1;
                var ren = dst;
                while (SystemProvider.FileSystem.Directory.Exists(ren))
                {
                    i++;
                    ren = $"{dst} ({i})";
                }
                dst = ren;

                SystemProvider.FileSystem.Directory.Move(tmp, dst);

                SetMetaFromName();

                using var con = new BmsManagerContext();
                var folder = con.BmsFolders.Include(f => f.Files).FirstOrDefault(f => f.Path == Path);
                if (folder != default)
                {
                    folder.Path = dst;
                    Path = dst;
                    SetMetaFromName();
                    folder.Title = Title;
                    folder.Artist = Artist;
                    foreach (var file in folder.Files)
                    {
                        file.Path = PathUtil.Combine(dst, SysPath.GetFileName(file.Path));
                    }
                    con.SaveChanges();
                }
            }
            catch (IOException)
            {
                if (SystemProvider.FileSystem.Directory.Exists(tmp))
                    SystemProvider.FileSystem.Directory.Move(tmp, Path);
                throw;
            }
        }

        public void SetMetaFromName()
        {
            var name = SysPath.GetFileName(Path);
            var index = name.IndexOf(']');
            if (index != -1)
            {
                Artist = name[1..index];
                Title = name[(index + 1)..];
            }
        }

        public void UpdateMeta()
        {
            using var context = new BmsManagerContext();
            var folder = context.BmsFolders.Find(ID);
            folder.Title = Title;
            folder.Artist = Artist;
            context.SaveChanges();
        }

        public async Task RegisterAsync()
        {
            using (var context = new BmsManagerContext())
            using (var con = context.Database.GetDbConnection())
            {
                con.Open();
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = @$"INSERT INTO BmsFolder
    (RootID
    ,Path
    ,Artist
    ,Title
    ,HasText
    ,Preview
    ,FolderUpdateDate)
VALUES
    (@{nameof(RootID)}
    ,@{nameof(Path)}
    ,@{nameof(Artist)}
    ,@{nameof(Title)}
    ,@{nameof(HasText)}
    ,@{nameof(Preview)}
    ,@{nameof(FolderUpdateDate)})";
                    cmd.AddParameter($"@{nameof(RootID)}", RootID, DbType.Int32);
                    cmd.AddParameter($"@{nameof(Path)}", Path, DbType.String);
                    cmd.AddParameter($"@{nameof(Artist)}", Artist, DbType.String);
                    cmd.AddParameter($"@{nameof(Title)}", Title, DbType.String);
                    cmd.AddParameter($"@{nameof(HasText)}", HasText, DbType.Boolean);
                    cmd.AddParameter($"@{nameof(Preview)}", Preview ?? (object)DBNull.Value, DbType.String);
                    cmd.AddParameter($"@{nameof(FolderUpdateDate)}", FolderUpdateDate, DbType.DateTime);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = $"SELECT ID FROM BmsFolder WHERE Path = @{nameof(Path)}";
                    cmd.AddParameter($"@{nameof(Path)}", Path, DbType.String);
                    var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    reader.Read();
                    ID = Convert.ToInt32(reader[0]);
                }
            }

            // TODO: .NET6 でChunkメソッドに変更する
            foreach (var files in Files.Select((file, i) => (file, i)).GroupBy(x => x.i / 50).Select(g => g.Select(x => x.file)))
            {
                using var context = new BmsManagerContext();
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
                    cmd.AddParameter($"@{nameof(BmsFile.FolderID)}{index}", ID, DbType.Int32);
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

        public void Merge()
        {
            var roots = Duplicates.Select(d => d.Root).ToArray();
            var ext = Settings.Default.Extentions;

            foreach (var fol in Duplicates)
            {
                foreach (var file in fol.Files)
                {
                    // 重複BMSファイルは先に削除しておく
                    if (Files.Any(f => f.MD5 == file.MD5))
                    {
                        if (SystemProvider.FileSystem.File.Exists(file.Path))
                            SystemProvider.FileSystem.File.Delete(file.Path);
                    }
                }

                if (SystemProvider.FileSystem.Directory.Exists(fol.Path))
                {
                    foreach (var file in SystemProvider.FileSystem.Directory.EnumerateFiles(fol.Path, "*.*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var toPath = PathUtil.Combine(Path, SysPath.GetFileName(file));
                            var fileExt = SysPath.GetExtension(file); // 拡張子が存在しない場合もある
                            if (fileExt.Length > 1 && ext.Contains(fileExt[1..]))
                            {
                                var i = 1;
                                var dst = toPath;
                                while (SystemProvider.FileSystem.File.Exists(toPath))
                                {
                                    i++;
                                    toPath = $"{dst} ({i})";
                                }
                            }

                            // 統合先のファイルを正とみなす
                            if (!SystemProvider.FileSystem.File.Exists(toPath))
                                SystemProvider.FileSystem.File.Move(file, toPath, true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                            return;
                        }
                    }
                }

                try
                {
                    SystemProvider.FileSystem.Directory.Delete(fol.Path, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return;
                }
            }

            foreach (var root in roots)
            {
                root.LoadFromFileSystem();
                root.Register();
            }
            Root.LoadFromFileSystem();
            Root.Register();

            //Application.Current.Dispatcher.Invoke(() => parent.Folders.Remove(this));
        }

        public void Install(BmsModel diffFile)
        {
            try
            {
                if (Files.Any(f => f.MD5 == diffFile.MD5))
                {
                    // 重複する場合はインストールしない
                    File.Delete(diffFile.Path);
                    return;
                }

                var toPath = PathUtil.Combine(Path, SysPath.GetFileName(diffFile.Path));
                var i = 1;
                var dst = toPath;
                while (SystemProvider.FileSystem.File.Exists(toPath))
                {
                    i++;
                    toPath = PathUtil.Combine(Path, $"{SysPath.GetFileNameWithoutExtension(dst)} ({i}){SysPath.GetExtension(dst)}");
                }

                SystemProvider.FileSystem.File.Move(diffFile.Path, toPath);

                using var con = new BmsManagerContext();
                var fol = con.BmsFolders.Include(f => f.Files).FirstOrDefault(f => f.Path == Path);
                var file = new BmsFile(toPath);
                fol.Files.Add(file);
                con.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }

    static class BmsFolderExtention
    {
        public static void CheckDuplicateByMD5(this IEnumerable<BmsFolder> folders)
        {
            using var con = new BmsManagerContext();
            // 重複BMSが存在するフォルダを全て取得する (まだどのフォルダと重複しているかは見ない)
            var dupMD5 = con.Files
                .GroupBy(f => f.MD5)
                .Select(g => new { MD5 = g.Key, Count = g.Count() })
                .Where(g => g.Count > 1);
            var folID = con.Files.Join(dupMD5, f => f.MD5, d => d.MD5, (f, d) => f.Folder.ID).Distinct();
            var fol = con.BmsFolders.Where(f => folID.Contains(f.ID))
                .Include(f => f.Files).Include(f => f.Root)
                .AsNoTracking().ToArray();

            foreach (var folder in folders)
            {
                // TODO: 重複判定をもう少しわかりやすくしたい
                folder.Duplicates = folders.Where(f => f.ID != folder.ID && f.Files.Any(f1 => folder.Files.Any(f2 => f1.MD5 == f2.MD5))).ToArray();
            }
        }

        public static void CheckDuplicateByMeta(this IEnumerable<BmsFolder> folders)
        {
            using var con = new BmsManagerContext();
            // 重複BMSが存在するフォルダを全て取得する (まだどのフォルダと重複しているかは見ない)
            var dupMeta = con.BmsFolders
                .GroupBy(f => new { f.Title, f.Artist })
                .Select(g => new { Meta = g.Key, Count = g.Count() })
                .Where(g => g.Count > 1);
            var folID = con.BmsFolders.Join(dupMeta, f => new { f.Title, f.Artist }, d => d.Meta, (f, d) => f.ID).Distinct();
            var fol = con.BmsFolders.Where(f => folID.Contains(f.ID))
                .Include(f => f.Files).Include(f => f.Root)
                .AsNoTracking().ToArray();

            foreach (var folder in folders)
            {
                folder.Duplicates = folders.Where(f => f.ID != folder.ID && f.Title == folder.Title && f.Artist == folder.Artist).ToArray();
            }
        }
    }
}
