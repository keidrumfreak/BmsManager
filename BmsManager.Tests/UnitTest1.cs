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
    }
}
