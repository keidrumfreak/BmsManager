using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Entity;
using CommonLib.Linq;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.ViewModel
{
    class DuplicateBmsCheckerViewModel : ViewModelBase
    {
        public BmsFileListViewModel FileList { get; set; }

        public ICommand CheckByMD5 { get; set; }

        public ICommand CheckByMeta { get; set; }

        public DuplicateBmsCheckerViewModel()
        {
            FileList = new BmsFileListViewModel();

            CheckByMD5 = CreateCommand(FileList.Folders.CheckDuplicateByMD5);
            CheckByMeta = CreateCommand(FileList.Folders.CheckDuplicateByMeta);
        }
    }
}
