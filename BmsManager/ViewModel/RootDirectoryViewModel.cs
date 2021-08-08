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
    class RootDirectoryViewModel : ViewModelBase
    {
        ObservableCollection<RootDirectoryViewModel> children;
        public ObservableCollection<RootDirectoryViewModel> Children
        {
            get { return children; }
            set { SetProperty(ref children, value); }
        }

        public string FolderPath => root.Path;

        public IEnumerable<BmsFolder> Folders => root.Descendants().Where(r => r.Folders?.Any() ?? false).SelectMany(r => r.Folders).ToArray();

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

            LoadFromFileSystem = CreateCommand(loadFromFileSystem);
            LoadFromDB = CreateCommand(loadFromDB);
            Remove = CreateCommand(input => remove());

            Children = new ObservableCollection<RootDirectoryViewModel>(root.Children.Select(m => new RootDirectoryViewModel(m, vm, this)).ToArray());
        }

        private void loadFromFileSystem(object input)
        {
            root.LoadFromFileSystem();
            Children = new ObservableCollection<RootDirectoryViewModel>(root.Children.Select(m => new RootDirectoryViewModel(m, vm, this)).ToArray());
        }

        private void loadFromDB(object input)
        {
            root.LoadFromDB();
            Children = new ObservableCollection<RootDirectoryViewModel>(root.Children.Select(m => new RootDirectoryViewModel(m, vm, this)).ToArray());
        }

        public void Register()
        {
            root.Register();
        }

        private void remove()
        {
            using (var con = new BmsManagerContext())
            {
                foreach (var root in con.RootDirectories.Include(r => r.Folders).ThenInclude(f => f.Files).Where(r => r.Path == FolderPath).ToArray())
                {
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
                con.SaveChanges();
            }
            if (parent != null)
                parent.Children.Remove(this);
            else
                vm.RootTree.Remove(this);
        }
    }
}
