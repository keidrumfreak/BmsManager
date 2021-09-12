using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.IO;
using CommonLib.Wpf;

namespace BmsManager
{
    class ExporterViewModel : ViewModelBase
    {
        public string BeatorajaFolder { get; set; }

        public string SongInfoDBPath { get; set; }

        public ICommand Export { get; set; }

        public ExporterViewModel()
        {
            Export = CreateCommand(input => export());
        }

        private void export()
        {
            using var con = new BmsManagerContext();
            con.ExportToBeatoragja(PathUtil.Combine(BeatorajaFolder, "songdata.db"), PathUtil.Combine(BeatorajaFolder, "songinfo.db"));
        }
    }
}
