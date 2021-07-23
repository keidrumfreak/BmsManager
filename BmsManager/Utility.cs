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
            if (title == null)
                return string.Empty;

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
            if (artists == null)
                return string.Empty;

            // 空白の場合無視する
            var names = artists.Select(a => RemoveObjer(a)).Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => RemoveObjer(a));
            if (!names.Any())
                return string.Empty;

            var name = names.FirstOrDefault(a => names.All(x => x.Contains(a))) ?? string.Empty;
            if (!string.IsNullOrEmpty(name))
                return name;

            // 区切りの一番前を参照してみる
            names = names.Select(a => a.Split('/')[0].Trim());
            return names.FirstOrDefault(a => names.All(x => x.Contains(a))) ?? string.Empty;
        }

        public static string RemoveObjer(string name)
        {
            if (name == null)
                return string.Empty;

            string remove(string target, string str)
            {
                var index = name.ToLower().IndexOf(str);
                if (index == -1)
                    return target;
                return target.Substring(0, index);
            }

            var ret = remove(name, "obj");
            ret = remove(ret, "note:");
            ret = remove(ret, "差分");

            return ret.Trim(' ', '/', '(');
        }
    }
}
