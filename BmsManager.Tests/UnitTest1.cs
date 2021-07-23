using Microsoft.VisualStudio.TestTools.UnitTesting;
using BmsManager;

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
    }
}
