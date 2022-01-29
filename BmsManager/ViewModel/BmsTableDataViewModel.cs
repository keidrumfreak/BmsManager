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

            OpenUrl = CreateCommand(() => openUrl(data.Url), () => !string.IsNullOrEmpty(data.Url));
            OpenDiffUrl = CreateCommand(() => openUrl(data.DiffUrl), () => !string.IsNullOrEmpty(data.DiffUrl));
            OpenPackUrl = CreateCommand(() => openUrl(data.PackUrl), () => !string.IsNullOrEmpty(data.PackUrl));
            OpenLR2IR = CreateCommand(() => openUrl($"http://www.dream-pro.info/~lavalse/LR2IR/search.cgi?mode=ranking&bmsmd5={data.MD5}"), () => !string.IsNullOrEmpty(data.MD5));
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
            var exts = Settings.Default.Extentions; ;
            if (exts.Any(e => url.EndsWith(e)) || url.EndsWith("zip") || url.EndsWith("rar"))
            {
                return PathUtil.Combine(targetDir, Path.GetFileName(url));
            }
            else if (url.Contains("get="))
            {
                return PathUtil.Combine(targetDir, url.Substring(data.DiffUrl.IndexOf("get=")).Replace("get=", "") + ".zip");
            }
            else if(url.EndsWith("dl=0") || url.EndsWith("dl=1"))
            {
                return PathUtil.Combine(targetDir, Path.GetFileName(url.Replace("?dl=0", "").Replace("?dl=1", "")).Replace("%", ""));
            }
            return null;
        }
    }
}
