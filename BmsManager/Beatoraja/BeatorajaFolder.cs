using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BmsManager.Beatoraja
{
    [Table("folder")]
    class BeatorajaFolder
    {
        [Column("path"), Key]
        public string Path { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("subtitle")]
        public string SubTitle { get; set; }

        [Column("command")]
        public string Command { get; set; }

        [Column("type")]
        public int Type { get; set; }

        [Column("banner")]
        public string Text { get; set; }

        [Column("parent")]
        public string Parent { get; set; }

        [Column("date")]
        public int Date { get; set; }

        [Column("max")]
        public int Max { get; set; }

        [Column("adddate")]
        public int AddDate { get; set; }
    }
}
