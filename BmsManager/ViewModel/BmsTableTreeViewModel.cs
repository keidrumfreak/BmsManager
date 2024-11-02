using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Data;
using BmsManager.Model;
using CommonLib.Wpf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;

namespace BmsManager
{
    class BmsTableTreeViewModel : ObservableObject
    {
        public string Url { get; set; }

        ObservableCollection<BmsTableViewModel> bmsTables;
        public ObservableCollection<BmsTableViewModel> BmsTables
        {
            get => bmsTables;
            set => SetProperty(ref bmsTables, value);
        }

        BmsTableViewModel selectedTreeItem;
        public BmsTableViewModel SelectedTreeItem
        {
            get { return selectedTreeItem; }
            set { SetProperty(ref selectedTreeItem, value); }
        }

        public ICommand LoadFromUrl { get; set; }

        public ICommand LoadAllTables { get; }

        public BmsTableTreeViewModel()
        {
            BmsTables = [new BmsTableViewModel()];
            LoadFromUrl = new AsyncRelayCommand(loadFromUrlAsync);
            LoadAllTables = new AsyncRelayCommand(loadAllTablesAsync);
        }

        private async Task loadAllTablesAsync()
        {
            using var con = new BmsManagerContext();
            // TODO: 検索処理の改善 (EntityFrameworkの改善待ち)
            var difficulties = await con.Difficulties
                .Include(d => d.TableDatas)
                .AsNoTracking().ToArrayAsync().ConfigureAwait(false);

            var tables = await con.Tables
                .AsNoTracking().ToArrayAsync().ConfigureAwait(false);

            foreach (var diff in difficulties.GroupBy(d => d.BmsTableID))
            {
                var parent = tables.FirstOrDefault(t => t.ID == diff.Key);
                parent.Difficulties = diff.ToList();
                foreach (var d in diff)
                {
                    d.Table = parent;
                }
            }

            BmsTables = new ObservableCollection<BmsTableViewModel>(tables.Select(t => new BmsTableViewModel(new BmsTableModel(t), this)).ToList());
        }

        private async Task loadFromUrlAsync()
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
            await doc.LoadAsync(Utility.GetHttpClient()).ConfigureAwait(false);

            var table = doc.ToEntity();

            using (var con = new BmsManagerContext())
            {
                con.Tables.Add(table);
                await con.SaveChangesAsync().ConfigureAwait(false);
            }

            Application.Current.Dispatcher.Invoke(() => BmsTables.Add(new BmsTableViewModel(new BmsTableModel(table), this)));
        }

        public void Reload()
        {
            OnPropertyChanged(nameof(BmsTables));
            OnPropertyChanged(nameof(SelectedTreeItem));
        }
    }
}
