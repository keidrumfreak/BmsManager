using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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
                if (!con.Extensions.Any())
                {
                    con.Extensions.AddRange(new[]
                    {
                        new BmsExtension { Extension = "bms" },
                        new BmsExtension { Extension = "bme" },
                        new BmsExtension { Extension = "bml" },
                        new BmsExtension { Extension = "pms" },
                    });
                    con.SaveChanges();
                }

                extensions = con.Extensions.AsNoTracking().Select(e => e.Extension).ToArray();
            }
            return extensions;
        }
    }
}
