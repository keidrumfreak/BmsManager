using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BmsManager
{
    /// <summary>
    /// TableManager.xaml の相互作用ロジック
    /// </summary>
    public partial class BmsTableManager : Window
    {
        public BmsTableManager()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ((BmsTableManagerViewModel)DataContext).SelectedTreeItem = (BmsTableTreeItem)e.NewValue;
        }
    }
}
