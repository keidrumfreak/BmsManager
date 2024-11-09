using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Entity;
using CommunityToolkit.Mvvm.Input;

namespace BmsManager.ViewModel
{
    internal class BmsFileViewModel
    {
        public string Title => entity.Title;

        public string Artist => entity.Artist;

        public string MD5 => entity.MD5;

        public string FullPath => entity.Path;

        public ICommand Delete { get; }

        BmsFile entity;

        public BmsFileViewModel(BmsFile entity)
        {
            this.entity = entity;
            Delete = new AsyncRelayCommand<ObservableCollection<BmsFileViewModel>>(delete);
        }

        private async Task delete(ObservableCollection<BmsFileViewModel>? root)
        {
            //SystemProvider.FileSystem.File.Delete(entity.Path);
            //using var con = new BmsManagerContext();
            //var file = con.Files.SingleOrDefault(f => f.Path == entity.Path);
            //if (file != default)
            //{
            //    con.Files.Remove(file);
            //    await con.SaveChangesAsync();
            //}

            if (root != null)
            {
                Application.Current.Dispatcher.Invoke(() => root.Remove(this));
            }
        }
    }
}
