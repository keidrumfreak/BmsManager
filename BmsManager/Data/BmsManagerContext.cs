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
                con.Folders.AddRange(RootDirectories.Include(r => r.Parent).AsNoTracking().ToArray().Select(r => new BeatorajaFolder(r)));
                con.Folders.AddRange(BmsFolders.Include(f => f.Root).AsNoTracking().ToArray().Select(f => new BeatorajaFolder(f)));
                con.Songs.AddRange(files.Select(f => new BeatorajaSong(f)));
                con.SaveChanges();
            }

            using (var con = new BeatorajaSonginfoContext(songInfoDB))
            {
                con.Informations.AddRange(files.Select(f => new BeatorajaInformation(f)));
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
