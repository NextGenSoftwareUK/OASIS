using System.Collections.Concurrent;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Gardens;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Places;
using NextGenSoftware.OASIS.Common;

public sealed class GardenNativeTests
{
    [PlacesNativeMongoFact]
    public async Task SharedTransactionsDeduplicatePlantingCareWaterAndMatureRewards()
    {
        string uri=Environment.GetEnvironmentVariable("OURWORLD_PLACES_NATIVE_TEST_MONGO_URI");
        var settings=MongoClientSettings.FromConnectionString(uri);settings.ServerSelectionTimeout=TimeSpan.FromSeconds(8);
        var client=new MongoClient(settings);var db=client.GetDatabase("oasis_gardens");
        const string prefix="controls_20261102_";
        Guid actor=Guid.Parse("9d1ba4cc-a701-4167-aec5-128503c72cd8"),helper=Guid.Parse("d3b92720-643f-4681-aa2e-055c6dd73e43"),plantId=Guid.Parse("da2a5684-2646-43eb-a057-a563295247fd");
        string a=actor.ToString("D"),b=helper.ToString("D"),p=plantId.ToString("D");var f=Builders<BsonDocument>.Filter;
        var plants=db.GetCollection<BsonDocument>(prefix+"plants");var gardeners=db.GetCollection<BsonDocument>(prefix+"gardeners");
        var care=db.GetCollection<BsonDocument>(prefix+"care");var rewards=db.GetCollection<BsonDocument>(prefix+"reward_outbox");
        Assert.False(await gardeners.Find(f.Eq("_id",a)).AnyAsync(),"Prior owned control must be reconciled before reuse.");
        Assert.False(await gardeners.Find(f.Eq("_id",b)).AnyAsync());
        Assert.False(await plants.Find(f.Eq("_id",p)).AnyAsync());
        var credited=new ConcurrentDictionary<Guid,int>();bool creditAvailable=false;
        Task<OASISResult<long>> Credit(Guid avatar,Guid operation,Guid plant,int amount,string description,DateTime time)
        {Assert.Contains(avatar,new[]{actor,helper});Assert.Equal(plantId,plant);if(!creditAvailable)return Task.FromResult(new OASISResult<long>{IsError=true,Message="Owned credit outage"});credited.TryAdd(operation,amount);return Task.FromResult(new OASISResult<long>{Result=credited.Values.Sum()});}
        Task<WorldscapePlace> Resolve(string id,double lat,double lon,CancellationToken token)=>Task.FromResult(new WorldscapePlace{Id=id,Name="Owned garden control",Category=id=="control:river"?"River":"Park",Latitude=21,Longitude=-86});
        var service=new GardenService(client,db,prefix,Resolve,Credit);
        GardenLocation Location()=>new(){Latitude=21,Longitude=-86,ObservedUtc=DateTime.UtcNow};
        try
        {
            await service.Wallet(actor,default);
            var request=new PlantSeedRequest{OperationId=plantId,SpeciesId="wildflower",ParkId="control:park",Latitude=21,Longitude=-86,ObservedUtc=DateTime.UtcNow};
            await Task.WhenAll(Enumerable.Range(0,6).Select(_=>service.Plant(actor,request,default)));
            Assert.Equal(1,await plants.CountDocumentsAsync(f.Eq("_id",p)));
            Assert.Equal(0,(await gardeners.Find(f.Eq("_id",a)).SingleAsync())["seeds"]["wildflower"].AsInt32);
            await service.History(actor,0,default); // Pending Karma must not hide the saved plant/history.
            Assert.Empty(credited);Assert.Equal(1,await rewards.CountDocumentsAsync(f.Eq("avatar",a)&f.Eq("credited",false)));
            creditAvailable=true;await service.Settle(actor,default);
            await Task.WhenAll(Enumerable.Range(0,6).Select(_=>service.BeginVisit(actor,p,Location(),default)));
            await Task.WhenAll(Enumerable.Range(0,6).Select(_=>service.CareFor(actor,p,new GardenActionRequest{Action="water",Latitude=21,Longitude=-86,ObservedUtc=DateTime.UtcNow},default)));
            Assert.Equal(4,(await gardeners.Find(f.Eq("_id",a)).SingleAsync())["water"].AsInt32);
            Assert.Equal(1,(await plants.Find(f.Eq("_id",p)).SingleAsync())["water"].AsInt32);
            await service.CareFor(actor,p,new GardenActionRequest{Action="love",Latitude=21,Longitude=-86,ObservedUtc=DateTime.UtcNow},default);
            await service.CloseVisit(actor,p,default);await service.BeginVisit(actor,p,Location(),default);
            Assert.True((await care.Find(f.Eq("_id",a+":"+p+":"+GardenRules.Day(DateTime.UtcNow))).SingleAsync())["closed"].AsBoolean);
            await Assert.ThrowsAsync<ArgumentException>(()=>service.CareFor(actor,p,new GardenActionRequest{Action="water",Latitude=22,Longitude=-86,ObservedUtc=DateTime.UtcNow},default));
            await service.Refill(actor,"control:river",Location(),default);
            Assert.Equal(5,(await gardeners.Find(f.Eq("_id",a)).SingleAsync())["water"].AsInt32);
            await service.BeginVisit(helper,p,Location(),default);
            foreach(string action in new[]{"water","love"})await service.CareFor(helper,p,new GardenActionRequest{Action=action,Latitude=21,Longitude=-86,ObservedUtc=DateTime.UtcNow},default);
            Assert.Equal(4,(await gardeners.Find(f.Eq("_id",b)).SingleAsync())["water"].AsInt32);
            Assert.Equal(5,(await gardeners.Find(f.Eq("_id",a)).SingleAsync())["water"].AsInt32);
            var helperHistory=await service.History(helper,0,default);Assert.Equal(1L,helperHistory.GetType().GetProperty("Total")!.GetValue(helperHistory));
            await plants.UpdateOneAsync(f.Eq("_id",p),Builders<BsonDocument>.Update.Set("plantedUtc",DateTime.UtcNow.AddDays(-30)).Set("water",10).Set("love",10));
            await Task.WhenAll(Enumerable.Range(0,6).Select(_=>service.MatureReward(actor,p,Location(),default)));await service.MatureReward(actor,p,Location(),default);await service.Settle(actor,default);
            await service.MatureReward(helper,p,Location(),default);await service.MatureReward(helper,p,Location(),default);await service.Settle(helper,default);
            Assert.Equal(1,(await gardeners.Find(f.Eq("_id",a)).SingleAsync())["seeds"]["wildflower"].AsInt32);
            Assert.Equal(7,credited.Count);Assert.Equal(900,credited.Values.Sum());
            Assert.Equal(4,await rewards.CountDocumentsAsync(f.Eq("avatar",a)));Assert.Equal(0,await rewards.CountDocumentsAsync(f.Eq("avatar",a)&f.Eq("credited",false)));
        }
        finally
        {
            await rewards.DeleteManyAsync(f.In("avatar",new[]{a,b})&f.Eq("plant",p));await care.DeleteManyAsync(f.In("avatar",new[]{a,b})&f.Eq("plant",p));
            await plants.DeleteOneAsync(f.Eq("_id",p)&f.Eq("owner",a));await gardeners.DeleteManyAsync(f.In("_id",new[]{a,b}));
            Assert.Equal(0,await plants.CountDocumentsAsync(f.Eq("_id",p)));Assert.Equal(0,await gardeners.CountDocumentsAsync(f.Eq("_id",a)));
            Assert.Equal(0,await care.CountDocumentsAsync(f.Eq("avatar",a)));Assert.Equal(0,await rewards.CountDocumentsAsync(f.Eq("avatar",a)));
            Assert.Equal(0,await gardeners.CountDocumentsAsync(f.Eq("_id",b)));Assert.Equal(0,await care.CountDocumentsAsync(f.Eq("avatar",b)));Assert.Equal(0,await rewards.CountDocumentsAsync(f.Eq("avatar",b)));
        }
    }
}
