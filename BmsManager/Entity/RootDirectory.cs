using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLib.Wpf;
using Microsoft.EntityFrameworkCore;
using ClsPath = System.IO.Path;

namespace BmsManager.Entity
{
    [Table("RootDirectory")]
    class RootDirectory : BindableBase
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        public string Path { get; set; }

        public int? ParentRootID { get; set; }

        public DateTime FolderUpdateDate { get; set; }

        [InverseProperty(nameof(BmsFolder.Root))]
        public virtual ICollection<BmsFolder> Folders { get; set; }

        [ForeignKey(nameof(ParentRootID))]
        public virtual RootDirectory Parent { get; set; }

        [InverseProperty(nameof(Parent))]
        public virtual ICollection<RootDirectory> Children { get; set; }

        public void Register()
        {
            using (var con = new BmsManagerContext())
            {
                registerRoot(this);

                void registerRoot(RootDirectory dir)
                {
                    var root = con.RootDirectories
                        .Include(d => d.Folders)
                        .Include(d => d.Children)
                        .FirstOrDefault(d => d.Path == dir.Path);

                    if (root == null)
                    {
                        // ルート未登録の場合そのまま登録
                        con.RootDirectories.Add(dir);
                        return;
                    }

                    var hasChild = (dir.Children?.Count ?? 0) == 0;
                    if (hasChild)
                    {
                        // 子が存在する場合それぞれ登録
                        foreach (var child in dir.Children)
                        {
                            registerRoot(child);
                            root.Children ??= [];
                            if (!root.Children.Any(c => c.Path == child.Path))
                                root.Children.Add(child);
                        }
                    }

                    if (dir.Folders == null || dir.Folders.Count == 0)
                    {
                        // フォルダも子も存在しない場合削除する
                        if (!hasChild)
                        {
                            if (root.Folders.Count != 0)
                            {
                                foreach (var folder in root.Folders)
                                {
                                    con.Entry(folder).Collection(f => f.Files);
                                    foreach (var file in folder.Files)
                                    {
                                        con.Files.Remove(file);
                                    }
                                    con.BmsFolders.Remove(folder);
                                }
                            }
                            con.RootDirectories.Remove(root);
                        }
                        return;
                    }


                    if (root.Folders.Count == 0)
                    {
                        // フォルダ未登録の場合そのまま登録
                        root.Folders = [.. dir.Folders];
                        return;
                    }

                    var registered = new List<BmsFolder>();
                    foreach (var folder in root.Folders.ToArray())
                    {
                        var fsFolder = dir.Folders.FirstOrDefault(f => f.Path == folder.Path);
                        if (fsFolder == default)
                        {
                            con.Entry(folder).Collection(f => f.Files).Load();
                            // 実体が存在しないフォルダを削除
                            foreach (var file in folder.Files)
                            {
                                con.Files.Remove(file);
                            }
                            root.Folders.Remove(folder);
                            continue;
                        }

                        con.Entry(folder).Collection(f => f.Files).Load();

                        foreach (var file in folder.Files.ToArray())
                        {
                            var fsFile = fsFolder.Files.FirstOrDefault(f => f.Path == file.Path);
                            if (fsFile == default)
                            {
                                // 実体が存在しないファイルを削除
                                folder.Files.Remove(file);
                                continue;
                            }
                        }

                        // ファイル情報更新・登録
                        foreach (var file in fsFolder.Files)
                        {
                            var dbFile = folder.Files.FirstOrDefault(f => f.Path == file.Path);
                            if (dbFile == default)
                            {
                                folder.Files.Add(file);
                            }
                            else
                            {
                                dbFile.Title = file.Title;
                                dbFile.Artist = file.Artist;
                                dbFile.MD5 = file.MD5;
                            }
                        }

                        // 登録済としてマーク
                        registered.Add(fsFolder);
                    }

                    // 未登録のフォルダを登録
                    foreach (var folder in dir.Folders.Where(f => !registered.Contains(f)))
                    {
                        root.Folders.Add(folder);
                    }
                }

                con.SaveChanges();
            }
            OnPropertyChanged(nameof(Children));
            OnPropertyChanged(nameof(Folders));
        }
    }
}
