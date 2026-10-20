using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    private sealed class PreparationKeys:IHolonMetadataKeyResolver
    {
        internal bool DenyOld;
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
            =>DenyOld&&request.KeyId=="key-a"&&request.Purpose==HolonMetadataKeyPurpose.DecryptPreparation
                ?new(){IsError=true,ErrorCode="OLD_PREPARATION_DENIED"}
                :new(new HolonMetadataKeyMaterial(Enumerable.Repeat(request.KeyId=="key-a"?(byte)17:(byte)34,32).ToArray()));
    }
    private static HolonMetadataEncryptionPolicy PreparationPolicy(PreparationKeys keys,string key="key-a",string context="owned-neo-root")
        =>new(context,key,keys,new[]{"search-tag"});
    private static HolonManager PreparationManager(Provider provider,PreparationKeys keys,string key="key-a")
    {
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        return new HolonManager(provider,dna,registry){MetadataEncryptionPolicy=PreparationPolicy(keys,key)};
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeCosmicConditionalGraphReceiptsAndRacesPreparation(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var first=new Provider(uri,"neo4j","oasis-local-test-only");var second=new Provider(uri,"neo4j","oasis-local-test-only");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspection=driver.AsyncSession();
        var fixture="owned-cosmic-preparation-"+synchronous;var actor=Guid.NewGuid();var operation=Guid.NewGuid();var priorOperation=Guid.NewGuid();
        var ids=new List<string>();var complete=false;
        var retained=await(await inspection.RunAsync("MATCH (f:CosmicPreparationFixture {Name:$fixture}) RETURN count(f) AS count",new{fixture})).SingleAsync();
        Assert.AreEqual(0L,retained["count"].As<long>(),"Retained failed preparation fixture requires independent reconciliation.");
        await(await inspection.RunAsync("CREATE (f:CosmicPreparationFixture {Name:$fixture,OperationId:$operation,PriorOperationId:$prior,ActorId:$actor})",
            new{fixture,operation=operation.ToString("D"),prior=priorOperation.ToString("D"),actor=actor.ToString("D")})).ConsumeAsync();
        Holon Input()=>new(){Name="owned encrypted Neo root",MetaData=new(){["private-note"]=1.000000000000000000000000001m,["search-tag"]="coffee"},
            Children=new List<IHolon>{new Holon{Name="owned encrypted Neo child",MetadataEncryptionOverride=PreparationPolicy(new PreparationKeys(),"key-b","owned-neo-child"),
                MetaData=new(){["private-note"]="child secret"}}}};
        Task<OASISResult<IHolon>> Save(HolonManager manager,Holon value,Guid? by=null)=>synchronous
            ?Task.Run(()=>manager.SaveHolon(value,by??actor,new CosmicWriteOptions(operation),providerType:ProviderType.Neo4jOASIS))
            :manager.SaveHolonAsync(value,by??actor,new CosmicWriteOptions(operation),providerType:ProviderType.Neo4jOASIS);
        async Task<string> Receipt()=>OasisJson.Serialize((await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN properties(r) AS body",
            new{operation=operation.ToString("D")})).SingleAsync())["body"]);
        async Task<string[]> Bodies()=>(await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN h.SnapshotJson AS json ORDER BY h.Id",new{ids})).ToListAsync())
            .Select(row=>row["json"].As<string>()).ToArray();
        try
        {
            Assert.IsFalse((await first.ActivateProviderAsync()).IsError);Assert.IsFalse((await second.ActivateProviderAsync()).IsError);
            var original=Input();var saved=await Save(PreparationManager(first,new PreparationKeys()),original);
            Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);
            ids.Add(original.Id.ToString("D"));ids.Add(original.Children.Single().Id.ToString("D"));
            var receipt=await Receipt();var bodies=await Bodies();Assert.AreEqual(2,bodies.Length);
            foreach(var body in bodies){Assert.IsFalse(body.Contains("child secret",StringComparison.Ordinal));Assert.IsFalse(body.Contains("1.000000000000000000000000001",StringComparison.Ordinal));}
            Assert.IsFalse(receipt.Contains("child secret",StringComparison.Ordinal));
            var raw=await first.LoadHolonAsync(original.Id,loadChildren:false);
            Assert.IsFalse(raw.IsError,raw.Message);Assert.AreEqual("coffee",raw.Result.MetaData["search-tag"].ToString());
            Assert.IsFalse(raw.Result.MetaData.ContainsKey("private-note"));StringAssert.StartsWith(raw.Result.MetaData["__oasis_enc__"].ToString(),"oasis2:");
            var fresh=Input();var replay=await Save(PreparationManager(second,new PreparationKeys(),"key-b"),fresh);
            Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);Assert.AreEqual(original.Id,fresh.Id);Assert.AreEqual(original.VersionId,fresh.VersionId);
            Assert.AreEqual(CosmicConcurrency.GetProviderRevisionId(original,ProviderType.Neo4jOASIS),CosmicConcurrency.GetProviderRevisionId(fresh,ProviderType.Neo4jOASIS));
            Assert.AreEqual(original.Children.Single().VersionId,fresh.Children.Single().VersionId);Assert.IsFalse(fresh.HasHolonChanged());
            StringAssert.Contains(StoredRecordJson.Serialize(fresh.MetaData),"1.000000000000000000000000001");
            Assert.AreEqual(receipt,await Receipt());CollectionAssert.AreEqual(bodies,await Bodies());
            var changed=Input();changed.MetaData["private-note"]=1.000000000000000000000000002m;
            Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",(await Save(PreparationManager(second,new PreparationKeys(),"key-b"),changed)).ErrorCode);
            Assert.AreEqual(Guid.Empty,changed.Id);
            Assert.AreEqual("COSMIC_PREPARATION_ACTOR_MISMATCH",(await Save(PreparationManager(second,new PreparationKeys(),"key-b"),Input(),Guid.NewGuid())).ErrorCode);
            Assert.AreEqual("OLD_PREPARATION_DENIED",(await Save(PreparationManager(second,new PreparationKeys{DenyOld=true},"key-b"),Input())).ErrorCode);
            Assert.AreEqual(receipt,await Receipt());CollectionAssert.AreEqual(bodies,await Bodies());
            var prior=new Holon{Id=Guid.NewGuid(),Name="owned pre-journal Neo receipt"};ids.Add(prior.Id.ToString("D"));
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(priorOperation,actor,ProviderType.Neo4jOASIS,
                new[]{new HolonWriteCondition(prior.Id,null)}),ConditionalHolonOperation.Save,new[]{prior},saveChildren:false);
            var committed=await first.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(committed.IsError,committed.Message+committed.Exception);
            var candidate=new HolonWritePreparation(priorOperation,actor,ProviderType.Neo4jOASIS,new[]{new ProtectedHolonPreparation(prior.Id,"oasis-preparation1:owned-cutover")});
            var refused=await second.ClaimHolonWritePreparationAsync(candidate);Assert.AreEqual("COSMIC_PREPARATION_RECONCILIATION_REQUIRED",refused.ErrorCode);Assert.IsNull(refused.Result);
            var priorRow=await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN r.PreparationJson IS NULL AS absent",new{operation=priorOperation.ToString("D")})).SingleAsync();
            Assert.IsTrue(priorRow["absent"].As<bool>());complete=true;
        }
        finally
        {
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $operations DETACH DELETE r",new{operations=new[]{operation.ToString("D"),priorOperation.ToString("D")}})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (f:CosmicPreparationFixture {Name:$fixture}) DELETE f",new{fixture})).ConsumeAsync();
            }
            else Console.WriteLine("Preserved preparation fixture: "+fixture+" operation="+operation);
            await first.DeActivateProviderAsync();await second.DeActivateProviderAsync();
        }
    }
}
