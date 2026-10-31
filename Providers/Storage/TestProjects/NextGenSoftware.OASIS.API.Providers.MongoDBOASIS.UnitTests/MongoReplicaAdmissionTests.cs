using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public sealed class MongoReplicaAdmissionTests
{
    [DataTestMethod]
    [DataRow("null")] [DataRow("uncommitted")] [DataRow("source-token")] [DataRow("cancelled")]
    public async Task InvalidCommittedReplicaFailsBeforeAnUnactivatedRepositoryIsUsed(string invalid)
    {
        var provider = new MongoDBOASIS("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1", "unactivated-replica-only");
        var root = new Holon { Id = Guid.NewGuid(), Version = invalid == "uncommitted" ? 0 : 1, VersionId = Guid.NewGuid() };
        if (invalid != "source-token") CosmicConcurrency.SetProviderRevisionId(root, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        var request = invalid == "null" ? null : new CommittedHolonReplicaRequest(Guid.NewGuid(), "owned source",
            ProviderType.SQLLiteDBOASIS, Guid.NewGuid(), ConditionalHolonOperation.Save, new[] { root });
        using var cancellation = new CancellationTokenSource(); if (invalid == "cancelled") cancellation.Cancel();
        var result = await provider.ApplyCommittedHolonReplicaAsync(request!, cancellation.Token);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.IsNull(result.Result);
        Assert.AreEqual(invalid == "cancelled" ? "COSMIC_REPLICA_WRITE_CANCELLED" : "COSMIC_REPLICA_WRITE_INVALID", result.ErrorCode);
        Assert.IsNotNull(result.Exception); Assert.IsFalse(result.Exception is NullReferenceException);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
