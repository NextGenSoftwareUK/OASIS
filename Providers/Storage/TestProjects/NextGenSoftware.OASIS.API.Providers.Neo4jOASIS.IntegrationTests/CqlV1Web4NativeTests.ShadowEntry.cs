using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeShadowEntriesPreserveOriginalsFlagsAndAcknowledgements(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyShadowEntryJourney,nativeJourneySuffix:"-shadow-entry-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyShadowEntryJourney(
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
        builder.Entity<ShadowStockHolon>().ShadowProperty<int>("Stock").IsRequired().HasMetadataKey("entry_stock");
        builder.Entity<ShadowStockHolon>().ShadowProperty<string>("Note").HasMetadataKey("entry_note");
        var built=builder.Build();Assert.IsFalse(built.IsError,built.Message);
        using var writer=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var entity=new ShadowStockHolon{Id=Guid.NewGuid(),Name="shadow entries",
            CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        var id=entity.Id;owned.Add(id);Assert.IsFalse(writer.Add(entity).IsError);
        var stock=writer.ShadowProperty<int>(entity,"Stock").Result;
        var note=writer.ShadowProperty<string>(entity,"Note").Result;
        Assert.IsFalse(stock.SetCurrentValue(10).IsError);
        Assert.IsFalse(note.SetCurrentValue("seed").IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var created=await Timed("create-shadow-entry",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        var version=entity.Version;
        var original=stock.GetOriginalValue();Assert.IsFalse(original.IsError,original.Message);Assert.AreEqual(10,original.Result);
        Assert.IsFalse(stock.SetCurrentValue(20).IsError);
        var values=stock.GetValues();Assert.IsFalse(values.IsError,values.Message);
        Assert.IsTrue(values.Result.OriginalExists);Assert.IsTrue(values.Result.CurrentExists);
        Assert.IsTrue(values.Result.IsValueModified);Assert.IsFalse(values.Result.IsExplicitlyModified);
        Assert.AreEqual(10,stock.GetOriginalValue().Result);
        Assert.IsFalse(stock.SetOriginalValue(20).IsError);Assert.AreEqual(20,stock.GetOriginalValue().Result);
        Assert.IsFalse(stock.GetValues().Result.IsModified);
        Assert.IsFalse(stock.SetOriginalValue(10).IsError);
        Assert.IsFalse(stock.SetModified(false).IsError);Assert.AreEqual(10,stock.GetCurrentValue().Result);
        Assert.IsFalse(stock.SetModified().IsError);Assert.IsTrue(stock.GetValues().Result.IsExplicitlyModified);
        Assert.IsFalse(stock.SetModified(false).IsError);Assert.IsFalse(stock.GetValues().Result.IsModified);
        Assert.IsFalse(note.SetCurrentValue(null).IsError);
        Assert.IsFalse(note.SetOriginalValue(null).IsError);
        var nullOriginal=note.GetOriginalValue();Assert.IsFalse(nullOriginal.IsError,nullOriginal.Message);Assert.IsNull(nullOriginal.Result);
        Assert.IsTrue(note.GetValues().Result.OriginalExists);Assert.IsTrue(note.GetValues().Result.CurrentExists);
        Assert.IsFalse(note.RemoveOriginalValue().IsError);
        var missingOriginal=note.GetOriginalValue();Assert.IsTrue(missingOriginal.IsError);
        Assert.AreEqual("COSMIC_SHADOW_VALUE_MISSING",missingOriginal.ErrorCode);
        Assert.IsFalse(note.GetValues().Result.OriginalExists);Assert.IsTrue(note.GetValues().Result.CurrentExists);
        Assert.IsFalse(note.SetOriginalValue("seed").IsError);
        Assert.IsFalse(note.SetModified(false).IsError);Assert.AreEqual("seed",note.GetCurrentValue().Result);
        Assert.AreEqual(version,entity.Version,"Local comparison edits must not publish a new native revision.");
        Assert.IsFalse(stock.RemoveCurrentValue().IsError);
        var refused=await Timed("required-shadow-entry-refused",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsTrue(refused.IsError);Assert.IsFalse(refused.IsSaved);Assert.AreEqual(version,entity.Version);
        Assert.IsFalse(stock.SetCurrentValue(20).IsError);Assert.IsFalse(stock.SetModified().IsError);
        var updated=await Timed("acknowledge-shadow-entry",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        Assert.AreEqual(version+1,entity.Version);Assert.AreEqual(20,stock.GetOriginalValue().Result);
        Assert.IsFalse(stock.GetValues().Result.IsModified);
        using var reader=CosmicSession.Create(manager,adapter,access,built.Result).Result;
        var fresh=await Timed("fresh-shadow-entry",()=>reader.ReadAsync(reader.Query<ShadowStockHolon>().Result
            .Where(row=>row.Id==id && CosmicShadow.Value<int>(row,"Stock")==20
                && CosmicShadow.Value<string>(row,"Note")=="seed").Result,trackResults:false));
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.AreEqual(1,fresh.Result.Count);
        Assert.AreEqual(version+1,fresh.Result[0].Version);
        using var foreign=CosmicSession.Create(manager,adapter,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result,built.Result).Result;
        var hidden=await Timed("foreign-shadow-entry",()=>foreign.ReadAsync(
            foreign.Query<ShadowStockHolon>().Result.Where(row=>row.Id==id).Result,trackResults:false));
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);
        Assert.IsTrue(foreign.ShadowProperty<int>(entity,"Stock").IsError);
        Assert.IsFalse(writer.Detach(entity).IsError);
        Assert.IsTrue(stock.GetOriginalValue().IsError);Assert.IsTrue(stock.GetValues().IsError);
        Assert.IsTrue(stock.SetOriginalValue(30).IsError);Assert.IsTrue(stock.SetModified().IsError);
        Assert.IsTrue(stock.RemoveOriginalValue().IsError);
        return new NativeFixtureJourneyProof(7,2,new Dictionary<string,bool>{
            ["shadowOriginalsUseCanonicalBaseline"]=true,["localOriginalEditsPreserveNativeRevision"]=true,
            ["explicitFlagsAndResetShareTrackingOwner"]=true,["missingAndNullOriginalsRemainDistinct"]=true,
            ["requiredValidationBeforeWrite"]=true,["acknowledgementRefreshesBaseline"]=true,
            ["callerAndEntryLifetimePreserved"]=true});
    }
}
