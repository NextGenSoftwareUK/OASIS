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
    public sealed class CollectionValue
    {
        public string? Code {get;set;}="DEFAULT";
        public decimal Amount {get;set;}
    }
    public sealed class CollectionModelHolon:Holon
    {
        public List<string?> Tags {get;set;}=new(){"DEFAULT"};
        public long[] Numbers {get;set;}=new long[]{-1};
        public List<CollectionValue> Components {get;set;}=new(){new()};
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeOwnedCollectionsPreserveAtomicOrderedValues(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyOwnedCollectionJourney,nativeJourneySuffix:"-owned-collection-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyOwnedCollectionJourney(
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
        builder.Entity<CollectionModelHolon>().OwnsMany(row=>row.Tags).HasMetadataKey("tags");
        builder.Entity<CollectionModelHolon>().OwnsMany(row=>row.Numbers).HasMetadataKey("numbers");
        builder.Entity<CollectionModelHolon>().OwnsMany(row=>row.Components).IsRequired().HasMetadataKey("components");
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        builder.Entity<CollectionModelHolon>().OwnsMany(row=>row.Tags).HasMetadataKey("later_tags");
        using var writer=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var entity=new CollectionModelHolon{Id=Guid.NewGuid(),Name="owned collections",
            Tags=new(){"alpha",null,"alpha"},Numbers=new long[]{9007199254740993L,2,9007199254740993L},
            Components=new(){new(){Code="first",Amount=12.25m},new(){Code=null,Amount=0.01m}},
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var id=entity.Id;owned.Add(id);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Assert.IsFalse(writer.Add(entity).IsError);
        var created=await Timed("create-collections",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        Assert.IsTrue(entity.MetaData.ContainsKey("tags"));Assert.IsFalse(entity.MetaData.ContainsKey("later_tags"));
        Assert.IsFalse(entity.MetaData["components"] is string);
        var version=entity.Version;
        using var reader=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var query=reader.Query<CollectionModelHolon>().Result.Where(row=>row.Id==id).Result;
        var read=await Timed("read-collections",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
        CollectionAssert.AreEqual(new string?[]{"alpha",null,"alpha"},read.Result[0].Tags);
        CollectionAssert.AreEqual(entity.Numbers,read.Result[0].Numbers);
        Assert.AreEqual(2,read.Result[0].Components.Count);
        Assert.AreEqual("first",read.Result[0].Components[0].Code);
        Assert.IsNull(read.Result[0].Components[1].Code);
        Assert.AreEqual(12.25m,read.Result[0].Components[0].Amount);
        Assert.AreEqual(0.01m,read.Result[0].Components[1].Amount);
        var before=entity.Components;
        entity.Components=null!;
        var required=await Timed("refuse-null-collection",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(required.IsError);Assert.IsFalse(required.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",required.ErrorCode);Assert.AreEqual(version,entity.Version);
        entity.Components=before;
        entity.Components.Reverse();entity.Components[0].Amount=23.50m;
        entity.Components.Add(new CollectionValue{Code="third",Amount=3m});
        entity.Tags=null!;entity.Numbers=new long[]{2,9007199254740993L};
        var updated=await Timed("update-collections",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        Assert.AreEqual(version+1,entity.Version);
        var fresh=await Timed("read-updated-collections",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.AreEqual(1,fresh.Result.Count);
        Assert.IsNull(fresh.Result[0].Tags);
        CollectionAssert.AreEqual(entity.Numbers,fresh.Result[0].Numbers);
        Assert.AreEqual(3,fresh.Result[0].Components.Count);
        Assert.IsNull(fresh.Result[0].Components[0].Code);
        Assert.AreEqual(23.50m,fresh.Result[0].Components[0].Amount);
        Assert.AreEqual("first",fresh.Result[0].Components[1].Code);
        Assert.AreEqual("third",fresh.Result[0].Components[2].Code);
        entity.Tags=new();entity.Numbers=Array.Empty<long>();entity.Components.Clear();
        var emptied=await Timed("empty-collections",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(emptied.IsError,emptied.Message+emptied.Exception);Assert.IsTrue(emptied.IsSaved);
        var empty=await Timed("read-empty-collections",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(empty.IsError,empty.Message+empty.Exception);Assert.AreEqual(1,empty.Result.Count);
        Assert.AreEqual(0,empty.Result[0].Tags.Count);Assert.AreEqual(0,empty.Result[0].Numbers.Length);
        Assert.AreEqual(0,empty.Result[0].Components.Count);Assert.AreEqual(version+2,empty.Result[0].Version);
        using var foreign=CosmicSession.Create(manager,adapter,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,built.Result).Result;
        var invisible=await Timed("foreign-collection-owner",()=>foreign.ReadAsync(
            foreign.Query<CollectionModelHolon>().Result.Where(row=>row.Id==id).Result,trackResults:false));
        Assert.IsFalse(invisible.IsError,invisible.Message+invisible.Exception);Assert.AreEqual(0,invisible.Result.Count);
        return new NativeFixtureJourneyProof(7,3,new Dictionary<string,bool>{
            ["primitiveOrderDuplicatesAndExactInteger"]=true,["plainValueCollectionRoundtrip"]=true,
            ["nestedEditsAndReorderAcknowledged"]=true,["nullAndEmptyRemainDistinct"]=true,
            ["requiredCollectionRefusedBeforeWrite"]=true,["immutableCollectionMapping"]=true,
            ["parentPermissionsEnforced"]=true});
    }
}
