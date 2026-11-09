using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using INode=Neo4j.Driver.INode;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [TestMethod]
    public async Task ReconcileSourceBindingReplayFixture()
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        Assert.AreEqual("2026-10-28",Environment.GetEnvironmentVariable("OASIS_SOURCE_BINDING_RECONCILE"));
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null && !File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Run-ReusableNeo4jTests.ps1")))root=root.Parent;
        Assert.IsNotNull(root);
        var journal=Path.Combine(root.FullName,"LocalState/COSMIC/OwnedRoutingCurrent/neo-source-binding-replay.pending.json");
        using var recorded=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
        var originalReport=recorded.RootElement.GetProperty("report").GetString()!;
        Assert.AreEqual(recorded.RootElement.GetProperty("sha256").GetString(),
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(originalReport))));
        var start=DateTimeOffset.Parse(recorded.RootElement.GetProperty("start").GetString()!).UtcDateTime;
        var end=DateTimeOffset.Parse(recorded.RootElement.GetProperty("end").GetString()!).UtcDateTime;
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspect=driver.AsyncSession();
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var identity=await provider.CaptureStorageIdentityAsync();Assert.IsFalse(identity.IsError);
            var candidates=await(await inspect.RunAsync("MATCH (o:CosmicHolonOutbox) WHERE o.SourceId STARTS WITH 'owned-neo-source-' RETURN o ORDER BY o.Sequence")).ToListAsync();
            var owned=new List<(Guid Operation,Guid[] Ids)>();
            foreach(var group in candidates.GroupBy(row=>row["o"].As<INode>().Properties["OperationId"].As<string>()))
            {
                var first=group.First()["o"].As<INode>();var payload=first.Properties["PayloadJson"].As<string>();
                var frozen=CommittedHolonReplicaPayload.Read(payload);var top=frozen.Roots.Single();
                if(top.CreatedDate<start || top.CreatedDate>end)continue;
                Assert.AreEqual(2,group.Count());Assert.AreEqual("owned primary outbox root",top.Name);Assert.AreEqual(1,top.Version);
                var child=top.Children.Single();Assert.AreEqual("owned primary outbox child",child.Name);Assert.AreEqual(top.Id,child.ParentHolonId);
                var operation=frozen.OperationId;var targets=new[]{"owned-a-"+operation,"owned-b-"+operation};
                CollectionAssert.AreEquivalent(targets,group.Select(row=>row["o"].As<INode>().Properties["TargetId"].As<string>()).ToArray());
                foreach(var row in group){var value=row["o"].As<INode>();Assert.AreEqual("Pending",value.Properties["DeliveryState"].As<string>());Assert.AreEqual(0L,value.Properties["AttemptCount"].As<long>());}
                var input=new Holon{Id=top.Id,Name=top.Name,Children=new List<IHolon>{new Holon{Id=child.Id,Name=child.Name,ParentHolonId=top.Id}}};
                var enrollment=new HolonReplicationEnrollment(frozen.SourceId,targets,identity.Result);
                var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,frozen.ActorId,ProviderType.Neo4jOASIS,
                    new[]{new HolonWriteCondition(top.Id,null),new HolonWriteCondition(child.Id,null)},enrollment),ConditionalHolonOperation.Save,new[]{input});
                var verified=await provider.ReadConditionalHolonWriteOutcomeAsync(request);
                Assert.IsFalse(verified.IsError,verified.Message+verified.Exception);Assert.AreEqual(ConditionalHolonReceiptState.Committed,verified.Result.ReceiptState);
                var live=await(await inspect.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN h",new{ids=new[]{top.Id.ToString("D"),child.Id.ToString("D")}})).ToListAsync();
                Assert.AreEqual(2,live.Count);
                foreach(var current in live)
                {
                    var snapshot=OasisJson.DeserializeStored<Holon>(current["h"].As<INode>().Properties["SnapshotJson"].As<string>());
                    var expected=snapshot.Id==top.Id?top:child;
                    Assert.AreEqual(expected.Id,snapshot.Id);Assert.AreEqual(expected.VersionId,snapshot.VersionId);
                    Assert.AreEqual(expected.CreatedByAvatarId,snapshot.CreatedByAvatarId);
                }
                owned.Add((operation,new[]{top.Id,child.Id}));
            }
            Assert.AreEqual(2,owned.Count,"Only the two timestamp-bound failed cases may be removed.");
            var operations=owned.Select(item=>item.Operation.ToString("D")).ToArray();var ids=owned.SelectMany(item=>item.Ids).Select(id=>id.ToString("D")).ToArray();
            var evidence=Path.Combine(root.FullName,"TestResults/Neo4jCurrent/source-binding-replay-reconciliation.json");
            await File.WriteAllTextAsync(evidence,JsonSerializer.Serialize(new{operations,ids,source=identity.Result,receiptState="Committed",originalReport=recorded.RootElement.Clone()}));
            await(await inspect.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids})).ConsumeAsync();
            await(await inspect.RunAsync("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId IN $operations DETACH DELETE r",new{operations})).ConsumeAsync();
            var remaining=await(await inspect.RunAsync("MATCH (r) WHERE (r:Holon AND r.Id IN $ids) OR ((r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId IN $operations) RETURN count(r) AS count",new{ids,operations})).SingleAsync();
            Assert.AreEqual(0L,remaining["count"].As<long>());
        }
        finally{Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);}
        File.Delete(journal);
    }
}
