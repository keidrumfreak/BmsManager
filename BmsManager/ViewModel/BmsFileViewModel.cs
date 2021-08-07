using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BmsManager.Data;
using CommonLib.Wpf;

namespace BmsManager
{
    class BmsFileViewModel : ViewModelBase
    {
        public string Title => file.Title;

        public string Artist => file.Artist;

        public string Path => file.Path;

        BmsFile file;

        public BmsFileViewModel(BmsFile file)
        {
            this.file = file;
        }
    }
}
