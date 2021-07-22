using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BmsManager.Data
{
    [Table("File")]
    class BmsFile
    {
        public int ID { get; set; }

        public int FolderID { get; set; }

        public string MD5 { get; set; }

        public string FileName { get; set; }

        [ForeignKey(nameof(FolderID))]
        public virtual BmsFolder Folder { get; set; }
    }
}
