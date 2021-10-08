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

namespace BmsManager
{
    class BmsTableTreeViewModel : ViewModelBase
    {
        public string Url { get; set; }

        public ObservableCollection<BmsTableViewModel> BmsTables { get; set; }

        BmsTableViewModel selectedTreeItem;
        public BmsTableViewModel SelectedTreeItem
        {
            get { return selectedTreeItem; }
            set { SetProperty(ref selectedTreeItem, value); }
        }

        public ICommand LoadFromUrl { get; set; }

        public BmsTableTreeViewModel()
        {
            LoadFromUrl = CreateCommand(loadFromUrlAsync);

            BmsTables = new ObservableCollection<BmsTableViewModel>(BmsTable.LoadAllTalbes().Select(t => new BmsTableViewModel(t, this)).ToList());
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

            var table = await BmsTable.LoadFromUrl(Url);

            using (var con = new BmsManagerContext())
            {
                con.Tables.Add(table);
                con.SaveChanges();
            }

            BmsTables.Add(new BmsTableViewModel(table, this));
        }

        public void Reload()
        {
            OnPropertyChanged(nameof(BmsTables));
            OnPropertyChanged(nameof(SelectedTreeItem));
        }
    }
}
