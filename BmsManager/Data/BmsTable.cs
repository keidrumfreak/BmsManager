using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.EntityFrameworkCore;

namespace BmsManager.Data
{
    [Table("BmsTable")]
    class BmsTable
    {
        [Key]
        public int ID { get; set; }

        public string Url { get; set; }

        public string Name { get; set; }

        public string Symbol { get; set; }

        public string Tag { get; set; }

        [InverseProperty(nameof(BmsTableDifficulty.Table))]
        public virtual ICollection<BmsTableDifficulty> Difficulties { get; set; }

        public BmsTable() { }

        public async Task RegisterAsync()
        {
            using var con = new BmsManagerContext();
            var table = await con.Tables.Include(t => t.Difficulties).FirstOrDefaultAsync(t => t.Url == Url).ConfigureAwait(false);
            if (table == default)
            {
                // 未登録の場合は追加するだけ
                con.Tables.Add(this);
                await con.SaveChangesAsync().ConfigureAwait(false);
                return;
            }

            table.Name = Name;
            table.Symbol = Symbol;
            table.Tag = Tag;

            foreach (var diff in table.Difficulties.Where(db => !Difficulties.Any(doc => doc.Difficulty == db.Difficulty)).ToArray())
            {
                // 難易度削除(多分無い)
                table.Difficulties.Remove(diff);
            }

            foreach (var difficulty in Difficulties)
            {
                var dbDiff = table.Difficulties.FirstOrDefault(d => d.Difficulty == difficulty.Difficulty);
                if (dbDiff == default)
                {
                    table.Difficulties.Add(difficulty);
                    continue;
                }

                dbDiff.DifficultyOrder = difficulty.DifficultyOrder;

                // TODO: 本来最初に読んでおくべきだが、現状ThenIncludeに難があるためここで読み込み
                await con.Entry(dbDiff).Collection(d => d.TableDatas).LoadAsync().ConfigureAwait(false);

                foreach (var data in dbDiff.TableDatas.Where(db => !difficulty.TableDatas.Any(doc => doc.MD5 == db.MD5)).ToArray())
                {
                    // 曲削除
                    difficulty.TableDatas.Remove(data);
                }

                foreach (var data in difficulty.TableDatas)
                {
                    var dbData = dbDiff.TableDatas.FirstOrDefault(d => d.MD5 == data.MD5);
                    if (dbData == default)
                    {
                        dbDiff.TableDatas.Add(data);
                        continue;
                    }
                    dbData.MD5 = data.MD5;
                    dbData.LR2BmsID = data.LR2BmsID;
                    dbData.Title = data.Title;
                    dbData.Artist = data.Artist;
                    dbData.Url = data.Url;
                    dbData.DiffUrl = data.DiffUrl;
                    dbData.DiffName = data.DiffName;
                    dbData.PackUrl = data.PackUrl;
                    dbData.PackName = data.PackName;
                    dbData.Comment = data.Comment;
                    dbData.OrgMD5 = data.OrgMD5;
                }
            }
            await con.SaveChangesAsync().ConfigureAwait(false);
        }
    }
}
