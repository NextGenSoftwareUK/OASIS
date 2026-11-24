using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    public sealed class FluentConstrainedHolon:Holon
    {
        public string? Caption {get;set;}
        public decimal Amount {get;set;}
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeFluentConstraintsValidateOwnedSnapshotAfterCallbacks(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyFluentConstraintJourney,nativeJourneySuffix:"-fluent-constraints-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyFluentConstraintJourney(
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
        builder.Entity<FluentConstrainedHolon>().Property(row=>row.Caption)
            .IsRequired().HasMaxLength(8).HasMetadataKey("caption_value");
        builder.Entity<FluentConstrainedHolon>().Property(row=>row.Amount)
            .HasPrecision(5,2).HasMetadataKey("amount_value");
        builder.Entity<FluentConstrainedHolon>().Property(row=>row.Name).HasMaxLength(128);
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        using var writer=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var entity=new FluentConstrainedHolon{Id=Guid.NewGuid(),Name="constrained model",Caption="start",Amount=1.25m,
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var id=entity.Id;owned.Add(id);Assert.IsFalse(writer.Add(entity).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var created=await Timed("create-valid",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        var version=entity.Version;
        Assert.IsTrue(entity.MetaData.ContainsKey("caption_value"));
        Assert.IsTrue(entity.MetaData.ContainsKey("amount_value"));
        builder.Entity<FluentConstrainedHolon>().Property(row=>row.Caption).HasMaxLength(2);
        entity.Caption="stage";
        Assert.IsFalse(writer.SetLifecycle(new CosmicSessionLifecycle((context,cancellation)=>{
            entity.Caption=null;
            return Task.FromResult(new OASISResult<bool>(true));
        })).IsError);
        var required=await Timed("required-after-callback",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(required.IsError);Assert.IsFalse(required.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",required.ErrorCode);Assert.AreEqual(version,entity.Version);
        Assert.IsFalse(writer.SetLifecycle(null).IsError);
        entity.Caption="too-long-caption";
        var length=await Timed("length-after-lifecycle",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(length.IsError);Assert.IsFalse(length.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",length.ErrorCode);Assert.AreEqual(version,entity.Version);
        entity.Caption="valid";entity.Amount=1234.56m;
        var precision=await Timed("precision-refused",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(precision.IsError);Assert.IsFalse(precision.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",precision.ErrorCode);Assert.AreEqual(version,entity.Version);
        entity.Amount=1.234m;
        var scale=await Timed("scale-refused",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(scale.IsError);Assert.IsFalse(scale.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",scale.ErrorCode);Assert.AreEqual(version,entity.Version);
        entity.Amount=12.34m;
        var updated=await Timed("update-valid",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        Assert.AreEqual(version+1,entity.Version);
        Assert.AreEqual(CosmicEntityState.Unchanged,writer.State(entity).Result);
        using var reader=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var query=reader.Query<FluentConstrainedHolon>().Result
            .Where(row=>row.Id==id && row.Caption=="valid" && row.Amount==12.34m).Result;
        var read=await Timed("read-valid",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
        Assert.AreEqual("valid",read.Result[0].Caption);Assert.AreEqual(12.34m,read.Result[0].Amount);
        Assert.AreEqual(version+1,read.Result[0].Version);
        return new NativeFixtureJourneyProof(6,2,new Dictionary<string,bool>{
            ["requiredAfterCallbackVerified"]=true,["lengthAfterLifecycleVerified"]=true,
            ["precisionAndScaleRefused"]=true,["rejectedWritesPreserveRevision"]=true,
            ["immutableRulesVerified"]=true,["mappedTypedRoundtripVerified"]=true});
    }
}
