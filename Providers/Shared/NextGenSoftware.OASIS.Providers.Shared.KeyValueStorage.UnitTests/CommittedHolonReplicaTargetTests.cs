using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class CommittedHolonReplicaTargetTests
{
    private static HostedSyncFanOutItem Item()
    {
        var child = Record(); var root = Record(); child.ParentHolonId = root.Id;
        root.Children = new List<IHolon> { child };
        return CommittedHolonReplicaTarget.CaptureItem(new CommittedHolonReplicaRequest(Guid.NewGuid(), "source",
            ProviderType.SQLLiteDBOASIS, Guid.NewGuid(), ConditionalHolonOperation.Save, new[] { root }));
    }
    private static Holon Record()
    {
        var row = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(), Name = "source value" };
        CosmicConcurrency.SetProviderRevisionId(row, ProviderType.SQLLiteDBOASIS, Guid.NewGuid()); return row;
    }
    private static CommittedHolonReplicaTarget Target(Receiver receiver, string source = "source")
        => new("mongo", receiver, ProviderType.MongoDBOASIS, source, ProviderType.SQLLiteDBOASIS);

    [TestMethod]
    public async Task FanOutDispatchesFrozenGraphToExplicitReceiverAndValidatesAcknowledgement()
    {
        var receiver = new Receiver(); var item = Item();
        var result = await new OASISProviderFanOutHandler(new[] { Target(receiver) }).ApplyAsync(item, default);
        Assert.IsFalse(result.IsError); Assert.IsTrue(result.Result); Assert.AreEqual(1, receiver.Calls);
        Assert.AreEqual(1, receiver.Received.Roots[0].Children.Count());
    }

    [DataTestMethod]
    [DataRow("operation")] [DataRow("actor")] [DataRow("root")] [DataRow("version")]
    [DataRow("kind")] [DataRow("type")] [DataRow("source")]
    public async Task RoutingOrSourceMismatchNeverDispatches(string field)
    {
        var receiver = new Receiver(); var item = Item();
        switch (field)
        {
            case "operation": item.OperationId = Guid.NewGuid(); break;
            case "actor": item.AvatarId = Guid.NewGuid(); break;
            case "root": item.EntityId = Guid.NewGuid(); break;
            case "version": item.VersionId = Guid.NewGuid(); break;
            case "kind": item.Kind = SyncOperationKind.Delete; break;
            case "type": item.EntityType = "other"; break;
        }
        var result = await Target(receiver, field == "source" ? "different" : "source")
            .ApplyReplicatedMutationAsync(item, default);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.Result); Assert.AreEqual(0, receiver.Calls);
    }

    [DataTestMethod]
    [DataRow("business")] [DataRow("edge")] [DataRow("revision")] [DataRow("null")]
    public async Task FalseAcknowledgementCannotCompleteDelivery(string fault)
    {
        var receiver = new Receiver { Fault = fault };
        var result = await Target(receiver).ApplyReplicatedMutationAsync(Item(), default);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.Result);
        Assert.AreEqual("COSMIC_REPLICA_ACKNOWLEDGEMENT_INVALID", result.ErrorCode);
    }

    [TestMethod]
    public async Task ProviderRejectionRetainsDiagnosticsAndDoesNotAcknowledge()
    {
        var cause = new InvalidOperationException("native conflict");
        var receiver = new Receiver { Rejection = new OASISResult<IEnumerable<IHolon>> {
            IsError = true, ErrorCode = "COSMIC_REPLICA_LINEAGE_CONFLICT", ErrorCount = 1, Exception = cause,
            MetaData = new Dictionary<string, string> { ["receipt"] = "original" } } };
        var result = await Target(receiver).ApplyReplicatedMutationAsync(Item(), default);
        Assert.IsTrue(result.IsError); Assert.AreSame(cause, result.Exception);
        Assert.AreEqual("original", result.MetaData["receipt"]);
        Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT", result.ErrorCode);
    }

    [TestMethod]
    public async Task CancelledDispatchDoesNotReachProvider()
    {
        var receiver = new Receiver(); using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => Target(receiver)
            .ApplyReplicatedMutationAsync(Item(), cancellation.Token));
        Assert.AreEqual(0, receiver.Calls);
    }

    [DataTestMethod]
    [DataRow("SaveChildren")] [DataRow("SourceAuthority")] [DataRow("MaxChildDepth")]
    public async Task CoercibleWireScalarsNeverReachTheProvider(string field)
    {
        var item=Item();var document=JObject.Parse(item.PayloadJson);
        document[field]=document[field].ToString();item.PayloadJson=document.ToString();
        var receiver=new Receiver();var result=await Target(receiver).ApplyReplicatedMutationAsync(item,default);
        Assert.IsTrue(result.IsError);Assert.AreEqual("COSMIC_REPLICA_PAYLOAD_INVALID",result.ErrorCode);
        Assert.AreEqual(0,receiver.Calls);
    }

    private sealed class Receiver : ICommittedHolonReplicaStorageProvider
    {
        public int Calls; public string Fault; public CommittedHolonReplicaRequest Received;
        public OASISResult<IEnumerable<IHolon>> Rejection;
        public OASISResult<IEnumerable<IHolon>> ApplyCommittedHolonReplica(CommittedHolonReplicaRequest request)
            => throw new NotSupportedException();
        public Task<OASISResult<IEnumerable<IHolon>>> ApplyCommittedHolonReplicaAsync(
            CommittedHolonReplicaRequest request, CancellationToken cancellationToken = default)
        {
            Calls++; Received = request;
            if (Rejection != null) return Task.FromResult(Rejection);
            var graph = HolonGraphWritePlan.Create(request.Roots, true, true, 0, 0);
            foreach (var node in graph.Nodes)
                CosmicConcurrency.SetProviderRevisionId(node.Source, ProviderType.MongoDBOASIS, Guid.NewGuid());
            if (Fault == "business") request.Roots[0].Name = "corrupted";
            if (Fault == "edge") request.Roots[0].Children.Clear();
            if (Fault == "revision") CosmicConcurrency.SetProviderRevisionId(request.Roots[0], ProviderType.MongoDBOASIS, Guid.Empty);
            return Task.FromResult(new OASISResult<IEnumerable<IHolon>>(Fault == "null" ? null : request.Roots));
        }
    }
}
