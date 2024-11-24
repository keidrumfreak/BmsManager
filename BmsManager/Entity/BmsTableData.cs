using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BmsManager.Entity
{
    [Table("BmsTableData")]
    class BmsTableData
    {
        [Key]
        public int ID { get; set; }

        public int BmsTableDifficultyID { get; set; }

        public required string MD5 { get; set; }

        public string? LR2BmsID { get; set; }

        public string? Title { get; set; }

        public string? Artist { get; set; }

        /// <summary>
        /// 本体URL
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// 差分URL
        /// </summary>
        public string? DiffUrl { get; set; }

        public string? DiffName { get; set; }

        public string? PackUrl { get; set; }

        public string? PackName { get; set; }

        public string? Comment { get; set; }

        public string? OrgMD5 { get; set; }

        BmsTableDifficulty? difficulty;
        [ForeignKey(nameof(BmsTableDifficultyID))]
        public virtual BmsTableDifficulty Difficulty
        {
            get => difficulty ?? throw new InvalidOperationException();
            set => difficulty = value;
        }
    }
}
