using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class CommittedOwnerReplicationPolicyTests
{
    private sealed class Receiver : ICommittedHolonReplicaStorageProvider
    {
        public int Calls;
        public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request) => throw new NotSupportedException();
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request, CancellationToken token = default)
        {
            Calls++;
            foreach (var node in HolonGraphWritePlan.Create(request.Roots, request.SaveChildren, request.Recursive, request.MaxChildDepth, 0).Nodes)
                CosmicConcurrency.SetProviderRevisionId(node.Source, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>>(request.Roots));
        }
    }

    [DataTestMethod]
    [DataRow("owner", true)]
    [DataRow("emptyActor", false)]
    [DataRow("foreignRoot", false)]
    [DataRow("foreignChild", false)]
    [DataRow("unselectedChild", true)]
    [DataRow("depthExcludedGrandchild", true)]
    public async Task DefaultPolicyValidatesOnlyTheCompleteSelectedCommittedGraph(string scenario, bool allowed)
    {
        var actor = Guid.NewGuid();
        Holon Record(Guid owner) => new() { Id = Guid.NewGuid(), CreatedByAvatarId = owner, Version = 1, VersionId = Guid.NewGuid() };
        var root = Record(scenario == "foreignRoot" ? Guid.NewGuid() : actor);
        var child = Record(scenario is "foreignChild" or "unselectedChild" ? Guid.NewGuid() : actor);
        child.ParentHolonId = root.Id;
        root.Children.Add(child);
        if (scenario == "depthExcludedGrandchild")
            child.Children.Add(new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(), ParentHolonId = child.Id, CreatedByAvatarId = Guid.NewGuid() });
        foreach (var node in HolonGraphWritePlan.Create(new[] { root }, true, true, 0, 0).Nodes)
            CosmicConcurrency.SetProviderRevisionId(node.Source, ProviderType.Neo4jOASIS, Guid.NewGuid());
        var mutation = new CommittedHolonReplicaRequest(Guid.NewGuid(), "source", ProviderType.Neo4jOASIS,
            scenario == "emptyActor" ? Guid.Empty : actor, ConditionalHolonOperation.Save, new[] { root },
            saveChildren: scenario != "unselectedChild", maxChildDepth: scenario == "depthExcludedGrandchild" ? 1 : 0);
        var receiver = new Receiver();
        var target = new CommittedHolonReplicaTarget("target", receiver, ProviderType.SQLLiteDBOASIS,
            "source", ProviderType.Neo4jOASIS, new CommittedOwnerReplicationPolicy(), "server-context", Guid.NewGuid());
        var result = await target.ApplyReplicatedMutationAsync(CommittedHolonReplicaTarget.CaptureItem(mutation), default);
        Assert.AreEqual(allowed, !result.IsError && result.Result);
        Assert.AreEqual(allowed ? 1 : 0, receiver.Calls);
        if (!allowed) Assert.AreEqual("COSMIC_REPLICA_AUTHORIZATION_DENIED", result.ErrorCode);
    }
}
