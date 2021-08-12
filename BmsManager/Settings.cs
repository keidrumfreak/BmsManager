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
        public static Settings Default => instance ?? (instance = new Settings());

        public string BmsManagerConnectionStrings => settings["bmsManagerConnectionStrings"];

        public string DatabaseKind => settings["databaseKind"];

        IConfiguration configuration;
        IConfigurationSection settings;

        private Settings()
        {
            configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName))
                .AddJsonFile("appsettings.json", true, true)
                .Build();
            settings = configuration.GetSection("settings");
        }
    }
}
