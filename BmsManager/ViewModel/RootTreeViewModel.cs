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
using BmsManager.Model;
using CommonLib.Wpf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace BmsManager.ViewModel
{
    class RootTreeViewModel : ObservableObject
    {
        readonly RootTreeModel model;

        string targetDirectory;
        public string TargetDirectory
        {
            get => targetDirectory;
            set => SetProperty(ref targetDirectory, value);
        }

        public ObservableCollection<RootDirectoryModel> RootTree
        {
            get => model.RootTree;
            set => model.RootTree = value;
        }

        RootDirectoryModel selectedRoot;
        public RootDirectoryModel SelectedRoot
        {
            get => selectedRoot;
            set => SetProperty(ref selectedRoot, value);
        }

        public string LoadingPath
        {
            get => model.LoadingPath;
            set => model.LoadingPath = value;
        }

        public IAsyncRelayCommand AddRoot { get; set; }

        public IAsyncRelayCommand LoadFromFileSystem { get; set; }

        public ICommand LoadFromDB { get; set; }

        public ICommand Remove { get; set; }

        public ICommand SelectFolder { get; }

        public IAsyncRelayCommand LoadRootTree { get; }

        public RootTreeViewModel()
        {
            model = new RootTreeModel();
            model.PropertyChanged += (sender, e) => OnPropertyChanged(e.PropertyName);
            AddRoot = new AsyncRelayCommand(async () => await model.AddRootAsync(TargetDirectory));
            LoadRootTree = new AsyncRelayCommand(model.LoadRootTreeAsync);
            SelectFolder = new RelayCommand(selectFolder);
            LoadFromFileSystem = new AsyncRelayCommand<RootDirectoryModel>(model.LoadFromFileSystemAsync);
            LoadFromDB = new RelayCommand<RootDirectoryModel>(loadFromDB);
            Remove = new RelayCommand<RootDirectoryModel>(remove);
        }

        private void selectFolder()
        {
            var dialog = new OpenFolderDialog() { Multiselect = false };
            if (dialog.ShowDialog() ?? false)
                TargetDirectory = dialog.FolderName;
        }

        private void loadFromDB(RootDirectoryModel root)
        {
            root.Root.LoadFromDB();
        }

        private void remove(RootDirectoryModel root)
        {
            using (var con = new BmsManagerContext())
            {
                inner(root.Root.Path);
                void inner(string path)
                {
                    foreach (var root in con.RootDirectories.Include(r => r.Children).Include(r => r.Folders).ThenInclude(f => f.Files).Where(r => r.Path == path).ToArray())
                    {
                        foreach (var child in root.Children)
                            inner(child.Path);

                        foreach (var folder in root.Folders)
                        {
                            foreach (var file in folder.Files)
                                con.Files.Remove(file);
                            con.BmsFolders.Remove(folder);
                        }
                        con.RootDirectories.Remove(root);
                    }
                }
                con.SaveChanges();
            }
            if (root.Root.Parent == null)
                RootTree.Remove(root);
            else
                RootTree.SelectMany(r => r.Descendants()).FirstOrDefault(r => r.Root.Path == root.Root.Parent.Path)?.Root.LoadFromDB();
        }
    }
}
