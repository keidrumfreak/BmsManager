using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Data;
using BmsManager.Entity;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using WinCopies.Util;

namespace BmsManager.ViewModel
{
    class BmsTableViewModel : ObservableObject
    {
        string name = string.Empty;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        IEnumerable<BmsTableViewModel> children = [];
        public IEnumerable<BmsTableViewModel> Children
        {
            get => children;
            set => SetProperty(ref children, value);
        }

        public IEnumerable<BmsTableData> TableDatas => difficulty?.TableDatas.ToArray() ?? Children.SelectMany(d => d.TableDatas).ToArray(); //[.. difficulty?.TableDatas ?? []];

        public int ID => table?.ID ?? difficulty?.ID ?? -1;

        bool isLoading = false;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        public ICommand Reload { get; set; }

        public bool IsTable => table != null;

        BmsTable? table;
        readonly BmsTableDifficulty? difficulty;
        readonly BmsTableTreeViewModel? parent;

        public BmsTableViewModel(BmsTableTreeViewModel parent)
        {
            Reload = new AsyncRelayCommand(reloadAsync);
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
            if (table == null)
                return;

            IsLoading = true;
            var doc = new BmsTableDocument(table.Url);
            await doc.LoadAsync(Utility.GetHttpClient());
            table = doc.ToEntity();
            table = await registerAsync(table).ConfigureAwait(false);
            Name = table.Name;
            Children = table.Difficulties.Select(d => new BmsTableViewModel(d));
            parent?.Reload();
            IsLoading = false;
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

        private async Task<BmsTable> registerAsync(BmsTable data)
        {
            using var con = new BmsManagerContext();
            var table = await con.Tables.Include(t => t.Difficulties).FirstOrDefaultAsync(t => t.Url == data.Url).ConfigureAwait(false);
            if (table == default)
            {
                // 未登録の場合は追加するだけ
                con.Tables.Add(data);
                await con.SaveChangesAsync().ConfigureAwait(false);
                return data;
            }

            table.Name = data.Name;
            table.Symbol = data.Symbol;
            table.Tag = data.Tag;

            foreach (var diff in table.Difficulties.Where(db => !data.Difficulties.Any(doc => doc.Difficulty == db.Difficulty)).ToArray())
            {
                // 難易度削除(多分無い)
                table.Difficulties.Remove(diff);
            }

            foreach (var difficulty in data.Difficulties)
            {
                var dbDiff = table.Difficulties.FirstOrDefault(d => d.Difficulty == difficulty.Difficulty);
                if (dbDiff == default)
                {
                    table.Difficulties.Add(difficulty);
                    continue;
                }

                dbDiff.DifficultyOrder = difficulty.DifficultyOrder;

                // TODO: 本来最初に読んでおくべきだが、現状ThenIncludeに難があるためここで読み込み
                await con.Entry(dbDiff).Collection(d => d.TableDatas).LoadAsync().ConfigureAwait(false);

                foreach (var item in dbDiff.TableDatas.Where(db => !difficulty.TableDatas.Any(doc => doc.MD5 == db.MD5)).ToArray())
                {
                    // 曲削除
                    difficulty.TableDatas.Remove(item);
                }

                foreach (var item in difficulty.TableDatas)
                {
                    var dbData = dbDiff.TableDatas.FirstOrDefault(d => d.MD5 == item.MD5);
                    if (dbData == default)
                    {
                        dbDiff.TableDatas.Add(item);
                        continue;
                    }
                    dbData.MD5 = item.MD5;
                    dbData.LR2BmsID = item.LR2BmsID;
                    dbData.Title = item.Title;
                    dbData.Artist = item.Artist;
                    dbData.Url = item.Url;
                    dbData.DiffUrl = item.DiffUrl;
                    dbData.DiffName = item.DiffName;
                    dbData.PackUrl = item.PackUrl;
                    dbData.PackName = item.PackName;
                    dbData.Comment = item.Comment;
                    dbData.OrgMD5 = item.OrgMD5;
                }
            }
            await con.SaveChangesAsync().ConfigureAwait(false);
            return table;
        }
    }
}
