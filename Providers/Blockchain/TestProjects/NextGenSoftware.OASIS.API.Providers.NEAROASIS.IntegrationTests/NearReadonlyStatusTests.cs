using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class NearReadonlyStatusTests
{
    [TestMethod]
    public async Task RetainedUnavailableOriginalRemainsErrorAndPending()
    {
        var endpoint=Environment.GetEnvironmentVariable("NEAROASIS_RPCENDPOINT");
        Assert.AreEqual("http://127.0.0.1:3030",endpoint,"Only the retained local node is authorized by this fixture.");
        var provider=new NEAROASIS(endpoint!,"oasis-near-local","oasis-near-local","test.near","test.near","");
        var watch=Stopwatch.StartNew();
        var result=await provider.GetTransactionStatusAsync("G8hXdSqWJXTVa9pr2VeztYZv7kEeJaXei9w7kmBHogWS");
        Assert.IsTrue(result.IsError,"This retained unavailable original must not be invented as completed or failed.");
        Assert.AreEqual(BridgeTransactionStatus.Pending,result.Result);
        Assert.IsNotNull(result.Exception);
        StringAssert.Contains(result.Message,"Unknown transaction G8hXdSqWJXTVa9pr2VeztYZv7kEeJaXei9w7kmBHogWS");
        Assert.IsTrue(watch.Elapsed<TimeSpan.FromSeconds(10),"NONE observation should not wait for finality.");
    }
}
