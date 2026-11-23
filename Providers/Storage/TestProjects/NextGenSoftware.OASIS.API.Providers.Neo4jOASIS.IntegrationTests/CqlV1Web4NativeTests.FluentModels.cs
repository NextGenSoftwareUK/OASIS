using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public sealed class FluentScoreHolon:Holon
    {
        public int Score {get;set;}
    }
    public sealed class FluentCollisionHolon:Holon
    {
        public static int GetterCalls;
        public int First {get{GetterCalls++;return 1;}set{}}
        public int Second {get;set;}
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeFluentModelsBindReadAndWriteWithoutGlobalOverrides(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyFluentModelJourney,nativeJourneySuffix:"-fluent-model-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyFluentModelJourney(
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
        var builder=new CosmicModelBuilder();
        builder.Entity<FluentScoreHolon>().Property(row=>row.Score).HasMetadataKey("score_a");
        var first=builder.Build();Assert.IsFalse(first.IsError,first.Message);
        builder.Entity<FluentScoreHolon>().Property(row=>row.Score).HasMetadataKey("score_b");
        var second=builder.Build();Assert.IsFalse(second.IsError,second.Message);
        Assert.AreNotSame(first.Result,second.Result);
        using var writerA=CosmicSession.Create(manager,adapter,access,first.Result).Result;
        using var writerB=CosmicSession.Create(manager,adapter,access,second.Result).Result;
        var a=new FluentScoreHolon{Id=Guid.NewGuid(),Name="fluent a",Score=17,
            CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        var b=new FluentScoreHolon{Id=Guid.NewGuid(),Name="fluent b",Score=71,
            CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        var aId=a.Id;var bId=b.Id;owned.Add(aId);owned.Add(bId);
        Assert.IsFalse(writerA.Add(a).IsError);Assert.IsFalse(writerB.Add(b).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}
        }
        var savedA=await Timed("create-a",()=>writerA.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(savedA.IsError,savedA.Message+savedA.Exception);Assert.IsTrue(savedA.IsSaved);
        var savedB=await Timed("create-b",()=>writerB.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(savedB.IsError,savedB.Message+savedB.Exception);Assert.IsTrue(savedB.IsSaved);
        Assert.IsTrue(a.MetaData.ContainsKey("score_a"));Assert.IsFalse(a.MetaData.ContainsKey("score_b"));
        Assert.IsTrue(b.MetaData.ContainsKey("score_b"));Assert.IsFalse(b.MetaData.ContainsKey("score_a"));
        Assert.IsFalse(a.MetaData.ContainsKey(nameof(FluentScoreHolon.Score)));
        Assert.IsFalse(b.MetaData.ContainsKey(nameof(FluentScoreHolon.Score)));
        using var readerA=CosmicSession.Create(manager,adapter,access,first.Result).Result;
        using var readerB=CosmicSession.Create(manager,adapter,access,second.Result).Result;
        var queryA=readerA.Query<FluentScoreHolon>().Result.Where(row=>row.Id==aId && row.Score==17).Result;
        var queryB=readerB.Query<FluentScoreHolon>().Result.Where(row=>row.Id==bId && row.Score==71).Result;
        Assert.AreSame(first.Result,queryA.Model);Assert.AreSame(second.Result,queryB.Model);
        var loadedA=await Timed("read-a",()=>readerA.ReadAsync(queryA,trackResults:false));
        var loadedB=await Timed("read-b",()=>readerB.ReadAsync(queryB,trackResults:false));
        Assert.IsFalse(loadedA.IsError,loadedA.Message+loadedA.Exception);
        Assert.IsFalse(loadedB.IsError,loadedB.Message+loadedB.Exception);
        Assert.AreEqual(1,loadedA.Result.Count);Assert.AreEqual(17,loadedA.Result[0].Score);
        Assert.AreEqual(1,loadedB.Result.Count);Assert.AreEqual(71,loadedB.Result[0].Score);
        Assert.AreNotSame(a,loadedA.Result[0]);Assert.AreNotSame(b,loadedB.Result[0]);
        var ordered=queryA.OrderBy(row=>row.Score).Result.ThenBy(row=>row.Id).Result.Take(2).Result;
        Assert.AreSame(first.Result,ordered.Model);
        var root=CosmicLinq.Create(manager,adapter,ordered).Result;
        var projected=await Timed("project-a",()=>root.Select(row=>new{row.Id,row.Score}).ToCosmicListAsync());
        Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);
        Assert.AreEqual(1,projected.Result.Count);Assert.AreEqual(aId,projected.Result[0].Id);
        Assert.AreEqual(17,projected.Result[0].Score);
        var crossed=await readerB.ReadAsync(queryA,trackResults:false);
        Assert.IsTrue(crossed.IsError);Assert.AreEqual("COSMIC_SESSION_SCOPE_MISMATCH",crossed.ErrorCode);
        var rebound=queryA.WithModel(second.Result);
        Assert.IsTrue(rebound.IsError);Assert.AreEqual("COSMIC_MODEL_REBIND_FORBIDDEN",rebound.ErrorCode);
        a.Score=23;Assert.AreEqual(CosmicEntityState.Modified,writerA.State(a).Result);
        var updated=await Timed("update-a",()=>writerA.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        var changedQuery=readerA.Query<FluentScoreHolon>().Result.Where(row=>row.Id==aId && row.Score==23).Result;
        var changed=await Timed("read-updated-a",()=>readerA.ReadAsync(changedQuery,trackResults:false));
        Assert.IsFalse(changed.IsError,changed.Message+changed.Exception);
        Assert.AreEqual(1,changed.Result.Count);Assert.AreEqual(23,changed.Result[0].Score);
        Assert.IsTrue(changed.Result[0].MetaData.ContainsKey("score_a"));
        Assert.IsFalse(changed.Result[0].MetaData.ContainsKey("score_b"));
        using var foreign=CosmicSession.Create(manager,adapter,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,first.Result).Result;
        var privateQuery=foreign.Query<FluentScoreHolon>().Result.Where(row=>row.Id==aId).Result;
        var hidden=await Timed("foreign-read",()=>foreign.ReadAsync(privateQuery,trackResults:false));
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        FluentCollisionHolon.GetterCalls=0;
        var invalid=new CosmicModelBuilder();
        invalid.Entity<FluentCollisionHolon>().Property(row=>row.First).HasMetadataKey("same");
        invalid.Entity<FluentCollisionHolon>().Property(row=>row.Second).HasMetadataKey("same");
        var refused=invalid.Build();Assert.IsTrue(refused.IsError);
        Assert.AreEqual("COSMIC_MODEL_INVALID",refused.ErrorCode);Assert.AreEqual(0,FluentCollisionHolon.GetterCalls);
        Assert.ThrowsException<ArgumentException>(()=>new CosmicModelBuilder().Entity<FluentScoreHolon>()
            .Property(row=>row.Score).HasMetadataKey(nameof(Holon.CreatedByAvatarId)));
        return new NativeFixtureJourneyProof(6,3,new Dictionary<string,bool>{
            ["immutableSnapshotsVerified"]=true,["fluentAliasesStored"]=true,
            ["freshTypedReadVerified"]=true,["nativePredicateProjectionVerified"]=true,
            ["acknowledgedUpdateVerified"]=true,["modelAffinityRefused"]=true,
            ["foreignPrivateExcluded"]=true,["collisionsRefusedBeforeGetters"]=true});
    }
}
