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
        public ObservableCollection<BmsFile> BmsFiles
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
                    return new ObservableCollection<BmsFile>(Folders.SelectMany(f => f.Files).ToArray());
                }
            }
        }

        ObservableCollection<BmsFolder> folders;
        public ObservableCollection<BmsFolder> Folders
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

        BmsFile selectedBmsFile;
        public BmsFile SelectedBmsFile
        {
            get { return selectedBmsFile; }
            set { SetProperty(ref selectedBmsFile, value); }
        }

        public bool Narrowed { get; set; }

        public ICommand ChangeNarrowing { get; set; }

        public ICommand DeleteFile { get; set; }

        public BmsFileListViewModel()
        {
            ChangeNarrowing = CreateCommand(() => OnPropertyChanged(nameof(BmsFiles)));
            DeleteFile = CreateCommand<BmsFile>(deleteFileAsync);
        }

        private async Task deleteFileAsync(BmsFile file)
        {
            await file.DeleteAsync();
            BmsFiles.Remove(file);
        }
    }
}
