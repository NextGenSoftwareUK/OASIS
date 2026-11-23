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
    public sealed class ShadowStockHolon:Holon
    {
        [CustomOASISProperty(MetadataKey="legacy_score")]
        public int LegacyScore {get;set;}=7;
    }

    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeShadowPropertiesUseSharedMetadataTrackingAndQueries(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyShadowPropertyJourney,nativeJourneySuffix:"-shadow-property-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyShadowPropertyJourney(
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
        builder.Entity<ShadowStockHolon>().ShadowProperty<int>("Stock").IsRequired().HasMetadataKey("stock_quantity");
        builder.Entity<ShadowStockHolon>().ShadowProperty<string>("Label").IsRequired().HasMaxLength(20).HasMetadataKey("stock_label");
        builder.Entity<ShadowStockHolon>().ShadowProperty<decimal>("Price").HasPrecision(8,2).HasMetadataKey("stock_price");
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        Assert.AreEqual(3,built.Result.GetShadowProperties<ShadowStockHolon>().Count);
        builder.Entity<ShadowStockHolon>().ShadowProperty<int>("Stock").HasMetadataKey("later_quantity");
        var readerModel=built.Result.WithoutQueryFilters();
        Assert.AreEqual(3,readerModel.GetShadowProperties<ShadowStockHolon>().Count);
        using var writer=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var entity=new ShadowStockHolon{Id=Guid.NewGuid(),Name="shadow metadata",
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var id=entity.Id;owned.Add(id);Assert.IsFalse(writer.Add(entity).IsError);
        var stock=writer.ShadowProperty<int>(entity,"Stock");Assert.IsFalse(stock.IsError,stock.Message);
        var label=writer.ShadowProperty<string>(entity,"Label");Assert.IsFalse(label.IsError,label.Message);
        var price=writer.ShadowProperty<decimal>(entity,"Price");Assert.IsFalse(price.IsError,price.Message);
        Assert.IsFalse(stock.Result.SetCurrentValue(10).IsError);
        Assert.IsFalse(label.Result.SetCurrentValue("start").IsError);
        Assert.IsFalse(price.Result.SetCurrentValue(12.25m).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var created=await Timed("create-shadow-model",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        Assert.IsTrue(entity.MetaData.ContainsKey("stock_quantity"));Assert.IsFalse(entity.MetaData.ContainsKey("later_quantity"));
        var version=entity.Version;
        using var reader=CosmicSession.Create(manager,adapter,access,readerModel).Result;
        var query=reader.Query<ShadowStockHolon>().Result.Where(row=>row.Id==id
            && CosmicShadow.Value<int>(row,"Stock")==10).Result.OrderBy(row=>CosmicShadow.Value<decimal>(row,"Price")).Result;
        var read=await Timed("read-shadow-model",()=>reader.ReadAsync(query));
        Assert.IsFalse(read.IsError,read.Message+read.Exception);Assert.AreEqual(1,read.Result.Count);
        Assert.AreEqual(7,read.Result[0].LegacyScore);
        var loadedStock=reader.ShadowProperty<int>(read.Result[0],"Stock");Assert.IsFalse(loadedStock.IsError,loadedStock.Message);
        var current=loadedStock.Result.GetCurrentValue();Assert.IsFalse(current.IsError,current.Message);Assert.AreEqual(10,current.Result);
        var source=CosmicLinq.Create(manager,adapter,query).Result;
        var projected=await Timed("project-shadow-model",()=>source.Select(row=>new{
            Stock=CosmicShadow.Value<int>(row,"Stock"),Label=CosmicShadow.Value<string>(row,"Label")}).ToCosmicListAsync());
        Assert.IsFalse(projected.IsError,projected.Message+projected.Exception);Assert.AreEqual(1,projected.Result.Count);
        Assert.AreEqual(10,projected.Result[0].Stock);Assert.AreEqual("start",projected.Result[0].Label);
        Assert.IsTrue(reader.Query<ShadowStockHolon>().Result.Where(row=>CosmicShadow.Value<long>(row,"Stock")==10L).IsError);
        Assert.IsTrue(reader.Query<ShadowStockHolon>().Result.Where(row=>CosmicShadow.Value<int>(row,"Undeclared")==10).IsError);
        Assert.IsFalse(stock.Result.RemoveCurrentValue().IsError);Assert.IsTrue(stock.Result.GetCurrentValue().IsError);
        var required=await Timed("refuse-missing-shadow",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(required.IsError);Assert.IsFalse(required.IsSaved);
        Assert.AreEqual("COSMIC_MODEL_VALIDATION_FAILED",required.ErrorCode);Assert.AreEqual(version,entity.Version);
        Assert.IsFalse(stock.Result.SetCurrentValue(20).IsError);Assert.IsFalse(label.Result.SetCurrentValue("updated").IsError);
        Assert.IsFalse(price.Result.SetCurrentValue(13.50m).IsError);
        var updated=await Timed("update-shadow-model",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        var changed=reader.Query<ShadowStockHolon>().Result.Where(row=>row.Id==id && CosmicShadow.Value<int>(row,"Stock")==20
            && CosmicShadow.Value<decimal>(row,"Price")==13.50m && CosmicShadow.Value<string>(row,"Label")=="updated").Result;
        var fresh=await Timed("read-updated-shadow",()=>reader.ReadAsync(changed,trackResults:false));
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.AreEqual(1,fresh.Result.Count);
        Assert.AreEqual(version+1,fresh.Result[0].Version);Assert.AreEqual(7,fresh.Result[0].LegacyScore);
        Assert.IsFalse(reader.Detach(read.Result[0]).IsError);Assert.IsTrue(loadedStock.Result.GetCurrentValue().IsError);
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,readerModel).Result;
        var hidden=await Timed("foreign-shadow-visibility",()=>foreign.ReadAsync(
            foreign.Query<ShadowStockHolon>().Result.Where(row=>row.Id==id && CosmicShadow.Value<int>(row,"Stock")==20).Result,trackResults:false));
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        return new NativeFixtureJourneyProof(7,2,new Dictionary<string,bool>{
            ["declaredMetadataWithoutClrProperties"]=true,["nativeLogicalNameFilterOrderProjection"]=true,
            ["shadowConstraintsBeforeWrite"]=true,["trackedMetadataUpdateAcknowledged"]=true,
            ["immutableShadowAndBypassSnapshots"]=true,["callerVisibilityAndEntryLifetime"]=true,
            ["existingClrMetadataDecoderPreserved"]=true});
    }
}
