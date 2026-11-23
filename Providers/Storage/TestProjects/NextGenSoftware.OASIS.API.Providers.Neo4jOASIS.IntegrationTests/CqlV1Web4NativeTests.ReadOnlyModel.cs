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
    public sealed class ReadOnlyMappedHolon:Holon {public int Score {get;set;}}

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeReadOnlyModelsRefuseWritesWithoutGlobalTypePolicy(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyReadOnlyEntityModelJourney,nativeJourneySuffix:"-read-only-model-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyReadOnlyEntityModelJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        var builder=new CosmicModelBuilder();
        builder.Entity<ReadOnlyMappedHolon>().Property(row=>row.Score).HasMetadataKey("readonly_score");
        var writable=builder.Build();Assert.IsFalse(writable.IsError,writable.Message);
        builder.Entity<ReadOnlyMappedHolon>().IsReadOnly();
        var locked=builder.Build();Assert.IsFalse(locked.IsError,locked.Message);
        builder.Entity<ReadOnlyMappedHolon>().IsReadOnly(false);
        var released=builder.Build();Assert.IsFalse(released.IsError,released.Message);
        using var writer=CosmicSession.Create(manager,adapter,access,writable.Result).Result;
        ReadOnlyMappedHolon New(string name)=>new(){Id=Guid.NewGuid(),Name=name,Score=10,
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var entity=New("model readonly");var denied=New("denied insert");
        var id=entity.Id;var deniedId=denied.Id;owned.AddRange(new[]{id,deniedId});
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Assert.IsFalse(writer.Add(entity).IsError);
        var created=await Timed("create-valid",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        var version=entity.Version;
        using var reader=CosmicSession.Create(manager,adapter,access,locked.Result).Result;
        var read=await Timed("read-locked",()=>reader.ReadAsync(
            reader.Query<ReadOnlyMappedHolon>().Result.Where(row=>row.Id==id).Result));
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
        var current=read.Result.Single();Assert.AreEqual(10,current.Score);
        current.Score=22;
        var update=await Timed("refuse-update",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(update.IsError);Assert.IsFalse(update.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_READ_ONLY_ENTITY",update.ErrorCode);Assert.AreEqual(version,current.Version);
        Assert.IsFalse(reader.Remove(current).IsError);
        var delete=await Timed("refuse-delete",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(delete.IsError);Assert.IsFalse(delete.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_READ_ONLY_ENTITY",delete.ErrorCode);Assert.AreEqual(version,current.Version);
        using(var insert=CosmicSession.Create(manager,adapter,access,locked.Result).Result)
        {
            Assert.IsFalse(insert.Add(denied).IsError);
            var refused=await Timed("refuse-insert",()=>insert.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsTrue(refused.IsError);Assert.IsFalse(refused.IsSaved);
            Assert.AreEqual("COSMIC_MODEL_READ_ONLY_ENTITY",refused.ErrorCode);
        }
        using var allowed=CosmicSession.Create(manager,adapter,access,released.Result).Result;
        var authority=await Timed("read-unchanged",()=>allowed.ReadAsync(
            allowed.Query<ReadOnlyMappedHolon>().Result.Where(row=>row.Id==id || row.Id==deniedId).Result));
        Assert.IsFalse(authority.IsError,authority.Message+authority.Exception);Assert.AreEqual(1,authority.Result.Count);
        Assert.AreEqual(id,authority.Result[0].Id);Assert.AreEqual(10,authority.Result[0].Score);
        Assert.AreEqual(version,authority.Result[0].Version);
        authority.Result[0].Score=12;
        var saved=await Timed("write-unlocked",()=>allowed.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        using var verifier=CosmicSession.Create(manager,adapter,access,locked.Result).Result;
        var final=await Timed("read-updated",()=>verifier.ReadAsync(
            verifier.Query<ReadOnlyMappedHolon>().Result.Where(row=>row.Id==id).Result,trackResults:false));
        Assert.IsFalse(final.IsError,final.Message+final.Exception);Assert.AreEqual(1,final.Result.Count);
        Assert.AreEqual(12,final.Result[0].Score);Assert.AreEqual(version+1,final.Result[0].Version);
        return new NativeFixtureJourneyProof(5,2,new Dictionary<string,bool>{
            ["readOnlyInsertRefused"]=true,["readOnlyUpdateAndDeleteRefused"]=true,
            ["authorityUnchangedAfterRefusals"]=true,["independentWritableModel"]=true,
            ["immutablePolicySnapshot"]=true});
    }
}
