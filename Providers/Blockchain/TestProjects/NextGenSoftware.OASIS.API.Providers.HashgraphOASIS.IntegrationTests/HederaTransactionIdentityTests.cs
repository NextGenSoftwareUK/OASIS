using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaTransactionIdentityTests
{
    [TestMethod]
    public void ValidStartIsBackdatedAndUniqueWithoutRegeneration()
    {
        var type=typeof(HashgraphOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.HederaSdkClient")!;
        var method=type.GetMethod("CreateTransactionId",BindingFlags.Static|BindingFlags.NonPublic)!;
        var before=DateTimeOffset.UtcNow;
        var ids=Enumerable.Range(0,100).Select(_=>(string)method.Invoke(null,new object[] {"0.0.1001"})!).ToArray();
        Assert.AreEqual(100,ids.Distinct().Count());
        foreach(var id in ids)
        {
            var parts=id.Split('@')[1].Split('.');
            var start=DateTimeOffset.FromUnixTimeSeconds(long.Parse(parts[0])).AddTicks(long.Parse(parts[1])/100);
            Assert.IsTrue(start>=before.AddSeconds(-9));
            Assert.IsTrue(start<=DateTimeOffset.UtcNow.AddSeconds(-7));
        }
    }
}
