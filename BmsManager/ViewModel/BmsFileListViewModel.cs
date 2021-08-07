using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;

namespace BmsManager
{
    class BmsFileListViewModel : ViewModelBase
    {
        RootDirectoryViewModel root;
        public RootDirectoryViewModel RootDirectory
        {
            get { return root; }
            set { root = value; searchFolder(); }
        }

        public ICommand Search { get; set; }

        public ICommand AutoRename { get; set; }

        public ICommand Rename { get; set; }

        public ICommand Register { get; set; }

        public ICommand ChangeNarrowing { get; set; }

        IEnumerable<BmsFile> bmsFiles;
        public IEnumerable<BmsFile> BmsFiles
        {
            get { return bmsFiles; }
            set { SetProperty(ref bmsFiles, value); }
        }

        string seachText;
        public string SearchText
        {
            get { return seachText; }
            set { SetProperty(ref seachText, value); }
        }

        IEnumerable<BmsFolderViewModel> bmsFolders;
        public IEnumerable<BmsFolderViewModel> BmsFolders
        {
            get { return bmsFolders; }
            set { SetProperty(ref bmsFolders, value); }
        }

        BmsFolderViewModel selectedBmsFolder;
        public BmsFolderViewModel SelectedBmsFolder
        {
            get { return selectedBmsFolder; }
            set { selectedBmsFolder = value; searchFile(); if (selectedBmsFolder != null) { RenameText = Path.GetFileName(selectedBmsFolder.Path); } }
        }

        BmsFile selectedBmsFile;
        public BmsFile SelectedBmsFile
        {
            get { return selectedBmsFile; }
            set { selectedBmsFile = value; if (selectedBmsFile != null) { RenameText = $"[{selectedBmsFile.Artist}]{Utility.GetTitle(selectedBmsFile.Title)}"; } }
        }

        string renameText;
        public string RenameText
        {
            get { return renameText; }
            set { SetProperty(ref renameText, value); }
        }

        public bool Narrowed { get; set; }

        public BmsFileListViewModel()
        {
            ChangeNarrowing = CreateCommand(input => searchFile());
            AutoRename = CreateCommand(autoRename);
            Rename = CreateCommand(rename);
            Register = CreateCommand(register);
            Search = CreateCommand(input => searchFolder());
        }

        private void searchFolder()
        {
            if (string.IsNullOrEmpty(SearchText))
            {
                BmsFolders = root.Folders.Select(f => new BmsFolderViewModel(f)).ToArray();
            }
            else
            {
                var files = root.Folders.SelectMany(f => f.Files)
                    .Where(f => (f.Artist?.Contains(SearchText) ?? false) || (f.Title?.Contains(SearchText) ?? false)).ToArray();
                var folders = files.Select(f => f.Folder).Distinct();

                BmsFolders = folders.Select(f => new BmsFolderViewModel(f)).ToArray();
            }
            searchFile();
        }

        private void searchFile()
        {
            if (Narrowed && SelectedBmsFolder != null)
            {
                if (string.IsNullOrEmpty(SearchText))
                {
                    BmsFiles = SelectedBmsFolder?.Files;
                }
                else
                {
                    BmsFiles = SelectedBmsFolder?.Files.Where(f => (f.Artist?.Contains(SearchText) ?? false) || (f.Title?.Contains(SearchText) ?? false)).ToArray();
                }
            }
            else
            {
                if (string.IsNullOrEmpty(SearchText))
                {
                    BmsFiles = RootDirectory?.Folders.SelectMany(f => f.Files).ToArray();
                }
                else
                {
                    BmsFiles = RootDirectory?.Folders.SelectMany(f => f.Files).Where(f => (f.Artist?.Contains(SearchText) ?? false) || (f.Title?.Contains(SearchText) ?? false)).ToArray();
                }
            }
        }

        private void autoRename(object input)
        {
            if (BmsFolders == null)
                return;

            foreach (var folder in BmsFolders)
            {
                try
                {
                    folder.AutoRename();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            RootDirectory.LoadFromFileSystem.Execute(null);
            OnPropertyChanged(nameof(BmsFiles));
            OnPropertyChanged(nameof(BmsFolders));
        }

        private void rename(object input)
        {
            if (string.IsNullOrEmpty(RenameText))
                return;

            try
            {
                SelectedBmsFolder.Rename(RenameText);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            RootDirectory.LoadFromFileSystem.Execute(null);
            OnPropertyChanged(nameof(BmsFiles));
            OnPropertyChanged(nameof(BmsFolders));
        }

        private void register(object input)
        {
            RootDirectory?.Register();

            MessageBox.Show("登録完了しました");
        }
    }
}
