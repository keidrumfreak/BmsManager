using Microsoft.VisualStudio.TestTools.UnitTesting;
using BmsManager;
using CommonLib.TestHelper.UnitTesting;
using System.Text;
using BmsManager.Entity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace BmsManager.Tests
{
    [TestClass]
    public class UtilityTests
    {
        [DataTestMethod]
        [DataRow("main-sub-", "main")]
        [DataRow("mainÅ`subÅ`", "main")]
        [DataRow("main(sub)", "main")]
        [DataRow("main[sub]", "main")]
        [DataRow("main<sub>", "main")]
        [DataRow("main  sub", "main")]
        [DataRow(@"main""sub""", "main")]
        public void GetTitleTest(string title, string main)
        {
            Assert.AreEqual(main, Utility.GetTitle(title));
        }

        [TestMethod]
        public void GetArtistTest()
        {
            var arr = new[] { "test", "test1", "test2", "1test", "2test" };
            Assert.AreEqual("test", Utility.GetArtist(arr));
        }

        [DataTestMethod]
        [DataRow("test obj:aa", "test")]
        [DataRow("test/obj:aa", "test")]
        [DataRow("test / obj:aa", "test")]
        [DataRow("test/Obj:aa", "test")]
        public void RemoveObjerTest(string name, string result)
        {
            Assert.AreEqual(result, Utility.RemoveObjer(name));
        }

        [DataTestMethod]
        [DataRow(@"F:\bms", "9788e931")]
        [DataRow(@"F:\bms\BMS", "6168b62b")]
        [DataRow(@"F:\bms\ìÔà’ìxï\", "17fd18a6")]
        [DataRow(@"F:\bms\Events\2005", "318c1bfa")]
        [DataRow(@"F:\bms\Events\2005\Rise in Revolt", "d853334b")]
        [DataRow(@"F:\bms\Events\2005\ëÊå‹âÒé©èÃñ≥ñºBMSçÏâ∆Ç™ï®ê\Ç∑ÅI", "80209614")]
        public void GetCrc32(string path, string crc)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Utility.GetCrc32(path).AreEqual(crc);
        }

        [DataTestMethod]
        [DataRow("Result", "Result")]
        [DataRow("Result\\", "ResultÅè")]
        [DataRow("Result<", "ResultÅÉ")]
        [DataRow("Result>", "ResultÅÑ")]
        [DataRow("Result/", "ResultÅ^")]
        [DataRow("Result*", "ResultÅñ")]
        [DataRow("Result:", "ResultÅF")]
        [DataRow("Result\"", "ResultÅh")]
        [DataRow("Result?", "ResultÅH")]
        [DataRow("Result|", "ResultÅb")]
        public void Rename(string name, string rename)
        {
            Utility.ToFileNameString(name).AreEqual(rename);
        }

        //[TestMethod]
        //public void GetFolderTitleAndArtist()
        //{
        //    using var con = new BmsManagerContext();
        //    var folders = con.BmsFolders.Include(f => f.Files).AsNoTracking().ToArray();
        //    var temp = folders.Select(f => (intersect(f.Files.Select(f => f.Artist)), intersect(f.Files.Select(f => f.Title)))).ToArray();
        //}

        //private string intersect(IEnumerable<string> source)
        //{
        //    foreach (var skip in Enumerable.Range(0, source.Count() + 1))
        //    {
        //        var arr = skip == 0 ? source : source.Take(skip - 1).Concat(source.Skip(skip));
        //        var ret = new List<char>();
        //        var tmp = arr.Take(1).First();
        //        foreach (var i in Enumerable.Range(0, arr.Min(s => s.Length)))
        //        {
        //            if (arr.Skip(1).All(s => tmp[i] == s[i]))
        //                ret.Add(tmp[i]);
        //            else
        //                break;
        //        }
        //        if (ret.Any())
        //        {
        //            if (ret.Count(c => c == '(') != ret.Count(c => c == ')'))
        //            {
        //                ret = ret.GetRange(0, ret.LastIndexOf('('));
        //            }
        //            if (ret.Count(c => c == '[') != ret.Count(c => c == ']'))
        //            {
        //                ret = ret.GetRange(0, ret.LastIndexOf('['));
        //            }

        //            var str = new string(ret.ToArray()).TrimEnd(' ', '/');
        //            if (ret.Count(c => c == '-') % 2 != 0)
        //            {
        //                str = str.TrimEnd('-');
        //            }
        //            return str.TrimEnd(' ', '/');
        //        }
        //    }
        //    return string.Empty;
        //}
    }
}
