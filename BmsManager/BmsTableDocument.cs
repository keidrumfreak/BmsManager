using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommonLib.Net;
using CommonLib.Net.Http;

namespace BmsManager
{
    /// <summary>
    ///  難易度表
    /// </summary>
    class BmsTableDocument : HtmlDocument
    {
        public BmsTalbeHeader Header { get; private set; }

        public IEnumerable<BmsTableData> Datas { get; private set; }

        public string Home { get; }

        public BmsTableDocument(string uri) : base(uri)
        {
            Home = uri.Substring(0, uri.LastIndexOf('/'));
        }

        public async Task LoadHeaderAsync()
        {
            var headerContent = Content.Descendants(Namespace + "meta")?
                .FirstOrDefault(e => e.Attribute("name")?.Value == "bmstable")?
                .Attribute("content").Value
                .TrimStart('.', '/'); // "./"から始まっている場合がある

            var headerUri = $"{Home}/{headerContent}";

            var json = await HttpClientProvider.GetClient().GetStringAsync(headerUri);
            Header = JsonSerializer.Deserialize<BmsTalbeHeader>(json);
        }

        public async Task LoadDatasAsync()
        {
            var dataUri = $"{Home}/{Header.DataUrl.TrimStart('.', '/')}";
            var json = await HttpClientProvider.GetClient().GetStringAsync(dataUri);
            Datas = JsonSerializer.Deserialize<BmsTableData[]>(json);
        }

        public class BmsTalbeHeader
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("data_url")]
            public string DataUrl { get; set; }

            [JsonPropertyName("symbol")]
            public string Symbol { get; set; }

            [JsonPropertyName("tag")]
            public string Tag { get; set; }

            [JsonPropertyName("level_order")]
            public string[] LevelOrder { get; set; }
        }

        public class BmsTableData
        {
            [JsonPropertyName("md5")]
            public string MD5 { get; set; }

            [JsonPropertyName("level")]
            public string Level { get; set; }

            [JsonPropertyName("lr2_bmsid")]
            public string LR2BmsID { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("artist")]
            public string Artist { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("url_diff")]
            public string UrlDiff { get; set; }

            [JsonPropertyName("name_diff")]
            public string NameDiff { get; set; }

            [JsonPropertyName("url_pack")]
            public string UrlPack { get; set; }

            [JsonPropertyName("name_pack")]
            public string NamePack { get; set; }

            [JsonPropertyName("comment")]
            public string Comment { get; set; }

            [JsonPropertyName("org_md5")]
            public string OrgMD5 { get; set; }
        }
    }
}
