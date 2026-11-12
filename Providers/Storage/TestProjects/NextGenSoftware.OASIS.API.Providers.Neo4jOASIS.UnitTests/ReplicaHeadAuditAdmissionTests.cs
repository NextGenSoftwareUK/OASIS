using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public sealed class ReplicaHeadAuditAdmissionTests
{
    private static HolonReplicaHeadReadRequest Request(HolonReplicaHeadReadConsistency consistency)
        => new(new CosmicStorageIdentity(ProviderType.Neo4jOASIS,new string('A',64),Guid.NewGuid()),
            new[]{Guid.NewGuid()},consistency);
    [TestMethod]
    public async Task StrictAuditDoesNotActivateADriverOrSilentlyDowngrade()
    {
        var provider = new Provider("bolt://127.0.0.1:1","unused","unused");
        var result = await provider.ReadHolonReplicaHeadsAsync(Request(HolonReplicaHeadReadConsistency.NativeSnapshot));
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_CONSISTENCY_UNSUPPORTED",result.ErrorCode);
    }
    [TestMethod]
    public async Task WeakAuditStillRequiresAnExistingActivatedDriver()
    {
        var provider = new Provider("bolt://127.0.0.1:1","unused","unused");
        var result = await provider.ReadHolonReplicaHeadsAsync(Request(HolonReplicaHeadReadConsistency.ReadCommittedValidated));
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_NOT_READY",result.ErrorCode);
    }
    [TestMethod]
    public async Task CancelledAuditCannotOpenADriver()
    {
        var provider = new Provider("bolt://127.0.0.1:1","unused","unused");
        using var canceled = new CancellationTokenSource();canceled.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>provider.ReadHolonReplicaHeadsAsync(
            Request(HolonReplicaHeadReadConsistency.ReadCommittedValidated),canceled.Token));
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
