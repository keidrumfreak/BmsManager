using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BmsManager.Beatoraja;
using BmsParser;
using CommonLib.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;

namespace BmsManager.Data
{
    class BmsManagerContext : DbContext
    {
        public virtual DbSet<BmsExtension> Extensions { get; set; }
        public virtual DbSet<BmsFile> Files { get; set; }
        public virtual DbSet<BmsFolder> BmsFolders { get; set; }
        public virtual DbSet<RootDirectory> RootDirectories { get; set; }

        public virtual DbSet<BmsTable> Tables { get; set; }
        public virtual DbSet<BmsTableDifficulty> Difficulties { get; set; }
        public virtual DbSet<BmsTableData> TableDatas { get; set; }

        public BmsManagerContext() : base()
        {
            if (Settings.Default.DatabaseKind == "SQLite")
            {
                Database.EnsureCreated();
            }
        }

        public static readonly LoggerFactory LoggerFactory = new LoggerFactory(new[] {
            new DebugLoggerProvider()
        });

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            switch (Settings.Default.DatabaseKind)
            {
                case "SQLServer":
                    optionsBuilder.UseSqlServer(Settings.Default.BmsManagerConnectionStrings)
                        .EnableSensitiveDataLogging()
                        .UseLoggerFactory(LoggerFactory);
                    break;
                case "SQLite":
                    var path = PathUtil.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName), "bms.db");
                    optionsBuilder.UseSqlite(new SqliteConnectionStringBuilder
                    {
                        Mode = SqliteOpenMode.ReadWriteCreate,
                        DataSource = path,
                        Cache = SqliteCacheMode.Shared
                    }.ToString());
                    break;
            }
        }

        public void ExportToBeatoragja(string songDB, string songInfoDB)
        {
            var decoder = new BmsDecoder();
            var files = Files.Include(f => f.Folder)
                .ThenInclude(f => f.Root).AsNoTracking().ToArray();
            using (var con = new BeatorajaSongdataContext(songDB))
            {
                var mnFol = BmsFolders.Include(r => r.Root).AsNoTracking().ToArray().Select(r => new BeatorajaFolder(r))
                    .Concat(RootDirectories.Include(r => r.Parent).AsNoTracking().ToArray().Select(r => new BeatorajaFolder(r)));
                var boFol = con.Folders.ToArray();
                var delFol = boFol.AsParallel().Where(bo => !mnFol.Any(mn => mn.Path == bo.Path));
                if (delFol.Any())
                    con.Folders.RemoveRange(delFol);
                foreach (var folder in mnFol)
                {
                    var entity = boFol.FirstOrDefault(bo => bo.Path == folder.Path);
                    if (entity == default)
                    {
                        con.Folders.Add(entity);
                    }
                    else
                    {
                        if (entity.Date != folder.Date)
                        {
                            entity.Date = folder.Date;
                        }
                    }
                }

                var songs = con.Songs.ToArray();
                var delSong = songs.AsParallel().Where(s => !files.Any(f => f.Path == s.Path));
                if (delSong.Any())
                    con.Songs.RemoveRange(delSong);

                foreach (var file in files)
                {
                    var song = songs.FirstOrDefault(s => s.Path == file.Path);
                    if (song == default)
                    {
                        con.Songs.Add(song);
                    }
                    else
                    {
                        // 同一なら変更無しのはず
                        if (file.Sha256 != song.Sha256)
                        {
                            // UPDATEが面倒なのでDELETE-INSERT
                            con.Songs.Remove(song);
                            con.Songs.Add(new BeatorajaSong(file));
                        }
                    }
                }
                con.SaveChanges();
            }

            using (var con = new BeatorajaSonginfoContext(songInfoDB))
            {
                con.ChangeTracker.AutoDetectChangesEnabled = false;
                var distinctFile = files.GroupBy(f => f.Sha256).Select(f => f.First());
                var infos = con.Informations.AsNoTracking().ToArray();
                foreach (var file in distinctFile.AsParallel().Where(f => !infos.Any(i => f.Sha256 == i.Sha256)))
                {
                    con.Informations.Add(new BeatorajaInformation(file));
                }
                con.SaveChanges();
            }
        }
    }

    static class DBExtensions
    {
        public static void AddParameter(this DbCommand cmd, string name, object value, DbType type)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            param.DbType = type;
            cmd.Parameters.Add(param);
        }
    }
}
