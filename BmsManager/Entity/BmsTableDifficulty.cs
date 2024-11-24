using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BmsManager.Entity
{
    [Table("BmsTableDifficulty")]
    class BmsTableDifficulty
    {
        [Key]
        public int ID { get; set; }

        public int BmsTableID { get; set; }

        public required string Difficulty { get; set; }

        public int? DifficultyOrder { get; set; }

        [InverseProperty(nameof(BmsTableData.Difficulty))]
        public virtual ICollection<BmsTableData> TableDatas { get; set; } = [];

        BmsTable? table;
        [ForeignKey(nameof(BmsTableID))]
        public virtual BmsTable Table
        {
            get => table ?? throw new InvalidOperationException();
            set => table = value;
        }
    }
}
