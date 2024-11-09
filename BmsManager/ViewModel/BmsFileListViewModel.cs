using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Entity;
using BmsParser;
using CommonLib.Wpf;

namespace BmsManager.ViewModel
{
    class BmsFileListViewModel : ViewModelBase
    {
        public ObservableCollection<BmsFileViewModel> BmsFiles
        {
            get
            {
                if (Folders == null)
                    return [];
                else if (Narrowed && SelectedBmsFolder != null)
                    return SelectedBmsFolder.Files;
                else
                    return new ObservableCollection<BmsFileViewModel>(Folders.SelectMany(f => f.Files ?? []).ToArray());
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

        public BmsModel SelectedDiffFile { get; set; }

        public bool Narrowed { get; set; }

        public ICommand ChangeNarrowing { get; set; }

        public ICommand DeleteFile { get; set; }

        public BmsFileListViewModel()
        {
            ChangeNarrowing = CreateCommand(() => OnPropertyChanged(nameof(BmsFiles)));
            //DeleteFile = CreateCommand<BmsFile>(deleteFileAsync);
        }

        //private async Task deleteFileAsync(BmsFile file)
        //{
        //    await file.DeleteAsync();
        //    BmsFiles.Remove(file);
        //}
    }
}
