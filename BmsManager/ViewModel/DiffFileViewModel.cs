using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Entity;
using BmsManager.ViewModel;
using BmsParser;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;
using SysPath = System.IO.Path;

namespace BmsManager
{
    class DiffFileViewModel : ViewModelBase
    {
        public string Title => bms.Title;
        public string Artist => bms.Artist;
        public string MD5 { get; set; }
        public string Path { get; set; }

        IEnumerable<BmsFolder> folders;
        public IEnumerable<BmsFolder> Folders
        {
            get { return folders; }
            set { SetProperty(ref folders, value); }
        }

        public ICommand EstimateDestination { get; set; }

        BmsModel bms;
        DiffRegisterViewModel vm;

        public DiffFileViewModel(BmsModel bms, DiffRegisterViewModel vm)
        {
            this.vm = vm;
            this.bms = bms;
            Path = bms.Path;

            MD5 = bms.MD5;

            EstimateDestination = CreateCommand(GetEstimatedDestination);
        }

        public void GetEstimatedDestination()
        {
            using (var con = new BmsManagerContext())
            {
                var query = con.BmsFolders
                    .Include(f => f.Files)
                    .AsNoTracking();
                if (!string.IsNullOrEmpty(bms.Title) && bms.Title.Length > 2)
                    query = query.Where(f => f.Title.Length > 1);
                if (!string.IsNullOrEmpty(bms.Artist) && bms.Artist.Length > 2)
                    query = query.Where(f => f.Artist.Length > 1);
                query = query.Where(f => bms.Title.Contains(f.Title) && bms.Artist.Contains(f.Artist));
                Folders = query.ToArray();
            }
        }

        public void Remove()
        {
            // ディレクトリが空になるなら消す (txtは無視する)
            var dir = SysPath.GetDirectoryName(Path);
            if (!SystemProvider.FileSystem.Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                .Where(f => !f.EndsWith("txt")).Any())
                SystemProvider.FileSystem.Directory.Delete(dir, true);

            vm.DiffFiles.Remove(this);
        }
    }
}
