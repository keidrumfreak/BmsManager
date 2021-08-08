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

            EstimateDestination = CreateCommand(input => GetEstimatedDestination());
        }

        public void GetEstimatedDestination()
        {
            using (var con = new BmsManagerContext())
            {
                Folders = con.BmsFolders
                    .Include(f => f.Files)
                    .AsNoTracking()
                    .Where(f => text.Title.Contains(f.Title) && text.Artist.Contains(f.Artist))
                    .ToArray();
            }
        }

        public void Remove()
        {
            vm.DiffFiles.Remove(this);
        }
    }
}
