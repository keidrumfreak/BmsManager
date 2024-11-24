using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.Entity
{
    [Table("BmsTable")]
    class BmsTable
    {
        [Key]
        public int ID { get; set; }

        public required string Url { get; set; }

        public required string Name { get; set; }

        public required string Symbol { get; set; }

        public string? Tag { get; set; }

        [InverseProperty(nameof(BmsTableDifficulty.Table))]
        public virtual ICollection<BmsTableDifficulty> Difficulties { get; set; } = [];
    }
}
