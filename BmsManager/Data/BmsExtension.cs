using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.Data
{
    [Table("BmsExtension")]
    class BmsExtension
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public string Extension { get; set; }

        static IEnumerable<string> extensions;
        public static IEnumerable<string> GetExtensions(bool reload = false)
        {
            if (extensions != null && !reload)
                return extensions;

            using (var con = new BmsManagerContext())
            {
                extensions = con.Extensions.AsNoTracking().Select(e => e.Extension).ToArray();
            }
            return extensions;
        }
    }
}
