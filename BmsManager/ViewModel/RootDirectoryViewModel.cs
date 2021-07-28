using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Wpf;

namespace BmsManager
{
    class RootDirectoryViewModel : ViewModelBase
    {
        IEnumerable<RootDirectoryViewModel> children;
        public IEnumerable<RootDirectoryViewModel> Children
        {
            get { return children; }
            set { SetProperty(ref children, value); }
        }

        public string FolderPath => root.Path;

        public IEnumerable<BmsFolder> Folders => root.Descendants().Where(r => r.Folders?.Any() ?? false).SelectMany(r => r.Folders).ToArray();

        public ICommand LoadFromFileSystem { get; set; }

        public ICommand LoadFromDB { get; set; }

        RootDirectory root;

        public RootDirectoryViewModel(RootDirectory root)
        {
            this.root = root;

            LoadFromFileSystem = CreateCommand(loadFromFileSystem);
            LoadFromDB = CreateCommand(loadFromDB);

            Children = root.Children.Select(m => new RootDirectoryViewModel(m)).ToArray();
        }

        private void loadFromFileSystem(object input)
        {
            root.LoadFromFileSystem();
            Children = root.Children.Select(m => new RootDirectoryViewModel(m)).ToArray();
        }

        private void loadFromDB(object input)
        {
            root.LoadFromDB();
            Children = root.Children.Select(m => new RootDirectoryViewModel(m)).ToArray();
        }

        public void Register()
        {
            root.Register();
        }
    }
}
