using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public BeatorajaSong(BmsModel model, bool containsText = false)
        {
            Title = model.Title;
            SubTitle = model.SubTitle;
            Genre = model.Genre;
            Artist = model.Artist;
            SubArtist = model.SubArtist;
            Path = model.Path;
            MD5 = model.MD5;
            Sha256 = model.Sha256;
            Banner = model.Banner;
            StageFile = model.StageFile;
            BackBmp = model.BackBmp;
            Preview = model.Preview;
            if (int.TryParse(model.PlayLevel, out var level))
            {
                Level = level;
            }
            Mode = model.Mode.ID;
            Difficulty = (int)model.Difficulty;
            Judge = model.JudgeRank;
            MinBpm = (int)model.MinBpm;
            MaxBpm = (int)model.MaxBpm;
            Features feature = 0;
            foreach (var tl in model.TimeLines)
            {
                if (tl.StopTime > 0)
                {
                    feature |= Features.StopSequence;
                }
                if (tl.Scroll != 1.0)
                {
                    feature |= Features.Scroll;
                }
                foreach (var i in Enumerable.Range(0, model.Mode.Key))
                {
                    if (tl.GetNote(i) is LongNote ln)
                    {
                        feature |= ln.Type switch
                        {
                            LNMode.Undefined => Features.UndefinedLN,
                            LNMode.LongNote => Features.LongNote,
                            LNMode.ChargeNote => Features.ChargeNote,
                            LNMode.HellChargeNote => Features.HellChargeNote,
                            _ => throw new NotSupportedException()
                        };
                    }
                    if (tl.GetNote(i) is MineNote)
                        feature |= Features.MineNote;
                }
            }
            Length = model.LastTime;
            Notes = model.GetTotalNotes();
            feature |= (model.Random?.Length ?? 0) > 0 ? Features.Random : 0;
            var content = containsText ? Contents.Text : 0;
            content |= (model.BgaList?.Length ?? 0) > 0 ? Contents.Bga : 0;
            content |= Length >= 30000 && (model.WavList?.Length ?? 0) <= (Length / (50 * 1000)) + 3 ? Contents.NoKeySound : 0;
            Feature = (int)feature;
            Content = (int)content;
            var sha256 = CrSha256.Create();
            var arr = sha256.ComputeHash(Encoding.GetEncoding("shift-jis").GetBytes(model.ToChartString()));
            ChartHash = BitConverter.ToString(arr).ToLower().Replace("-", "");
            Folder = Utility.GetCrc32(SysPath.GetDirectoryName(model.Path));
            Parent = Utility.GetCrc32(SysPath.GetDirectoryName(SysPath.GetDirectoryName(model.Path)));

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

            // TODO: preview
        }

        [Flags]
        enum Features
        {
            UndefinedLN = 1,
            MineNote = 2,
            Random = 4,
            LongNote = 8,
            ChargeNote = 16,
            HellChargeNote = 32,
            StopSequence = 64,
            Scroll = 128
        }

        [Flags]
        enum Contents
        {
            Text = 1,
            Bga = 2,
            Preview = 4,
            NoKeySound = 128
        }
    }
}
