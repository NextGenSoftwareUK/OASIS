using Ipfs.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

using Newtonsoft.Json;
using System.Collections.Generic;

namespace Ipfs.Http
{
    
    [TestClass]
    public class FileSystemNodeTest
    {
        [TestMethod]
        public async Task Serialization()
        {
            var ipfs = TestFixture.Ipfs;
       /*     var a = await ipfs.FileSystem.AddTextAsync("hello world");
            Assert.AreEqual("Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD", (string)a.Id);
            string s = "Qmf412jQZiuVUtdgnB36FXFX7xg5V6KEbSJ4dpQuhkLyfD";
            var text = await ipfs.FileSystem.ReadAllTextAsync((Cid)s);
       */
            List<test> tests = new List<test>();
            test t = new test();
            t.name = "aaa";
            t.age = 24;

            tests.Add(t);
            t = new test();
            t.name = "bbb";
            t.age = 99;
            tests.Add(t);

            var json = JsonConvert.SerializeObject(tests);

            var output = await ipfs.FileSystem.AddTextAsync(json);

            var text1 = await ipfs.FileSystem.ReadAllTextAsync((Cid)output.Id);
            /*
            var b = await ipfs.FileSystem.ListFileAsync(a.Id); 
            var json1 = JsonConvert.SerializeObject(b);
            var c = JsonConvert.DeserializeObject<FileSystemNode>(json);
            Assert.AreEqual(b.Id, c.Id);
            Assert.AreEqual(b.IsDirectory, c.IsDirectory);
            Assert.AreEqual(b.Size, c.Size);
            CollectionAssert.AreEqual(b.Links.ToArray(), c.Links.ToArray());
            */
        }
    }
}
