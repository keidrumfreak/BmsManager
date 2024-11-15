using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Entity;
using BmsManager.Model;
using BmsParser;
using CommonLib.IO;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.ViewModel
{
    class DiffRegistererViewModel : ViewModelBase
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
                    FileList.Folders = null;
                else
                {
                    if (selectedDiffFile.Folders != null)
                        FileList.Folders = new ObservableCollection<BmsFolderViewModel>(selectedDiffFile.Folders.ToArray());
                    selectedDiffFile.PropertyChanged += DiffFile_PropertyChanged;
                }
            }
        }

        public ICommand SearchDiff { get; set; }

        public ICommand EstimateAll { get; set; }

        public ICommand InstallByTable { get; set; }

        public ICommand InstallAll { get; set; }

        public DiffRegistererViewModel()
        {
            FileList = new BmsFileListViewModel();
            SearchDiff = CreateCommand(searchDiff);
            EstimateAll = CreateCommand(estimateAll);
            InstallByTable = CreateCommand(installByTable);
            InstallAll = CreateCommand(() => Task.Run(() => installAll()));
        }

        private void searchDiff()
        {
            if (string.IsNullOrEmpty(TargetPath))
                return;

            var extentions = Settings.Default.Extentions;
            var files = SystemProvider.FileSystem.Directory.EnumerateFiles(TargetPath, "*.*", SearchOption.AllDirectories)
                .Where(f => extentions.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant())).ToArray();
            DiffFiles = new ObservableCollection<DiffFileViewModel>(files.Select(f => ChartDecoder.GetDecoder(f)?.Decode(f)).Where(m => m != null).Select(m => new DiffFileViewModel(m, this)));
        }

        private void estimateAll()
        {
            foreach (var file in DiffFiles)
                file.GetEstimatedDestination();
        }

        private void installAll()
        {
            foreach (var diffFile in DiffFiles.ToArray())
            {
                if (diffFile.Folders.Count() != 1)
                    continue;

                var folder = diffFile.Folders.First();
                folder.Install(diffFile.Path, diffFile.MD5);
                Application.Current.Dispatcher.Invoke(() => diffFile.Remove());
            }
        }

        private void DiffFile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DiffFileViewModel.Folders))
                FileList.Folders = new ObservableCollection<BmsFolderViewModel>(selectedDiffFile.Folders);
        }

        private async void installByTable()
        {
            using var con = new BmsManagerContext();
            foreach (var file in DiffFiles)
            {
                var table = con.TableDatas.FirstOrDefault(d => d.MD5 == file.MD5);
                if (table == default)
                    continue;
                var folder = con.Files.Include(f => f.Folder).FirstOrDefault(f => f.MD5 == table.OrgMD5)?.Folder;
                if (folder == default)
                    continue;

                var toPath = PathUtil.Combine(folder.Path, Path.GetFileName(file.Path));
                var i = 1;
                var dst = toPath;
                while (SystemProvider.FileSystem.File.Exists(toPath))
                {
                    i++;
                    toPath = $"{Path.GetFileNameWithoutExtension(dst)} ({i}){Path.GetExtension(dst)}";
                }

                SystemProvider.FileSystem.File.Move(file.Path, toPath);

                var model = ChartDecoder.GetDecoder(toPath)?.Decode(toPath);
                folder.Files.Add(model?.ToEntity() ?? throw new Exception());
                file.Remove();
            }
            con.SaveChanges();
        }
    }
}
