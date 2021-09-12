using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BmsManager.Data;
using SysPath = System.IO.Path;

namespace BmsManager.Beatoraja
{
    [Table("folder")]
    class BeatorajaFolder
    {
        [NotMapped]
        public static readonly string RootCrc = "e2977170";

        [Column("path"), Key]
        public string Path { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("subtitle")]
        public string SubTitle { get; set; }

        [Column("command")]
        public string Command { get; set; }

        [Column("type")]
        public int Type { get; set; }

        [Column("banner")]
        public string Text { get; set; }

        [Column("parent")]
        public string Parent { get; set; }

        [Column("date")]
        public int Date { get; set; }

        [Column("max")]
        public int Max { get; set; }

        [Column("adddate")]
        public int AddDate { get; set; }

        public BeatorajaFolder() { }

        public BeatorajaFolder(BmsFolder folder)
        {
            Title = SysPath.GetFileName(folder.Path);
            Path = folder.Path;
            Parent = Utility.GetCrc32(folder.Root.Path);
            Date = folder.FolderUpdateDate.ToUnixMilliseconds();
        }

        public BeatorajaFolder(RootDirectory root)
        {
            Title = SysPath.GetFileName(root.Path);
            Path = root.Path;
            Parent = root.Parent == null ? RootCrc : Utility.GetCrc32(root.Parent.Path);
            Date = root.FolderUpdateDate.ToUnixMilliseconds();
        }
    }
}
