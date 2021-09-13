using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BmsManager.Data;
using BmsParser;
using CrSha256 = System.Security.Cryptography.SHA256;
using SysPath = System.IO.Path;

namespace BmsManager.Beatoraja
{
    [Table("song")]
    class BeatorajaSong
    {
        [Column("md5"), Required]
        public string MD5 { get; set; }

        [Column("sha256")]
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

        [Column("path")]
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

        public BeatorajaSong() { }

        public BeatorajaSong(BmsFile file)
        {
            Title = file.Title;
            SubTitle = file.SubTitle;
            Genre = file.Genre;
            Artist = file.Artist;
            SubArtist = file.SubArtist;
            Path = file.Path;
            MD5 = file.MD5;
            Sha256 = file.Sha256;
            Banner = file.Banner;
            StageFile = file.StageFile;
            BackBmp = file.BackBmp;
            Preview = file.Preview;
            if (int.TryParse(file.PlayLevel, out var level))
            {
                Level = level;
            }
            Mode = file.Mode;
            Difficulty = (int)file.Difficulty;
            Judge = file.Judge;
            MinBpm = (int)file.MinBpm;
            MaxBpm = (int)file.MaxBpm;
            Length = file.Length;
            Notes = file.Notes;
            Feature = file.Feature;
            Content = file.Content;
            ChartHash = file.ChartHash;
            Folder = Utility.GetCrc32(file.Folder.Path);
            Parent = Utility.GetCrc32(file.Folder.Root.Path);

            if (Difficulty == 0)
            {
                var title = (Title + SubTitle).ToUpper();
                Difficulty = title.Contains("BEGINNER") ? 1
                    : title.Contains("NORMAL") ? 2
                    : title.Contains("HYPER") ? 3
                    : title.Contains("ANOTHER") ? 4
                    : title.Contains("INSANE") ? 5
                    : Notes < 250 ? 1
                    : Notes < 600 ? 2
                    : Notes < 1000 ? 3
                    : Notes < 2000 ? 4
                    : 5;
            }

            if (string.IsNullOrEmpty(Preview))
            {
                Preview = file.Folder.Preview;
            }
        }


    }
}
