using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

public partial class HolonReplicationHostingTests
{
    private sealed class BoundReceiver : IBoundCommittedHolonReplicaStorageProvider
    {
        internal readonly Receiver Inner = new();
        internal CosmicStorageIdentity Expected;
        internal int BoundCalls;
        public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
            => throw new AssertFailedException("Bound admission must never downgrade.");
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
            CancellationToken token = default) => throw new AssertFailedException("Bound admission must never downgrade.");
        public OASISResult<IEnumerable<IHolon>> ApplyBoundCommittedHolonReplica(CommittedHolonReplicaRequest request,
            CosmicStorageIdentity expected) => throw new NotSupportedException();
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyBoundCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,
            CosmicStorageIdentity expected, CancellationToken token = default)
        { Expected = expected; BoundCalls++; return Inner.ApplyCommittedHolonReplicaAsync(request, token); }
    }

    [TestMethod]
    public async Task BoundRoutePassesCapturedIdentityOnlyThroughTransactionBoundReceiver()
    {
        var receiver = new BoundReceiver();
        var identity = new CosmicStorageIdentity(ProviderType.MongoDBOASIS, new string('A', 64), Guid.NewGuid());
        var policy = new Policy();
        var target = new CommittedHolonReplicaTarget("bound", receiver, identity.Authority, "source",
            ProviderType.SQLLiteDBOASIS, policy, "catalog", Guid.NewGuid(), policy.BindingIdentity, identity);
        Assert.IsTrue((await target.ApplyReplicatedMutationAsync(Item(), default)).Result);
        Assert.AreSame(identity, receiver.Expected);
        Assert.AreEqual(1, receiver.BoundCalls);
        policy.BindingIdentity = "changed";
        Assert.AreEqual("COSMIC_REPLICA_AUTHORIZATION_FAILED", (await target.ApplyReplicatedMutationAsync(Item(), default)).ErrorCode);
        Assert.AreEqual(1, receiver.BoundCalls);
    }

    [TestMethod]
    public void BoundRouteRequiresMatchingNativeCapabilityAndChangesIdentityWithStore()
    {
        var identity = new CosmicStorageIdentity(ProviderType.MongoDBOASIS, new string('A', 64), Guid.NewGuid());
        var policy = new Policy();
        Assert.ThrowsException<ArgumentException>(() => new CommittedHolonReplicaTarget("bound", new Receiver(), identity.Authority,
            "source", ProviderType.SQLLiteDBOASIS, policy, "catalog", Guid.NewGuid(), null, identity));
        var receiver = new BoundReceiver();
        Assert.ThrowsException<ArgumentException>(() => new CommittedHolonReplicaTarget("bound", receiver, ProviderType.Neo4jOASIS,
            "source", ProviderType.SQLLiteDBOASIS, policy, "catalog", Guid.NewGuid(), null, identity));
        var version = Guid.NewGuid();
        HolonReplicationCatalog Catalog(CosmicStorageIdentity expected) => new("catalog", version, "source", "trusted-source",
            ProviderType.SQLLiteDBOASIS, new Store(), new[] { new HolonReplicationDestination("target", expected, receiver, policy) });
        var original = Catalog(identity);
        Assert.AreEqual(original.TargetIds[0], Catalog(identity).TargetIds[0]);
        var unbound = new HolonReplicationCatalog("catalog", version, "source", "trusted-source",
            ProviderType.SQLLiteDBOASIS, new Store(), new[] { new HolonReplicationDestination("target",
                identity.BindingFingerprint, identity.Authority, receiver, policy) });
        Assert.AreNotEqual(original.TargetIds[0], unbound.TargetIds[0],
            "A string descriptor must not impersonate the transaction-bound route contract.");
        Assert.AreNotEqual(original.TargetIds[0], Catalog(new CosmicStorageIdentity(identity.Authority,
            identity.EndpointFingerprint, Guid.NewGuid())).TargetIds[0]);
        Assert.AreNotEqual(original.TargetIds[0], Catalog(new CosmicStorageIdentity(identity.Authority,
            new string('B', 64), identity.StoreId)).TargetIds[0]);
    }
}
