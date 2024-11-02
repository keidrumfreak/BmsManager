using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Data;
using BmsManager.Model;
using CommonLib.Wpf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BmsManager
{
    class BmsTableViewModel : ObservableObject
    {
        public string Name { get; set; }

        public IEnumerable<BmsTableViewModel> Children { get; set; }

        public IEnumerable<BmsTableData> TableDatas => Children?.SelectMany(d => d.TableDatas).ToArray() ?? difficulty.TableDatas.ToArray();

        public int ID => table?.ID ?? difficulty.ID;

        bool isLoading = false;
        public bool IsLoading { get => isLoading; set => SetProperty(ref isLoading, value); }

        public ICommand Reload { get; set; }

        public bool IsTable => table != null;

        BmsTableModel table;
        BmsTableDifficulty difficulty;
        BmsTableTreeViewModel parent;

        public BmsTableViewModel()
        {
            IsLoading = true;
        }

        public BmsTableViewModel(BmsTableModel table, BmsTableTreeViewModel parent)
        {
            this.parent = parent;
            Name = table.Name;
            this.table = table;
            Children = table.Difficulties.Select(d => new BmsTableViewModel(d));
            Reload = new AsyncRelayCommand(reloadAsync);
        }

        private async Task reloadAsync()
        {
            await table.ReloadAsync();
            parent.Reload();
        }

        public BmsTableViewModel(BmsTableDifficulty difficulty)
        {
            Name = $"{difficulty.Table.Symbol}{difficulty.Difficulty}";
            this.difficulty = difficulty;
        }
    }
}
