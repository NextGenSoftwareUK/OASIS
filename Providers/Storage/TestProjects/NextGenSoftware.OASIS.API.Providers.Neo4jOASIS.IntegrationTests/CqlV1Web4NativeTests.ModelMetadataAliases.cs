using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public sealed class AliasedScoreHolon:Holon
    {
        [CustomOASISProperty(MetadataKey="score_value")]
        public int Score {get;set;}
    }
    public sealed class CollidingAliasHolon:Holon
    {
        public static int GetterCalls;
        [CustomOASISProperty(MetadataKey="same_value")]
        public int First {get{GetterCalls++;return 1;}set{}}
        [CustomOASISProperty(MetadataKey="same_value")]
        public int Second {get;set;}
    }
    public sealed class ReservedAliasHolon:Holon
    {
        public static int GetterCalls;
        [CustomOASISProperty(MetadataKey=nameof(Holon.CreatedByAvatarId))]
        public int Score {get{GetterCalls++;return 1;}set{}}
    }
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeMetadataAliasSaveReadQueryAndUpdateUseOneMapping(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyMetadataAliasJourney,nativeJourneySuffix:"-model-metadata-alias-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyMetadataAliasJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();
        var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        using var session=CosmicSession.Create(manager,adapter,access).Result;
        var model=new AliasedScoreHolon{Id=Guid.NewGuid(),Name="owned mapped score",Score=17,
            CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        var modelId=model.Id;
        owned.Add(modelId);
        Assert.IsFalse(session.Add(model).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}
        }
        var created=await Timed("create-mapped",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        Assert.IsTrue(model.MetaData.ContainsKey("score_value"));
        Assert.IsFalse(model.MetaData.ContainsKey(nameof(AliasedScoreHolon.Score)));
        using var reader=CosmicSession.Create(manager,adapter,access).Result;
        var query=reader.Query<AliasedScoreHolon>().Result.Where(row=>row.Id==modelId && row.Score==17).Result;
        var loaded=await Timed("load-mapped",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);
        Assert.AreEqual(1,loaded.Result.Count);Assert.AreEqual(17,loaded.Result[0].Score);
        Assert.AreNotSame(model,loaded.Result[0]);
        var root=CosmicLinq.Create(manager,adapter,query).Result;
        var projected=await Timed("project-mapped",()=>root.Select(row=>new{
            row.Score,Stored=CosmicQueryMetadata.Value<int>(row,"score_value")}).ToCosmicListAsync());
        Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);
        Assert.AreEqual(1,projected.Result.Count);
        Assert.AreEqual(17,projected.Result[0].Score);Assert.AreEqual(17,projected.Result[0].Stored);
        model.Score=23;
        Assert.AreEqual(CosmicEntityState.Modified,session.State(model).Result);
        var saved=await Timed("update-mapped",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        var changedQuery=reader.Query<AliasedScoreHolon>().Result.Where(row=>row.Id==modelId && row.Score==23).Result;
        var changed=await Timed("reload-mapped",()=>reader.ReadAsync(changedQuery,trackResults:false));
        Assert.IsFalse(changed.IsError,changed.Message+changed.Exception);
        Assert.AreEqual(1,changed.Result.Count);Assert.AreEqual(23,changed.Result[0].Score);
        Assert.IsTrue(changed.Result[0].MetaData.ContainsKey("score_value"));
        Assert.IsFalse(changed.Result[0].MetaData.ContainsKey(nameof(AliasedScoreHolon.Score)));
        var groupRoot=CosmicLinq.Create(manager,adapter,changedQuery).Result;
        var groups=await Timed("typed-mapped-groups",()=>groupRoot.GroupBy(row=>row.Score).ToCosmicListAsync());
        Assert.IsFalse(groups.IsError,groups.Message+groups.Exception);
        Assert.AreEqual(1,groups.Result.Count);Assert.AreEqual(23,groups.Result[0].Key);
        Assert.AreEqual(1,groups.Result[0].Count());
        Assert.AreEqual(modelId,groups.Result[0].Single().Id);
        Assert.AreEqual(23,groups.Result[0].Single().Score);
        Assert.AreNotSame(model,groups.Result[0].Single());
        var joined=await Timed("typed-mapped-join",()=>groupRoot.Join(groupRoot,
            left=>left.Score,right=>right.Score,(left,right)=>right).ToCosmicListAsync());
        Assert.IsFalse(joined.IsError,joined.Message+joined.Exception);
        Assert.AreEqual(1,joined.Result.Count);Assert.AreEqual(modelId,joined.Result[0].Id);
        Assert.AreEqual(23,joined.Result[0].Score);Assert.AreNotSame(model,joined.Result[0]);
        var matched=await Timed("typed-mapped-groupjoin",()=>groupRoot.GroupJoin(groupRoot,
            left=>left.Score,right=>right.Score,(left,matches)=>new{
                left.Score,Left=left,Matches=matches,Count=matches.Count()}).ToCosmicListAsync());
        Assert.IsFalse(matched.IsError,matched.Message+matched.Exception);
        Assert.AreEqual(1,matched.Result.Count);Assert.AreEqual(23,matched.Result[0].Score);
        Assert.AreEqual(modelId,matched.Result[0].Left.Id);Assert.AreEqual(23,matched.Result[0].Left.Score);
        Assert.AreEqual(1,matched.Result[0].Count);
        Assert.AreEqual(modelId,matched.Result[0].Matches.Single().Id);
        Assert.AreEqual(23,matched.Result[0].Matches.Single().Score);
        var emptyQuery=reader.Query<AliasedScoreHolon>().Result.Where(row=>row.Id==modelId && row.Score==999).Result;
        var emptyRoot=CosmicLinq.Create(manager,adapter,emptyQuery).Result;
        var unmatched=await Timed("typed-mapped-unmatched-group",()=>groupRoot.GroupJoin(emptyRoot,
            left=>left.Score,right=>right.Score,(left,matches)=>new{
                left.Score,Left=left,Matches=matches,Count=matches.Count()}).ToCosmicListAsync());
        Assert.IsFalse(unmatched.IsError,unmatched.Message+unmatched.Exception);
        Assert.AreEqual(1,unmatched.Result.Count);Assert.AreEqual(23,unmatched.Result[0].Score);
        Assert.AreEqual(modelId,unmatched.Result[0].Left.Id);
        Assert.AreEqual(0,unmatched.Result[0].Count);Assert.IsFalse(unmatched.Result[0].Matches.Any());
        CollidingAliasHolon.GetterCalls=0;ReservedAliasHolon.GetterCalls=0;
        var scope=CosmicQueryScope.Named(adapter.QueryProviderType).Result;
        var collision=CosmicQuery<CollidingAliasHolon>.Create(scope).Where(row=>row.First==1);
        var reserved=CosmicQuery<ReservedAliasHolon>.Create(scope).Where(row=>row.Score==1);
        Assert.IsTrue(collision.IsError);Assert.IsNull(collision.Result);
        Assert.IsTrue(reserved.IsError);Assert.IsNull(reserved.Result);
        Assert.AreEqual(0,CollidingAliasHolon.GetterCalls);Assert.AreEqual(0,ReservedAliasHolon.GetterCalls);
        return new NativeFixtureJourneyProof(8,2,new Dictionary<string,bool>{
            ["aliasStoredWithoutClrKey"]=true,["freshTypedReadVerified"]=true,
            ["nativePredicateProjectionVerified"]=true,["selectedUpdateVerified"]=true,["typedIdentityGroupsVerified"]=true,
            ["typedJoinEntitiesVerified"]=true,["typedMappedGroupJoinVerified"]=true,["unmatchedTypedGroupVerified"]=true,
            ["collisionAndReservedRefusedBeforeGetters"]=true});
    }
}
