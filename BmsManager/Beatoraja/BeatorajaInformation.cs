using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BmsParser;

namespace BmsManager.Beatoraja
{
    [Table("information")]
    class BeatorajaInformation
    {
        [Column("sha256"), Key]
        public string Sha256 { get; set; }

        [Column("n")]
        public int N { get; set; }

        [Column("ln")]
        public int LN { get; set; }

        [Column("s")]
        public int S { get; set; }

        [Column("ls")]
        public int LS { get; set; }

        [Column("total")]
        public double Total { get; set; }

        [Column("density")]
        public double Density { get; set; }

        [Column("peakdensity")]
        public double PeakDensity { get; set; }

        [Column("enddensity")]
        public double EndDensity { get; set; }

        [Column("distribution")]
        public string Distribution { get; set; }

        [Column("mainbpm")]
        public double MainBpm { get; set; }

        [Column("speedchange")]
        public string SpeedChange { get; set; }

        [Column("lanenotes")]
        public string LaneNotes { get; set; }

        public BeatorajaInformation(BmsModel model)
        {
            Sha256 = model.Sha256;
            N = model.GetTotalNotes(BmsModel.NoteType.Key);
            LN = model.GetTotalNotes(BmsModel.NoteType.LongKey);
            S = model.GetTotalNotes(BmsModel.NoteType.Scratch);
            LS = model.GetTotalNotes(BmsModel.NoteType.LongScratch);
            Total = model.Total;

            var laneNotes = new int[model.Mode.Key, 3];
            var data = new int[model.LastTime / 1000 + 2, 7];
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
                            data[index, model.Mode.IsScratchKey(i) ? 1 : 4]++;
                        }
                        if (model.LNType == LNType.LongNote)
                            continue;
                    }

                    switch (note)
                    {
                        case NormalNote:
                            data[tl.Time / 1000, model.Mode.IsScratchKey(i) ? 2 : 5]++;
                            laneNotes[i, 0]++;
                            break;
                        case LongNote:
                            data[tl.Time / 1000, model.Mode.IsScratchKey(i) ? 0 : 3]++;
                            data[tl.Time / 1000, model.Mode.IsScratchKey(i) ? 1 : 4]++;
                            laneNotes[i, 1]++;
                            break;
                        case MineNote:
                            data[tl.Time / 1000, 6]++;
                            laneNotes[i, 2]++;
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
                var notes = data[i, 0] + data[i, 1] + data[i, 2] + data[i, 3] + data[i, 4] + data[i, 5];
                PeakDensity = PeakDensity > notes ? PeakDensity : notes;
                if (notes >= bd)
                {
                    Density += notes;
                    count++;
                }
            }

            var d = 5 < (data.Length - borderpos - 1) ? 5 : (data.Length - borderpos - 1);
            EndDensity = 0;
            for (var i = borderpos; i < data.Length - d; i++)
            {
                var notes = 0;
                foreach (var j in Enumerable.Range(0, d))
                {
                    notes += data[i + j, 0] + data[i + j, 1] + data[i + j, 2] + data[i + j, 3] + data[i + j, 4] + data[i + j, 5];
                }
                EndDensity = EndDensity > ((double)notes / d) ? EndDensity : ((double)notes / d);
            }

            var distribution = new StringBuilder();
            distribution.Append('#');
            foreach (var i in Enumerable.Range(0, data.Length))
            {
                foreach (var j in Enumerable.Range(0, 7))
                {
                    var value = data[i, j] < 36 * 36 - 1 ? data[i, j] : 36 * 36 - 1;
                    var val1 = value / 36;
                    distribution.Append(value >= 10 ? val1 - 10 + 'a' : val1 + '0');
                    val1 = value % 36;
                    distribution.Append(value >= 10 ? val1 - 10 + 'a' : val1 + '0');
                }
            }
            Distribution = distribution.ToString();

            List<double[]> speedList = new ();
            Dictionary<double, int> bpmNoteCountMap = new ();
            double nowSpeed = model.Bpm;
            speedList.Add(new double[] { nowSpeed, 0.0 });
            foreach (var tl in model.TimeLines)
            {
                int notecount = bpmNoteCountMap.TryGetValue(tl.Bpm, out var cnt) ? cnt : 0;
                bpmNoteCountMap[tl.Bpm] =notecount + tl.GetTotalNotes();

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
        }
    }
}
