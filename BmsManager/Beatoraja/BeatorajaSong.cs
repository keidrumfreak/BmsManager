using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BmsManager.Beatoraja
{
    [Table("song")]
    class BeatorajaSong
    {
        [Column("md5"), Required]
        public string MD5 { get; set; }

        [Column("sha256", Order = 0), Key]
        public string Sha256 { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("subtitle")]
        public string SubTitle { get; set; }

        [Column("genre")]
        public string Genre { get; set; }

        [Column("artist")]
        public string Artist { get; set; }

        [Column("subartist")]
        public string SubArtist { get; set; }

        [Column("tag")]
        public string Tag { get; set; }

        [Column("path", Order = 1), Key]
        public string Path { get; set; }

        [Column("folder")]
        public string Folder { get; set; }

        [Column("stagefile")]
        public string StageFile { get; set; }

        [Column("banner")]
        public string Banner { get; set; }

        [Column("backbmp")]
        public string BackBmp { get; set; }

        [Column("preview")]
        public string Preview { get; set; }

        [Column("parent")]
        public string Parent { get; set; }

        [Column("level")]
        public int Level { get; set; }

        [Column("difficulty")]
        public int Difficulty { get; set; }

        [Column("maxbpm")]
        public int MaxBpm { get; set; }

        [Column("minbpm")]
        public int MinBpm { get; set; }

        [Column("mode")]
        public int Mode { get; set; }

        [Column("judge")]
        public int Judge { get; set; }

        [Column("feature")]
        public int Feature { get; set; }

        [Column("content")]
        public int Content { get; set; }

        [Column("date")]
        public int Date { get; set; }

        [Column("favorite")]
        public int Favorite { get; set; }

        [Column("notes")]
        public int Notes { get; set; }

        [Column("adddate")]
        public int AddDate { get; set; }

        [Column("charthash")]
        public string ChartHash { get; set; }

        [Column("length")]
        public int Length { get; set; }
    }
}
