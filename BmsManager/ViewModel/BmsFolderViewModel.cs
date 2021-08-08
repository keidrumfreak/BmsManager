using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.IO;
using CommonLib.Wpf;
using SysPath = System.IO.Path;

namespace BmsManager
{
    class BmsFolderViewModel : ViewModelBase
    {
        public string Title => folder.Title;

        public string Artist => folder.Artist;

        public string Path => folder.Path;

        public ICommand OpenFolder { get; set; }

        public ICommand Merge { get; set; }

        public IEnumerable<BmsFileViewModel> Files { get; set; }

        public IEnumerable<BmsFolder> Duplicates { get; set; }

        BmsFolder folder;
        BmsFileListViewModel parent;

        public BmsFolderViewModel(BmsFolder folder, BmsFileListViewModel parent)
        {
            this.parent = parent;
            this.folder = folder;
            Files = folder.Files.Select(f => new BmsFileViewModel(f, parent)).ToArray();
            OpenFolder = CreateCommand(input => openFolder());
            Merge = CreateCommand(input => Task.Run(() => merge()), input => Duplicates?.Any() ?? false);
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

        public void merge()
        {
            var roots = Duplicates.Select(d => d.Root).ToArray();
            var ext = BmsExtension.GetExtensions();

            foreach (var fol in Duplicates)
            {
                foreach (var file in fol.Files)
                {
                    // 重複BMSファイルは先に削除しておく
                    folder.Files.Any(f => f.MD5 == file.MD5);
                    if (SystemProvider.FileSystem.File.Exists(file.Path))
                        SystemProvider.FileSystem.File.Delete(file.Path);
                }

                if (SystemProvider.FileSystem.Directory.Exists(fol.Path))
                {
                    foreach (var file in SystemProvider.FileSystem.Directory.EnumerateFiles(fol.Path, "*.*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var toPath = PathUtil.Combine(Path, SysPath.GetFileName(file));
                            var fileExt = SysPath.GetExtension(file); // 拡張子が存在しない場合もある
                            if (fileExt.Length > 1 && ext.Contains(fileExt.Substring(1)))
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
                    }
                }

                Application.Current.Dispatcher.Invoke(() => parent.BmsFolders.Remove(parent.BmsFolders.FirstOrDefault(f => f.Path == fol.Path)));

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
                root.LoadFromFileSystem();
                root.Register();
            }
            folder.Root.LoadFromFileSystem();
            folder.Root.Register();

            Application.Current.Dispatcher.Invoke(() => parent.BmsFolders.Remove(this));
        }
    }
}
