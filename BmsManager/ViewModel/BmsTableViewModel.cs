using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
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
        string name;
        public string Name
        { 
            get => name;
            set => SetProperty(ref name, value);
        }

        IEnumerable<BmsTableViewModel> children;
        public IEnumerable<BmsTableViewModel> Children
        {
            get => children;
            set => SetProperty(ref children, value);
        }

        public IEnumerable<BmsTableData> TableDatas => Children?.SelectMany(d => d.TableDatas).ToArray() ?? difficulty.TableDatas.ToArray();

        public int ID => table?.ID ?? difficulty.ID;

        bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        public ICommand Reload { get; set; }

        public bool IsTable => table != null;

        BmsTableModel table;
        BmsTableDifficulty difficulty;
        BmsTableTreeViewModel parent;

        public BmsTableViewModel(BmsTableTreeViewModel parent)
        {
            this.parent = parent;
            IsLoading = true;
        }

        public BmsTableViewModel(BmsTableModel table, BmsTableTreeViewModel parent)
        {
            this.parent = parent;
            this.table = table;
            Name = table.Name;
            Children = table.Difficulties.Select(d => new BmsTableViewModel(d));
            Reload = new AsyncRelayCommand(reloadAsync);
        }

        public BmsTableViewModel(BmsTableDifficulty difficulty)
        {
            Name = $"{difficulty.Table.Symbol}{difficulty.Difficulty}";
            this.difficulty = difficulty;
        }

        private async Task reloadAsync()
        {
            await table.ReloadAsync();
            parent.Reload();
        }

        public async Task LoadFromUrlAsync(string url)
        {
            var doc = new BmsTableDocument(url);
            await doc.LoadAsync(Utility.GetHttpClient()).ConfigureAwait(false);

            var entity = doc.ToEntity();
            table = new BmsTableModel(entity);
            Name = table.Name;

            using var con = new BmsManagerContext();
            con.Tables.Add(entity);
            await con.SaveChangesAsync().ConfigureAwait(false);
            Children = table.Difficulties.Select(d => new BmsTableViewModel(d));

            IsLoading = false;
        }
    }
}
