using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class NativeStorageCorrelationTests
{
    [TestMethod]
    public void InvalidPrefixRejectsBeforeAnOperationCanStart()
    {
        foreach(var prefix in new[]{null,""," ",new string('x',110),new string('x',128)})
            Assert.ThrowsException<ArgumentException>(()=>new NativeStorageCorrelation(prefix));
    }
    [TestMethod]
    public void LargestPhysicalSequenceFitsAndExhaustionNeverWrapsToAReusedIdentity()
    {
        var correlation=new NativeStorageCorrelation(new string('x',NativeStorageCorrelation.MaxPrefixLength));
        typeof(NativeStorageCorrelation).GetField("_sequence",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(correlation,(long)int.MaxValue-1);
        var last=correlation.Next();Assert.AreEqual(128,last.Length);StringAssert.EndsWith(last,".storage.2147483647");
        Assert.ThrowsException<InvalidOperationException>(()=>correlation.Next());
        Assert.ThrowsException<InvalidOperationException>(()=>correlation.Next());
    }
    [TestMethod]
    public void ConcurrentWritesHaveUniqueStableCorrelations()
    {
        var correlation=new NativeStorageCorrelation("owned");var ids=new ConcurrentBag<string>();
        Parallel.For(0,1000,_=>ids.Add(correlation.Next()));
        Assert.AreEqual(1000,ids.Distinct().Count());
        CollectionAssert.AreEquivalent(Enumerable.Range(1,1000).Select(index=>"owned.storage."+index).ToArray(),ids.ToArray());
    }
}
