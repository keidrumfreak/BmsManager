using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using BmsParser;
using CrSha256 = System.Security.Cryptography.SHA256;

namespace BmsManager.Data
{
    [Table("BmsFile")]
    class BmsFile
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int FolderID { get; set; }

        public string Path { get; set; }

        public string Title { get; set; }

        public string SubTitle { get; set; }

        public string Genre { get; set; }

        public string Artist { get; set; }

        public string SubArtist { get; set; }

        public string MD5 { get; set; }

        public string Sha256 { get; set; }

        public string Banner { get; set; }

        public string StageFile { get; set; }

        public string BackBmp { get; set; }

        public string Preview { get; set; }

        public string PlayLevel { get; set; }

        public int Mode { get; set; }

        public int Difficulty { get; set; }

        public int Judge { get; set; }

        public double MinBpm { get; set; }

        public double MaxBpm { get; set; }

        public int Length { get; set; }

        public int Notes { get; set; }

        public int Feature { get; set; }

        public bool HasBga { get; set; }

        public bool IsNoKeySound { get; set; }

        public string ChartHash { get; set; }

        public int N { get; set; }

        public int LN { get; set; }

        public int S { get; set; }

        public int LS { get; set; }

        public double Total { get; set; }

        public double Density { get; set; }

        public double PeakDensity { get; set; }

        public double EndDensity { get; set; }

        public string Distribution { get; set; }

        public double MainBpm { get; set; }

        public string SpeedChange { get; set; }

        public string LaneNotes { get; set; }

        [ForeignKey(nameof(FolderID))]
        public virtual BmsFolder Folder { get; set; }

        public BmsFile() { }

        public BmsFile(string path) : this(path, SystemProvider.FileSystem.File.ReadAllLines(path, Encoding.GetEncoding("shift-jis"))) { }

