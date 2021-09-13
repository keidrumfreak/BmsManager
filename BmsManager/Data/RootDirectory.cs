using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

        public void LoadFromFileSystem(RootDirectory root = null)
        {
            var previewExt = new[] { "wav", "ogg", "mp3", "flac" };
            var extentions = Settings.Default.Extentions;
            Folders = new List<BmsFolder>();
            Children = new List<RootDirectory>();
            FolderUpdateDate = SystemProvider.FileSystem.DirectoryInfo.FromDirectoryName(Path).LastWriteTimeUtc;
            var folders = SystemProvider.FileSystem.Directory.EnumerateDirectories(Path).ToArray();

            using (var con = new BmsManagerContext())
            {
                var ent = con.RootDirectories
                    .Include(r => r.Folders)
                    .Include(r => r.Children).AsNoTracking().FirstOrDefault(r => r.Path == Path);
                if (ent != default)
                {
                    var delFol = ent.Folders?.Where(f => !folders.Contains(f.Path));
                    if (delFol?.Any() ?? false)
                        con.BmsFolders.RemoveRange(delFol);
                    var delRoot = ent.Children?.Where(f => !folders.Contains(f.Path));
                    if (delRoot?.Any() ?? false)
                        con.RootDirectories.RemoveRange(delRoot);
                }
            }

            foreach (var folder in folders)
            {
                (root ?? this).LoadingPath = folder;
                var files = SystemProvider.FileSystem.Directory.EnumerateFiles(folder)
                    .Where(f =>
                    extentions.Concat(new[] { "txt" }).Contains(ClsPath.GetExtension(f).TrimStart('.').ToLowerInvariant())
                    || f.ToLower().StartsWith("preview") && previewExt.Contains(ClsPath.GetExtension(f).Trim('.').ToLowerInvariant())).ToArray();

                var bmsFiles = files.Where(f => extentions.Contains(ClsPath.GetExtension(f).TrimStart('.').ToLowerInvariant())).ToArray();
                if (bmsFiles.Any())
                {
                    var bmsEntities = bmsFiles.Select(file => new BmsFile(file)).Where(f => !string.IsNullOrEmpty(f.Path)).ToList();
                    var meta = bmsEntities.GetMetaFromFiles();

                    BmsFolder bmsFolder;
                    using (var con = new BmsManagerContext())
                    {
                        bmsFolder = con.BmsFolders
                            .Include(f => f.Files).FirstOrDefault(f => f.Path == folder);
                        var isNew = bmsFolder == default;
                        if (isNew)
                            bmsFolder = new BmsFolder();
                        bmsFolder.Path = folder;
                        bmsFolder.Title = meta.Title;
                        bmsFolder.Artist = meta.Artist;
                        bmsFolder.FolderUpdateDate = SystemProvider.FileSystem.DirectoryInfo.FromDirectoryName(folder).LastWriteTimeUtc;
                        bmsFolder.HasText = files.Any(f => f.ToLower().EndsWith("txt"));
                        var previews = files.Where(f => f.ToLower().StartsWith("preview") && previewExt.Contains(ClsPath.GetExtension(f).Trim('.').ToLowerInvariant()));
                        if (previews.Any())
                        {
                            bmsFolder.Preview = previews.First();
                        }
                        bmsFolder.RootID = ID;

                        if (isNew)
                            con.BmsFolders.Add(bmsFolder);

                        if (!(bmsFolder.Files?.Any() ?? false))
                        {
                            bmsFolder.Files = bmsEntities;
                        }
                        else
                        {
                            con.Files.RemoveRange(bmsFolder.Files.Where(f => !bmsFiles.Contains(f.Path)));
                            foreach (var file in bmsEntities)
                            {
                                var ent = bmsFolder.Files.FirstOrDefault(f => f.Path == file.Path);
                                if (ent == default)
                                {
                                    bmsFolder.Files.Add(file);
                                    continue;
                                }
                                if (ent.MD5 == file.MD5)
                                    continue;
                                bmsFolder.Files.Remove(ent);
                                bmsFolder.Files.Add(file);
                            }
                        }
                        con.SaveChanges();
                    }

                    Folders.Add(bmsFolder);
                }
                else
                {
                    RootDirectory child;
                    using (var con = new BmsManagerContext())
                    {
                        child = con.RootDirectories.FirstOrDefault(r => r.Path == folder);
                        var isNew = child == default;
                        if (isNew)
                            child = new RootDirectory();
                        child.Path = folder;
                        child.FolderUpdateDate = SystemProvider.FileSystem.DirectoryInfo.FromDirectoryName(folder).LastWriteTimeUtc;
                        child.ParentRootID = ID;
                        if (isNew)
                            con.RootDirectories.Add(child);
                        con.SaveChanges();
                    }
                    child.LoadFromFileSystem(root ?? this);
                    if (child.Children.Any() || child.Folders.Any())
                        Children.Add(child);
                }
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

                foreach (var parent in allRoots)
                {
                    parent.Children = allRoots.Where(r => r.ParentRootID == parent.ID).ToList();
                }

                var root = allRoots.FirstOrDefault(r => r.Path == Path);

                if (root == default)
                    throw new BmsManagerException("DB未登録のルートフォルダです。");

                Children = root.Children;
                Folders = root.Folders;
            }
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
