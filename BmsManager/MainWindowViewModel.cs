using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Entity;
using BmsManager.View;
using CommonLib.Wpf;

namespace BmsManager
{
    class MainWindowViewModel : ViewModelBase
    {
        public ICommand ShowFileRegister { get; set; }

        public ICommand ShowTableManager { get; set; }

        public ICommand ShowDuplicateChecker { get; set; }

        public ICommand ShowDiffRegister { get; set; }

        public ICommand ShowExporter { get; set; }

        public MainWindowViewModel()
        {
            ShowFileRegister = CreateCommand(() => new FolderRegisterer().Show());
            ShowTableManager = CreateCommand(() => new BmsTableManager().Show());
            ShowDuplicateChecker = CreateCommand(() => new DuplicateBmsChecker().Show());
            ShowDiffRegister = CreateCommand(() => new DiffRegister().Show());
            ShowExporter = CreateCommand(() => new Exporter().Show());
        }
    }
}