        public BmsFile(string path, string[] fileLines)
        {
            var model = new BmsDecoder().Decode(path, fileLines);
            if (model == null)
                return;

            Path = path;
            Title = model.Title;
            SubTitle = model.SubTitle;
            Genre = model.Genre;
            Artist = model.Artist;
            SubArtist = model.SubArtist;
            MD5 = model.MD5;
            Sha256 = model.Sha256;
            Banner = model.Banner;
            StageFile = model.StageFile;
            BackBmp = model.BackBmp;
            Preview = model.Preview;
            PlayLevel = model.PlayLevel;
            Mode = model.Mode.ID;
            Difficulty = (int)model.Difficulty;
            MinBpm = model.MinBpm;
            MaxBpm = model.MaxBpm;
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
            HasBga = (model.BgaList?.Length ?? 0) > 0;
            IsNoKeySound = Length >= 30000 && (model.WavList?.Length ?? 0) <= (Length / (50 * 1000)) + 3;
            Feature = (int)feature;
            var sha256 = CrSha256.Create();
            var arr = sha256.ComputeHash(Encoding.GetEncoding("shift-jis").GetBytes(model.ToChartString()));
            ChartHash = BitConverter.ToString(arr).ToLower().Replace("-", "");
            N = model.GetTotalNotes(BmsModel.NoteType.Key);
            LN = model.GetTotalNotes(BmsModel.NoteType.LongKey);
            S = model.GetTotalNotes(BmsModel.NoteType.Scratch);
            LS = model.GetTotalNotes(BmsModel.NoteType.LongScratch);
            Total = model.Total;

            var laneNotes = new int[model.Mode.Key][];
            foreach (var i in Enumerable.Range(0, laneNotes.Length)) { laneNotes[i] = new int[3]; }
            var data = new int[model.LastTime / 1000 + 2][];
            foreach (var i in Enumerable.Range(0, data.Length)) { data[i] = new int[7]; }
            var pos = 0;
            var border = (int)(model.GetTotalNotes() * (1.0 - 100.0 / model.Total));
            var borderpos = 0;
            foreach (var tl in model.TimeLines)
            {
                if (tl.Time / 1000 != pos)
                {
                    pos = tl.Time / 1000;
                }
                foreach (var i in Enumerable.Range(0, model.Mode.Key).Where(n => tl.GetNote(n) != null))
                {
                    var note = tl.GetNote(i);
                    if (note is LongNote ln && ln.IsEnd)
                    {
                        for (int index = tl.Time / 1000; index <= ln.Pair.Time / 1000; index++)
                        {
                            data[index][model.Mode.IsScratchKey(i) ? 1 : 4]++;
                        }
                        if (model.LNType == LNType.LongNote)
                            continue;
                    }

                    switch (note)
                    {
                        case NormalNote:
                            data[tl.Time / 1000][model.Mode.IsScratchKey(i) ? 2 : 5]++;
                            laneNotes[i][0]++;
                            break;
                        case LongNote:
                            data[tl.Time / 1000][model.Mode.IsScratchKey(i) ? 0 : 3]++;
                            data[tl.Time / 1000][model.Mode.IsScratchKey(i) ? 1 : 4]++;
                            laneNotes[i][1]++;
                            break;
                        case MineNote:
                            data[tl.Time / 1000][6]++;
                            laneNotes[i][2]++;
                            break;
                    }

                    border--;
                    if (border == 0)
                        borderpos = pos;
                }
            }

            var bd = model.GetTotalNotes() / data.Length / 4;
            Density = 0;
            PeakDensity = 0;
            var count = 0;
            foreach (var i in Enumerable.Range(0, data.Length))
            {
                var notes = data[i][0] + data[i][1] + data[i][2] + data[i][3] + data[i][4] + data[i][5];
                PeakDensity = PeakDensity > notes ? PeakDensity : notes;
                if (notes >= bd)
                {
                    Density += notes;
                    count++;
                }
            }

            var d = Math.Min(5, data.Length - borderpos - 1);
            EndDensity = 0;
            for (var i = borderpos; i < data.Length - d; i++)
            {
                var notes = 0;
                foreach (var j in Enumerable.Range(0, d))
                {
                    notes += data[i + j][0] + data[i + j][1] + data[i + j][2] + data[i + j][3] + data[i + j][4] + data[i + j][5];
                }
                EndDensity = EndDensity > ((double)notes / d) ? EndDensity : ((double)notes / d);
            }

            var distribution = new StringBuilder();
            distribution.Append('#');
            foreach (var i in Enumerable.Range(0, data.Length))
            {
                foreach (var j in Enumerable.Range(0, 7))
                {
                    var value = Math.Min(data[i][j], 36 * 36 - 1);
                    var val1 = value / 36;
                    distribution.Append((char)(val1 >= 10 ? val1 - 10 + 'a' : val1 + '0'));
                    val1 = value % 36;
                    distribution.Append((char)(val1 >= 10 ? val1 - 10 + 'a' : val1 + '0'));
                }
            }
            Distribution = distribution.ToString();

            List<double[]> speedList = new();
            Dictionary<double, int> bpmNoteCountMap = new();
            double nowSpeed = model.Bpm;
            speedList.Add(new double[] { nowSpeed, 0.0 });
            foreach (var tl in model.TimeLines)
            {
                int notecount = bpmNoteCountMap.TryGetValue(tl.Bpm, out var cnt) ? cnt : 0;
                bpmNoteCountMap[tl.Bpm] = notecount + tl.GetTotalNotes();

                if (tl.StopTime > 0)
                {
                    if (nowSpeed != 0)
                    {
                        nowSpeed = 0;
                        speedList.Add(new double[] { nowSpeed, tl.Time });
                    }
                }
                else if (nowSpeed != tl.Bpm * tl.Scroll)
                {
                    nowSpeed = tl.Bpm * tl.Scroll;
                    speedList.Add(new double[] { nowSpeed, tl.Time });
                }
            }

            int maxcount = 0;
            foreach (var bpm in bpmNoteCountMap.Keys)
            {
                if (bpmNoteCountMap[bpm] > maxcount)
                {
                    maxcount = bpmNoteCountMap[bpm];
                    MainBpm = bpm;
                }
            }
            if (speedList[speedList.Count - 1][1] != model.TimeLines[model.TimeLines.Length - 1].Time)
            {
                speedList.Add(new double[] { nowSpeed, model.TimeLines[model.TimeLines.Length - 1].Time });
            }

            var speedChange = new StringBuilder();
            foreach (var speed in speedList)
            {
                speedChange.Append(speed[0]).Append(',').Append(speed[1]).Append(',');
            }
            speedChange.Remove(speedChange.Length - 1, 1);
            SpeedChange = speedChange.ToString();

            var laneNote = new StringBuilder();
            foreach (var i in Enumerable.Range(0, laneNotes.GetUpperBound(0) + 1))
            {
                laneNote.Append(laneNotes[i][0])
                    .Append(',')
                    .Append(laneNotes[i][1])
                    .Append(',')
                    .Append(laneNotes[i][2]);
            }
            laneNote.Remove(laneNote.Length - 1, 1);
            LaneNotes = laneNote.ToString();
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
    }
}
