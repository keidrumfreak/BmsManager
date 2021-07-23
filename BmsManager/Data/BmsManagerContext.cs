using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.Data
{
    class BmsManagerContext : DbContext
    {
        public virtual DbSet<BmsExtension> Extensions { get; set; }
        public virtual DbSet<BmsFile> Files { get; set; }
        public virtual DbSet<BmsFolder> BmsFolders { get; set; }
        public virtual DbSet<RootDirectory> RootDirectories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(Settings.Default.BmsManagerConnectionStrings);
        }
    }
}
