using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BmsManager.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.Model
{
    internal class BmsTableModel : ObservableObject
    {
        public int ID => entity.ID;

        public string Name => entity.Name;

        public IEnumerable<BmsTableDifficulty> Difficulties => entity.Difficulties;

        BmsTable entity;

        public BmsTableModel(BmsTable entity)
        {
            this.entity = entity;
        }

        public async Task ReloadAsync()
        {
            var doc = new BmsTableDocument(entity.Url);
            await doc.LoadAsync(Utility.GetHttpClient());
            entity = doc.ToEntity();
            await entity.RegisterAsync();
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Difficulties));
        }
    }
}
