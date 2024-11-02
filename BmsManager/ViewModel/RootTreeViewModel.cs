using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Entity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace BmsManager.ViewModel
{
    class RootTreeViewModel : ObservableObject
    {
        string targetDirectory;
        public string TargetDirectory
        {
            get => targetDirectory;
            set => SetProperty(ref targetDirectory, value);
        }

        ObservableCollection<RootDirectoryViewModel> rootTree;
        public ObservableCollection<RootDirectoryViewModel> RootTree
        {
            get => rootTree;
            set => SetProperty(ref rootTree, value);
        }

        RootDirectoryViewModel selectedRoot;
        public RootDirectoryViewModel SelectedRoot
        {
            get => selectedRoot;
            set => SetProperty(ref selectedRoot, value);
        }

        string loadingPath;
        public string LoadingPath
        {
            get => loadingPath;
            set => SetProperty(ref loadingPath, value);
        }

        public IAsyncRelayCommand AddRoot { get; set; }

        public ICommand SelectFolder { get; }

        public IAsyncRelayCommand LoadRootTree { get; }

        public RootTreeViewModel()
        {
            AddRoot = new AsyncRelayCommand(addRootAsync);
            LoadRootTree = new AsyncRelayCommand(loadRootTreeAsync);
            SelectFolder = new RelayCommand(selectFolder);
        }

        private async Task loadRootTreeAsync()
        {
            RootTree = [new RootDirectoryViewModel(this)];

            var con = new BmsManagerContext();

            var roots = await con.RootDirectories.Where(r => r.ParentRootID == null)
                .Include(r => r.Children)
                .Include(r => r.Folders)
                .ThenInclude(r => r.Files)
                .AsNoTracking().ToArrayAsync().ConfigureAwait(false);

            RootTree = new ObservableCollection<RootDirectoryViewModel>(roots.Select(r => new RootDirectoryViewModel(this, r, true)).ToArray());

            foreach (var root in RootTree)
            {
                await root.LoadChildAsync(Task.CompletedTask).ConfigureAwait(false);
            }
        }

        private async Task addRootAsync()
        {
            if (string.IsNullOrWhiteSpace(TargetDirectory))
                return;

            using var con = new BmsManagerContext();

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

            var parent =  rootTree.SelectMany(r => r.Descendants()).FirstOrDefault(r => r.FullPath == System.IO.Path.GetDirectoryName(TargetDirectory));
            if (parent != default)
                root.ParentRootID = parent.ID;

            con.RootDirectories.Add(root);
            await con.SaveChangesAsync().ConfigureAwait(false);
            Application.Current.Dispatcher.Invoke(() => (parent?.Children ?? RootTree).Add(new RootDirectoryViewModel(this, root)));
        }

        private void selectFolder()
        {
            var dialog = new OpenFolderDialog() { Multiselect = false };
            if (dialog.ShowDialog() ?? false)
                TargetDirectory = dialog.FolderName;
        }
    }
}
