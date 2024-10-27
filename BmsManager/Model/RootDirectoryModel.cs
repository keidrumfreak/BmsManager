using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        string path;
        public string Path
        {
            get => path;
            set => SetProperty(ref path, value);
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

        public RootDirectoryModel(string message)
        {
            Path = message;
        }

        public RootDirectoryModel(RootDirectory entity)
        {
            this.entity = entity;
            Path = entity.Path;
        }

        public async Task LoadChildAsync()
        {
            using var con = new BmsManagerContext();
            if (entity.Children.Any())
            {
                var childrenEntity = await con.RootDirectories.Where(r => r.ParentRootID == entity.ID)
                    .Include(r => r.Children)
                    .Include(r => r.Folders)
                    .ThenInclude(r => r.Files)
                    .ToArrayAsync().ConfigureAwait(false);
                Children = new ObservableCollection<RootDirectoryModel>(childrenEntity.Select(e => new RootDirectoryModel(e)).ToArray());

                foreach (var child in children)
                {
                    await child.LoadChildAsync().ConfigureAwait(false);
                }
            }
            else if (entity.Folders.Any())
            {
                Folders = new ObservableCollection<BmsFolder>(entity.Folders);
            }
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
