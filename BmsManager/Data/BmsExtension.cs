using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BmsManager.Data
{
    [Table("BmsExtension")]
    class BmsExtension
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public string Extension { get; set; }

        public static IEnumerable<string> GetExtensions()
        {
            using (var con = new BmsManagerContext())
            {
                return con.Extensions.Select(e => e.Extension).ToArray();
            }
        }
    }
}
