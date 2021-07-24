using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BmsManager.Tests
{
    [TestClass]
    public class BmsTableTests
    {
        [TestMethod]
        public void LoadHeader()
        {
            // satelliteをサンプルに使用
            var table = new BmsTableDocument(@"https://stellabms.xyz/sl/table.html");
            table.LoadAsync().Wait();
            Assert.AreEqual(@"https://stellabms.xyz/sl", table.Home);
            table.LoadHeaderAsync().Wait();
            Assert.AreEqual("score.json", table.Header.DataUrl);
        }

        [TestMethod]
        public void LoadData()
        {
            // satelliteをサンプルに使用
            var table = new BmsTableDocument(@"https://stellabms.xyz/sl/table.html");
            table.LoadAsync().Wait();
            Assert.AreEqual(@"https://stellabms.xyz/sl", table.Home);
            table.LoadHeaderAsync().Wait();
            Assert.AreEqual("score.json", table.Header.DataUrl);
            table.LoadDatasAsync().Wait();
            Assert.IsTrue(table.Datas.Any());
        }
    }
}
