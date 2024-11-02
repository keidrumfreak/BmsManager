using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Entity;
using CommonLib.IO;
using CommonLib.Wpf;

namespace BmsManager.ViewModel
{
    class ExporterViewModel : ViewModelBase
    {
        public string BeatorajaFolder { get; set; }

        public string SongInfoDBPath { get; set; }

        public ICommand Export { get; set; }

        public ExporterViewModel()
        {
            Export = CreateCommand(export);
        }

        private async Task export()
        {
            using var con = new BmsManagerContext();
            await con.ExportToBeatoragjaAsync(PathUtil.Combine(BeatorajaFolder, "songdata.db"), PathUtil.Combine(BeatorajaFolder, "songinfo.db"));
            MessageBox.Show("完了しました");
        }
    }
}
