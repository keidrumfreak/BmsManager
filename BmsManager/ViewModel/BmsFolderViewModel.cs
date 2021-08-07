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
    class BmsFolderViewModel : ViewModelBase
    {
        public string Title => folder.Title;

        public string Artist => folder.Artist;

        public string Path => folder.Path;

        public ICommand OpenFolder { get; set; }

        public IEnumerable<BmsFile> Files => folder.Files;

        BmsFolder folder;

        public BmsFolderViewModel(BmsFolder folder)
        {
            this.folder = folder;
            OpenFolder = CreateCommand(input => openFolder());
        }

        public void AutoRename()
        {
            folder.AutoRename();
        }

        public void Rename(string name)
        {
            folder.Rename(name);
        }

        private void openFolder()
        {
            Process.Start(new ProcessStartInfo { FileName = Path, UseShellExecute = true, Verb = "open" });
        }
    }
}
