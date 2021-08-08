using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        ObservableCollection<BmsFileViewModel> bmsFiles;
        public ObservableCollection<BmsFileViewModel> BmsFiles
        {
            get { return bmsFiles; }
            set { SetProperty(ref bmsFiles, value); }
        }

        ObservableCollection<BmsFolderViewModel> bmsFolders;
        public ObservableCollection<BmsFolderViewModel> BmsFolders
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
                BmsFiles = new ObservableCollection<BmsFileViewModel>(SelectedBmsFolder.Files);
            }
            else
            {
                BmsFiles = new ObservableCollection<BmsFileViewModel>(BmsFolders.SelectMany(f => f.Files).ToArray());
            }
        }
    }
}
