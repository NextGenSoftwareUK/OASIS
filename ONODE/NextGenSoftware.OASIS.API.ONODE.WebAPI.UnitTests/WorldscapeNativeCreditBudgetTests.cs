using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Places;

public sealed class WorldscapeNativeCreditBudgetTests
{
    [PlacesNativeMongoFact]
    public async Task ConcurrentNativeReservationsNeverExceedTheFreeAppLimit()
    {
        string uri=Environment.GetEnvironmentVariable("OURWORLD_PLACES_NATIVE_TEST_MONGO_URI");
        Assert.False(string.IsNullOrWhiteSpace(uri),"This explicitly selected native control requires the existing development Mongo connection in process memory.");
        var settings=MongoClientSettings.FromConnectionString(uri);settings.ServerSelectionTimeout=TimeSpan.FromSeconds(8);
        var ledger=new MongoClient(settings).GetDatabase("oasis_places").GetCollection<BsonDocument>("provider_daily_budget_controls");
        const string owner="our-world-places-credit-control-20261102";
        var budget=new MongoPlaceCreditBudget(ledger,owner);string id=budget.CurrentId;
        var filter=Builders<BsonDocument>.Filter.Eq("_id",id)&Builders<BsonDocument>.Filter.Eq("owner",owner);
        bool created=false,passed=false,cleaned=false;string error=null;
        try
        {
            await ledger.InsertOneAsync(new BsonDocument{{"_id",id},{"owner",owner},{"used",990},{"createdUtc",DateTime.UtcNow}});created=true;
            var attempts=Enumerable.Range(0,20).Select(async _=>{try{await new MongoPlaceCreditBudget(ledger,owner).Reserve(5,default);return true;}catch(InvalidOperationException){return false;}});
            var accepted=await Task.WhenAll(attempts);
            var row=await ledger.Find(filter).SingleAsync();
            passed=accepted.Count(a=>a)==2 && row["used"].AsInt32==MongoPlaceCreditBudget.DailyLimit;
        }
        catch(Exception ex){error=ex.GetType().Name;}
        finally
        {
            if(created)
            {
                try{var deleted=await ledger.DeleteOneAsync(filter);cleaned=deleted.DeletedCount==1&&await ledger.Find(filter).CountDocumentsAsync()==0;}
                catch(Exception ex){error="Cleanup "+ex.GetType().Name;}
            }
        }
        Assert.True(passed,"Native concurrent quota control failed: "+error);
        Assert.True(cleaned,"Owned quota control document cleanup could not be verified: "+error);
    }
}

public sealed class PlacesNativeMongoFactAttribute : Xunit.FactAttribute
{
    public PlacesNativeMongoFactAttribute()
    {
        if(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OURWORLD_PLACES_NATIVE_TEST_MONGO_URI")))
            Skip="Explicit native Mongo control; set the existing development test connection through the secure process environment.";
    }
}
