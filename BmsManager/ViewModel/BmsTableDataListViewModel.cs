using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Entity;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace BmsManager.ViewModel
{
    class BmsTableDataListViewModel : ViewModelBase
    {
        public bool Narrowed { get; set; }

        public ICommand ChangeNarrowing { get; set; }

        BmsTableViewModel table;
        public BmsTableViewModel Table
        {
            get { return table; }
            set { SetProperty(ref table, value); loadTableData(); }
        }

        IEnumerable<BmsTableDataViewModel> tableDatas;
        public IEnumerable<BmsTableDataViewModel> TableDatas
        {
            get { return tableDatas; }
            set { SetProperty(ref tableDatas, value); }
        }

        public ICommand DownloadAll { get; set; }

        public BmsTableDataListViewModel()
        {
            ChangeNarrowing = CreateCommand(loadTableData);
            DownloadAll = CreateCommand(downloadAllAsync);
        }

        private void loadTableData()
        {
            TableDatas = Table?.TableDatas.Select(d => new BmsTableDataViewModel(d)).ToArray();

            if (Table == null)
                return;

            if (Narrowed)
                using (var con = new BmsManagerContext())
                {
                    var query = con.Files.Join(con.TableDatas,
                        f => f.MD5, d => d.MD5, (f, d) => d);
                    if (Table.IsTable)
                        query = query.Where(d => d.Difficulty.BmsTableID == Table.ID);
                    else
                        query = query.Where(d => d.BmsTableDifficultyID == Table.ID);
                    var existFile = query.AsNoTracking().ToArray();
                    TableDatas = TableDatas.Where(d => !existFile.Any(f => f.MD5 == d.MD5)).ToArray();
                }
        }

        private async Task downloadAllAsync()
        {
            string targetDir;
            var diag = new OpenFolderDialog() { Multiselect = false };
            if (!(diag.ShowDialog() ?? false))
                return;

            targetDir = diag.FolderName;

            foreach (var data in TableDatas)
                await data.DownloadAsync(targetDir);
        }
    }
}
