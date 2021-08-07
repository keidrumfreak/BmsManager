using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Wpf;

namespace BmsManager
{
    class BmsTableDataViewModel : ViewModelBase
    {
        public string Difficulty => data.Difficulty.Difficulty;

        public string Title => data.Title;

        public string Artist => data.Artist;

        public string MD5 => data.MD5;

        public string Comment => data.Comment;

        public ICommand OpenUrl { get; }

        public ICommand OpenDiffUrl { get; }

        public ICommand OpenPackUrl { get; }

        BmsTableData data;

        public BmsTableDataViewModel(BmsTableData data)
        {
            this.data = data;

            OpenUrl = CreateCommand(input => openUrl(data.Url), input => !string.IsNullOrEmpty(data.Url));
            OpenDiffUrl = CreateCommand(input => openUrl(data.DiffUrl), input => !string.IsNullOrEmpty(data.DiffUrl));
            OpenPackUrl = CreateCommand(input => openUrl(data.PackUrl), input => !string.IsNullOrEmpty(data.PackUrl));
        }

        private void openUrl(string url)
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
    }
}
