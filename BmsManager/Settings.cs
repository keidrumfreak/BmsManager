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
        static Settings? instance;
        public static Setting Default => (instance ??= new Settings()).settings;

        readonly Setting settings;

        private Settings()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName) ?? throw new Exception())
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            settings = configuration.GetSection("settings").Get<Setting>() ?? throw new Exception();
        }

        public class Setting
        {
            public required string BmsManagerConnectionStrings { get; set; }
            public required string DatabaseKind { get; set; }
            public required string[] Extentions { get; set; }
            public required string LogFolder { get; set; }
        }
    }
}
