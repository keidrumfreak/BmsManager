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
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace BmsManager
{
    class RootTreeViewModel : ObservableObject
    {
        string targetDirectory;
        public string TargetDirectory
        {
            get => targetDirectory;
            set => SetProperty(ref targetDirectory, value);
        }

        ObservableCollection<RootDirectory> rootTree;
        public ObservableCollection<RootDirectory> RootTree
        {
            get => rootTree;
            set => SetProperty(ref rootTree, value);
        }

        RootDirectory selectedRoot;
        public RootDirectory SelectedRoot
        {
            get => selectedRoot;
            set => SetProperty(ref selectedRoot, value);
        }

        public ICommand AddRoot { get; set; }

        public ICommand LoadFromFileSystem { get; set; }

        public ICommand LoadFromDB { get; set; }

        public ICommand Remove { get; set; }

        public ICommand SelectFolder { get; }

        public ICommand LoadRootTree { get; }

        public RootTreeViewModel()
        {
            AddRoot = new AsyncRelayCommand(addRoot);
            LoadRootTree = new AsyncRelayCommand(loadRootTree);
            SelectFolder = new RelayCommand(selectFolder);
            LoadFromFileSystem = new AsyncRelayCommand<RootDirectory>(loadFromFileSystem);
            LoadFromDB = new RelayCommand<RootDirectory>(loadFromDB);
            Remove = new RelayCommand<RootDirectory>(remove);
        }

        private async Task loadRootTree()
        {
            RootTree = new ObservableCollection<RootDirectory> { new RootDirectory { Path = "loading..." } };
            RootTree = new ObservableCollection<RootDirectory>(await RootDirectory.LoadTopRootAsync());
        }

        private void selectFolder()
        {
            var dialog = new OpenFolderDialog() { Multiselect = false };
            if (dialog.ShowDialog() ?? false)
            {
                TargetDirectory = dialog.FolderName;
            }
        }

        private async Task loadFromFileSystem(RootDirectory root)
        {
            await Task.Run(() => root.LoadFromFileSystem());
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

        private async Task addRoot()
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
                    FolderUpdateDate = SystemProvider.FileSystem.DirectoryInfo.New(TargetDirectory).LastWriteTimeUtc
                };
                var parent = con.RootDirectories.FirstOrDefault(r => r.Path == Path.GetDirectoryName(TargetDirectory));
                if (parent != default)
                    root.ParentRootID = parent.ID;
                con.RootDirectories.Add(root);
                con.SaveChanges();

                await loadRootTree();
            }
        }
    }
}
