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
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;

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

        public ICommand Search { get; set; }

        public ICommand LoadDB { get; set; }

        private IEnumerable<BmsFile> bmsFiles;
        public IEnumerable<BmsFile> BmsFiles { get; set; }

        public IEnumerable<BmsFolder> BmsFolders { get; set; }

        BmsFolder selectedBmsFolder;
        public BmsFolder SelectedBmsFolder
        {
            get { return selectedBmsFolder; }
            set { selectedBmsFolder = value; changeNarrowing(null); if (selectedBmsFolder != null) { RenameText = Path.GetFileName(selectedBmsFolder.Path); } }
        }

        BmsFile selectedBmsFile;
        public BmsFile SelectedBmsFile
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

        IEnumerable<RootDirectory> roots;
        public IEnumerable<RootDirectory> Roots
        {
            get { return roots; }
            set { SetProperty(ref roots, value); }
        }

        RootDirectory selectedRoot;
        public RootDirectory SelectedRoot
        {
            get { return selectedRoot; }
            set { SetProperty(ref selectedRoot, value); TreeRoot = new[] { SelectedRoot }; }
        }

        IEnumerable<RootDirectory> treeRoot;
        public IEnumerable<RootDirectory> TreeRoot
        {
            get { return treeRoot; }
            set { SetProperty(ref treeRoot, value); }
        }

        RootDirectory selectedNode;
        public RootDirectory SelectedNode
        {
            get { return selectedNode; }
            set { SetProperty(ref selectedNode, value); setNodeData(selectedNode); }
        }

        public bool Narrowed { get; set; }

        public FolderRegisterViewModel()
        {
            Load = CreateCommand(load);
            AddRoot = CreateCommand(addRoot);
            ChangeNarrowing = CreateCommand(changeNarrowing);
            AutoRename = CreateCommand(autoRename);
            Rename = CreateCommand(rename);
            Register = CreateCommand(register);
            Search = CreateCommand(searchRoot);
            LoadDB = CreateCommand(loadDB);

            using (var con = new BmsManagerContext())
            {
                Roots = con.RootDirectories.AsNoTracking().ToArray();
            }
        }

        private void addRoot(object input)
        {
            if (string.IsNullOrEmpty(TargetDirectory))
                return;

            using (var con = new BmsManagerContext())
            {
                if (con.RootDirectories.Any(f => f.Path == TargetDirectory))
                {
                    MessageBox.Show("既に登録済のフォルダは登録できません。");
                    return;
                }

                con.RootDirectories.Add(new RootDirectory { Path = TargetDirectory });
                con.SaveChanges();

                Roots = con.RootDirectories.ToArray();
                SelectedRoot = Roots.Last();
            }
        }

        private void load(object input)
        {
            if (SelectedNode == null)
                return;

            var extensions = BmsExtension.GetExtensions();

            var files = Directory.EnumerateFiles(SelectedNode.Path, "*.*", SearchOption.AllDirectories)
                .Where(f => extensions.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant()));

            var bmses = files.Select(f => new BmsText(f));

            bmsFiles = bmses.Select(bms => new BmsFile { Path = bms.FullPath, Artist = bms.Artist, Title = bms.Title, MD5 = Utility.GetMd5Hash(bms.FullPath) }).ToList();
            BmsFiles = bmsFiles;

            BmsFolders = BmsFiles.GroupBy(bms => Path.GetDirectoryName(bms.Path), (path, bms) => bms.Where(b => b.Path.StartsWith(path)))
                .Select(g => new BmsFolder { Path = Path.GetDirectoryName(g.First().Path), Files = g.ToList() }).ToList();

            foreach (var folder in BmsFolders)
            {
                folder.Root = getRoot(folder.Path);
                var name = Path.GetFileName(folder.Path);
                var index = name.IndexOf("]");
                if (index != -1)
                {
                    folder.Artist = name.Substring(1, index - 1);
                    folder.Title = name.Substring(index + 1);
                }
            }

            foreach (var grp in BmsFolders.GroupBy(f => f.Root))
            {
                var root = grp.Key;
                var folders = grp.Select(g => g);
                if (root.Folders == null)
                {
                    root.Folders = folders.ToList();
                    continue;
                }

                foreach (var folder in root.Folders.ToArray())
                {
                    if (folders.Any(f => f.Path == folder.Path))
                        continue;

                    // 実体が存在しないフォルダを削除する
                    foreach (var file in folder.Files.ToArray())
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

            OnPropertyChanged(nameof(BmsFolders));
            OnPropertyChanged(nameof(BmsFiles));
        }

        private void loadDB(object input)
        {
            using (var con = new BmsManagerContext())
            {
                var folders = con.BmsFolders
                    .Include(f => f.Files)
                    .AsNoTracking().ToArray();

                var allRoots = con.RootDirectories
                    .AsNoTracking().ToArray();

                foreach (var folder in folders.GroupBy(f => f.RootID))
                {
                    var root = allRoots.FirstOrDefault(r => r.ID == folder.Key);
                    root.Folders = folder.ToList();
                    foreach (var fol in folder)
                    {
                        fol.Root = root;
                    }
                }

                foreach (var root in allRoots)
                {
                    root.Children = allRoots.Where(r => r.ParentRootID == root.ID).ToList();
                }

                var roots = allRoots.Where(r => r.ParentRootID == null).ToList();

                TreeRoot = roots;
            }
        }

        private RootDirectory getRoot(string folder)
        {
            var roots = descendants(SelectedNode).ToArray();
            var path = Path.GetDirectoryName(folder);
            while (true)
            {
                var root = roots.FirstOrDefault(r => r.Path == path);
                if (root != default)
                    return root;
                path = Path.GetDirectoryName(path);
            }

            IEnumerable<RootDirectory> descendants(RootDirectory root)
            {
                yield return root;
                if (root.Children != null)
                {
                    foreach (var child in root.Children)
                    {
                        foreach (var son in descendants(child))
                            yield return son;
                    }
                }
            };
        }

        private void setNodeData(RootDirectory root)
        {
            if (root.Folders == null && root.Children == null)
            {
                BmsFolders = null;
                BmsFiles = null;
            }

            BmsFolders = descendants(root);
            BmsFiles = BmsFolders.SelectMany(f => f.Files);

            IEnumerable<BmsFolder> descendants(RootDirectory root)
            {
                if (root.Folders != null)
                {
                    foreach (var folder in root.Folders)
                        yield return folder;
                }

                if (root.Children != null)
                {
                    foreach (var child in root.Children)
                    {
                        foreach (var folder in descendants(child))
                            yield return folder;
                    }
                }

            };

            OnPropertyChanged(nameof(BmsFolders));
            OnPropertyChanged(nameof(BmsFiles));
        }

        private void searchRoot(object input)
        {
            if (SelectedRoot == null)
                return;

            var extensions = BmsExtension.GetExtensions();

            SelectedRoot.Children = searchRoot(SelectedRoot).ToArray();

            IEnumerable<RootDirectory> searchRoot(RootDirectory root)
            {
                foreach (var folder in Directory.EnumerateDirectories(root.Path))
                {
                    var bmses = Directory.EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Where(f => extensions.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant()));
                    if (bmses.Any())
                        continue; // ルートではない
                    var child = new RootDirectory { Path = folder, Parent = root };
                    child.Children = searchRoot(child).ToArray();
                    yield return child;
                }
            };

            TreeRoot = new[] { SelectedRoot };
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
            if (BmsFolders == null)
                return;

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
                    .Replace("\"", "”").Replace('?', '？').Replace('|', '｜');

                var dst = Path.Combine(Path.GetDirectoryName(folder.Path), rename);
                var tmp = Path.Combine(Path.GetDirectoryName(folder.Path), "tmp");

                bool retry = false;
                while (true)
                {
                    try
                    {
                        Directory.Move(folder.Path, tmp);
                        Directory.Move(tmp, dst);
                    }
                    catch (Exception ex)
                    {
                        if (Directory.Exists(tmp))
                            Directory.Move(tmp, folder.Path);

                        if (!retry)
                        {
                            // 一回だけリトライ (名称は適当につける)
                            rename = $"[{Utility.GetArtist(folder.Files.Select(f => f.Artist))}]{folder.Files.First().Title}";
                            dst = Path.Combine(Path.GetDirectoryName(folder.Path), rename);
                            if (Directory.Exists(dst))
                            {
                                var i = 1;
                                var ren = dst;
                                while (true)
                                {
                                    i++;
                                    ren = $"{dst} ({i})";
                                    if (!Directory.Exists(ren))
                                    {
                                        dst = ren;
                                        break;
                                    }
                                }
                            }
                            retry = true;
                            continue;
                        }

                        MessageBox.Show($"FileName:{rename}\r\n{ex}");
                    }
                    break;
                }
            }

            load(input);
        }

        private void rename(object input)
        {
            if (string.IsNullOrEmpty(RenameText))
                return;

            var rename = RenameText
                .Replace('\\', '￥').Replace('<', '＜').Replace('>', '＞').Replace('/', '／').Replace('*', '＊').Replace(":", "：")
                .Replace("\"", "”").Replace('?', '？').Replace('|', '｜'); ;

            var dst = Path.Combine(Path.GetDirectoryName(SelectedBmsFolder.Path), rename);
            var tmp = Path.Combine(Path.GetDirectoryName(SelectedBmsFolder.Path), "tmp");

            try
            {
                Directory.Move(SelectedBmsFolder.Path, tmp);
                Directory.Move(tmp, dst);
            }
            catch (Exception ex)
            {
                if (Directory.Exists(tmp))
                    Directory.Move(tmp, SelectedBmsFolder.Path);

                MessageBox.Show($"FileName:{RenameText}\r\n{ex}");
            }

            load(input);
        }

        private void register(object input)
        {
            if (BmsFolders == null)
                return;

            using (var con = new BmsManagerContext())
            {
                foreach (var group in BmsFolders.GroupBy(f => f.Root))
                {
                    var root = con.RootDirectories.AsNoTracking().FirstOrDefault(r => r.Path == group.Key.Path);
                    var folders = group.Select(g => g);
                    if (root == null)
                    {
                        // ルート未登録の場合ルートごと登録
                        // 親が存在しない場合それも登録
                        int registerParent(RootDirectory dir)
                        {
                            var parent = con.RootDirectories.AsNoTracking().FirstOrDefault(d => d.Path == dir.Parent.Path);
                            if (parent == default)
                            {
                                dir.ParentRootID = registerParent(dir.Parent);
                            }
                            else
                            {
                                dir.ParentRootID = parent.ID;
                            }
                            // Childrenが参照されるとIDが明示的に入ってしまうため、インスタンスを新規作成
                            var tmp = new RootDirectory { Path = dir.Path, ParentRootID = dir.ParentRootID };
                            con.RootDirectories.Add(tmp);
                            con.SaveChanges();
                            return tmp.ID;
                        }

                        root = con.RootDirectories.Find(registerParent(group.Key));
                    }

                    if (root.Folders == null)
                    {
                        // ルートにフォルダが未登録の場合そのまま追加
                        root.Folders = folders.ToArray();
                        continue;
                    }

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
                var roots = con.RootDirectories.ToArray();
                con.SaveChanges();
            }

            MessageBox.Show("登録完了しました");
        }
    }
}
