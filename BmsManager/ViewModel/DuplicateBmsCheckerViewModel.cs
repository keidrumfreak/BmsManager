using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Linq;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;

namespace BmsManager
{
    class DuplicateBmsCheckerViewModel : ViewModelBase
    {
        public BmsFileListViewModel FileList { get; set; }

        public ICommand CheckByMD5 { get; set; }

        public ICommand CheckByMeta { get; set; }

        public DuplicateBmsCheckerViewModel()
        {
            FileList = new BmsFileListViewModel();

            CheckByMD5 = CreateCommand(input => checkByMD5());
        }

        private void checkByMD5()
        {
            using (var con = new BmsManagerContext())
            {
                var dupMD5 = con.Files
                    .GroupBy(f => f.MD5)
                    .Select(g => new { MD5 = g.Key, Count = g.Count() })
                    .Where(g => g.Count > 1);
                var folID = con.Files.Join(dupMD5, f => f.MD5, d => d.MD5, (f, d) => f.Folder.ID).Distinct();
                var fol = con.BmsFolders.Where(f => folID.Contains(f.ID))
                    .Include(f => f.Files).Include(f => f.Root)
                    .AsNoTracking().ToArray();

                FileList.BmsFolders = fol.Select(f => new BmsFolderViewModel(f)).ToArray();
            }
        }
    }
}
