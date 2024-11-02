using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BmsManager.Data;
using CommonLib.Net;
using CommonLib.Net.Http;

namespace BmsManager
{
    /// <summary>
    ///  難易度表
    /// </summary>
    class BmsTableDocument(string uri) : HtmlDocument(uri)
    {
        public BmsTalbeHeader Header { get; private set; }

        public IEnumerable<BmsTableData> Datas { get; private set; }

        public string Home { get; } = uri[..uri.LastIndexOf('/')];

        public override async Task LoadAsync(HttpClient client)
        {
            await base.LoadAsync(client).ConfigureAwait(false);
            await loadHeaderAsync().ConfigureAwait(false);
            await loadDatasAsync().ConfigureAwait(false);
        }

        private async Task loadHeaderAsync()
        {
            var headerContent = Content.Descendants(Namespace + "meta")?
                .FirstOrDefault(e => e.Attribute("name")?.Value == "bmstable")?
                .Attribute("content").Value
                .TrimStart('.', '/'); // "./"から始まっている場合がある

            var headerUri = (headerContent.StartsWith("http:") || headerContent.StartsWith("https:"))
                ? headerContent
                : $"{Home}/{headerContent}";

            var json = await Utility.GetHttpClient().GetStringAsync(headerUri).ConfigureAwait(false);
            Header = JsonSerializer.Deserialize<BmsTalbeHeader>(json);
        }

        private async Task loadDatasAsync()
        {
            var dataUri = (Header.DataUrl.StartsWith("http:") || Header.DataUrl.StartsWith("https:"))
                ? Header.DataUrl
                : $"{Home}/{Header.DataUrl.TrimStart('.', '/')}";
            var json = await Utility.GetHttpClient().GetStringAsync(dataUri).ConfigureAwait(false);
            Datas = JsonSerializer.Deserialize<BmsTableData[]>(json);
        }

        public BmsTable ToEntity()
        {
            return new BmsTable
            {
                Name = Header.Name,
                Url = Uri.AbsoluteUri,
                Symbol = Header.Symbol,
                Tag = Header.Tag,
                Difficulties =Datas.GroupBy(d => d.Level).Select(d => new BmsTableDifficulty
                {
                    Difficulty = d.Key,
                    DifficultyOrder = ((Header.LevelOrder?.Length ?? 0) == 0)
                        ? Array.IndexOf(Header.LevelOrder, d.Key) + 1
                        : int.TryParse(d.Key, out var index) ? index : null,
                    TableDatas = d.Select(d => new Data.BmsTableData
                    {
                        MD5 = d.MD5,
                        LR2BmsID = d.LR2BmsID,
                        Title = d.Title,
                        Artist = d.Artist,
                        Url = d.Url,
                        DiffUrl = d.UrlDiff,
                        DiffName = d.NameDiff,
                        PackUrl = d.UrlPack,
                        PackName = d.NamePack,
                        Comment = d.Comment,
                        OrgMD5 = d.OrgMD5
                    }).ToList()
                }).ToList()
            };
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
            public JsonElement LevelValue { get; set; }

            string level;
            [JsonIgnore]
            public string Level => level ??= LevelValue.ValueKind == JsonValueKind.String ? LevelValue.GetString() : LevelValue.GetInt32().ToString();

            [JsonPropertyName("lr2_bmsid")]
            public JsonElement? LR2BmsIDValue { get; set; }

            string lr2BmsID;
            [JsonIgnore]
            public string LR2BmsID
            {
                get
                {
                    if (lr2BmsID != null) return lr2BmsID;
                    if (!LR2BmsIDValue.HasValue) return null;
                    return lr2BmsID = LR2BmsIDValue.Value.ValueKind == JsonValueKind.String ? LR2BmsIDValue.Value.GetString() : LR2BmsIDValue.Value.GetInt32().ToString();
                }
            }

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
