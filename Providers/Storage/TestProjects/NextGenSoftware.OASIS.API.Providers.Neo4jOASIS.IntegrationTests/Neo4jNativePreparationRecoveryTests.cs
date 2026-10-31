using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeCosmicConditionalGraphReceiptsAndRacesPreparationCommitRecovery(bool synchronous,bool batch)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var first=new Provider(uri,"neo4j","oasis-local-test-only");var second=new Provider(uri,"neo4j","oasis-local-test-only");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspection=driver.AsyncSession();
        var operation=Guid.NewGuid();var actor=Guid.NewGuid();var fixture="owned-preparation-recovery-"+synchronous+"-"+batch;
        var ids=new List<string>();var complete=false;var commits=0;
        var previous=await(await inspection.RunAsync("MATCH (f:CosmicPreparationFixture {Name:$fixture}) RETURN count(f) AS count",new{fixture})).SingleAsync();
        Assert.AreEqual(0L,previous["count"].As<long>(),"Reconcile retained recovery fixture before rerunning.");
        await(await inspection.RunAsync("CREATE (f:CosmicPreparationFixture {Name:$fixture,OperationId:$operation,ActorId:$actor})",
            new{fixture,operation=operation.ToString("D"),actor=actor.ToString("D")})).ConsumeAsync();
        Holon[] Input()=>Enumerable.Range(0,batch?2:1).Select(index=>new Holon{Name="owned uncertain preparation root "+index,
            MetaData=new(){["private-note"]="root secret",["search-tag"]="coffee"},Children=new List<IHolon>{new Holon{
                Name="owned uncertain preparation child "+index,MetadataEncryptionOverride=PreparationPolicy(new PreparationKeys(),"key-b","owned-neo-child"),
                MetaData=new(){["private-note"]=1.000000000000000000000000001m}}}}).ToArray();
        async Task<OASISResult<IEnumerable<IHolon>>> Save(HolonManager manager,Holon[] roots)
        {
            if(batch)return synchronous?manager.SaveHolons(roots.Cast<IHolon>(),actor,new CosmicWriteOptions(operation),providerType:ProviderType.Neo4jOASIS)
                :await manager.SaveHolonsAsync(roots.Cast<IHolon>(),actor,new CosmicWriteOptions(operation),providerType:ProviderType.Neo4jOASIS);
            var saved=synchronous?manager.SaveHolon(roots[0],actor,new CosmicWriteOptions(operation),providerType:ProviderType.Neo4jOASIS)
                :await manager.SaveHolonAsync(roots[0],actor,new CosmicWriteOptions(operation),providerType:ProviderType.Neo4jOASIS);
            // Preserve the actual single-save outcome; only normalize its successful row container.
            return new OASISResult<IEnumerable<IHolon>>{Result=saved.Result==null?null:new[]{saved.Result},IsError=saved.IsError,
                ErrorCode=saved.ErrorCode,Exception=saved.Exception,Message=saved.Message,IsSaved=saved.IsSaved};
        }
        async Task<Dictionary<string,object>> Receipt()=>new((await(await inspection.RunAsync(
            "MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN properties(r) AS body",new{operation=operation.ToString("D")})).SingleAsync())["body"].As<Dictionary<string,object>>());
        try
        {
            Assert.IsFalse((await first.ActivateProviderAsync()).IsError);Assert.IsFalse((await second.ActivateProviderAsync()).IsError);
            var field=typeof(Provider).GetField("_driver",BindingFlags.Instance|BindingFlags.NonPublic)!;
            var healthy=(IDriver)field.GetValue(first)!;var lost=new IOException("owned preparation commit acknowledgement loss");
            field.SetValue(first,Neo4jCommitFaultProxy.Wrap(healthy,lost,()=>Interlocked.Increment(ref commits)));
            var original=Input();var failed=await Save(PreparationManager(first,new PreparationKeys()),original);
            Assert.IsTrue(failed.IsError);Assert.IsFalse(failed.IsSaved);Assert.AreEqual(1,commits);
            Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",failed.ErrorCode);
            Assert.IsInstanceOfType(failed.Exception,typeof(INativeMutationReconciliationRequired));
            foreach(var row in original.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}))
            {Assert.AreEqual(Guid.Empty,row.Id);Assert.AreEqual(Guid.Empty,row.VersionId);Assert.AreEqual(0,row.Version);}
            var journal=await Receipt();Assert.IsFalse(journal.ContainsKey("ResultJson"));
            var encoded=(string)journal["PreparationJson"];Assert.IsFalse(encoded.Contains("root secret",StringComparison.Ordinal));
            var owner=new HolonWritePreparation(operation,actor,ProviderType.Neo4jOASIS,new[]{new ProtectedHolonPreparation(Guid.NewGuid(),"oasis-preparation1:owned-reader")});
            var preparation=ProtectedPreparationJson.Read(owner,encoded);Assert.AreEqual(batch?4:2,preparation.Records.Count);
            ids.AddRange(preparation.Records.Select(row=>row.HolonId.ToString("D")));
            var absent=await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS count",new{ids})).SingleAsync();
            Assert.AreEqual(0L,absent["count"].As<long>(),"A preparation claim cannot create holons.");
            var fresh=Input();var retryManager=PreparationManager(second,new PreparationKeys(),"key-b");var recovered=await Save(retryManager,fresh);
            Assert.IsFalse(recovered.IsError,recovered.Message+recovered.Exception);Assert.IsTrue(recovered.IsSaved);Assert.AreEqual(batch?2:1,recovered.Result.Count());
            var rows=fresh.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).ToArray();
            CollectionAssert.AreEquivalent(ids,rows.Select(row=>row.Id.ToString("D")).ToList());
            foreach(var row in rows){Assert.AreEqual(1,row.Version);Assert.IsFalse(row.HasHolonChanged());Assert.IsNotNull(CosmicConcurrency.GetProviderRevisionId(row,ProviderType.Neo4jOASIS));}
            var committed=await Receipt();Assert.AreEqual(encoded,(string)committed["PreparationJson"]);Assert.IsTrue(committed.ContainsKey("ResultJson"));
            var versions=rows.Select(row=>row.VersionId).ToArray();var replayInput=Input();var replay=await Save(retryManager,replayInput);
            Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);
            CollectionAssert.AreEqual(versions,replayInput.SelectMany(root=>new[]{(IHolon)root,root.Children.Single()}).Select(row=>row.VersionId).ToArray());
            Assert.AreEqual((string)committed["ResultJson"],(string)(await Receipt())["ResultJson"]);Assert.AreEqual(1,commits);complete=true;
        }
        finally
        {
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) DETACH DELETE r",new{operation=operation.ToString("D")})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (f:CosmicPreparationFixture {Name:$fixture}) DELETE f",new{fixture})).ConsumeAsync();
            }
            else Console.WriteLine("Preserved preparation recovery: "+fixture+" operation="+operation);
            await first.DeActivateProviderAsync();await second.DeActivateProviderAsync();
        }
    }
}
