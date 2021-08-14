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
    class BmsFileListViewModel : ViewModelBase, IBmsFolderParentViewModel
    {
        public ObservableCollection<BmsFileViewModel> BmsFiles
        {
            get
            {
                if (Folders == null)
                {
                    return null;
                }
                else if (Narrowed && SelectedBmsFolder != null)
                {
                    return SelectedBmsFolder.Files;
                }
                else
                {
                    return new ObservableCollection<BmsFileViewModel>(Folders.SelectMany(f => f.Files).ToArray());
                }
            }
        }

        ObservableCollection<BmsFolderViewModel> folders;
        public ObservableCollection<BmsFolderViewModel> Folders
        {
            get { return folders; }
            set { SetProperty(ref folders, value); OnPropertyChanged(nameof(BmsFiles)); }
        }

        BmsFolderViewModel selectedBmsFolder;
        public BmsFolderViewModel SelectedBmsFolder
        {
            get { return selectedBmsFolder; }
            set { SetProperty(ref selectedBmsFolder, value); OnPropertyChanged(nameof(BmsFiles)); }
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
            ChangeNarrowing = CreateCommand(input => OnPropertyChanged(nameof(BmsFiles)));
        }

        private void searchFile()
        {
            //if (Folders == null)
            //{
            //    BmsFiles = null;
            //}
            //else if (Narrowed && SelectedBmsFolder != null)
            //{
            //    BmsFiles = new ObservableCollection<BmsFileViewModel>(SelectedBmsFolder.Files);
            //}
            //else
            //{
            //    BmsFiles = new ObservableCollection<BmsFileViewModel>(Folders.SelectMany(f => f.Files).ToArray());
            //}
        }
    }
}
