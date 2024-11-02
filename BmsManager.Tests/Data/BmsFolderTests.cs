using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BmsManager.Entity;
using CommonLib.TestHelper.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BmsManager.Tests.Data
{
    [TestClass]
    public class BmsFolderTests
    {
        MockFileSystem mock;
        [TestInitialize]
        public void Initialize()
        {
            mock = new MockFileSystem();
            SystemProvider.Instance = new SystemProvider(mock);
        }

        //[DataTestMethod]
        //[DataRow("Result", "Result")]
        //[DataRow("Result\\", "Result￥")]
        //[DataRow("Result<", "Result＜")]
        //[DataRow("Result>", "Result＞")]
        //[DataRow("Result/", "Result／")]
        //[DataRow("Result*", "Result＊")]
        //[DataRow("Result:", "Result：")]
        //[DataRow("Result\"", "Result”")]
        //[DataRow("Result?", "Result？")]
        //[DataRow("Result|", "Result｜")]
        //public void Rename(string name, string rename)
        //{
        //    mock.AddDirectory(@"D:");
        //    mock.AddDirectory(@"D:\Test");

        //    var folder = new BmsFolder { Path = @"D:\Test" };
        //    folder.Rename();

        //    mock.Directory.Exists(@"D:\Test").IsFalse();
        //    mock.Directory.Exists(@$"D:\{rename}").IsTrue();
        //    folder.Path.AreEqual(@$"D:\{rename}");
        //}

        //[TestMethod]
        //public void RenameIfExists()
        //{
        //    mock.AddDirectory(@"D:");
        //    mock.AddDirectory(@"D:\Test");
        //    mock.AddDirectory(@"D:\Result");

        //    var folder = new BmsFolder { Path = @"D:\Test" };
        //    folder.Rename();

        //    mock.Directory.Exists(@"D:\Test").IsFalse();
        //    mock.Directory.Exists(@$"D:\Result (2)").IsTrue();
        //    folder.Path.AreEqual(@$"D:\Result (2)");

        //    mock.AddDirectory(@"D:\Test");

        //    folder = new BmsFolder { Path = @"D:\Test" };
        //    folder.Rename();

        //    mock.Directory.Exists(@"D:\Test").IsFalse();
        //    mock.Directory.Exists(@$"D:\Result (3)").IsTrue();
        //    folder.Path.AreEqual(@$"D:\Result (3)");
        //}
    }
}