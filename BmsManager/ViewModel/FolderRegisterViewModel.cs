using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Wpf;

namespace BmsManager
{
    class FolderRegisterViewModel : ViewModelBase
    {
        public string TargetDirectory { get; set; }

        public ICommand Load { get; set; }

        public ICommand AddRoot { get; set; }

        public ICommand ChangeNarrowing { get; set; }

        public ICommand AutoRename { get; set; }

        public ICommand Rename { get; set; }

        public ICommand Register { get; set; }

        private IEnumerable<BmsFileListItem> bmsFiles;
        public IEnumerable<BmsFileListItem> BmsFiles { get; set; }

        public IEnumerable<BmsFolderListItem> BmsFolders { get; set; }

        BmsFolderListItem selectedBmsFolder;
        public BmsFolderListItem SelectedBmsFolder
        {
            get { return selectedBmsFolder; }
            set { selectedBmsFolder = value; changeNarrowing(null); if (selectedBmsFolder != null) { RenameText = Path.GetFileName(selectedBmsFolder.FolderPath); } }
        }

        BmsFileListItem selectedBmsFile;
        public BmsFileListItem SelectedBmsFile
        {
            get { return selectedBmsFile; }
            set { selectedBmsFile = value; if (selectedBmsFile != null) { RenameText = $"[{selectedBmsFile.Artist}]{Utility.GetTitle(selectedBmsFile.Title)}"; } }
        }

        string renameText;
        public string RenameText
        {
            get { return renameText; }
            set { SetProperty(ref renameText, value); }
        }

        public IEnumerable<RootDirectory> Roots { get; set; }

        public RootDirectory SelectedRoot { get; set; }

        public bool Narrowed { get; set; }

        public FolderRegisterViewModel()
        {
            Load = CreateCommand(load);
            AddRoot = CreateCommand(addRoot);
            ChangeNarrowing = CreateCommand(changeNarrowing);
            AutoRename = CreateCommand(autoRename);
            Rename = CreateCommand(rename);
            Register = CreateCommand(register);

            using (var con = new BmsManagerContext())
            {
                Roots = con.RootDirectories.ToArray();
            }
        }

        private void addRoot(object input)
        {
            using (var con = new BmsManagerContext())
            {
                if (con.RootDirectories.Any(f => TargetDirectory.Contains(f.Path)))
                {
                    MessageBox.Show("既に登録済のフォルダの下位フォルダは登録できません。");
                    return;
                }

                con.RootDirectories.Add(new RootDirectory { Path = TargetDirectory });
                con.SaveChanges();

                Roots = con.RootDirectories.ToArray();
                OnPropertyChanged(nameof(Roots));
            }
        }

