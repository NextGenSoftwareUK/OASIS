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
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeUtcDatePartsPreserveCalendarAndVisibility(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyUtcDatePartsJourney,nativeJourneySuffix:"-utc-date-parts-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyUtcDatePartsJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var foreignActor=Guid.NewGuid();
        var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        using var session=CosmicSession.Create(manager,adapter,access).Result;
        var before=new DateTime(2024,12,31,23,59,59,DateTimeKind.Utc).AddTicks(9999999);
        var after=new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc);
        Holon Model(Guid creator,string name,DateTime? observed)
        {
            var model=new Holon{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=creator,
                IsPublic=false,IsActive=true,IsNewHolon=true};
            if(observed.HasValue)model.MetaData["observed"]=observed.Value;
            owned.Add(model.Id);return model;
        }
        var first=Model(actor,"before",before);var second=Model(actor,"after",after);
        var missing=Model(actor,"missing",null);var hidden=Model(foreignActor,"hidden",after);
        foreach(var model in new[]{first,second,missing})Assert.IsFalse(session.Add(model).IsError);
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,timer.ElapsedMilliseconds);}
        }
        var created=await Timed("create-owned-dates",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(foreignActor).Result).Result;
        Assert.IsFalse(foreign.Add(hidden).IsError);
        var foreignSaved=await Timed("create-hidden-date",()=>foreign.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(foreignSaved.IsError,foreignSaved.Message+foreignSaved.Exception);
        using var reader=CosmicSession.Create(manager,adapter,access).Result;
        var firstId=first.Id;var secondId=second.Id;var missingId=missing.Id;var hiddenId=hidden.Id;
        var query=reader.Query<Holon>().Result.Where(row=>row.Id==firstId || row.Id==secondId || row.Id==hiddenId).Result;
        var root=CosmicLinq.Create(manager,adapter,query).Result;
        var parts=await Timed("date-part-family",()=>root.Select(row=>new{
            row.Id,Year=CosmicQueryMetadata.Value<DateTime>(row,"observed").Year,
            Month=CosmicQueryMetadata.Value<DateTime>(row,"observed").Month,
            Day=CosmicQueryMetadata.Value<DateTime>(row,"observed").Day,
            DayOfYear=CosmicQueryMetadata.Value<DateTime>(row,"observed").DayOfYear,
            Hour=CosmicQueryMetadata.Value<DateTime>(row,"observed").Hour,
            Minute=CosmicQueryMetadata.Value<DateTime>(row,"observed").Minute,
            Second=CosmicQueryMetadata.Value<DateTime>(row,"observed").Second}).ToCosmicListAsync());
        Assert.IsFalse(parts.IsError,parts.Message+parts.Exception);Assert.AreEqual(2,parts.Result.Count);
        var expected=new Dictionary<Guid,DateTime>{{firstId,before},{secondId,after}};
        foreach(var row in parts.Result)
        {
            var stamp=expected[row.Id];
            Assert.AreEqual(stamp.Year,row.Year);Assert.AreEqual(stamp.Month,row.Month);
            Assert.AreEqual(stamp.Day,row.Day);Assert.AreEqual(stamp.DayOfYear,row.DayOfYear);
            Assert.AreEqual(stamp.Hour,row.Hour);Assert.AreEqual(stamp.Minute,row.Minute);Assert.AreEqual(stamp.Second,row.Second);
        }
        var allQuery=reader.Query<Holon>().Result.Where(row=>row.Id==firstId || row.Id==secondId || row.Id==missingId || row.Id==hiddenId).Result;
        var all=CosmicLinq.Create(manager,adapter,allQuery).Result;
        var nullable=await Timed("nullable-date-part",()=>all.Select(row=>new{
            row.Id,Year=(int?)CosmicQueryMetadata.Value<DateTime>(row,"observed").Year}).ToCosmicListAsync());
        Assert.IsFalse(nullable.IsError,nullable.Message+nullable.Exception);Assert.AreEqual(3,nullable.Result.Count);
        Assert.IsNull(nullable.Result.Single(row=>row.Id==missingId).Year);
        var selected=await Timed("selected-date-part",()=>root.Select(row=>new{
            row.Id,Year=CosmicQueryMetadata.Value<DateTime>(row,"observed").Year})
            .Where(row=>row.Year==2025).OrderBy(row=>row.Year).Take(1).ToCosmicListAsync());
        Assert.IsFalse(selected.IsError,selected.Message+selected.Exception);
        Assert.AreEqual(1,selected.Result.Count);Assert.AreEqual(secondId,selected.Result[0].Id);
        var missingQuery=reader.Query<Holon>().Result.Where(row=>row.Id==missingId).Result;
        var missingRoot=CosmicLinq.Create(manager,adapter,missingQuery).Result;
        var required=await Timed("required-missing-date",()=>missingRoot.Select(
            row=>CosmicQueryMetadata.Value<DateTime>(row,"observed").Year).ToCosmicListAsync());
        Assert.IsTrue(required.IsError);Assert.IsNull(required.Result);
        return new NativeFixtureJourneyProof(4,4,new Dictionary<string,bool>{
            ["datePartsVerified"]=true,["submillisecondBoundaryPreserved"]=true,
            ["nullableMissingPreserved"]=true,["selectedCompositionVerified"]=true,
            ["requiredMissingRefused"]=true,["foreignRowsHidden"]=true});
    }
}
