using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClsPath = System.IO.Path;

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

        public void LoadFromFileSystem()
        {
            var extentions = BmsExtension.GetExtensions();
            Folders = new List<BmsFolder>();
            Children = new List<RootDirectory>();
            foreach (var folder in SystemProvider.FileSystem.Directory.EnumerateDirectories(Path))
            {
                var files = SystemProvider.FileSystem.Directory.EnumerateFiles(folder)
                    .Where(f => extentions.Contains(ClsPath.GetExtension(f).TrimStart('.').ToLowerInvariant()));
                if (files.Any())
                {
                    var fol = new BmsFolder
                    {
                        Path = folder,
                        Files = files.Select(file => new BmsFile(file)).ToList(),
                    };
                    fol.SetMetaFromName();
                    Folders.Add(fol);
                }
                else
                {
                    var child = new RootDirectory { Path = folder };
                    child.LoadFromFileSystem();
                    if (child.Children.Any() || child.Folders.Any())
                        Children.Add(child);
                }
            }
        }

        public IEnumerable<RootDirectory> Descendants()
        {
            yield return this;
            if (Children == null || !Children.Any())
                yield break;
            foreach (var child in Children.SelectMany(c => c.Descendants()))
                yield return child;
        }
    }
}
