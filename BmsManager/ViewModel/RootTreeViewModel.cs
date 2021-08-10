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

namespace BmsManager
{
    class RootTreeViewModel : ViewModelBase
    {
        public string TargetDirectory { get; set; }

        public ObservableCollection<RootDirectoryViewModel> RootTree { get; set; }

        RootDirectoryViewModel selectedRoot;
        public RootDirectoryViewModel SelectedRoot
        {
            get { return selectedRoot; }
            set { SetProperty(ref selectedRoot, value); }
        }

        public ICommand AddRoot { get; set; }

        public RootTreeViewModel()
        {
            AddRoot = CreateCommand(addRoot);

            RootTree = new ObservableCollection<RootDirectoryViewModel>(RootDirectory.LoadTopRoot().Select(r => new RootDirectoryViewModel(r, this, null)));
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

                var root = new RootDirectory { Path = TargetDirectory };
                var parent = con.RootDirectories.FirstOrDefault(r => r.Path == Path.GetDirectoryName(TargetDirectory));
                if (parent != default)
                    root.ParentRootID = parent.ID;
                con.RootDirectories.Add(root);
                con.SaveChanges();

                RootTree.Add(new RootDirectoryViewModel(root, this, null));
            }
        }
    }
}
