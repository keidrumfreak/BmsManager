using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BmsManager.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.Model
{
    internal class RootDirectoryModel : ObservableObject
    {
        public string Text
        {
            get => $"{(entity == null ? string.Empty
                : entity.ParentRootID == null ? entity.Path
                : Path.GetFileName(entity.Path))}{(isLoading ? " (loading...)" : string.Empty)}";
        }

        bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value, nameof(Text));
        }

        ObservableCollection<BmsFolder> folders;
        public ObservableCollection<BmsFolder> Folders
        {
            get => folders;
            set => SetProperty(ref folders, value);
        }

        ObservableCollection<RootDirectoryModel> children;
        public ObservableCollection<RootDirectoryModel> Children
        {
            get => children;
            set => SetProperty(ref children, value);
        }

        public BmsFolder[] DescendantFolders => Descendants().Where(r => r.Folders?.Any() ?? false).SelectMany(r => r.Folders).ToArray();

        public int ID => entity.ID;

        public RootDirectory Root => entity;

        RootDirectory entity;

        public RootDirectoryModel() : this(new RootDirectory(), true) { }

        public RootDirectoryModel(RootDirectory entity, bool isLoading)
        {
            this.entity = entity;
            IsLoading = isLoading;
        }

        public async Task LoadChildAsync()
        {
            using var con = new BmsManagerContext();
            if (entity.Children.Any())
            {
                var childrenEntity = await con.RootDirectories.Where(r => r.ParentRootID == entity.ID)
                    .Include(r => r.Children)
                    .Include(r => r.Folders)
                    .ToArrayAsync().ConfigureAwait(false);

                Folders = new ObservableCollection<BmsFolder>();
                Children = new ObservableCollection<RootDirectoryModel>(childrenEntity.Select(e => new RootDirectoryModel(e, true)).ToArray());

                foreach (var child in children)
                {
                    await child.LoadChildAsync().ConfigureAwait(false);
                }

            }
            if (entity.Folders.Any())
            {
                var folderEntity = await con.BmsFolders.Where(r => r.RootID == entity.ID)
                    .Include(f => f.Files)
                    .ToArrayAsync().ConfigureAwait(false);
                Folders = new ObservableCollection<BmsFolder>(folderEntity);
            }

            IsLoading = false;
        }

        public IEnumerable<RootDirectoryModel> Descendants()
        {
            yield return this;
            if (Children == null || !Children.Any())
                yield break;
            foreach (var child in Children.SelectMany(c => c.Descendants()))
                yield return child;
        }
    }
}
