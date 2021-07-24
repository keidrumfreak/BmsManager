using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BmsManager.Data
{
    [Table("BmsFile")]
    class BmsFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int FolderID { get; set; }

        public string MD5 { get; set; }

        public string Path { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        [ForeignKey(nameof(FolderID))]
        public virtual BmsFolder Folder { get; set; }
    }
}
