using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using BmsManager.Entity;
using CommonLib.Net;
using CommonLib.Net.Http;

namespace BmsManager.Data
{
    /// <summary>
    ///  難易度表
    /// </summary>
    class BmsTableDocument(string uri) : HtmlDocument(uri)
    {
        BmsTalbeHeader? header;

        IEnumerable<BmsTableData>? data;

        public string Home { get; } = uri[..uri.LastIndexOf('/')];

        public override async Task LoadAsync(HttpClient client)
        {
            await base.LoadAsync(client).ConfigureAwait(false);
            var headerContent = Content.Descendants(Namespace + "meta")?
                .FirstOrDefault(e => e.Attribute("name")?.Value == "bmstable")?
                .Attribute("content")?.Value?
                .TrimStart('.', '/'); // "./"から始まっている場合がある

            if (headerContent == null)
            {
                throw new Exception();
            }

            var headerUri = headerContent.StartsWith("http:") || headerContent.StartsWith("https:")
                ? headerContent
                : $"{Home}/{headerContent}";

            var headerJson = await Utility.GetHttpClient().GetStringAsync(headerUri).ConfigureAwait(false);
            header = JsonSerializer.Deserialize<BmsTalbeHeader>(headerJson);

            if (header == null)
            {
                throw new Exception();
            }

            var dataUri = header.DataUrl.StartsWith("http:") || header.DataUrl.StartsWith("https:")
                ? header.DataUrl
                : $"{Home}/{header.DataUrl.TrimStart('.', '/')}";
            var dataJson = await Utility.GetHttpClient().GetStringAsync(dataUri).ConfigureAwait(false);
            data = JsonSerializer.Deserialize<BmsTableData[]>(dataJson);
        }

        public BmsTable ToEntity()
        {
            if (header == null || data == null)
                throw new InvalidOperationException();

            return new BmsTable
            {
                Name = header.Name,
                Url = Uri.AbsoluteUri,
                Symbol = header.Symbol,
                Tag = header.Tag,
                Difficulties = data.GroupBy(d => d.Level).Select(d => new BmsTableDifficulty
                {
                    Difficulty = d.Key,
                    DifficultyOrder = header.LevelOrder != null && header.LevelOrder.Length > 0
                        ? Array.IndexOf(header.LevelOrder, d.Key) + 1
                        : int.TryParse(d.Key, out var index) ? index : null,
                    TableDatas = d.Select(d => new Entity.BmsTableData
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
                }).OrderBy(d => d.DifficultyOrder).ToList()
            };
        }

        public class BmsTalbeHeader
        {
            [JsonPropertyName("name")]
            public required string Name { get; set; }

            [JsonPropertyName("data_url")]
            public required string DataUrl { get; set; }

            [JsonPropertyName("symbol")]
            public required string Symbol { get; set; }

            [JsonPropertyName("tag")]
            public string? Tag { get; set; }

            [JsonPropertyName("level_order")]
            public string[]? LevelOrder { get; set; }
        }

        public class BmsTableData
        {
            [JsonPropertyName("md5")]
            public required string MD5 { get; set; }

            [JsonPropertyName("level")]
            public JsonElement LevelValue { get; set; }

            string? level;
            [JsonIgnore]
            public string Level => level ??= (LevelValue.ValueKind == JsonValueKind.String ? LevelValue.GetString() : LevelValue.GetInt32().ToString()) ?? throw new Exception();

            [JsonPropertyName("lr2_bmsid")]
            public JsonElement? LR2BmsIDValue { get; set; }

            string? lr2BmsID;
            [JsonIgnore]
            public string? LR2BmsID
            {
                get
                {
                    if (lr2BmsID != null) return lr2BmsID;
                    if (!LR2BmsIDValue.HasValue) return null;
                    return lr2BmsID = LR2BmsIDValue?.ValueKind == JsonValueKind.String ? LR2BmsIDValue.Value.GetString() : LR2BmsIDValue?.GetInt32().ToString();
                }
            }

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("artist")]
            public string? Artist { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("url_diff")]
            public string? UrlDiff { get; set; }

            [JsonPropertyName("name_diff")]
            public string? NameDiff { get; set; }

            [JsonPropertyName("url_pack")]
            public string? UrlPack { get; set; }

            [JsonPropertyName("name_pack")]
            public string? NamePack { get; set; }

            [JsonPropertyName("comment")]
            public string? Comment { get; set; }

            [JsonPropertyName("org_md5")]
            public string? OrgMD5 { get; set; }
        }
    }
}
