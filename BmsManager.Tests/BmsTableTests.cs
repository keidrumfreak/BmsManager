using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BmsManager.Tests
{
    [TestClass]
    public class BmsTableTests
    {
        [TestMethod]
        public void Load()
        {
            // satelliteをサンプルに使用
            var table = new BmsTableDocument(@"https://stellabms.xyz/sl/table.html");
            table.LoadAsync(Utility.GetHttpClient()).Wait();
            Assert.AreEqual(@"https://stellabms.xyz/sl", table.Home);
            Assert.AreEqual("score.json", table.Header.DataUrl);
            Assert.IsTrue(table.Datas.Any());
        }
    }
}
