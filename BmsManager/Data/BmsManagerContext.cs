using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
