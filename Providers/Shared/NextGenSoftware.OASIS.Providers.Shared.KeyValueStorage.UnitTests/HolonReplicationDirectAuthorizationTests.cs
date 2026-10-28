using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

public partial class HolonReplicationHostingTests
{
    [TestMethod]
    public async Task DirectReceiverUsesExistingActorGrantExpiryAndOriginalRouteIdentity()
    {
        var clock = new Clock();
        var receiver = new Receiver();
        var policy = new HolonReplicationActorGrant(new[] { Actor }, Now.AddMinutes(1), clock);
        const string route = "hyperdrive-target:MongoDBOASIS";
        var target = new CommittedHolonReplicaTarget(route, receiver, ProviderType.MongoDBOASIS,
            "source", ProviderType.SQLLiteDBOASIS, policy, "server-owned-context", Guid.NewGuid());

        Assert.AreEqual(route, target.ReplicationTargetId);
        Assert.IsTrue((await target.ApplyReplicatedMutationAsync(Item(), default)).Result);
        Assert.AreEqual("COSMIC_REPLICA_AUTHORIZATION_DENIED",
            (await target.ApplyReplicatedMutationAsync(Item(actor: Guid.NewGuid()), default)).ErrorCode);
        clock.Value = Now.AddMinutes(1);
        Assert.AreEqual("COSMIC_REPLICA_AUTHORIZATION_DENIED",
            (await target.ApplyReplicatedMutationAsync(Item(), default)).ErrorCode);
        Assert.AreEqual(1, receiver.Calls);
    }

    [TestMethod]
    public async Task ExplicitCapturedPolicyBindingCannotBeReplacedDuringConstruction()
    {
        var receiver = new Receiver();
        var policy = new Policy { BindingIdentity = "replacement" };
        var target = new CommittedHolonReplicaTarget("target", receiver, ProviderType.MongoDBOASIS,
            "source", ProviderType.SQLLiteDBOASIS, policy, "catalog", Guid.NewGuid(), "original");
        var denied = await target.ApplyReplicatedMutationAsync(Item(), default);
        Assert.AreEqual("COSMIC_REPLICA_AUTHORIZATION_FAILED", denied.ErrorCode);
        Assert.AreEqual(0, policy.Calls);
        Assert.AreEqual(0, receiver.Calls);
    }

    [TestMethod]
    public async Task DirectPolicyCannotRewriteTheAuthorizedTransportOrGraph()
    {
        var receiver = new Receiver();
        var item = Item();
        var operation = item.OperationId;
        var policy = new Policy { Apply = (context, _) =>
        {
            context.Mutation.Roots[0].Name = "policy-rewritten graph";
            item.AvatarId = Guid.NewGuid();
            item.PayloadJson = Item(actor: item.AvatarId).PayloadJson;
            return Task.FromResult(new OASISResult<bool>(true));
        } };
        var target = new CommittedHolonReplicaTarget("target", receiver, ProviderType.MongoDBOASIS,
            "source", ProviderType.SQLLiteDBOASIS, policy, "catalog", Guid.NewGuid());
        var applied = await target.ApplyReplicatedMutationAsync(item, default);
        Assert.IsFalse(applied.IsError, applied.Message);
        Assert.IsTrue(applied.Result);
        Assert.AreEqual("frozen original", receiver.Name);
        Assert.AreEqual(Actor, receiver.ActorId);
        Assert.AreEqual(operation, receiver.OperationId);
    }
}
