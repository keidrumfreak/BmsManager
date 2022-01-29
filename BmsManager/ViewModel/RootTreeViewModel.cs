using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    class RootTreeViewModel : ViewModelBase
    {
        public string TargetDirectory { get; set; }

        public ObservableCollection<RootDirectory> RootTree { get; set; }

        RootDirectory selectedRoot;
        public RootDirectory SelectedRoot
        {
            get { return selectedRoot; }
            set { SetProperty(ref selectedRoot, value); }
        }

        public ICommand AddRoot { get; set; }

        public ICommand LoadFromFileSystem { get; set; }

        public ICommand LoadFromDB { get; set; }

        public ICommand Remove { get; set; }

        public RootTreeViewModel()
        {
            AddRoot = CreateCommand(addRoot);

            RootTree = new ObservableCollection<RootDirectory>(RootDirectory.LoadTopRoot());

            LoadFromFileSystem = CreateCommand<RootDirectory>(loadFromFileSystem);
            LoadFromDB = CreateCommand<RootDirectory>(loadFromDB);
            Remove = CreateCommand<RootDirectory>(remove);
        }

        private void loadFromFileSystem(RootDirectory root)
        {
            root.LoadFromFileSystem();
        }

        private void loadFromDB(RootDirectory root)
        {
            root.LoadFromDB();
        }

        private void remove(RootDirectory root)
        {
            using (var con = new BmsManagerContext())
            {
                inner(root.Path);
                void inner(string path)
                {
                    foreach (var root in con.RootDirectories.Include(r => r.Children).Include(r => r.Folders).ThenInclude(f => f.Files).Where(r => r.Path == path).ToArray())
                    {
                        foreach (var child in root.Children)
                        {
                            inner(child.Path);
                        }

                        foreach (var folder in root.Folders)
                        {
                            foreach (var file in folder.Files)
                            {
                                con.Files.Remove(file);
                            }
                            con.BmsFolders.Remove(folder);
                        }
                        con.RootDirectories.Remove(root);
                    }
                }
                con.SaveChanges();
            }
            if (root.Parent == null)
            {
                RootTree.Remove(root);
            }
            else
            {
                RootTree.SelectMany(r => r.Descendants()).FirstOrDefault(r => r.Path == root.Parent.Path)?.LoadFromDB();
            }
        }

        private void addRoot()
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

                var root = new RootDirectory
                {
                    Path = TargetDirectory,
                    FolderUpdateDate = SystemProvider.FileSystem.DirectoryInfo.FromDirectoryName(TargetDirectory).LastWriteTimeUtc
                };
                var parent = con.RootDirectories.FirstOrDefault(r => r.Path == Path.GetDirectoryName(TargetDirectory));
                if (parent != default)
                    root.ParentRootID = parent.ID;
                con.RootDirectories.Add(root);
                con.SaveChanges();

                RootTree.Add(root);
            }
        }
    }
}
