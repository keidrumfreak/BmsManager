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
using BmsManager.Data;
using BmsParser;
using CommonLib.IO;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;

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
                    FileList.Folders = null;
                }
                else
                {
                    if (selectedDiffFile.Folders != null)
                        FileList.Folders = new ObservableCollection<BmsFolderViewModel>(selectedDiffFile.Folders.Select(f => new BmsFolderViewModel(f, FileList, selectedDiffFile)));
                    selectedDiffFile.PropertyChanged += DiffFile_PropertyChanged;
                }
            }
        }

        public ICommand SearchDiff { get; set; }

        public ICommand EstimateAll { get; set; }

        public ICommand InstallByTable { get; set; }

        public ICommand InstallAll { get; set; }

        public DiffRegisterViewModel()
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
            DiffFiles = new ObservableCollection<DiffFileViewModel>(files.Select(f => BmsModel.Decode(f)).Where(m => m != null).Select(m => new DiffFileViewModel(m, this)));
        }

        private void estimateAll()
        {
            foreach (var file in DiffFiles)
            {
                file.GetEstimatedDestination();
            }
        }

        private void installAll()
        {
            foreach (var diffFile in DiffFiles.ToArray())
            {
                if (diffFile.Folders.Count() != 1)
                    continue;

                var folder = diffFile.Folders.First();
                try
                {
                    if (folder.Files.Any(f => f.MD5 == diffFile.MD5))
                    {
                        // 重複する場合はインストールしない
                        File.Delete(diffFile.Path);
                        Application.Current.Dispatcher.Invoke(() => diffFile.Remove());
                        continue;
                    }


                    var toPath = PathUtil.Combine(folder.Path, Path.GetFileName(diffFile.Path));
                    var i = 1;
                    var dst = toPath;
                    while (SystemProvider.FileSystem.File.Exists(toPath))
                    {
                        i++;
                        toPath = PathUtil.Combine(folder.Path, $"{Path.GetFileNameWithoutExtension(dst)} ({i}){Path.GetExtension(dst)}");
                    }

                    SystemProvider.FileSystem.File.Move(diffFile.Path, toPath);

                    using (var con = new BmsManagerContext())
                    {
                        var fol = con.BmsFolders.Include(f => f.Files).FirstOrDefault(f => f.Path == folder.Path);
                        var file = new BmsFile(toPath);
                        fol.Files.Add(file);
                        con.SaveChanges();
                    }

                    Application.Current.Dispatcher.Invoke(() => diffFile.Remove());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }

        private void DiffFile_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DiffFileViewModel.Folders))
            {
                FileList.Folders = new ObservableCollection<BmsFolderViewModel>(selectedDiffFile.Folders.Select(f => new BmsFolderViewModel(f, FileList, SelectedDiffFile)));
            }
        }

        private void installByTable()
        {
            using (var con = new BmsManagerContext())
            {
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

                    folder.Files.Add(new BmsFile(toPath));
                    file.Remove();
                }
                con.SaveChanges();
            }
        }
    }
}
