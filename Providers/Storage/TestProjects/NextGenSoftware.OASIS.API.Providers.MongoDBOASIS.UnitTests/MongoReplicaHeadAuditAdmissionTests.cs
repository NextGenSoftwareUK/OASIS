using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Mongo = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoReplicaHeadAuditAdmissionTests
{
    private static HolonReplicaHeadReadRequest Request() => new(
        new CosmicStorageIdentity(ProviderType.MongoDBOASIS, new string('A', 64), Guid.NewGuid()), new[] { Guid.NewGuid() });
    [TestMethod]
    public async Task InactiveAuditCannotActivateOrInitializeAStore()
    {
        var provider = new Mongo("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1", "not-created");
        var result = await provider.ReadHolonReplicaHeadsAsync(Request());
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual("COSMIC_REPLICA_HEAD_AUDIT_NOT_READY", result.ErrorCode);
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [TestMethod]
    public async Task CancelledAuditDoesNotActivateOrInitializeAStore()
    {
        var provider = new Mongo("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1", "not-created");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => provider.ReadHolonReplicaHeadsAsync(Request(), cancellation.Token));
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
