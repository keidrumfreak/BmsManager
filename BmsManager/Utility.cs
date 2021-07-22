using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
    }
}
