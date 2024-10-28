using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Logging;

namespace BmsManager
{
    class SystemProvider
    {
        static SystemProvider instance;
        public static SystemProvider Instance
        {
            get { return instance ?? (instance = new SystemProvider(new FileSystem())); }
            set { instance = value; }
        }

        IFileSystem fileSystem;
        public static IFileSystem FileSystem => Instance.fileSystem;

        ILogger logger;

        public static ILogger Logger => Instance.logger;

        public SystemProvider(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
            logger = new TextLogger(Settings.Default.LogFolder);
        }
    }
}
