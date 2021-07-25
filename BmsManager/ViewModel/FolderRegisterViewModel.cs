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

        public ObservableCollection<BmsFolder> BmsFolders { get; set; }

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

            SelectedNode.LoadFromFileSystem();

            setNodeData(SelectedNode);

            //var extensions = BmsExtension.GetExtensions();

            //var files = Directory.EnumerateFiles(SelectedNode.Path, "*.*", SearchOption.AllDirectories)
            //    .Where(f => extensions.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant()));

            //var bmses = files.Select(f => new BmsText(f));

            //bmsFiles = bmses.Select(bms => new BmsFile { Path = bms.FullPath, Artist = bms.Artist, Title = bms.Title, MD5 = Utility.GetMd5Hash(bms.FullPath) }).ToList();
            //BmsFiles = bmsFiles;

            //BmsFolders = new ObservableCollection<BmsFolder>(BmsFiles.GroupBy(bms => Path.GetDirectoryName(bms.Path), (path, bms) => bms.Where(b => b.Path.StartsWith(path)))
            //    .Select(g => new BmsFolder { Path = Path.GetDirectoryName(g.First().Path), Files = g.ToList() }).ToList());

            //foreach (var folder in BmsFolders)
            //{
            //    folder.Root = getRoot(folder.Path);
            //    var name = Path.GetFileName(folder.Path);
            //    var index = name.IndexOf("]");
            //    if (index != -1)
            //    {
            //        folder.Artist = name.Substring(1, index - 1);
            //        folder.Title = name.Substring(index + 1);
            //    }
            //}

            //foreach (var grp in BmsFolders.GroupBy(f => f.Root))
            //{
            //    var root = grp.Key;
            //    var folders = grp.Select(g => g);
            //    if (root.Folders == null)
            //    {
            //        root.Folders = folders.ToList();
            //        continue;
            //    }

            //    foreach (var folder in root.Folders.ToArray())
            //    {
            //        if (folders.Any(f => f.Path == folder.Path))
            //            continue;

            //        // 実体が存在しないフォルダを削除する
            //        foreach (var file in folder.Files.ToArray())
            //        {
            //            folder.Files.Remove(file);
            //        }
            //        root.Folders.Remove(folder);
            //    }

            //    foreach (var folder in folders)
            //    {
            //        var dbFolder = root.Folders?.FirstOrDefault(f => f.Path == folder.Path);
            //        if (dbFolder == default)
            //        {
            //            // フォルダ自体未登録なら追加するだけ
            //            root.Folders.Add(folder);
            //            continue;
            //        }

            //        dbFolder.Artist = folder.Artist;
            //        dbFolder.Title = folder.Title;

            //        foreach (var file in dbFolder.Files.ToArray())
            //        {
            //            if (folder.Files.Any(f => f.MD5 == file.MD5))
            //                continue;

            //            // 実体が存在しないファイルを削除する
            //            folder.Files.Remove(file);
            //        }

            //        foreach (var file in folder.Files)
            //        {
            //            if (!dbFolder.Files.Any(f => f.MD5 == file.MD5))
            //                dbFolder.Files.Add(file); // 登録されていないファイルなら登録する
            //        }
            //    }
            //}

            //OnPropertyChanged(nameof(BmsFolders));
            //OnPropertyChanged(nameof(BmsFiles));
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

            BmsFolders = new ObservableCollection<BmsFolder>(descendants(root));
            bmsFiles = BmsFolders.SelectMany(f => f.Files);
            BmsFiles = bmsFiles;

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
                try
                {
                    folder.AutoRename();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            load(input);
        }

        private void rename(object input)
        {
            if (string.IsNullOrEmpty(RenameText))
                return;

            try
            {
                SelectedBmsFolder.Rename(RenameText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            load(input);
        }

        private void register(object input)
        {
            if (SelectedNode == null)
                return;

            using (var con = new BmsManagerContext())
            {
                registerRoot(SelectedNode);

                void registerRoot(RootDirectory dir)
                {
                    var root = con.RootDirectories
                        .Include(d => d.Folders)
                        .FirstOrDefault(d => d.Path == dir.Path);

                    if (root == null)
                    {
                        // ルート未登録の場合そのまま登録
                        con.RootDirectories.Add(dir);
                        return;
                    }

                    if (dir.Children?.Any() ?? false)
                    {
                        // 子が存在する場合それぞれ登録
                        foreach (var child in dir.Children)
                        {
                            registerRoot(child);
                        }
                    }

                    if (dir.Folders == null || !dir.Folders.Any())
                        return;


                    if (!root.Folders.Any())
                    {
                        // フォルダ未登録の場合そのまま登録
                        root.Folders = dir.Folders.ToArray();
                        return;
                    }

                    var registered = new List<BmsFolder>();
                    foreach (var folder in root.Folders.ToArray())
                    {
                        var fsFolder = dir.Folders.FirstOrDefault(f => f.Path == folder.Path);
                        if (fsFolder == default)
                        {
                            // 実体が存在しないフォルダを削除
                            root.Folders.Remove(folder);
                            continue;
                        }

                        con.Entry(folder).Collection(f => f.Files).Load();

                        foreach (var file in folder.Files.ToArray())
                        {
                            var fsFile = fsFolder.Files.FirstOrDefault(f => f.Path == file.Path);
                            if (fsFile == default)
                            {
                                // 実体が存在しないファイルを削除
                                folder.Files.Remove(file);
                                continue;
                            }
                        }

                        // ファイル情報更新・登録
                        foreach (var file in fsFolder.Files)
                        {
                            var dbFile = folder.Files.FirstOrDefault(f => f.Path == file.Path);
                            if (dbFile == default)
                            {
                                folder.Files.Add(file);
                            }
                            else
                            {
                                dbFile.Title = file.Title;
                                dbFile.Artist = file.Artist;
                                dbFile.MD5 = file.MD5;
                            }
                        }

                        // 登録済としてマーク
                        registered.Add(fsFolder);
                    }

                    // 未登録のフォルダを登録
                    foreach (var folder in dir.Folders.Where(f => !registered.Contains(f)))
                    {
                        root.Folders.Add(folder);
                    }
                }

                con.SaveChanges();
            }

            MessageBox.Show("登録完了しました");
        }
    }
}
