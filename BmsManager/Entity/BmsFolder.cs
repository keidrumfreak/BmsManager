using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BmsManager.Entity
{
    [Table("BmsFolder")]
    class BmsFolder
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int RootID { get; set; }

        public required string Path { get; set; }

        public string? Artist { get; set; }

        public string? Title { get; set; }

        public bool HasText { get; set; }

        public string? Preview { get; set; }

        public DateTime FolderUpdateDate { get; set; }

        [InverseProperty(nameof(BmsFile.Folder))]
        public virtual ICollection<BmsFile> Files { get; set; } = [];

        RootDirectory? root;
        [ForeignKey(nameof(RootID))]
        public virtual RootDirectory Root
        {
            get => root ?? throw new InvalidOperationException();
            set => root = value;
        }
    }
}
