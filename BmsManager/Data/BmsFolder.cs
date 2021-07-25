using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLib.IO;
using ClsPath = System.IO.Path;

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
            var title = Utility.GetTitle(Files.First().Title);
            if (artist.Length > 50)
                artist = artist.Substring(0, 50);
            if (title.Length > 50)
                title = title.Substring(0, 50);
            var rename = $"[{Utility.GetArtist(Files.Select(f => f.Artist))}]{Utility.GetTitle(Files.First().Title)}";
            Rename(rename);
        }

        public void Rename(string name)
        {
            var rename = name.Replace('\\', '￥').Replace('<', '＜').Replace('>', '＞').Replace('/', '／').Replace('*', '＊').Replace(":", "：")
                .Replace("\"", "”").Replace('?', '？').Replace('|', '｜');

            var dst = PathUtil.Combine(ClsPath.GetDirectoryName(Path), rename);
            var tmp = PathUtil.Combine(ClsPath.GetDirectoryName(Path), "tmp");

            try
            {
                Directory.Move(Path, tmp);

                var i = 1;
                var ren = dst;
                while (Directory.Exists(ren))
                {
                    i++;
                    ren = $"{dst} ({i})";
                }
                dst = ren;

                Directory.Move(tmp, dst);
                return;
            }
            catch (IOException)
            {
                if (Directory.Exists(tmp))
                    Directory.Move(tmp, Path);
                throw;
            }
        }
    }
}
