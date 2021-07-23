using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BmsManager
{
    class BmsText
    {
        public string FullPath { get; private set; }

        public string Title { get; private set; }

        public string Artist { get; private set; }

        string[] text;

        public BmsText(string path)
        {
            FullPath = path;
            text = File.ReadAllLines(path, Encoding.GetEncoding("shift-jis"));
            Title = text.FirstOrDefault(t => t.StartsWith("#TITLE"))?.Replace("#TITLE ", "");
            Artist = text.FirstOrDefault(t => t.StartsWith("#ARTIST"))?.Replace("#ARTIST ", "");
        }
    }
}
