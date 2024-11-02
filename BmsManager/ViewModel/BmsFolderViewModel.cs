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
using CommonLib.IO;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;
using SysPath = System.IO.Path;

namespace BmsManager.ViewModel
{
    interface IBmsFolderParentViewModel
    {
        ObservableCollection<BmsFolderViewModel> Folders { get; set; }
    }

    class BmsFolderViewModel : ViewModelBase
    {
        public string Title
        {
            get { return folder.Title; }
            set { folder.Title = value; }
        }

        public string Artist
        {
            get { return folder.Artist; }
            set { folder.Artist = value; }
        }

        public string Path => folder.Path;

        public ICommand OpenFolder { get; set; }

        public ICommand Merge { get; set; }

        public ICommand Install { get; set; }

        ObservableCollection<BmsFile> files;
        public ObservableCollection<BmsFile> Files
        {
            get { return files; }
            set { SetProperty(ref files, value); }
        }

        public IEnumerable<BmsFolder> Duplicates { get; set; }

        public IEnumerable<DiffFileViewModel> DiffFiles { get; set; }

        readonly BmsFolder folder;
        readonly IBmsFolderParentViewModel parent;
        readonly DiffFileViewModel diffFile;

        public BmsFolderViewModel(BmsFolder folder, IBmsFolderParentViewModel parent, DiffFileViewModel diffFile = null)
        {
            this.parent = parent;
            this.folder = folder;
            this.diffFile = diffFile;
            Files = new ObservableCollection<BmsFile>(folder.Files);
            OpenFolder = CreateCommand(openFolder);
            Merge = CreateCommand(() => Task.Run(() => merge()), () => Duplicates?.Any() ?? false);
            Install = CreateCommand(install, () => diffFile != null);
        }

        public void Rename()
        {
            folder.Rename();
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Artist));
            OnPropertyChanged(nameof(Path));
            Files = new ObservableCollection<BmsFile>(folder.Files);
        }

        public void UpdateMeta()
        {
            folder.UpdateMeta();
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Artist));
        }

        private void openFolder()
        {
            Process.Start(new ProcessStartInfo { FileName = Path, UseShellExecute = true, Verb = "open" });
        }

        private async Task merge()
        {
            var roots = Duplicates.Select(d => d.Root).ToArray();
            var ext = Settings.Default.Extentions;

            foreach (var fol in Duplicates)
            {
                foreach (var file in fol.Files)
                    // 重複BMSファイルは先に削除しておく
                    if (folder.Files.Any(f => f.MD5 == file.MD5))
                        if (SystemProvider.FileSystem.File.Exists(file.Path))
                            SystemProvider.FileSystem.File.Delete(file.Path);

                if (SystemProvider.FileSystem.Directory.Exists(fol.Path))
                    foreach (var file in SystemProvider.FileSystem.Directory.EnumerateFiles(fol.Path, "*.*", SearchOption.AllDirectories))
                        try
                        {
                            var toPath = PathUtil.Combine(Path, SysPath.GetFileName(file));
                            var fileExt = SysPath.GetExtension(file); // 拡張子が存在しない場合もある
                            if (fileExt.Length > 1 && ext.Contains(fileExt[1..]))
                            {
                                var i = 1;
                                var dst = toPath;
                                while (SystemProvider.FileSystem.File.Exists(toPath))
                                {
                                    i++;
                                    toPath = $"{dst} ({i})";
                                }
                            }

                            // 統合先のファイルを正とみなす
                            if (!SystemProvider.FileSystem.File.Exists(toPath))
                                SystemProvider.FileSystem.File.Move(file, toPath, true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                            return;
                        }

                Application.Current.Dispatcher.Invoke(() => parent.Folders.Remove(parent.Folders.FirstOrDefault(f => f.Path == fol.Path)));

                try
                {
                    SystemProvider.FileSystem.Directory.Delete(fol.Path, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return;
                }
            }

            foreach (var root in roots)
            {
                await root.LoadFromFileSystem();
                root.Register();
            }
            await folder.Root.LoadFromFileSystem();
            folder.Root.Register();

            Application.Current.Dispatcher.Invoke(() => parent.Folders.Remove(this));
        }

        private void install()
        {
            try
            {
                if (folder.Files.Any(f => f.MD5 == diffFile.MD5))
                {
                    // 重複する場合はインストールしない
                    File.Delete(diffFile.Path);
                    Application.Current.Dispatcher.Invoke(() => diffFile.Remove());
                    return;
                }

                var toPath = PathUtil.Combine(folder.Path, SysPath.GetFileName(diffFile.Path));
                var i = 1;
                var dst = toPath;
                while (SystemProvider.FileSystem.File.Exists(toPath))
                {
                    i++;
                    toPath = PathUtil.Combine(folder.Path, $"{SysPath.GetFileNameWithoutExtension(dst)} ({i}){SysPath.GetExtension(dst)}");
                }

                SystemProvider.FileSystem.File.Move(diffFile.Path, toPath);

                using (var con = new BmsManagerContext())
                {
                    var fol = con.BmsFolders.Include(f => f.Files).FirstOrDefault(f => f.Path == folder.Path);
                    var file = new BmsFile(toPath);
                    fol.Files.Add(file);
                    con.SaveChanges();
                }

                diffFile.Remove();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
