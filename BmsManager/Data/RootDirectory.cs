using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BmsManager.Data
{
    [Table("RootDirectory")]
    class RootDirectory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public string Path { get; set; }

        public int? ParentRootID { get; set; }

        [InverseProperty(nameof(BmsFolder.Root))]
        public virtual ICollection<BmsFolder> Folders { get; set; }

        [ForeignKey(nameof(ParentRootID))]
        public virtual RootDirectory Parent { get; set; }

        [InverseProperty(nameof(Parent))]
        public virtual ICollection<RootDirectory> Children { get; set; }
    }
}
