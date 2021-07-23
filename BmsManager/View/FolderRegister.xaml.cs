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
using BmsManager.Data;

namespace BmsManager
{
    /// <summary>
    /// FileRegister.xaml の相互作用ロジック
    /// </summary>
    public partial class FolderRegister : Window
    {
        public FolderRegister()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ((FolderRegisterViewModel)DataContext).SelectedNode = (RootDirectory)e.NewValue;
        }
    }
}