        private void load(object input)
        {
            var extensions = getExtensions();

            var files = Directory.EnumerateFiles(SelectedRoot.Path, "*.*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant()));

            var bmses = files.Select(f => new BmsText(f));

            bmsFiles = bmses.Select(bms => new BmsFileListItem { FilePath = bms.FullPath, Artist = bms.Artist, Title = bms.Title });
            BmsFiles = bmsFiles;

            BmsFolders = BmsFiles.GroupBy(bms => Path.GetDirectoryName(bms.FilePath), (path, bms) => bms.Where(b => b.FilePath.StartsWith(path)))
                .Select(g => new BmsFolderListItem { FolderPath = Path.GetDirectoryName(g.First().FilePath), Files = g });

            OnPropertyChanged(nameof(BmsFolders));
            OnPropertyChanged(nameof(BmsFiles));
        }

        private IEnumerable<string> getExtensions()
        {
            using (var con = new BmsManagerContext())
            {
                return con.Extensions.Select(e => e.Extension).ToArray();
            }
        }

        private void changeNarrowing(object input)
        {
            if (Narrowed && SelectedBmsFolder != null)
            {
                BmsFiles = SelectedBmsFolder.Files;
            }
            else
            {
                BmsFiles = bmsFiles;
            }

            OnPropertyChanged(nameof(BmsFiles));
        }

        private void autoRename(object input)
        {
            foreach (var folder in BmsFolders)
            {
                var artist = Utility.GetArtist(folder.Files.Select(f => f.Artist));
                var title = Utility.GetTitle(folder.Files.First().Title);
                if (artist.Length > 50)
                    artist = artist.Substring(0, 50);
                if (title.Length > 50)
                    title = title.Substring(0, 50);
                var rename = $"[{Utility.GetArtist(folder.Files.Select(f => f.Artist))}]{Utility.GetTitle(folder.Files.First().Title)}"
                    .Replace('\\', '￥').Replace('<', '＜').Replace('>', '＞').Replace('/', '／').Replace('*', '＊').Replace(":", "：")
                    .Replace("\"", "”").Replace('?', '？');

                var dst = Path.Combine(Path.GetDirectoryName(folder.FolderPath), rename);
                var tmp = Path.Combine(Path.GetDirectoryName(folder.FolderPath), "tmp");

                try
                {
                    Directory.Move(folder.FolderPath, tmp);
                    Directory.Move(tmp, dst);
                }
                catch (Exception ex)
                {
                    if (Directory.Exists(tmp))
                        Directory.Move(tmp, folder.FolderPath);

                    MessageBox.Show($"FileName:{rename}\r\n{ex}");
                }
            }

            load(input);
        }

        private void rename(object input)
        {
            var rename = RenameText
                .Replace('\\', '￥').Replace('<', '＜').Replace('>', '＞').Replace('/', '／').Replace('*', '＊').Replace(":", "：")
                .Replace("\"", "”").Replace('?', '？');

            var dst = Path.Combine(Path.GetDirectoryName(SelectedBmsFolder.FolderPath), rename);
            var tmp = Path.Combine(Path.GetDirectoryName(SelectedBmsFolder.FolderPath), "tmp");

            try
            {
                Directory.Move(SelectedBmsFolder.FolderPath, tmp);
                Directory.Move(tmp, dst);
            }
            catch (Exception ex)
            {
                if (Directory.Exists(tmp))
                    Directory.Move(tmp, SelectedBmsFolder.FolderPath);

                MessageBox.Show($"FileName:{RenameText}\r\n{ex}");
            }

            load(input);
        }

        private void register(object input)
        {
            var folders = BmsFolders.Select(folder =>
            new BmsFolder
            {
                Path = folder.FolderPath,
                Artist = folder.Artist,
                Title = folder.Title,
                Files = folder.Files.Select(file => new BmsFile { FileName = Path.GetFileName(file.FilePath), MD5 = Utility.GetMd5Hash(file.FilePath) }).ToArray()
            });

            using (var con = new BmsManagerContext())
            {
                var root = con.RootDirectories.Find(SelectedRoot.ID);
                if (root.Folders == null)
                {
                    root.Folders = folders.ToArray();
                }
                else
                {
                    foreach (var folder in root.Folders.ToArray())
                    {
                        if (folders.Any(f => f.Path == folder.Path))
                            continue;

                        // 実体が存在しないフォルダを削除する
                        foreach (var file in folder.Files)
                        {
                            folder.Files.Remove(file);
                        }
                        root.Folders.Remove(folder);
                    }

                    foreach (var folder in folders)
                    {
                        var dbFolder = root.Folders?.FirstOrDefault(f => f.Path == folder.Path);
                        if (dbFolder == default)
                        {
                            // フォルダ自体未登録なら追加するだけ
                            root.Folders.Add(folder);
                            continue;
                        }

                        dbFolder.Artist = folder.Artist;
                        dbFolder.Title = folder.Title;

                        foreach (var file in dbFolder.Files.ToArray())
                        {
                            if (folder.Files.Any(f => f.MD5 == file.MD5))
                                continue;

                            // 実体が存在しないファイルを削除する
                            folder.Files.Remove(file);
                        }

                        foreach (var file in folder.Files)
                        {
                            if (!dbFolder.Files.Any(f => f.MD5 == file.MD5))
                                dbFolder.Files.Add(file); // 登録されていないファイルなら登録する
                        }
                    }
                }
                con.SaveChanges();
            }

            MessageBox.Show("登録完了しました");
        }

        public class BmsFolderListItem
        {
            string folderPath;
            public string FolderPath
            {
                get { return folderPath; }
                set
                {
                    folderPath = value;
                    var name = Path.GetFileName(folderPath);
                    var index = name.IndexOf("]");
                    if (index != -1)
                    {
                        Artist = name.Substring(1, index - 1);
                        Title = name.Substring(index + 1);
                    }
                }
            }
            public IEnumerable<BmsFileListItem> Files { get; set; }

            public string Artist { get; set; }
            public string Title { get; set; }
        }

        public class BmsFileListItem
        {
            public string FilePath { get; set; }

            public string Title { get; set; }

            public string Artist { get; set; }
        }
    }
}
