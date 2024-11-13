using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BmsManager.Model;
using BmsParser;
using CommonLib.IO;
using CommonLib.Linq;
using CommonLib.Wpf;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SysPath = System.IO.Path;

namespace BmsManager.Entity
{
    [Table("BmsFolder")]
    class BmsFolder : BindableBase
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int RootID { get; set; }

        public string Path { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        public bool HasText { get; set; }

        public string? Preview { get; set; }

        public DateTime FolderUpdateDate { get; set; }

        [InverseProperty(nameof(BmsFile.Folder))]
        public virtual ICollection<BmsFile> Files { get; set; }

        [ForeignKey(nameof(RootID))]
        public virtual RootDirectory Root { get; set; }
    }
}
