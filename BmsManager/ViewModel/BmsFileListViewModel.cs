using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Wpf;

namespace BmsManager
{
    class BmsFileListViewModel : ViewModelBase
    {
        IEnumerable<BmsFileViewModel> bmsFiles;
        public IEnumerable<BmsFileViewModel> BmsFiles
        {
            get { return bmsFiles; }
            set { SetProperty(ref bmsFiles, value); }
        }

        IEnumerable<BmsFolderViewModel> bmsFolders;
        public IEnumerable<BmsFolderViewModel> BmsFolders
        {
            get { return bmsFolders; }
            set { SetProperty(ref bmsFolders, value); searchFile(); }
        }

        BmsFolderViewModel selectedBmsFolder;
        public BmsFolderViewModel SelectedBmsFolder
        {
            get { return selectedBmsFolder; }
            set { SetProperty(ref selectedBmsFolder, value); searchFile(); }
        }

        BmsFileViewModel selectedBmsFile;
        public BmsFileViewModel SelectedBmsFile
        {
            get { return selectedBmsFile; }
            set { SetProperty(ref selectedBmsFile, value); }
        }

        public bool Narrowed { get; set; }

        public ICommand ChangeNarrowing { get; set; }

        public BmsFileListViewModel()
        {
            ChangeNarrowing = CreateCommand(input => searchFile());
        }

        private void searchFile()
        {
            if (Narrowed && SelectedBmsFolder != null)
            {
                BmsFiles = SelectedBmsFolder.Files;
            }
            else
            {
                BmsFiles = BmsFolders.SelectMany(f => f.Files).ToArray();
            }
        }
    }
}
