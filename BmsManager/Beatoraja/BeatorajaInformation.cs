using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BmsManager.Data;
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

        public BeatorajaInformation() { }

        public BeatorajaInformation(BmsFile file)
        {
            Sha256 = file.Sha256;
            N = file.N;
            LN = file.LN;
            S = file.S;
            LS = file.LS;
            Total = file.Total;
            Density = file.Density;
            PeakDensity = file.PeakDensity;
            EndDensity = file.EndDensity;
            Distribution = file.Distribution;
            MainBpm = file.MainBpm;
            SpeedChange = file.SpeedChange;
            LaneNotes = file.LaneNotes;
        }
    }
}
