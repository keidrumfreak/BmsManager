using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        public ICommand OpenLR2IR { get; }

        BmsTableData data;

        public BmsTableDataViewModel(BmsTableData data)
        {
            this.data = data;

            OpenUrl = CreateCommand(input => openUrl(data.Url), input => !string.IsNullOrEmpty(data.Url));
            OpenDiffUrl = CreateCommand(input => openUrl(data.DiffUrl), input => !string.IsNullOrEmpty(data.DiffUrl));
            OpenPackUrl = CreateCommand(input => openUrl(data.PackUrl), input => !string.IsNullOrEmpty(data.PackUrl));
            OpenLR2IR = CreateCommand(input => openUrl($"http://www.dream-pro.info/~lavalse/LR2IR/search.cgi?mode=ranking&bmsmd5={data.MD5}"), input => !string.IsNullOrEmpty(data.MD5));
        }

        private void openUrl(string url)
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }

        public async Task DownloadAsync(string targetDir)
        {
            
            if (!string.IsNullOrEmpty(data.DiffUrl))
            {
                var targetPath = getTargetPath(targetDir, data.DiffUrl);
                if (!string.IsNullOrEmpty(targetPath))
                {
                    try
                    {
                        var client = HttpClientProvider.GetClient();
                        using (var res = await client.GetAsync(data.DiffUrl))
                        using (var stream = await res.Content.ReadAsStreamAsync())
                        using (var fs = SystemProvider.FileSystem.FileStream.Create(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            stream.CopyTo(fs);
                        }
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"{data.DiffUrl}\r\n{ex}");
                    }
                }
            }
            if (!string.IsNullOrEmpty(data.PackUrl))
            {
                var targetPath = getTargetPath(targetDir, data.PackUrl);
                try
                {
                    if (!string.IsNullOrEmpty(targetPath))
                    {
                        var client = HttpClientProvider.GetClient();
                        using (var res = await client.GetAsync(data.PackUrl))
                        using (var stream = await res.Content.ReadAsStreamAsync())
                        using (var fs = SystemProvider.FileSystem.FileStream.Create(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            stream.CopyTo(fs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{data.PackUrl}\r\n{ex}");
                }
            }
        }

        private string getTargetPath(string targetDir, string url)
        {
            var exts = BmsExtension.GetExtensions();
            if (exts.Any(e => data.DiffUrl.EndsWith(e)) || data.DiffUrl.EndsWith("zip"))
            {
                return PathUtil.Combine(targetDir, Path.GetFileName(data.DiffUrl));
            }
            else if (data.DiffUrl.Contains("get="))
            {
                return PathUtil.Combine(targetDir, data.DiffUrl.Substring(data.DiffUrl.IndexOf("get=")).Replace("get=", "") + ".zip");
            }
            return null;
        }
    }
}
