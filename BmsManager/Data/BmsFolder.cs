using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLib.IO;
using Microsoft.EntityFrameworkCore;
using SysPath = System.IO.Path;

namespace BmsManager.Data
{
    [Table("BmsFolder")]
    class BmsFolder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public int RootID { get; set; }

        public string Path { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        [InverseProperty(nameof(BmsFile.Folder))]
        public virtual ICollection<BmsFile> Files { get; set; }

        [ForeignKey(nameof(RootID))]
        public virtual RootDirectory Root { get; set; }

        public void AutoRename()
        {
            var artist = Utility.GetArtist(Files.Select(f => f.Artist));
            var title = Utility.GetTitle(Files.Where(f => !string.IsNullOrWhiteSpace(f.Title)).FirstOrDefault()?.Title);
            if (artist.Length > 50)
                artist = artist.Substring(0, 50);
            if (title.Length > 50)
                title = title.Substring(0, 50);
            var rename = $"[{Utility.GetArtist(Files.Select(f => f.Artist))}]{Utility.GetTitle(Files.First().Title)}".Trim();
            Rename(rename);
        }

        public void Rename(string name)
        {
            var rename = name.Replace('\\', '￥').Replace('<', '＜').Replace('>', '＞').Replace('/', '／').Replace('*', '＊').Replace(":", "：")
                .Replace("\"", "”").Replace('?', '？').Replace('|', '｜');

            var dst = PathUtil.Combine(SysPath.GetDirectoryName(Path), rename);
            var tmp = PathUtil.Combine(SysPath.GetDirectoryName(Path), "tmp");

            if (SysPath.GetFileName(Path) == rename)
                return;

            try
            {
                SystemProvider.FileSystem.Directory.Move(Path, tmp);

                var i = 1;
                var ren = dst;
                while (SystemProvider.FileSystem.Directory.Exists(ren))
                {
                    i++;
                    ren = $"{dst} ({i})";
                }
                dst = ren;

                SystemProvider.FileSystem.Directory.Move(tmp, dst);

                SetMetaFromName();

                using (var con = new BmsManagerContext())
                {
                    var folder = con.BmsFolders.Include(f => f.Files).FirstOrDefault(f => f.Path == Path);
                    folder.Path = dst;
                    Path = dst;
                    SetMetaFromName();
                    folder.Title = Title;
                    folder.Artist = Artist;
                    foreach (var file in folder.Files)
                    {
                        file.Path = PathUtil.Combine(dst, SysPath.GetFileName(file.Path));
                    }
                    con.SaveChanges();
                }
            }
            catch (IOException)
            {
                if (SystemProvider.FileSystem.Directory.Exists(tmp))
                    SystemProvider.FileSystem.Directory.Move(tmp, Path);
                throw;
            }
        }

        public void SetMetaFromName()
        {
            var name = SysPath.GetFileName(Path);
            var index = name.IndexOf("]");
            if (index != -1)
            {
                Artist = name.Substring(1, index - 1);
                Title = name.Substring(index + 1);
            }
        }
    }
}
