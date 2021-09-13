using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BmsManager
{
    class Settings
    {
        static Settings instance;
        public static Setting Default => (instance ?? (instance = new Settings())).settings;

        //public string BmsManagerConnectionStrings => settings["bmsManagerConnectionStrings"];

        //public string DatabaseKind => settings["databaseKind"];

        Setting settings;

        private Settings()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName))
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            settings = configuration.GetSection("settings").Get<Setting>();
        }

        public class Setting
        {
            public string BmsManagerConnectionStrings { get; set; }
            public string DatabaseKind { get; set; }
            public string[] Extentions { get; set; }
        }
    }
}
