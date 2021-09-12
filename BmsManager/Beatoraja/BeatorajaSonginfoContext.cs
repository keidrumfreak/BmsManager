using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.Beatoraja
{
    class BeatorajaSonginfoContext : DbContext
    {
        public virtual DbSet<BeatorajaInformation> Informations { get; set; }

        string path;

        public BeatorajaSonginfoContext(string path)
        {
            this.path = path;
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(new SqliteConnectionStringBuilder
            {
                Mode = SqliteOpenMode.ReadWriteCreate,
                DataSource = path,
                Cache = SqliteCacheMode.Shared
            }.ToString());
        }
    }
}
