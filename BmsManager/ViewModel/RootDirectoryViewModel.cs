using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;

namespace BmsManager
{
    class RootDirectoryViewModel : ViewModelBase, IBmsFolderParentViewModel
    {
        ObservableCollection<RootDirectoryViewModel> children;
        public ObservableCollection<RootDirectoryViewModel> Children
        {
            get { return children; }
            set { SetProperty(ref children, value); }
        }

        public string FolderPath => root.Path;

        ObservableCollection<BmsFolderViewModel> folders;
        public ObservableCollection<BmsFolderViewModel> Folders
        {
            get { return folders; }
            set { SetProperty(ref folders, value); }
        }

        public string LoadingPath => root.LoadingPath;

        public ICommand LoadFromFileSystem { get; set; }

        public ICommand LoadFromDB { get; set; }

        public ICommand Remove { get; set; }

        RootDirectory root;
        RootTreeViewModel vm;
        RootDirectoryViewModel parent;

        public RootDirectoryViewModel(RootDirectory root, RootTreeViewModel vm, RootDirectoryViewModel parent)
        {
            this.root = root;
            this.vm = vm;
            this.parent = parent;

            LoadFromFileSystem = CreateCommand(() => Task.Run(() => loadFromFileSystem()));
            LoadFromDB = CreateCommand(loadFromDB);
            Remove = CreateCommand(remove);

            if (root.Children == null)
                Children = null;
            else
                Children = new ObservableCollection<RootDirectoryViewModel>(root.Children.Select(m => new RootDirectoryViewModel(m, vm, this)).ToArray());
            Folders = new ObservableCollection<BmsFolderViewModel>(root.Descendants().Where(r => r.Folders?.Any() ?? false).SelectMany(r => r.Folders).Select(f => new BmsFolderViewModel(f, this)).ToArray());

            AddModelObserver(root);
        }

        private void loadFromFileSystem()
        {
            root.LoadFromFileSystem();
            if (root.Children == null)
                Children = null;
            else
                Children = new ObservableCollection<RootDirectoryViewModel>(root.Children.Select(m => new RootDirectoryViewModel(m, vm, this)).ToArray());
            Folders = new ObservableCollection<BmsFolderViewModel>(root.Descendants().Where(r => r.Folders?.Any() ?? false).SelectMany(r => r.Folders).Select(f => new BmsFolderViewModel(f, this)).ToArray());
        }

        private void loadFromDB()
        {
            root.LoadFromDB();
            if (root.Children == null)
                Children = null;
            else
                Children = new ObservableCollection<RootDirectoryViewModel>(root.Children.Select(m => new RootDirectoryViewModel(m, vm, this)).ToArray());
            Folders = new ObservableCollection<BmsFolderViewModel>(root.Descendants().Where(r => r.Folders?.Any() ?? false).SelectMany(r => r.Folders).Select(f => new BmsFolderViewModel(f, this)).ToArray());
        }

        public void Register()
        {
            root.Register();
        }

        private void remove()
        {
            using (var con = new BmsManagerContext())
            {
                inner(FolderPath);
                void inner(string path)
                {
                    foreach (var root in con.RootDirectories.Include(r => r.Children).Include(r => r.Folders).ThenInclude(f => f.Files).Where(r => r.Path == path).ToArray())
                    {
                        foreach (var child in children)
                        {
                            inner(child.FolderPath);
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
            if (parent != null)
                parent.Children.Remove(this);
            else
                vm.RootTree.Remove(this);
        }
    }
}
