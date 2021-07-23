using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BmsManager
{
    class Utility
    {
        public static string GetMd5Hash(string path)
        {
            using (var file = File.OpenRead(path))
            {
                var md5 = MD5.Create();
                var arr = md5.ComputeHash(file);
                return BitConverter.ToString(arr).ToLower().Replace("-", "");
            }
        }

        public static string GetTitle(string title)
        {
            var index = -1;
            if (title.Contains("  "))
            {
                return title.Split("  ")[0];
            }
            else if (title.EndsWith("-") || title.EndsWith("～") || title.EndsWith("\""))
            {
                var delimiter = title.Last();
                var trim = title.Substring(0, title.Length - 1);
                index = trim.LastIndexOf(delimiter);
            }
            else if (title.EndsWith(")"))
            {
                index = title.LastIndexOf("(");
            }
            else if (title.EndsWith("]"))
            {
                index = title.LastIndexOf("[");
            }
            else if (title.EndsWith(">"))
            {
                index = title.LastIndexOf("<");
            }

            return index == -1 ? title : title.Substring(0, index);
        }

        public static string GetArtist(IEnumerable<string> artists)
        {
            return artists.FirstOrDefault(a => artists.All(x => x.Contains(a))) ?? string.Empty;
        }
    }
}
