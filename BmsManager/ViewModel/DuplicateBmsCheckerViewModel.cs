using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            CheckByMeta = CreateCommand(input => checkByMeta());
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

                IEnumerable<BmsFolderViewModel> inner(IEnumerable<BmsFolder> folders)
                {
                    foreach (var folder in folders)
                    {
                        var vm = new BmsFolderViewModel(folder, FileList);
                        vm.Duplicates = folders.Where(f => f.ID != folder.ID && f.Files.Any(f1 => folder.Files.Any(f2 => f1.MD5 == f2.MD5))).ToArray();
                        yield return vm;
                    }
                }

                FileList.Folders = new ObservableCollection<BmsFolderViewModel>(inner(fol).ToArray());
            }
        }

        private void checkByMeta()
        {
            using (var con = new BmsManagerContext())
            {
                var dupMeta = con.BmsFolders
                    .GroupBy(f => new { f.Title, f.Artist })
                    .Select(g => new { Meta = g.Key, Count = g.Count() })
                    .Where(g => g.Count > 1);
                var folID = con.BmsFolders.Join(dupMeta, f => new { f.Title, f.Artist }, d => d.Meta, (f, d) => f.ID).Distinct();
                var fol = con.BmsFolders.Where(f => folID.Contains(f.ID))
                    .Include(f => f.Files).Include(f => f.Root)
                    .AsNoTracking().ToArray();

                IEnumerable<BmsFolderViewModel> inner(IEnumerable<BmsFolder> folders)
                {
                    foreach (var folder in folders)
                    {
                        var vm = new BmsFolderViewModel(folder, FileList);
                        vm.Duplicates = folders.Where(f => f.ID != folder.ID && f.Title == folder.Title && f.Artist == folder.Artist).ToArray();
                        yield return vm;
                    }
                }

                FileList.Folders = new ObservableCollection<BmsFolderViewModel>(inner(fol).ToArray());
            }
        }
    }
}
