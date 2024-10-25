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

namespace BmsManager.Data
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

        string loadingPath;
        [NotMapped]
        public string LoadingPath
        {
            get { return loadingPath; }
            set { SetProperty(ref loadingPath, value); }
        }

        [NotMapped]
        public BmsFolder[] DescendantFolders => Descendants().Where(r => r.Folders?.Any() ?? false).SelectMany(r => r.Folders).ToArray();

        public void LoadFromFileSystem(RootDirectory root = null, IEnumerable<RootDirectory> cacheRoots = null, IEnumerable<BmsFolder> cacheFolders = null, IEnumerable<BmsFile> cacheFiles = null, Task parentRegister = null, List<Task> allTasks = null)
        {
            IEnumerable<RootDirectory> dbRoots = cacheRoots;
            IEnumerable<BmsFolder> dbFolders = cacheFolders;
            IEnumerable<BmsFile> dbFiles = cacheFiles;
            if (root == null)
            {
                using (var con = new BmsManagerContext())
                {
                    dbRoots = con.RootDirectories.AsNoTracking().ToArray();
                    dbFolders = con.BmsFolders.AsNoTracking().ToArray();
                    dbFiles = con.Files.AsNoTracking().ToArray();
                }
                allTasks = new List<Task>();
            }

            var childrenRoots = dbRoots.Where(r => r.ParentRootID == ID);
            var childrenFolders = dbFolders.Where(r => r.RootID == ID);

            var previewExt = new[] { "wav", "ogg", "mp3", "flac" };
            var extentions = Settings.Default.Extentions;
            Folders = new List<BmsFolder>();
            Children = new List<RootDirectory>();
            FolderUpdateDate = SystemProvider.FileSystem.DirectoryInfo.New(Path).LastWriteTimeUtc;
            var folders = SystemProvider.FileSystem.Directory.EnumerateDirectories(Path).ToArray();

            allTasks.Add(Task.Run(() =>
            {
                var delRoot = childrenRoots.Where(r => !folders.Contains(r.Path));
                if (delRoot.Any())
                {
                    using var con = new BmsManagerContext();
                    con.ChangeTracker.AutoDetectChangesEnabled = false;
                    con.RootDirectories.RemoveRange(delRoot);
                    con.SaveChanges();
                }
                var delFol = childrenFolders.Where(r => !folders.Contains(r.Path));
                if (delFol.Any())
                {
                    using var con = new BmsManagerContext();
                    con.ChangeTracker.AutoDetectChangesEnabled = false;
                    con.BmsFolders.RemoveRange(delFol);
                    con.SaveChanges();
                }
            }));

            foreach (var folder in folders) // ディスク読込のため、並列化する意味は無い
            {
                (root ?? this).LoadingPath = folder;

                var updateDate = SystemProvider.FileSystem.DirectoryInfo.New(folder).LastWriteTimeUtc;
                var files = SystemProvider.FileSystem.Directory.EnumerateFiles(folder)
                    .Where(f =>
                    extentions.Concat(new[] { "txt" }).Contains(ClsPath.GetExtension(f).TrimStart('.').ToLowerInvariant())
                    || f.ToLower().StartsWith("preview") && previewExt.Contains(ClsPath.GetExtension(f).Trim('.').ToLowerInvariant())).ToArray();

                var bmsFileDatas = files.Where(f => extentions.Contains(ClsPath.GetExtension(f).TrimStart('.').ToLowerInvariant()))
                    .Select(file => (file, SystemProvider.FileSystem.File.ReadAllBytes(file)));
                
                if (bmsFileDatas.Any())
                {
                    allTasks.Add(Task.Run(async () =>
                    {
                        // 読込済データの解析なので並列化
                        var bmsFiles = bmsFileDatas.AsParallel().Select(d => new BmsFile(d.file, d.Item2)).Where(f => !string.IsNullOrEmpty(f.Path)).ToArray();
                        if (!bmsFiles.Any())
                            return;

                        var bmsFolder = dbFolders.FirstOrDefault(f => f.Path == folder);
                        var meta = bmsFiles.GetMetaFromFiles();
                        var hasText = files.Any(f => f.ToLower().EndsWith("txt"));
                        var preview = files.FirstOrDefault(f => f.ToLower().StartsWith("preview") && previewExt.Contains(ClsPath.GetExtension(f).Trim('.').ToLowerInvariant()));
                        if (bmsFolder == default)
                        {
                            bmsFolder = new BmsFolder
                            {
                                Path = folder,
                                Title = meta.Title,
                                Artist = meta.Artist,
                                FolderUpdateDate = updateDate,
                                HasText = hasText,
                                Preview = preview,
                                Files = bmsFiles
                            };
                            // 新規Folder
                            await Task.Run(async () =>
                            {
                                if (parentRegister != null)
                                    await parentRegister;
                                bmsFolder.RootID = ID;
                                //using var con = new BmsManagerContext();
                                //con.ChangeTracker.AutoDetectChangesEnabled = false;
                                //con.BmsFolders.Add(bmsFolder);
                                //con.SaveChanges();
                                bmsFolder.Register();
                            });
                        }
                        else
                        {
                            // 既存Folder
                            if (bmsFolder.Title != meta.Title || bmsFolder.Artist != meta.Artist || bmsFolder.HasText != hasText || bmsFolder.Preview != preview
                                || (bmsFolder.FolderUpdateDate.Date != updateDate.Date
                                && bmsFolder.FolderUpdateDate.Hour != updateDate.Hour
                                && bmsFolder.FolderUpdateDate.Minute != updateDate.Minute
                                && bmsFolder.FolderUpdateDate.Second != updateDate.Second)) // 何故かミリ秒がずれるのでミリ秒以外で比較
                            {
                                allTasks.Add(Task.Run(() =>
                                {
                                    using var con = new BmsManagerContext();
                                    var entity = con.BmsFolders.Find(bmsFolder.ID);
                                    entity.Title = meta.Title;
                                    entity.Artist = meta.Artist;
                                    entity.FolderUpdateDate = updateDate;
                                    entity.HasText = hasText;
                                    entity.Preview = preview;
                                    con.SaveChanges();
                                }));
                            }

                            var childrenFiles = dbFiles.Where(f => f.FolderID == bmsFolder.ID);
                            var delete = Task.Run(() =>
                            {
                                var del = childrenFiles.Where(f => !bmsFiles.Any(e => f.MD5 == e.MD5));
                                if (del.Any())
                                {
                                    using var con = new BmsManagerContext();
                                    con.ChangeTracker.AutoDetectChangesEnabled = false;
                                    con.Files.RemoveRange(del);
                                    con.SaveChanges();
                                }
                            });

                            await Task.Run(async () =>
                            {
                                var upd = bmsFiles.Where(f => !childrenFiles.Any(e => e.MD5 == f.MD5));
                                if (!upd.Any())
                                    return;
                                if (parentRegister != null)
                                    await parentRegister;
                                await delete;
                                using var con = new BmsManagerContext();
                                con.ChangeTracker.AutoDetectChangesEnabled = false;
                                foreach (var file in upd)
                                {
                                    file.FolderID = bmsFolder.ID;
                                    con.Files.Add(file);
                                }
                                con.SaveChanges();
                            });
                        }

                        if ((root ?? this).LoadingPath.StartsWith("DB登録"))
                            (root ?? this).LoadingPath = $"DB登録完了:{folder}";

                        //bmsFolder.Files = bmsFiles;
                        //Folders.Add(bmsFolder);
                    }));
                }
                else
                {
                    Task rootRegisterer = null;
                    RootDirectory child = dbRoots.FirstOrDefault(r => r.Path == folder);
                    if (child == default)
                    {
                        // 新規Root
                        child = new RootDirectory
                        {
                            Path = folder,
                            FolderUpdateDate = updateDate,
                        };
                        rootRegisterer = Task.Run(async () =>
                        {
                            if (parentRegister != null)
                                await parentRegister;
                            child.ParentRootID = ID;
                            using var context = new BmsManagerContext();
                            //con.ChangeTracker.AutoDetectChangesEnabled = false;
                            //con.RootDirectories.Add(child);
                            //con.SaveChanges();
                            using var con = context.Database.GetDbConnection();
                            con.Open();
                            using (var cmd = con.CreateCommand())
                            {
                                cmd.CommandText = $"INSERT INTO RootDirectory (Path,ParentRootID,FolderUpdateDate) VALUES (@{nameof(Path)},@{nameof(ParentRootID)},@{nameof(FolderUpdateDate)})";
                                cmd.AddParameter($"{nameof(Path)}", child.Path, DbType.String);
                                cmd.AddParameter($"{nameof(ParentRootID)}", child.ParentRootID, DbType.Int32);
                                cmd.AddParameter($"{nameof(FolderUpdateDate)}", child.FolderUpdateDate, DbType.DateTime);
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = con.CreateCommand())
                            {
                                cmd.CommandText = $"SELECT ID FROM RootDirectory WHERE Path = @{nameof(Path)}";
                                cmd.AddParameter($"@{nameof(Path)}", child.Path, DbType.String);
                                var reader = cmd.ExecuteReader();
                                reader.Read();
                                child.ID = Convert.ToInt32(reader[0]);
                            }
                        });
                    }
                    else
                    {
                        // 既存Root
                        if (child.FolderUpdateDate.Date != updateDate.Date
                            && child.FolderUpdateDate.Hour != updateDate.Hour
                            && child.FolderUpdateDate.Minute != updateDate.Minute
                            && child.FolderUpdateDate.Second != updateDate.Second) // 何故かミリ秒がずれるのでミリ秒以外で比較
                        {
                            allTasks.Add(Task.Run(() =>
                            {
                                using var con = new BmsManagerContext();
                                con.RootDirectories.Find(child.ID).FolderUpdateDate = updateDate;
                                con.SaveChanges();
                            }));
                        }
                    }
                    child.LoadFromFileSystem(root ?? this, dbRoots, dbFolders, dbFiles, rootRegisterer, allTasks);
                    //if (child.Children.Any() || child.Folders.Any())
                    //    Children.Add(child);
                }
            }
            if (root == null)
            {
                LoadingPath = "DB登録中...";
                Task.WhenAll(allTasks.ToArray()).ConfigureAwait(false).GetAwaiter().GetResult();
                LoadFromDB();
            }
            LoadingPath = string.Empty;
        }

        public static IEnumerable<RootDirectory> LoadTopRoot()
        {
            using (var con = new BmsManagerContext())
            {
                // TODO: 検索処理の改善 (EntityFrameworkの改善待ち)
                var folders = con.BmsFolders
                    .Include(f => f.Files)
                    .AsNoTracking().ToArray();

                var allRoots = con.RootDirectories
                    .AsNoTracking().ToArray();

                foreach (var folder in folders.GroupBy(f => f.RootID))
                {
                    var parent = allRoots.FirstOrDefault(r => r.ID == folder.Key);
                    if (parent == null)
                        continue;
                    parent.Folders = folder.ToList();
                    foreach (var fol in folder)
                    {
                        fol.Root = parent;
                    }
                }

                foreach (var parent in allRoots)
                {
                    parent.Children = allRoots.Where(r => r.ParentRootID == parent.ID).ToList();
                }

                return allRoots.Where(r => r.ParentRootID == null).ToArray();
            }
        }

        public void LoadFromDB()
        {
            using (var con = new BmsManagerContext())
            {
                // 親子構造の取得が難しいのでとりあえず全部引っ張る
                // TODO: 検索処理の改善
                var folders = con.BmsFolders
                    .Include(f => f.Files)
                    .AsNoTracking().ToArray();

                var allRoots = con.RootDirectories
                    .AsNoTracking().ToArray();

                foreach (var folder in folders.GroupBy(f => f.RootID))
                {
                    var parent = allRoots.FirstOrDefault(r => r.ID == folder.Key);
                    parent.Folders = folder.ToList();
                    foreach (var fol in folder)
                    {
                        fol.Root = parent;
                    }
                }

                // 親子関係を放り込む
                foreach (var parent in allRoots)
                {
                    var children = allRoots.Where(r => r.ParentRootID == parent.ID);
                    parent.Children = children.ToList();
                    foreach (var child in children)
                    {
                        child.Parent = parent;
                    }
                }

                var root = allRoots.FirstOrDefault(r => r.Path == Path);

                if (root == default)
                    throw new BmsManagerException("DB未登録のルートフォルダです。");

                Children = root.Children;
                Folders = root.Folders;
            }
            OnPropertyChanged(nameof(Children));
            OnPropertyChanged(nameof(Folders));
        }

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

                    var hasChild = dir.Children?.Any() ?? false;
                    if (hasChild)
                    {
                        // 子が存在する場合それぞれ登録
                        foreach (var child in dir.Children)
                        {
                            registerRoot(child);
                            if (root.Children == null)
                                root.Children = new List<RootDirectory>();
                            if (!root.Children.Any(c => c.Path == child.Path))
                                root.Children.Add(child);
                        }
                    }

                    if (dir.Folders == null || !dir.Folders.Any())
                    {
                        // フォルダも子も存在しない場合削除する
                        if (!hasChild)
                        {
                            if (root.Folders.Any())
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


                    if (!root.Folders.Any())
                    {
                        // フォルダ未登録の場合そのまま登録
                        root.Folders = dir.Folders.ToArray();
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

        public IEnumerable<RootDirectory> Descendants()
        {
            yield return this;
            if (Children == null || !Children.Any())
                yield break;
            foreach (var child in Children.SelectMany(c => c.Descendants()))
                yield return child;
        }
    }
}
