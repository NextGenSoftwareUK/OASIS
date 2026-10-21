using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using INode = Neo4j.Driver.INode;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeCosmicPrimaryOutboxSharesGraphReceiptAndSequenceTransaction(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=driver.AsyncSession();
        var root=new Holon {Id=Guid.NewGuid(),Name="owned primary outbox root"};
        var child=new Holon {Id=Guid.NewGuid(),Name="owned primary outbox child",ParentHolonId=root.Id};
        root.Children=new List<IHolon>{child};
        var operation=Guid.NewGuid();var actor=Guid.NewGuid();var source="owned-neo-source-"+operation;
        var targetA="owned-a-"+operation;var targetB="owned-b-"+operation;
        var enrollment=new HolonReplicationEnrollment(source,new[]{targetB,targetA});
        var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.Neo4jOASIS,
            new[]{new HolonWriteCondition(root.Id,null),new HolonWriteCondition(child.Id,null)}),
            ConditionalHolonOperation.Save,new[]{root});
        Task<OASISResult<IEnumerable<IHolon>>> Write(HolonReplicationEnrollment e)
            =>synchronous ? Task.Run(()=>provider.ExecuteReplicatingHolonWrite(request,e))
                :provider.ExecuteReplicatingHolonWriteAsync(request,e);
        async Task<long> Count(string query,object args)=>(await(await inspection.RunAsync(query,args)).SingleAsync())["count"].As<long>();
        var complete=false;
        try
        {
            var activation=await provider.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Message+activation.Exception);
            // An owned conflicting second obligation forces rollback after the first obligation was inserted.
            await(await inspection.RunAsync("CREATE (:CosmicHolonOutbox {OperationId:$op,TargetId:$target,Sequence:-1,OwnedFixture:true})",
                new{op=operation.ToString("D"),target=targetB})).ConsumeAsync();
            var before=await(await inspection.RunAsync("OPTIONAL MATCH (s:CosmicHolonOutboxSequence {Name:'primary-holon-outbox-v1'}) RETURN s.Value AS value")).SingleAsync();
            var failed=await Write(enrollment);Assert.IsTrue(failed.IsError,failed.Message);
            Assert.AreEqual(0L,await Count("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS count",new{ids=new[]{root.Id.ToString(),child.Id.ToString()}}));
            Assert.AreEqual(0L,await Count("MATCH (r:CosmicWriteReceipt {OperationId:$op}) RETURN count(r) AS count",new{op=operation.ToString("D")}));
            Assert.AreEqual(1L,await Count("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN count(o) AS count",new{op=operation.ToString("D")}));
            var after=await(await inspection.RunAsync("OPTIONAL MATCH (s:CosmicHolonOutboxSequence {Name:'primary-holon-outbox-v1'}) RETURN s.Value AS value")).SingleAsync();
            Assert.AreEqual(before["value"],after["value"]);
            Assert.AreEqual(0,root.Version);Assert.AreEqual(0,child.Version);
            await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$op,OwnedFixture:true}) DELETE o",new{op=operation.ToString("D")})).ConsumeAsync();
            var saved=await Write(enrollment);Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            var rows=await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN o ORDER BY o.Sequence",new{op=operation.ToString("D")})).ToListAsync();
            Assert.AreEqual(2,rows.Count);var first=rows[0]["o"].As<INode>();var second=rows[1]["o"].As<INode>();
            Assert.AreEqual(first["Sequence"].As<long>()+1,second["Sequence"].As<long>());
            Assert.AreEqual(targetA,first["TargetId"].As<string>());Assert.AreEqual(targetB,second["TargetId"].As<string>());
            var json=first["PayloadJson"].As<string>();Assert.AreEqual(json,second["PayloadJson"].As<string>());
            Assert.AreEqual(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json))),first["PayloadDigest"].As<string>());
            var frozen=CommittedHolonReplicaPayload.Read(json);Assert.AreEqual(operation,frozen.OperationId);
            Assert.AreEqual(saved.Result.Single().VersionId,frozen.Roots.Single().VersionId);
            Assert.AreEqual(child.Id,frozen.Roots.Single().Children.Single().Id);
            Assert.AreEqual("Pending",first["DeliveryState"].As<string>());
            await provider.DeActivateProviderAsync();provider=new Provider(uri,"neo4j","oasis-local-test-only");
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var replay=await Write(new HolonReplicationEnrollment(source,new[]{targetA,targetB}));
            Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);
            Assert.AreEqual(saved.Result.Single().VersionId,replay.Result.Single().VersionId);
            Assert.AreEqual(2L,await Count("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN count(o) AS count",new{op=operation.ToString("D")}));
            var replayCounter=await(await inspection.RunAsync("MATCH (s:CosmicHolonOutboxSequence {Name:'primary-holon-outbox-v1'}) RETURN s.Value AS value")).SingleAsync();
            Assert.AreEqual(second["Sequence"].As<long>(),replayCounter["value"].As<long>(),"Receipt replay must not allocate another sequence range.");
            var changed=await Write(new HolonReplicationEnrollment(source,new[]{targetA}));Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",changed.ErrorCode);
            var plain=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",plain.ErrorCode);
            complete=true;
        }
        finally
        {
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=new[]{root.Id.ToString(),child.Id.ToString()}})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId=$op DETACH DELETE r",new{op=operation.ToString("D")})).ConsumeAsync();
            }
            await provider.DeActivateProviderAsync();
        }
    }
}
