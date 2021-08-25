using Microsoft.VisualStudio.TestTools.UnitTesting;
using BmsManager;
using CommonLib.TestHelper.UnitTesting;
using System.Text;

namespace BmsManager.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var md5 = "87bf3f70b00cc56c8b1f93ee1961d6b3";
            var hash = Utility.GetMd5Hash(@"E:\_source\BmsManager\BmsManager.Tests\_ld2013_a.bms");
            Assert.AreEqual(md5, hash);
        }

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
        [DataRow(@"F:\bms\BMS", "6168b62b")]
        [DataRow(@"F:\bms\Events\2005", "318c1bfa")]
        [DataRow(@"F:\bms\Events\2005\Rise in Revolt", "d853334b")]
        [DataRow(@"F:\bms\Events\2005\ëÊå‹âÒé©èÃñ≥ñºBMSçÏâ∆Ç™ï®ê\Ç∑ÅI", "d1fe61ba")]
        public void GetCrc32(string path, string crc)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Utility.GetCrc32(path).AreEqual(crc);
        }
    }
}
