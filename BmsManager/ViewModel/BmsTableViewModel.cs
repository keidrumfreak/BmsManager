using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Data;
using BmsManager.Entity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BmsManager.ViewModel
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

        public IEnumerable<BmsTableData> TableDatas => Children?.SelectMany(d => d.TableDatas).ToArray() ?? [.. difficulty?.TableDatas ?? []];

        public int ID => table?.ID ?? difficulty.ID;

        bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        public ICommand Reload { get; set; }

        public bool IsTable => table != null;

        BmsTable table;
        readonly BmsTableDifficulty difficulty;
        readonly BmsTableTreeViewModel parent;

        public BmsTableViewModel(BmsTableTreeViewModel parent)
        {
            this.parent = parent;
            IsLoading = true;
        }

        public BmsTableViewModel(BmsTable table, BmsTableTreeViewModel parent)
        {
            this.parent = parent;
            this.table = table;
            Name = table.Name;
            Children = table.Difficulties.OrderBy(d => d.DifficultyOrder).Select(d => new BmsTableViewModel(d));
            Reload = new AsyncRelayCommand(reloadAsync);
        }

        public BmsTableViewModel(BmsTableDifficulty difficulty)
        {
            Name = $"{difficulty.Table.Symbol}{difficulty.Difficulty}";
            this.difficulty = difficulty;
        }

        private async Task reloadAsync()
        {
            var doc = new BmsTableDocument(table.Url);
            await doc.LoadAsync(Utility.GetHttpClient());
            table = doc.ToEntity();
            await table.RegisterAsync();
            Name = table.Name;
            Children = table.Difficulties.Select(d => new BmsTableViewModel(d));
            parent.Reload();
        }

        public async Task LoadFromUrlAsync(string url)
        {
            var doc = new BmsTableDocument(url);
            await doc.LoadAsync(Utility.GetHttpClient()).ConfigureAwait(false);

            table= doc.ToEntity();
            Name = table.Name;

            using var con = new BmsManagerContext();
            con.Tables.Add(table);
            await con.SaveChangesAsync().ConfigureAwait(false);
            Children = table.Difficulties.Select(d => new BmsTableViewModel(d));

            IsLoading = false;
        }
    }
}
