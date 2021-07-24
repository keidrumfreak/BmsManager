using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static readonly LoggerFactory LoggerFactory = new LoggerFactory(new[] {
            new DebugLoggerProvider()
        });

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Settings.Default.BmsManagerConnectionStrings)
                .EnableSensitiveDataLogging()
                .UseLoggerFactory(LoggerFactory);
        }
    }
}
