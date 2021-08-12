using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;

namespace BmsManager
{
    class DiffFileViewModel : ViewModelBase
    {
        public string Title => text.Title;
        public string Artist => text.Artist;
        public string MD5 { get; set; }
        public string Path { get; set; }

        IEnumerable<BmsFolder> folders;
        public IEnumerable<BmsFolder> Folders
        {
            get { return folders; }
            set { SetProperty(ref folders, value); }
        }

        public ICommand EstimateDestination { get; set; }

        BmsText text;
        DiffRegisterViewModel vm;

        public DiffFileViewModel(string path, DiffRegisterViewModel vm)
        {
            this.vm = vm;
            text = new BmsText(path);
            Path = path;

            MD5 = Utility.GetMd5Hash(path);

            EstimateDestination = CreateCommand(input => GetEstimatedDestination());
        }

        public void GetEstimatedDestination()
        {
            using (var con = new BmsManagerContext())
            {
                var title = Utility.ToFileNameString(text.Title);
                var artist = Utility.ToFileNameString(text.Artist);
                var query = con.BmsFolders
                    .Include(f => f.Files)
                    .AsNoTracking();
                if (!string.IsNullOrEmpty(title) && title.Length > 2)
                    query = query.Where(f => f.Title.Length > 1);
                if (!string.IsNullOrEmpty(artist) && artist.Length > 2)
                    query = query.Where(f => f.Artist.Length > 1);
                query = query.Where(f => title.Contains(f.Title) && artist.Contains(f.Artist));
                Folders = query.ToArray();
            }
        }

        public void Remove()
        {
            vm.DiffFiles.Remove(this);
        }
    }
}
