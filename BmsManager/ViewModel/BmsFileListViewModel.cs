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
            set { root = value; BmsFolders = root.Folders.Select(f => new BmsFolderViewModel(f)).ToArray(); updateBmsFiles(); }
        }

        public ICommand AutoRename { get; set; }

        public ICommand Rename { get; set; }

        public ICommand Register { get; set; }

        public ICommand ChangeNarrowing { get; set; }

        public IEnumerable<BmsFile> BmsFiles { get; set; }

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
            set { selectedBmsFolder = value; updateBmsFiles(); if (selectedBmsFolder != null) { RenameText = Path.GetFileName(selectedBmsFolder.Path); } }
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
            ChangeNarrowing = CreateCommand(input => updateBmsFiles());
            AutoRename = CreateCommand(autoRename);
            Rename = CreateCommand(rename);
            Register = CreateCommand(register);
        }

        private void updateBmsFiles()
        {
            if (Narrowed && SelectedBmsFolder != null)
            {
                BmsFiles = SelectedBmsFolder?.Files;
            }
            else
            {
                BmsFiles = RootDirectory?.Folders.SelectMany(f => f.Files).ToArray();
            }
            OnPropertyChanged(nameof(BmsFiles));
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
