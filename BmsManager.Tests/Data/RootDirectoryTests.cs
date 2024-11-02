using System;
using System.Collections.Generic;
using System.IO;
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
    public class RootDirectoryTests
    {
        //MockFileSystem mock;
        //[TestInitialize]
        //public void Initialize()
        //{
        //    mock = new MockFileSystem();
        //    SystemProvider.Instance = new SystemProvider(mock);
        //}

        //[TestMethod]
        //public void LoadFromFileSystem()
        //{
        //    mock.AddDirectory(@"D:\parent");
        //    var root1Path = @"D:\parent\root1";
        //    mock.AddDirectory(root1Path);
        //    var folder1Path = @"D:\parent\root1\folder1";
        //    mock.AddDirectory(folder1Path);
        //    var folder2Path = @"D:\parent\root1\folder2";
        //    mock.AddDirectory(folder2Path);
        //    var root2Path = @"D:\parent\root2";
        //    mock.AddDirectory(root2Path);
        //    var notRootPath = @"D:\parent\root2\notroot";
        //    mock.AddDirectory(notRootPath);
        //    var childPath = @"D:\parent\root2\child";
        //    mock.AddDirectory(childPath);
        //    var folder3Path = @"D:\parent\root2\child\folder3";
        //    mock.AddDirectory(folder3Path);
        //    var folder4Path = @"D:\parent\root2\child\folder4";
        //    mock.AddDirectory(folder4Path);
        //    var file1Path = @"D:\parent\root1\folder1\test.bms";
        //    mock.AddFile(file1Path, new MockFileData(File.ReadAllText(@"E:\_source\BmsManager\BmsManager.Tests\_ld2013_a.bms")));
        //    var file2Path = @"D:\parent\root1\folder2\test.bme";
        //    mock.AddFile(file2Path, new MockFileData(File.ReadAllText(@"E:\_source\BmsManager\BmsManager.Tests\_ld2013_a.bms")));
        //    var file3Path = @"D:\parent\root2\child\folder3\test.bml";
        //    mock.AddFile(file3Path, new MockFileData(File.ReadAllText(@"E:\_source\BmsManager\BmsManager.Tests\_ld2013_a.bms")));
        //    var file4Path = @"D:\parent\root2\child\folder4\test.pms";
        //    mock.AddFile(file4Path, new MockFileData(File.ReadAllText(@"E:\_source\BmsManager\BmsManager.Tests\_ld2013_a.bms")));

        //    var root = new RootDirectory { Path = @"D:\parent" };
        //    root.LoadFromFileSystem();

        //    var root1 = root.Children.FirstOrDefault(r => r.Path == root1Path);
        //    if (root1 == default) Assert.Fail();
        //    var folder1 = root1.Folders.FirstOrDefault(f => f.Path == folder1Path);
        //    if (folder1 == default) Assert.Fail();
        //    folder1.Files.Any(f => f.Path == file1Path).IsTrue();
        //    var folder2 = root1.Folders.FirstOrDefault(f => f.Path == folder2Path);
        //    if (folder2 == default) Assert.Fail();
        //    folder2.Files.Any(f => f.Path == file2Path).IsTrue();
        //    var root2 = root.Children.FirstOrDefault(r => r.Path == root2Path);
        //    root2.Children.Any(r => r.Path == notRootPath).IsFalse();
        //    var child = root2.Children.FirstOrDefault(r => r.Path == childPath);
        //    if (child == default) Assert.Fail();
        //    var folder3 = child.Folders.FirstOrDefault(f => f.Path == folder3Path);
        //    if (folder3 == default) Assert.Fail();
        //    folder3.Files.Any(f => f.Path == file3Path).IsTrue();
        //    var folder4 = child.Folders.FirstOrDefault(f => f.Path == folder4Path);
        //    folder4.Files.Any(f => f.Path == file4Path).IsTrue();
        //}

        //[TestMethod]
        //public void Descendants()
        //{
        //    var root = new RootDirectory { Path = @"root" };
        //    var child1 = new RootDirectory { Path = @"child1" };
        //    var child2 = new RootDirectory { Path = @"child2" };
        //    var son1 = new RootDirectory { Path = @"son1" };
        //    var son2 = new RootDirectory { Path = @"son2" };
        //    var son3 = new RootDirectory { Path = @"son3" };
        //    root.Children = new[] { child1, child2 };
        //    child1.Children = new[] { son1, son2 };
        //    child2.Children = new[] { son3 };
        //    AssertUtil.ArePropertyValueEqualAll(new[] { root, child1, child2, son1, son2, son3 }, root.Descendants().ToArray());
        //}
    }
}
