using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BmsManager.Data;
using CommonLib.IO;
using CommonLib.Net;
using CommonLib.Wpf;

namespace BmsManager
{
    class BmsTableDataViewModel : ViewModelBase
    {
        public string Difficulty => data.Difficulty.Difficulty;

        public string Title => data.Title;

        public string Artist => data.Artist;

        public string MD5 => data.MD5;

        public string Comment => data.Comment;

        public ICommand OpenUrl { get; }

        public ICommand OpenDiffUrl { get; }

        public ICommand OpenPackUrl { get; }

        BmsTableData data;

        public BmsTableDataViewModel(BmsTableData data)
        {
            this.data = data;

            OpenUrl = CreateCommand(input => openUrl(data.Url), input => !string.IsNullOrEmpty(data.Url));
            OpenDiffUrl = CreateCommand(input => openUrl(data.DiffUrl), input => !string.IsNullOrEmpty(data.DiffUrl));
            OpenPackUrl = CreateCommand(input => openUrl(data.PackUrl), input => !string.IsNullOrEmpty(data.PackUrl));
        }

        private void openUrl(string url)
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        public async Task DownloadAsync(string targetDir)
        {
            var exts = BmsExtension.GetExtensions();
            if (!string.IsNullOrEmpty(data.DiffUrl) && (exts.Any(e => data.DiffUrl.EndsWith(e)) || data.DiffUrl.EndsWith("zip")))
            {
                var targetPath = PathUtil.Combine(targetDir, Path.GetFileName(data.DiffUrl));
                var client = HttpClientProvider.GetClient();
                using (var res = await client.GetAsync(data.DiffUrl))
                using (var stream = await res.Content.ReadAsStreamAsync())
                using (var fs = SystemProvider.FileSystem.FileStream.Create(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.CopyTo(fs);
                }
            }
            else if (!string.IsNullOrEmpty(data.PackUrl) && (exts.Any(e => data.PackUrl.EndsWith(e)) || data.PackUrl.EndsWith("zip")))
            {
                var targetPath = PathUtil.Combine(targetDir, Path.GetFileName(data.PackUrl));
                var client = HttpClientProvider.GetClient();
                using (var res = await client.GetAsync(data.PackUrl))
                using (var stream = await res.Content.ReadAsStreamAsync())
                using (var fs = SystemProvider.FileSystem.FileStream.Create(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.CopyTo(fs);
                }
            }
        }
    }
}
