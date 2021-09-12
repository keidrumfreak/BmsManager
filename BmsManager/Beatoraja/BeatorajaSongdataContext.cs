using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.Beatoraja
{
    class BeatorajaSongdataContext : DbContext
    {
        public virtual DbSet<BeatorajaFolder> Folders { get; set; }

        public virtual DbSet<BeatorajaSong> Songs { get; set; }

        string path;

        public BeatorajaSongdataContext(string path)
        {
            this.path = path;
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(new SqliteConnectionStringBuilder
            {
                Mode = SqliteOpenMode.ReadWrite,
                DataSource = path,
                Cache = SqliteCacheMode.Shared
            }.ToString());
        }
    }
}
