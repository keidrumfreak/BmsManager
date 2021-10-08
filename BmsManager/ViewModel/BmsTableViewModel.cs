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
    class BmsTableViewModel : ViewModelBase
    {
        public string Name { get; set; }

        public IEnumerable<BmsTableViewModel> Children { get; set; }

        public IEnumerable<BmsTableData> TableDatas => Children?.SelectMany(d => d.TableDatas).ToArray() ?? difficulty.TableDatas.ToArray();

        public int ID => table?.ID ?? difficulty.ID;

        public ICommand Reload { get; set; }

        public bool IsTable => table != null;

        BmsTable table;
        BmsTableDifficulty difficulty;
        BmsTableTreeViewModel parent;

        public BmsTableViewModel(BmsTable table, BmsTableTreeViewModel parent)
        {
            this.parent = parent;
            Name = table.Name;
            this.table = table;
            Children = table.Difficulties.Select(d => new BmsTableViewModel(d));
            Reload = CreateCommand(async input => await reload());
        }

        private async Task reload()
        {
            await table.Reload();
            await table.Register();
            parent.Reload();
        }

        public BmsTableViewModel(BmsTableDifficulty difficulty)
        {
            Name = $"{difficulty.Table.Symbol}{difficulty.Difficulty}";
            this.difficulty = difficulty;
        }
    }
}
