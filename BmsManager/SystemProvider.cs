using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Logging;

namespace BmsManager
{
    class SystemProvider(IFileSystem fileSystem)
    {
        static SystemProvider? instance;
        public static SystemProvider Instance
        {
            get { return instance ??= new SystemProvider(new FileSystem()); }
            set { instance = value; }
        }

        readonly IFileSystem fileSystem = fileSystem;
        public static IFileSystem FileSystem => Instance.fileSystem;

        readonly ILogger logger = new TextLogger(Settings.Default.LogFolder);

        public static ILogger Logger => Instance.logger;
    }
}
