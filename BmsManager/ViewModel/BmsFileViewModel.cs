using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Wpf;

namespace BmsManager
{
    class BmsFileViewModel : ViewModelBase
    {
        public string Title => file.Title;

        public string Artist => file.Artist;

        public string Path => file.Path;

        public string MD5 => file.MD5;

        public ICommand Remove { get; set; }

        BmsFile file;

        BmsFolderViewModel parent;

        public BmsFileViewModel(BmsFile file, BmsFolderViewModel parent)
        {
            this.parent = parent;
            this.file = file;

            Remove = CreateCommand(input => remove());
        }

        private void remove()
        {
            try
            {
                SystemProvider.FileSystem.File.Delete(file.Path);
                using (var con = new BmsManagerContext())
                {
                    var entity = con.Files.FirstOrDefault(f => f.Path == file.Path);
                    if (entity != default)
                    {
                        con.Files.Remove(entity);
                        con.SaveChanges();
                    }
                }
                parent.Files.Remove(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
