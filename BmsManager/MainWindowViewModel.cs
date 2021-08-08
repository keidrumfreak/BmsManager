﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Wpf;

namespace BmsManager
{
    class MainWindowViewModel : ViewModelBase
    {
        public ICommand ShowFileRegister { get; set; }

        public ICommand ShowTableManager { get; set; }

        public ICommand ShowDuplicateChecker { get; set; }

        public ICommand ShowDiffRegister { get; set; }

        public MainWindowViewModel()
        {
            ShowFileRegister = CreateCommand(input => new FolderRegister().Show());
            ShowTableManager = CreateCommand(input => new BmsTableManager().Show());
            ShowDuplicateChecker = CreateCommand(input => new DuplicateBmsChecker().Show());
            ShowDiffRegister = CreateCommand(input => new DiffRegister().Show());
        }
    }
}
