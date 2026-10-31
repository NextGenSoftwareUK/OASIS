using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public class PolkadotPreparationPolicyTests
{
    [DataTestMethod][DataRow(0)][DataRow(5)]
    public void FeeEstimateCannotSatisfyAnAdmittedAllFeesMaximum(int budget)
    {
        // Controlled admission-policy guard only; no actual outbox/authority/network is invoked.
        var constructor=typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
            .Single(candidate=>candidate.GetParameters().Length==5);
        var operation=new UsageOutboxOperation{Policy=new UsageEndpointPolicy{MaximumNativeFeeCostUsd=budget}};
        var execution=(UsageExecutionHandle)constructor.Invoke(new object?[]{operation,null,null,null,null});
        using var provider=new PolkadotOASIS("http://127.0.0.1:1");
        typeof(PolkadotOASIS).GetField("_isActivated",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,true);
        typeof(PolkadotOASIS).GetProperty("ReportedGenesisHash")!.SetValue(provider,"0x"+new string('a',64));
        var rejected=Assert.ThrowsException<UsageProtocolException>(()=>provider.CreateNativePreparationServer(execution,"owned-original","owned-sender","0x"+new string('b',64)));
        Assert.AreEqual("POLKADOT_VERIFIED_FEE_MAXIMUM_REQUIRED",rejected.Code);
        StringAssert.Contains(rejected.Message,"not a signed all-fees ceiling");
    }

    [TestMethod]
    public void MissingExecutionOwnerFailsBeforePreparationTransport()
    {
        using var provider=new PolkadotOASIS("http://127.0.0.1:1");
        Assert.ThrowsException<ArgumentNullException>(()=>provider.CreateNativePreparationServer(null!,"owned-original","owned-sender","0x"+new string('b',64)));
    }
}
