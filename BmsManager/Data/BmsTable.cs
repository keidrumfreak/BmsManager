using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BmsManager.Data
{
    [Table("BmsTable")]
    class BmsTable
    {
        [Key]
        public int ID { get; set; }

        public string Url { get; set; }

        public string Name { get; set; }

        public string Symbol { get; set; }

        public string Tag { get; set; }

        [InverseProperty(nameof(BmsTableDifficulty.Table))]
        public virtual ICollection<BmsTableDifficulty> Difficulties { get; set; }
    }
}
