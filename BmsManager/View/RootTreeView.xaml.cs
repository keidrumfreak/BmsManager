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
using System.Windows.Navigation;
using System.Windows.Shapes;
using BmsManager.Entity;
using BmsManager.ViewModel;

namespace BmsManager.View
{
    /// <summary>
    /// RootRegister.xaml の相互作用ロジック
    /// </summary>
    public partial class RootTreeView : UserControl
    {
        public RootTreeView()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ((RootTreeViewModel)DataContext).SelectedRoot = (RootDirectoryViewModel)e.NewValue;
        }
    }
}
