using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Entity;
using BmsManager.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.ViewModel
{
    internal class RootDirectoryViewModel : ObservableObject
    {
        public string Text
        {
            get => entity == null ? string.Empty
                : entity.ParentRootID == null ? entity.Path
                : Path.GetFileName(entity.Path);
        }

        bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set
            {
                // 下位フォルダのロード状況を優先する
                if (Children.Any() && Children.Any(c => c.Descendants().Any(c => c.IsLoading)) != value)
                    return;
                SetProperty(ref isLoading, value);
                if (parent != null && parent.IsLoading != value)
                    parent.IsLoading = value;
            }
        }

        bool isError = false;
        public bool IsError
        {
            get => isError;
            set
            {
                // 下位フォルダのエラー状況を優先する
                if (Children.Any() && Children.Any(c => c.Descendants().Any(c => c.IsError)) != value)
                    return;
                SetProperty(ref isError, value);
                if (parent != null && parent.IsError != value)
                    parent.IsError = value;
            }
        }

        public string FullPath => entity.Path;

        ObservableCollection<BmsFolder> folders = [];
        public ObservableCollection<BmsFolder> Folders
        {
            get => folders;
            set => SetProperty(ref folders, value);
        }

        ObservableCollection<RootDirectoryViewModel> children = [];
        public ObservableCollection<RootDirectoryViewModel> Children
        {
            get => children;
            set => SetProperty(ref children, value);
        }

        public BmsFolder[] DescendantFolders => Descendants().Where(r => r.Folders?.Any() ?? false).SelectMany(r => r.Folders).ToArray();

        public int ID => entity.ID;

        public ICommand LoadFromFileSystem { get; }

        public ICommand LoadFromDB { get; }

        public ICommand Remove { get; }

        readonly RootDirectory entity;

        readonly RootDirectoryViewModel? parent;

        readonly RootTreeViewModel tree;

        static readonly object lockObj = new();

        public RootDirectoryViewModel(RootTreeViewModel tree) : this(tree, new RootDirectory(), true) { }

        public RootDirectoryViewModel(RootTreeViewModel tree, RootDirectory entity, bool isLoading = false, RootDirectoryViewModel? parent = null)
        {
            this.tree = tree;
            this.entity = entity;
            this.parent = parent;
            Folders = new ObservableCollection<BmsFolder>(entity.Folders ?? []);
            LoadFromFileSystem = new AsyncRelayCommand(loadFromFileSystemAsync);
            Remove = new AsyncRelayCommand(removeAsync);
            LoadFromDB = new AsyncRelayCommand(loadFromDBAsymc);
            IsLoading = isLoading;
        }

        public async Task LoadChildAsync(Task loadRootTask)
        {
            try
            {
                if (entity.Children.Count != 0)
                {
                    using var con = new BmsManagerContext();
                    {
                        var childrenEntity = await con.RootDirectories.Where(r => r.ParentRootID == entity.ID)
                            .Include(r => r.Children)
                            .Include(r => r.Folders)
                            .AsNoTracking().ToArrayAsync().ConfigureAwait(false);
                        Folders = [];
                        Children = new ObservableCollection<RootDirectoryViewModel>(childrenEntity.Select(e => new RootDirectoryViewModel(tree, e, true, this)).ToArray());
                    }

                    foreach (var child in children)
                        loadRootTask = loadRootTask.ContinueWith((t) => child.LoadChildAsync(loadRootTask));

                }
                await loadRootTask.ConfigureAwait(false);
                if (entity.Folders.Count != 0)
                    lock (lockObj)
                    {
                        using var con = new BmsManagerContext();
                        var folderEntity = con.BmsFolders.Where(r => r.RootID == entity.ID)
                            .Include(f => f.Files)
                            .AsNoTracking().ToArray();
                        Folders = new ObservableCollection<BmsFolder>(folderEntity);
                    }

                IsLoading = false;
            }
            catch (Exception e)
            {
                SystemProvider.Logger.TraceExceptionLog(e);
                IsError = true;
                IsLoading = false;
            }
        }

        private async Task removeAsync()
        {
            using (var con = new BmsManagerContext())
            {
                inner(FullPath);
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
                await con.SaveChangesAsync().ConfigureAwait(false);
            }
            if (parent == null)
                Application.Current.Dispatcher.Invoke(() => tree.RootTree.Remove(this));
            else
                Application.Current.Dispatcher.Invoke(() => parent.Children.Remove(this));
        }

        private async Task loadFromDBAsymc()
        {
            IsLoading = true;
            using var con = new BmsManagerContext();
            // 親子構造の取得が難しいのでとりあえず全部引っ張る
            // TODO: 検索処理の改善
            var folders = await con.BmsFolders
                .Include(f => f.Files)
                .AsNoTracking().ToArrayAsync().ConfigureAwait(false);

            var allRoots = await con.RootDirectories
                .AsNoTracking().ToArrayAsync().ConfigureAwait(false);

            foreach (var folder in folders.GroupBy(f => f.RootID))
            {
                var parent = allRoots.First(r => r.ID == folder.Key);
                parent.Folders = [.. folder];
                foreach (var fol in folder)
                {
                    fol.Root = parent;
                }
            }

            // 親子関係を放り込む
            foreach (var parent in allRoots)
            {
                var children = allRoots.Where(r => r.ParentRootID == parent.ID);
                parent.Children = children.ToList();
                foreach (var child in children)
                {
                    child.Parent = parent;
                }
            }

            var root = allRoots.FirstOrDefault(r => r.Path == entity.Path);

            if (root == default)
                throw new BmsManagerException("DB未登録のルートフォルダです。");

            Children = new ObservableCollection<RootDirectoryViewModel>(root.Children.Select(r => new RootDirectoryViewModel(tree, r)));
            Folders = new ObservableCollection<BmsFolder>(root.Folders ?? []);
            IsLoading = false;
        }

        private async Task loadFromFileSystemAsync()
        {
            var loader = new FolderLoader();
            loader.PropertyChanged += Loader_PropertyChanged;
            await loadFromFileSystemAsync(loader).ConfigureAwait(false);
            loader.PropertyChanged -= Loader_PropertyChanged;
            tree.LoadingPath = "読込完了";
        }

        private void Loader_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FolderLoader.LoadingPath))
                tree.LoadingPath = ((FolderLoader?)sender)?.LoadingPath ?? string.Empty;
        }

        private async Task loadFromFileSystemAsync(FolderLoader loader)
        {
            IsLoading = true;
            await loader.LoadAsync(entity, inner, folder => Folders.Add(folder)).ConfigureAwait(false);

            async Task inner(RootDirectory child)
            {
                var model = Children.FirstOrDefault(c => c.FullPath == child.Path);
                if (model == default)
                {
                    model = new RootDirectoryViewModel(tree, child, true, this);
                    Application.Current.Dispatcher.Invoke(() => Children.Add(model));
                }
                else
                    model.Folders = [];
                await model.loadFromFileSystemAsync(loader).ConfigureAwait(false);
            }
            IsLoading = false;
        }

        public IEnumerable<RootDirectoryViewModel> Descendants()
        {
            yield return this;
            if (Children == null || !Children.Any())
                yield break;
            foreach (var child in Children.SelectMany(c => c.Descendants()))
                yield return child;
        }
    }
}
