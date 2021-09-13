﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using BmsManager.Data;

namespace BmsManager
{
    static class Utility
    {
        public static string GetCrc32(string path)
        {
            // Javaのコードの実行結果と一致させるための実装。要調査
            var polynomial = 0xEDB88320u;
            var crc = ~0;
            foreach (var b in Encoding.GetEncoding("shift-jis").GetBytes(path + "\\\0").Cast<sbyte>())
            {
                crc ^= b; // sbyteにしない場合ここで差異が出ていると思われる
                for (var i = 0; i < 8; i++)
                {
                    if ((crc & 1) != 0)
                        crc = (int)(((uint)crc >> 1) ^ polynomial); // 論理シフトするため一度uintにキャスト
                    else
                        crc = (int)((uint)crc >> 1); // 論理シフトするため一度uintにキャスト
                }
            }
            return (~crc).ToString("x");
        }

        public static (string Title, string Artist) GetMetaFromFiles(this IEnumerable<BmsFile> files)
        {

            string intersect(IEnumerable<string> source)
            {
                foreach (var skip in Enumerable.Range(0, source.Count()))
                {
                    var arr = skip == 0 ? source : source.Take(skip - 1).Concat(source.Skip(skip));
                    var ret = new List<char>();
                    var tmp = arr.Take(1).First();
                    foreach (var i in Enumerable.Range(0, arr.Min(s => s.Length)))
                    {
                        if (arr.Skip(1).All(s => tmp[i] == s[i]))
                            ret.Add(tmp[i]);
                        else
                            break;
                    }
                    if (ret.Any())
                    {
                        if (ret.Count(c => c == '(' || c == '（') != ret.Count(c => c == ')' || c == '）'))
                        {
                            var index = ret.LastIndexOf('(');
                            index = index == -1 ? ret.LastIndexOf('（') : index;
                            if (index != -1)
                                ret = ret.GetRange(0, index);
                        }
                        if (ret.Count(c => c == '[') != ret.Count(c => c == ']'))
                        {
                            var index = ret.LastIndexOf('[');
                            if (index != -1)
                                ret = ret.GetRange(0, index);
                        }

                        var str = new string(ret.ToArray()).TrimEnd(' ', '/');
                        if (ret.Count(c => c == '-') % 2 != 0)
                        {
                            str = str.TrimEnd('-');
                        }
                        return str.TrimEnd(' ', '/');
                    }
                }
                return string.Empty;
            }

            return (intersect(files.Select(f => f.Title)), intersect(files.Select(f => f.Artist)));
        }

        /// <summary>
        /// 日時をミリ秒に変換します
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static int ToUnixMilliseconds(this DateTime dt)
        {
            return (int)((dt - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000);
        }

        public static string GetMd5Hash(string path)
        {
            using (var file = SystemProvider.FileSystem.File.OpenRead(path))
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

        public static string ToFileNameString(string name)
        {
            return name.Replace('\\', '￥').Replace('<', '＜').Replace('>', '＞').Replace('/', '／').Replace('*', '＊').Replace(":", "：")
                .Replace("\"", "”").Replace('?', '？').Replace('|', '｜');
        }
    }

    class BmsManagerException : Exception
    {
        public BmsManagerException(string message) : base(message) { }

        public BmsManagerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
