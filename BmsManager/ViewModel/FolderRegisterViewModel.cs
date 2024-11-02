using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Entity;
using BmsManager.ViewModel;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;

namespace BmsManager
{
    class FolderRegisterViewModel : ViewModelBase
    {
        public RootTreeViewModel RootTree { get; set; }

        public BmsFileSearcherViewModel FileSearcher { get; set; }

        public FolderRegisterViewModel()
        {
            RootTree = new RootTreeViewModel();
            FileSearcher = new BmsFileSearcherViewModel();

            RootTree.PropertyChanged += RootTree_PropertyChanged;
        }

        private void RootTree_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RootTreeViewModel.SelectedRoot))
            {
                FileSearcher.RootDirectory = RootTree.SelectedRoot;
            }
        }
    }
}
