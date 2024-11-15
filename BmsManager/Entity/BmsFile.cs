using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BmsManager.Entity
{
    [Table("BmsFile")]
    class BmsFile
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int FolderID { get; set; }

        public required string Path { get; set; }

        public required string Title { get; set; }

        public required string Subtitle { get; set; }

        public required string Genre { get; set; }

        public required string Artist { get; set; }

        public required string SubArtist { get; set; }

        public required string MD5 { get; set; }

        public required string Sha256 { get; set; }

        public required string Banner { get; set; }

        public required string StageFile { get; set; }

        public required string BackBmp { get; set; }

        public required string Preview { get; set; }

        public required string PlayLevel { get; set; }

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

        public required string ChartHash { get; set; }

        public int N { get; set; }

        public int LN { get; set; }

        public int S { get; set; }

        public int LS { get; set; }

        public double Total { get; set; }

        public double Density { get; set; }

        public double PeakDensity { get; set; }

        public double EndDensity { get; set; }

        public required string Distribution { get; set; }

        public double MainBpm { get; set; }

        public required string SpeedChange { get; set; }

        public required string LaneNotes { get; set; }

        BmsFolder? folder;
        [ForeignKey(nameof(FolderID))]
        public virtual BmsFolder Folder
        {
            get => folder ?? throw new InvalidOperationException();
            set => folder = value;
        }
    }
}
