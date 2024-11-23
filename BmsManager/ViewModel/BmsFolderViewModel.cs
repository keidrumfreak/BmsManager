using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BmsManager.Entity;
using BmsManager.Model;
using BmsParser;
using CommonLib.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.ViewModel
{
    internal class BmsFolderViewModel : ObservableObject
    {
        public ObservableCollection<BmsFileViewModel> Files { get; set; }

        public ICommand OpenFolder { get; }

        public ICommand Merge { get; }

        public string Title => entity.Title;

        public string Artist => entity.Artist;

        public string FullPath => entity.Path;

        //public string Path => entity.Path;

        public int ID => entity.ID;

        public IEnumerable<BmsFolderViewModel> Duplicates { get; set; }

        BmsFolder entity;

        public BmsFolderViewModel(BmsFolder entity)
        {
            this.entity = entity;
            Files = new ObservableCollection<BmsFileViewModel>(entity.Files.Select(f => new BmsFileViewModel(f)));
            OpenFolder = new RelayCommand(openFolder);
        }

        private void openFolder() => Process.Start(new ProcessStartInfo { FileName = entity.Path, UseShellExecute = true, Verb = "open" });

        public void UpdateMeta(string title, string artist)
        {
            using var context = new BmsManagerContext();
            var folder = context.BmsFolders.First(f => f.ID == entity.ID);
            folder.Title = title;
            folder.Artist = artist;
            context.SaveChanges();
        }

        public void Rename()
        {
            var rename = $"[{Utility.ToFileNameString(Artist)}]{Utility.ToFileNameString(Title)}";

            var dst = PathUtil.Combine(Path.GetDirectoryName(entity.Path), rename);
            var tmp = PathUtil.Combine(Path.GetDirectoryName(entity.Path), "tmp");

            if (Path.GetFileName(entity.Path) == rename)
                return;

            try
            {
                SystemProvider.FileSystem.Directory.Move(entity.Path, tmp);

                var i = 1;
                var ren = dst;
                while (SystemProvider.FileSystem.Directory.Exists(ren))
                {
                    i++;
                    ren = $"{dst} ({i})";
                }
                dst = ren;

                SystemProvider.FileSystem.Directory.Move(tmp, dst);

                SetMetaFromName();

                using var con = new BmsManagerContext();
                var folder = con.BmsFolders.Include(f => f.Files).FirstOrDefault(f => f.Path == entity.Path);
                if (folder != default)
                {
                    folder.Path = dst;
                    entity.Path = dst;
                    SetMetaFromName();
                    folder.Title = Title;
                    folder.Artist = Artist;
                    foreach (var file in folder.Files)
                    {
                        file.Path = PathUtil.Combine(dst, Path.GetFileName(file.Path));
                    }
                    con.SaveChanges();
                }
            }
            catch (IOException)
            {
                if (SystemProvider.FileSystem.Directory.Exists(tmp))
                    SystemProvider.FileSystem.Directory.Move(tmp, entity.Path);
                throw;
            }
        }

        private async Task merge()
        {
            var roots = Duplicates.Select(d => d.entity.Root).ToArray();
            var ext = Settings.Default.Extentions;

            foreach (var fol in Duplicates)
            {
                foreach (var file in fol.Files)
                {
                    // 重複BMSファイルは先に削除しておく
                    if (Files.Any(f => f.MD5 == file.MD5))
                    {
                        if (SystemProvider.FileSystem.File.Exists(file.FullPath))
                            SystemProvider.FileSystem.File.Delete(file.FullPath);
                    }
                }

                if (SystemProvider.FileSystem.Directory.Exists(fol.FullPath))
                {
                    foreach (var file in SystemProvider.FileSystem.Directory.EnumerateFiles(fol.FullPath, "*.*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var toPath = PathUtil.Combine(FullPath, Path.GetFileName(file));
                            var fileExt = Path.GetExtension(file); // 拡張子が存在しない場合もある
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
                    }
                }

                try
                {
                    SystemProvider.FileSystem.Directory.Delete(fol.FullPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    return;
                }
            }

            var loader = new FolderLoader();
            foreach (var root in roots)
            {
                await loader.LoadAsync(root);
                //root.Register();
            }
            await loader.LoadAsync(entity.Root);
            //Root.Register();

            //Application.Current.Dispatcher.Invoke(() => parent.Folders.Remove(this));
        }

        public async void Install(string path, string md5)
        {
            try
            {
                if (entity.Files.Any(f => f.MD5 == md5))
                {
                    // 重複する場合はインストールしない
                    File.Delete(path);
                    return;
                }


                var toPath = PathUtil.Combine(entity.Path, Path.GetFileName(path));
                var i = 1;
                var dst = toPath;
                while (SystemProvider.FileSystem.File.Exists(toPath))
                {
                    i++;
                    toPath = PathUtil.Combine(entity.Path, $"{Path.GetFileNameWithoutExtension(dst)} ({i}){Path.GetExtension(dst)}");
                }

                SystemProvider.FileSystem.File.Move(path, toPath);

                using (var con = new BmsManagerContext())
                {
                    var fol = con.BmsFolders.Include(f => f.Files).First(f => f.Path == entity.Path);
                    var file = ChartDecoder.GetDecoder(toPath)?.Decode(toPath);
                    fol.Files.Add(file?.ToEntity() ?? throw new Exception());
                    con.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void SetMetaFromName()
        {
            var name = Path.GetFileName(entity.Path);
            var index = name.IndexOf(']');
            if (index != -1)
            {
                entity.Artist = name[1..index];
                entity.Title = name[(index + 1)..];
            }
        }

        public void CheckDuplicateByMD5() { }

        public void CheckDuplicateByMeta() { }
    }

    static class BmsFolderExtention
    {
        public static void CheckDuplicateByMD5(this IEnumerable<BmsFolderViewModel> folders)
        {
            using var con = new BmsManagerContext();
            // 重複BMSが存在するフォルダを全て取得する (まだどのフォルダと重複しているかは見ない)
            var dupMD5 = con.Files
                .GroupBy(f => f.MD5)
                .Select(g => new { MD5 = g.Key, Count = g.Count() })
                .Where(g => g.Count > 1);
            var folID = con.Files.Join(dupMD5, f => f.MD5, d => d.MD5, (f, d) => f.Folder.ID).Distinct();
            var fol = con.BmsFolders.Where(f => folID.Contains(f.ID))
                .Include(f => f.Files).Include(f => f.Root)
                .AsNoTracking().ToArray();

            foreach (var folder in folders)
            {
                // TODO: 重複判定をもう少しわかりやすくしたい
                folder.Duplicates = folders.Where(f => f.ID != folder.ID && f.Files.Any(f1 => folder.Files.Any(f2 => f1.MD5 == f2.MD5))).ToArray();
            }
        }

        public static void CheckDuplicateByMeta(this IEnumerable<BmsFolderViewModel> folders)
        {
            using var con = new BmsManagerContext();
            // 重複BMSが存在するフォルダを全て取得する (まだどのフォルダと重複しているかは見ない)
            var dupMeta = con.BmsFolders
                .GroupBy(f => new { f.Title, f.Artist })
                .Select(g => new { Meta = g.Key, Count = g.Count() })
                .Where(g => g.Count > 1);
            var folID = con.BmsFolders.Join(dupMeta, f => new { f.Title, f.Artist }, d => d.Meta, (f, d) => f.ID).Distinct();
            var fol = con.BmsFolders.Where(f => folID.Contains(f.ID))
                .Include(f => f.Files).Include(f => f.Root)
                .AsNoTracking().ToArray();

            foreach (var folder in folders)
            {
                folder.Duplicates = folders.Where(f => f.ID != folder.ID && f.Title == folder.Title && f.Artist == folder.Artist).ToArray();
            }
        }
    }
}
