using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Entity;
using CommonLib.Wpf;

namespace BmsManager.ViewModel
{
    class BmsFileSearcherViewModel : ViewModelBase
    {
        RootDirectoryViewModel root;
        public RootDirectoryViewModel RootDirectory
        {
            get { return root; }
            set
            {
                if (root != null)
                    root.PropertyChanged -= Root_PropertyChanged;

                root = value;
                if (root != null)
                    root.PropertyChanged += Root_PropertyChanged;
                search();
            }
        }

        private void Root_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RootDirectory.Folders))
                search();
        }

        public BmsFileListViewModel FileList { get; set; }

        public ICommand Search { get; set; }

        public ICommand Clear { get; set; }

        public ICommand RenameAll { get; set; }

        public ICommand UpdateMeta { get; set; }

        public ICommand Rename { get; set; }

        string seachText;
        public string SearchText
        {
            get { return seachText; }
            set { SetProperty(ref seachText, value); }
        }

        string title;
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        string artist;
        public string Artist
        {
            get { return artist; }
            set { SetProperty(ref artist, value); }
        }

        public BmsFileSearcherViewModel()
        {
            FileList = new BmsFileListViewModel();
            FileList.PropertyChanged += FileList_PropertyChanged;

            RenameAll = CreateCommand(() => Task.Run(() => renameAll()));
            UpdateMeta = CreateCommand(updateMeta);
            Rename = CreateCommand(rename);
            Search = CreateCommand(search);
            Clear = CreateCommand(() => { SearchText = null; search(); });
        }

        private void search()
        {
            if (root == null)
                return;

            if (string.IsNullOrEmpty(SearchText))
            {
                FileList.Folders = new ObservableCollection<BmsFolder>(root.DescendantFolders);
                return;
            }

            FileList.Folders = new ObservableCollection<BmsFolder>(inner(root).ToArray());

            IEnumerable<BmsFolder> inner(RootDirectoryViewModel root)
            {
                if (root.DescendantFolders != null)
                    foreach (var folder in root.DescendantFolders)
                    {
                        var files = folder.Files.Where(f => (f.Artist?.Contains(SearchText) ?? false) || (f.Title?.Contains(SearchText) ?? false));
                        if (!files.Any()) continue;
                        //var vm = new BmsFolderViewModel(folder, FileList);
                        folder.Files = new ObservableCollection<BmsFile>(files.ToArray());
                        yield return folder;
                    }
            }
        }

        private void FileList_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BmsFileListViewModel.SelectedBmsFolder))
                if (FileList.SelectedBmsFolder != null)
                {
                    Title = FileList.SelectedBmsFolder.Title;
                    Artist = FileList.SelectedBmsFolder.Artist;
                }
            else if (e.PropertyName == nameof(BmsFileListViewModel.SelectedBmsFile))
                if (FileList.SelectedBmsFile != null)
                {
                    Title = Utility.GetTitle(FileList.SelectedBmsFile.Title);
                    Artist = FileList.SelectedBmsFile.Artist;
                }
        }

        private void updateMeta()
        {
            FileList.SelectedBmsFolder.Title = Title;
            FileList.SelectedBmsFolder.Artist = Artist;
            FileList.SelectedBmsFolder.UpdateMeta();
        }

        private void renameAll()
        {
            if (FileList.Folders == null)
                return;

            foreach (var folder in FileList.Folders)
                try
                {
                    folder.Rename();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

            //RootDirectory.LoadFromFileSystem.Execute(null);

            search();
        }

        private void rename()
        {
            try
            {
                FileList.SelectedBmsFolder.Rename();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            search();
        }

        private void register(object input)
        {
            RootDirectory?.Root.Register();

            MessageBox.Show("登録完了しました");
        }
    }
}
