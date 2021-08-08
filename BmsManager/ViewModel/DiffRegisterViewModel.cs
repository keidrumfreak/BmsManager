using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Wpf;

namespace BmsManager
{
    class DiffRegisterViewModel : ViewModelBase
    {
        public BmsFileListViewModel FileList { get; set; }

        public string TargetPath { get; set; }

        ObservableCollection<DiffFileViewModel> diffFiles;
        public ObservableCollection<DiffFileViewModel> DiffFiles
        {
            get { return diffFiles; }
            set { SetProperty(ref diffFiles, value); }
        }

        DiffFileViewModel selectedDiffFile;
        public DiffFileViewModel SelectedDiffFile
        {
            get { return selectedDiffFile; }
            set
            {
                if (selectedDiffFile != null)
                    selectedDiffFile.PropertyChanged -= DiffFile_PropertyChanged;
                SetProperty(ref selectedDiffFile, value);
                if (selectedDiffFile == null)
                {
                    FileList.BmsFolders = null;
                }
                else
                {
                    if (selectedDiffFile.Folders != null)
                        FileList.BmsFolders = new ObservableCollection<BmsFolderViewModel>(selectedDiffFile.Folders.Select(f => new BmsFolderViewModel(f, FileList)));
                    selectedDiffFile.PropertyChanged += DiffFile_PropertyChanged;
                }
            }
        }

        public ICommand SearchDiff { get; set; }

        public DiffRegisterViewModel()
        {
            FileList = new BmsFileListViewModel();
            SearchDiff = CreateCommand(input => searchDiff());
        }

        private void searchDiff()
        {
            if (string.IsNullOrEmpty(TargetPath))
                return;

            var extentions = BmsExtension.GetExtensions();
            var files = SystemProvider.FileSystem.Directory.EnumerateFiles(TargetPath, "*.*", SearchOption.AllDirectories)
                .Where(f => extentions.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant())).ToArray();
            DiffFiles = new ObservableCollection<DiffFileViewModel>(files.Select(f => new DiffFileViewModel(f, this)));
        }

        private void DiffFile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DiffFileViewModel.Folders))
            {
                FileList.BmsFolders = new ObservableCollection<BmsFolderViewModel>(selectedDiffFile.Folders.Select(f => new BmsFolderViewModel(f, FileList, SelectedDiffFile)));
            }
        }
    }
}
