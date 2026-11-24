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
    public sealed class OwnedRegion {public string? Code {get;set;}="DEFAULT";}
    public sealed class OwnedAddress
    {
        public string City {get;set;}="";
        public int PostCode {get;set;}
        public OwnedRegion Region {get;set;}=new();
    }
    public sealed class OwnedAddressHolon:Holon {public OwnedAddress Address {get;set;}=new();}

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeOwnedValuesUseAtomicParentStorageAndNativeLeafQueries(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyOwnedValueModelJourney,nativeJourneySuffix:"-owned-value-model-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyOwnedValueModelJourney(
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
        builder.Entity<OwnedAddressHolon>().OwnsOne(row=>row.Address).IsRequired().HasMetadataKey("mailing_address");
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        builder.Entity<OwnedAddressHolon>().OwnsOne(row=>row.Address).HasMetadataKey("later_address");
        using var writer=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var entity=new OwnedAddressHolon{Id=Guid.NewGuid(),Name="owned address",
            Address=new OwnedAddress{City="Alpha",PostCode=123,Region=new OwnedRegion{Code="NW"}},
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var id=entity.Id;owned.Add(id);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Assert.IsFalse(writer.Add(entity).IsError);
        var created=await Timed("create-owned",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        Assert.IsTrue(entity.MetaData.ContainsKey("mailing_address"));Assert.IsFalse(entity.MetaData.ContainsKey("later_address"));
        var version=entity.Version;
        using var reader=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var query=reader.Query<OwnedAddressHolon>().Result
            .Where(row=>row.Id==id && row.Address.City=="Alpha" && row.Address.Region.Code=="NW").Result
            .OrderBy(row=>row.Address.PostCode).Result;
        var read=await Timed("read-owned",()=>reader.ReadAsync(query,trackResults:false));
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
        Assert.AreEqual("Alpha",read.Result[0].Address.City);Assert.AreEqual(123,read.Result[0].Address.PostCode);
        Assert.AreEqual("NW",read.Result[0].Address.Region.Code);
        var source=CosmicLinq.Create(manager,adapter,query).Result;
        var projected=await Timed("project-owned-leaves",()=>source.Select(row=>new{
            row.Address.City,Zip=row.Address.PostCode,Code=row.Address.Region.Code}).ToCosmicListAsync());
        Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);Assert.AreEqual(1,projected.Result.Count);
        Assert.AreEqual("Alpha",projected.Result[0].City);Assert.AreEqual(123,projected.Result[0].Zip);
        Assert.AreEqual("NW",projected.Result[0].Code);
        entity.Address=null!;
        var required=await Timed("refuse-null-owned",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(required.IsError);Assert.IsFalse(required.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",required.ErrorCode);Assert.AreEqual(version,entity.Version);
        entity.Address=new OwnedAddress{City="Beta",PostCode=124,Region=new OwnedRegion{Code=null}};
        var updated=await Timed("update-owned",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        Assert.AreEqual(version+1,entity.Version);
        var changed=reader.Query<OwnedAddressHolon>().Result.Where(row=>row.Id==id
            && row.Address.City=="Beta" && row.Address.PostCode==124 && row.Address.Region.Code==null).Result;
        var fresh=await Timed("read-updated-owned",()=>reader.ReadAsync(changed,trackResults:false));
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.AreEqual(1,fresh.Result.Count);
        Assert.AreEqual("Beta",fresh.Result[0].Address.City);Assert.AreEqual(124,fresh.Result[0].Address.PostCode);
        Assert.IsNull(fresh.Result[0].Address.Region.Code);Assert.AreEqual(version+1,fresh.Result[0].Version);
        return new NativeFixtureJourneyProof(6,2,new Dictionary<string,bool>{
            ["requiredOwnedNullPreservedBeforeValidation"]=true,["ownedMetadataAliasStored"]=true,["typedComponentRoundtrip"]=true,
            ["nativeLeafFilterOrderProjection"]=true,["nestedUpdateAcknowledged"]=true,
            ["immutableOwnedSnapshot"]=true});
    }
}
