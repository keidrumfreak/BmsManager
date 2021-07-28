using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;

namespace BmsManager
{
    class BmsTableManagerViewModel : ViewModelBase
    {
        public bool Narrowed { get; set; }

        public ICommand ChangeNarrowing { get; set; }

        IEnumerable<BmsTableData> tableDatas;
        public IEnumerable<BmsTableData> TableDatas
        {
            get { return tableDatas; }
            set { SetProperty(ref tableDatas, value); }
        }

        public BmsTableTreeViewModel BmsTableTree { get; set; }

        public BmsTableManagerViewModel()
        {
            BmsTableTree = new BmsTableTreeViewModel();
            ChangeNarrowing = CreateCommand(loadTableData);
            BmsTableTree.PropertyChanged += BmsTableTree_PropertyChanged;
        }

        private void BmsTableTree_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BmsTableTreeViewModel.SelectedTreeItem))
            {
                loadTableData(null);
            }
        }

        private void loadTableData(object input)
        {
            TableDatas = BmsTableTree.SelectedTreeItem?.TableDatas.ToArray();

            if (Narrowed)
            {
                using (var con = new BmsManagerContext())
                {
                    var query = con.Files.Join(con.TableDatas,
                        f => f.MD5, d => d.MD5, (f, d) => d);
                    if (BmsTableTree.SelectedTreeItem.IsTable)
                    {
                        query = query.Where(d => d.Difficulty.BmsTableID == BmsTableTree.SelectedTreeItem.ID);
                    }
                    else
                    {
                        query = query.Where(d => d.BmsTableDifficultyID == BmsTableTree.SelectedTreeItem.ID);
                    }
                    var existFile = query.AsNoTracking().ToArray();
                    TableDatas = TableDatas.Where(d => !existFile.Any(f => f.MD5 == d.MD5)).ToArray();
                }
            }
        }
    }
}
