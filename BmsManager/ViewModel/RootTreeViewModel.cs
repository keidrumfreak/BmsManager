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

        public IAsyncRelayCommand LoadFromFileSystem { get; set; }

        public ICommand LoadFromDB { get; set; }

        public ICommand Remove { get; set; }

        public ICommand SelectFolder { get; }

        public IAsyncRelayCommand LoadRootTree { get; }

        public RootTreeViewModel()
        {
            AddRoot = new AsyncRelayCommand(addRootAsync);
            LoadRootTree = new AsyncRelayCommand(loadRootTreeAsync);
            SelectFolder = new RelayCommand(selectFolder);
            LoadFromFileSystem = new AsyncRelayCommand<RootDirectoryViewModel>(loadFromFileSystemAsync);
            LoadFromDB = new RelayCommand<RootDirectoryViewModel>(loadFromDB);
            Remove = new RelayCommand<RootDirectoryViewModel>(remove);
        }

        private async Task loadRootTreeAsync()
        {
            RootTree = [new RootDirectoryViewModel()];

            var con = new BmsManagerContext();

            var roots = await con.RootDirectories.Where(r => r.ParentRootID == null)
                .Include(r => r.Children)
                .Include(r => r.Folders)
                .ThenInclude(r => r.Files)
                .AsNoTracking().ToArrayAsync().ConfigureAwait(false);

            RootTree = new ObservableCollection<RootDirectoryViewModel>(roots.Select(r => new RootDirectoryViewModel(r, true)).ToArray());

            foreach (var root in RootTree)
            {
                await root.LoadChildAsync(Task.CompletedTask).ConfigureAwait(false);
            }
        }

        public async Task addRootAsync()
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

            var parent = rootTree.SelectMany(r => r.Descendants()).FirstOrDefault(r => r.Root.Path == System.IO.Path.GetDirectoryName(TargetDirectory));
            if (parent != default)
                root.ParentRootID = parent.ID;

            con.RootDirectories.Add(root);
            await con.SaveChangesAsync().ConfigureAwait(false);
            Application.Current.Dispatcher.Invoke(() => (parent?.Children ?? RootTree).Add(new RootDirectoryViewModel(root)));
        }

        private void selectFolder()
        {
            var dialog = new OpenFolderDialog() { Multiselect = false };
            if (dialog.ShowDialog() ?? false)
                TargetDirectory = dialog.FolderName;
        }

        private void loadFromDB(RootDirectoryViewModel root)
        {
            root.Root.LoadFromDB();
        }

        private void remove(RootDirectoryViewModel root)
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

        public async Task loadFromFileSystemAsync(RootDirectoryViewModel root)
        {
            await root.LoadFromFileSystemAsync(this).ConfigureAwait(false);
        }
    }
}
