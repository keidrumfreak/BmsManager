using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        public string Url { get; set; }

        public ICommand LoadFromUrl { get; set; }

        public ObservableCollection<BmsTableTreeItem> BmsTables { get; set; }

        public bool Narrowed { get; set; }

        public ICommand ChangeNarrowing { get; set; }

        BmsTableTreeItem selectedTreeItem;
        public BmsTableTreeItem SelectedTreeItem
        {
            get { return selectedTreeItem; }
            set { SetProperty(ref selectedTreeItem, value); loadTableData(null); }
        }

        IEnumerable<BmsTableData> tableDatas;
        public IEnumerable<BmsTableData> TableDatas
        {
            get { return tableDatas; }
            set { SetProperty(ref tableDatas, value); }
        }

        public BmsTableManagerViewModel()
        {
            LoadFromUrl = CreateCommand(loadFromUrlAsync);
            ChangeNarrowing = CreateCommand(loadTableData);

            using (var con = new BmsManagerContext())
            {
                var tables = con.Tables
                    .Include(t => t.Difficulties)
                        .ThenInclude(d => d.TableDatas)
                    .AsNoTracking().ToArray();
                BmsTables = new ObservableCollection<BmsTableTreeItem>(tables.Select(t => new BmsTableTreeItem(t)).ToList());
            }
        }

        public async void loadFromUrlAsync(object input)
        {
            using (var con = new BmsManagerContext())
            {
                if (con.Tables.Any(t => t.Url == Url))
                {
                    MessageBox.Show("登録済です");
                    return;
                }
            }

                var doc = new BmsTableDocument(Url);
            await doc.LoadAsync();
            await doc.LoadHeaderAsync();
            await doc.LoadDatasAsync();

            var table = new BmsTable
            {
                Name = doc.Header.Name,
                Url = doc.Uri.AbsoluteUri,
                Symbol = doc.Header.Symbol,
                Tag = doc.Header.Tag,
                Difficulties = doc.Datas.GroupBy(d => d.Level).Select(d => new BmsTableDifficulty
                {
                    Difficulty = d.Key,
                    DifficultyOrder = doc.Header.LevelOrder?.Any() ?? false
                        ? Array.IndexOf(doc.Header.LevelOrder, d.Key) + 1
                        : int.TryParse(d.Key, out var index) ? index : null,
                    TableDatas = d.Select(d => new BmsTableData
                    {
                        MD5 = d.MD5,
                        LR2BmsID = d.LR2BmsID,
                        Title = d.Title,
                        Artist = d.Artist,
                        Url = d.Url,
                        DiffUrl = d.UrlDiff,
                        DiffName = d.NameDiff,
                        PackUrl = d.UrlPack,
                        PackName = d.NamePack,
                        Comment = d.Comment,
                        OrgMD5 = d.OrgMD5                        
                    }).ToList()
                }).ToList()
            };

            using (var con = new BmsManagerContext())
            {
                con.Tables.Add(table);
                con.SaveChanges();
            }
            
            BmsTables.Add(new BmsTableTreeItem(table));
        }

        private void loadTableData(object input)
        {
            TableDatas = SelectedTreeItem.Table?.Difficulties.SelectMany(d => d.TableDatas).ToArray()
                ?? SelectedTreeItem.Difficulty.TableDatas.ToArray();

            if (Narrowed)
            {
                using (var con = new BmsManagerContext())
                {
                    var query = con.Files.Join(con.TableDatas,
                        f => f.MD5, d => d.MD5, (f, d) => d);
                    if (SelectedTreeItem.Table == null)
                    {
                        query = query.Where(d => d.BmsTableDifficultyID == SelectedTreeItem.Difficulty.ID);
                    }
                    else
                    {
                        query = query.Where(d => d.Difficulty.BmsTableID == SelectedTreeItem.Table.ID);
                    }
                    var existFile = query.AsNoTracking().ToArray();
                    TableDatas = TableDatas.Where(d => !existFile.Any(f => f.MD5 == d.MD5)).ToArray();
                }
            }
        }
    }

    class BmsTableTreeItem
    {
        public string Name { get; set; }

        public BmsTable Table { get; set; }

        public BmsTableDifficulty Difficulty { get; set; }

        public IEnumerable<BmsTableTreeItem> Difficulties { get; set; }

        public BmsTableTreeItem() { }

        public BmsTableTreeItem(BmsTable table)
        {
            Name = table.Name;
            Table = table;
            Difficulties = table.Difficulties.Select(d => new BmsTableTreeItem
            {
                Name = $"{table.Symbol}{d.Difficulty}",
                Difficulty = d
            }).ToArray();
        }
    }
}
