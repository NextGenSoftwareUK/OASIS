using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Places;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Gardens;

public class GardenLocation
{
    public double Latitude {get;set;}
    public double Longitude {get;set;}
    public DateTime ObservedUtc {get;set;}
}
public sealed class PlantSeedRequest:GardenLocation
{
    public Guid OperationId {get;set;}
    public string SpeciesId {get;set;}
    public string ParkId {get;set;}
}
public sealed class GardenActionRequest:GardenLocation
{
    public string Action {get;set;}
}
public sealed class GardenRewardDelivery
{
    public int Pending {get;set;}
    public string Message {get;set;}
}

/// <summary>Shared gardening aggregates and reward outbox commit together in Mongo transactions.
/// An outbox credit is acknowledged only after the idempotent Karma ledger has committed.</summary>
public sealed class GardenService
{
    private readonly IMongoClient client;
    private readonly IMongoDatabase db;
    private readonly WorldscapePlaceCatalogue places;
    private readonly Lazy<Task> indexes;
    private readonly string prefix="";
    private readonly Func<string,double,double,CancellationToken,Task<WorldscapePlace>> resolve;
    private readonly Func<Guid,Guid,Guid,int,string,DateTime,Task<NextGenSoftware.OASIS.Common.OASISResult<long>>> credit;
    private IMongoCollection<BsonDocument> Plants=>db.GetCollection<BsonDocument>(prefix+"plants");
    private IMongoCollection<BsonDocument> Gardeners=>db.GetCollection<BsonDocument>(prefix+"gardeners");
    private IMongoCollection<BsonDocument> Care=>db.GetCollection<BsonDocument>(prefix+"care");
    private IMongoCollection<BsonDocument> Rewards=>db.GetCollection<BsonDocument>(prefix+"reward_outbox");
    private static FilterDefinition<BsonDocument> Id(string id)=>Builders<BsonDocument>.Filter.Eq("_id",id);
    public GardenService(IConfiguration configuration,WorldscapePlaceCatalogue places)
    {
        client=new MongoClient(SubscriptionMongoConfiguration.ConnectionString(configuration));
        db=client.GetDatabase("oasis_gardens").WithWriteConcern(WriteConcern.WMajority);
        this.places=places;
        GardenRules.Configure(configuration.GetSection("Gardens:Species").Get<SeedSpecies[]>());
        resolve=async(id,lat,lon,t)=>{var place=await places.Resolve(id,lat,lon,t);await OsmPlaceGeometry.Enrich(place,t);return place;};
        credit=(a,o,p,n,d,t)=>KarmaManager.Instance.CreditGardenRewardAsync(a,o,p,n,d,t);
        indexes=new Lazy<Task>(CreateIndexes);
    }
    public GardenService(IMongoClient client,IMongoDatabase database,string collectionPrefix,
        Func<string,double,double,CancellationToken,Task<WorldscapePlace>> resolver,
        Func<Guid,Guid,Guid,int,string,DateTime,Task<NextGenSoftware.OASIS.Common.OASISResult<long>>> credit)
    {this.client=client;db=database.WithWriteConcern(WriteConcern.WMajority);prefix=collectionPrefix;resolve=resolver;this.credit=credit;indexes=new Lazy<Task>(CreateIndexes);}
    private Task CreateIndexes()=>Task.WhenAll(
        Plants.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Geo2DSphere("location"))),
        Care.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("avatar").Ascending("plant"))),
        Rewards.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("avatar").Ascending("credited"))));
    private static void Location(GardenLocation location)
    {
        if(location==null)throw new ArgumentException("Location is required.");
        GardenRules.ValidateObservation(location.ObservedUtc,DateTime.UtcNow);
        WorldscapePlaceCatalogue.ValidateCoordinates(location.Latitude,location.Longitude);
    }
    private static string Actor(Guid actor)=>actor.ToString("D");
    private static string Day=>GardenRules.Day(DateTime.UtcNow);
    private static BsonDocument NewGardener(Guid actor)=>new(){
        {"_id",Actor(actor)},{"waterDay",Day},{"water",GardenRules.WaterCapacity},
        // One starter packet per species, granted exactly once with the gardener aggregate.
        {"seeds",new BsonDocument(GardenRules.Species.Select(s=>new BsonElement(s.Id,1)))}};
    private async Task<BsonDocument> Gardener(IClientSessionHandle session,Guid actor,CancellationToken token)
    {
        var gardener=await Gardeners.Find(session,Id(Actor(actor))).SingleOrDefaultAsync(token);
        if(gardener==null){gardener=NewGardener(actor);await Gardeners.InsertOneAsync(session,gardener,cancellationToken:token);}
        if(gardener["waterDay"].AsString!=Day){gardener["waterDay"]=Day;gardener["water"]=GardenRules.WaterCapacity;}
        return gardener;
    }
    private async Task EnsureGardener(Guid actor,CancellationToken token)
    {
        try{await Gardeners.UpdateOneAsync(Id(Actor(actor)),new BsonDocument("$setOnInsert",NewGardener(actor)),new UpdateOptions{IsUpsert=true},token);}
        catch(MongoWriteException ex)when(ex.WriteError.Category==ServerErrorCategory.DuplicateKey)
        {if(!await Gardeners.Find(Id(Actor(actor))).AnyAsync(token))throw; /* Concurrent first-use creation committed the same avatar aggregate. */}
    }
    private Task SaveGardener(IClientSessionHandle session,BsonDocument gardener,CancellationToken token)
        =>Gardeners.ReplaceOneAsync(session,Id(gardener["_id"].AsString),gardener,cancellationToken:token);
    private async Task<T> Transaction<T>(Func<IClientSessionHandle,CancellationToken,Task<T>> operation,CancellationToken token)
    {
        await indexes.Value;
        using var session=await client.StartSessionAsync(cancellationToken:token);
        return await session.WithTransactionAsync(operation,new TransactionOptions(ReadConcern.Snapshot,writeConcern:WriteConcern.WMajority),token);
    }
    private async Task Reward(IClientSessionHandle session,Guid actor,string id,string plant,int amount,string description,CancellationToken token)
    {
        // ID is server-generated from the committed gameplay action, never a client reward amount.
        await Rewards.InsertOneAsync(session,new BsonDocument{{"_id",id},{"avatar",Actor(actor)},{"plant",plant},
            {"amount",amount},{"description",description},{"at",DateTime.UtcNow},{"credited",false}},cancellationToken:token);
    }
    public async Task<object> Wallet(Guid actor,CancellationToken token)
    {
        await EnsureGardener(actor,token);
        var gardener=await Transaction(async(s,t)=>{
            var before=await Gardeners.Find(s,Id(Actor(actor))).SingleAsync(t);
            if(before["waterDay"].AsString==Day)return before;
            var g=await Gardener(s,actor,t);await SaveGardener(s,g,t);return g;
        },token);
        var delivery=await Settle(actor,token);
        return new{Water=gardener["water"].AsInt32,Capacity=GardenRules.WaterCapacity,Reset="Daily at 00:00 UTC",
            Seeds=gardener["seeds"].AsBsonDocument.Elements.ToDictionary(e=>e.Name,e=>e.Value.AsInt32),PendingKarma=delivery.Pending,RewardMessage=delivery.Message};
    }
    public async Task<object> Plant(Guid actor,PlantSeedRequest request,CancellationToken token)
    {
        if(request==null)throw new ArgumentException("A planting request is required.");
        WorldscapePlaceCatalogue.ValidateCoordinates(request.Latitude,request.Longitude);var species=GardenRules.Get(request.SpeciesId);
        if(request.OperationId==Guid.Empty)throw new ArgumentException("A planting operation ID is required.");
        string id=request.OperationId.ToString("D");
        var existing=await Plants.Find(Id(id)).SingleOrDefaultAsync(token);
        if(existing==null)
        {
            Location(request);
            await EnsureGardener(actor,token);
            var park=await resolve(request.ParkId,request.Latitude,request.Longitude,token);
            if(park.Category!="Park"&&park.Category!="Forest"&&park.Category!="Woodland")throw new ArgumentException("Plant seeds in a park or nature area.");
            if(WorldscapePlaceCatalogue.DistanceToPlace(park,request.Latitude,request.Longitude)>15)
                throw new ArgumentException("Move inside the selected park before planting.");
            try{await Transaction(async(s,t)=>{
                var prior=await Plants.Find(s,Id(id)).SingleOrDefaultAsync(t);
                if(prior!=null)return true;
                var gardener=await Gardener(s,actor,t);var seeds=gardener["seeds"].AsBsonDocument;
                if(seeds[species.Id].AsInt32<1)throw new InvalidOperationException("You have no seeds of this species. Mature plants give their helpers another seed.");
                seeds[species.Id]=seeds[species.Id].AsInt32-1;
                var plant=new BsonDocument{{"_id",id},{"owner",Actor(actor)},{"species",species.Id},{"speciesName",species.Name},
                    {"growthDays",species.GrowthDays},{"careRequired",species.CareRequired},{"matureKarma",species.MatureKarma},{"questId",species.QuestId==null?BsonNull.Value:new BsonString(species.QuestId)},
                    {"park",park.Id},{"parkName",park.Name},
                    {"latitude",request.Latitude},{"longitude",request.Longitude},{"location",new BsonDocument{{"type","Point"},{"coordinates",new BsonArray{request.Longitude,request.Latitude}}}},
                    {"plantedUtc",DateTime.UtcNow},{"water",0},{"love",0}};
                await Plants.InsertOneAsync(s,plant,cancellationToken:t);await SaveGardener(s,gardener,t);
                await Reward(s,actor,Guid.NewGuid().ToString("D"),id,GardenRules.PlantKarma,"Planted "+species.Name+" at "+park.Name,t);
                return true;
            },token);}
            catch(MongoWriteException ex)when(ex.WriteError.Category==ServerErrorCategory.DuplicateKey)
            {if(!await Plants.Find(Id(id)).AnyAsync(token))throw; /* Idempotent planting race: the request binding is verified below. */}
        }
        var saved=await Plants.Find(Id(id)).SingleAsync(token);
        if(saved["owner"]!=Actor(actor)||saved["species"]!=species.Id||saved["park"]!=request.ParkId)
            throw new InvalidOperationException("That planting operation belongs to a different request.");
        if(WorldscapePlaceCatalogue.Distance(saved["latitude"].ToDouble(),saved["longitude"].ToDouble(),request.Latitude,request.Longitude)>1)
            throw new InvalidOperationException("That planting operation was recorded at a different location.");
        var delivery=await Settle(actor,token);return new{Plant=View(saved),PendingKarma=delivery.Pending,RewardMessage=delivery.Message};
    }
    public async Task<object> Locations(string category,double latitude,double longitude,CancellationToken token)
    {
        if(category!="Park"&&category!="River"&&category!="Stream")throw new ArgumentException("Choose a park, river or stream.");
        var rows=(await places.Search(category,latitude,longitude,token)).Take(5).ToArray();
        foreach(var row in rows){await OsmPlaceGeometry.Enrich(row,token);row.DistanceMetres=WorldscapePlaceCatalogue.DistanceToPlace(row,latitude,longitude);}
        return rows.OrderBy(p=>p.DistanceMetres).ToArray();
    }
    private static void Near(BsonDocument plant,GardenLocation location)
    {
        if(WorldscapePlaceCatalogue.Distance(plant["latitude"].ToDouble(),plant["longitude"].ToDouble(),location.Latitude,location.Longitude)>30)
            throw new ArgumentException("Move within 30 metres of this seed.");
    }
    public async Task<object> BeginVisit(Guid actor,string plantId,GardenLocation location,CancellationToken token)
    {
        Location(location);
        BsonDocument visit;
        try{visit=await Transaction(async(s,t)=>{
            var plant=await Plants.Find(s,Id(plantId)).SingleOrDefaultAsync(t)??throw new ArgumentException("Seed not found.");Near(plant,location);
            string key=Actor(actor)+":"+plantId+":"+Day;
            var row=await Care.Find(s,Id(key)).SingleOrDefaultAsync(t);
            if(row==null){row=new BsonDocument{{"_id",key},{"avatar",Actor(actor)},{"plant",plantId},{"day",Day},{"startedUtc",DateTime.UtcNow},
                {"expiresUtc",DateTime.UtcNow.AddMinutes(30)},{"water",false},{"love",false},{"closed",false}};await Care.InsertOneAsync(s,row,cancellationToken:t);}
            return row;
        },token);}
        catch(MongoWriteException ex)when(ex.WriteError.Category==ServerErrorCategory.DuplicateKey)
        {visit=await Care.Find(Id(Actor(actor)+":"+plantId+":"+Day)).SingleOrDefaultAsync(token);if(visit==null)throw;}
        return new{Watered=visit["water"].AsBoolean,Loved=visit["love"].AsBoolean,
            CanCare=!visit["closed"].AsBoolean&&visit["expiresUtc"].ToUniversalTime()>DateTime.UtcNow,
            ExpiresUtc=visit["expiresUtc"].ToUniversalTime(),Message="One care visit per seed each UTC day; Water and Give love are both available during this visit."};
    }
    public async Task<object> CareFor(Guid actor,string plantId,GardenActionRequest request,CancellationToken token)
    {
        Location(request);GardenRules.ValidateAction(request.Action);
        await EnsureGardener(actor,token);
        await Transaction(async(s,t)=>{
            var plant=await Plants.Find(s,Id(plantId)).SingleOrDefaultAsync(t)??throw new ArgumentException("Seed not found.");Near(plant,request);
            var visit=await Care.Find(s,Id(Actor(actor)+":"+plantId+":"+Day)).SingleOrDefaultAsync(t)
                ??throw new InvalidOperationException("Open Water Seed to begin today's care visit.");
            if(visit[request.Action].AsBoolean)return true; // Acknowledged replay never spends water or grants again.
            if(visit["closed"].AsBoolean||visit["expiresUtc"].ToUniversalTime()<=DateTime.UtcNow)throw new InvalidOperationException("Today's visit has ended. Come back tomorrow.");
            var gardener=await Gardener(s,actor,t);
            if(request.Action=="water"){
                if(gardener["water"].AsInt32<=0)throw new InvalidOperationException("Your watering can is empty. Refill near a river or stream, or return after the daily reset.");
                gardener["water"]=gardener["water"].AsInt32-1;
            }
            visit[request.Action]=true;plant[request.Action]=plant[request.Action].AsInt32+1;
            await Care.ReplaceOneAsync(s,Id(visit["_id"].AsString),visit,cancellationToken:t);
            await Plants.ReplaceOneAsync(s,Id(plantId),plant,cancellationToken:t);await SaveGardener(s,gardener,t);
            var species=SpeciesFor(plant);
            await Reward(s,actor,Guid.NewGuid().ToString("D"),plantId,GardenRules.CareKarma,
                (request.Action=="water"?"Watered ":"Gave love to ")+species.Name+" at "+plant["parkName"].AsString,t);
            return true;
        },token);
        var delivery=await Settle(actor,token);return new{Plant=View(await Plants.Find(Id(plantId)).SingleAsync(token)),PendingKarma=delivery.Pending,RewardMessage=delivery.Message};
    }
    public async Task CloseVisit(Guid actor,string plantId,CancellationToken token)
        =>await Care.UpdateOneAsync(Id(Actor(actor)+":"+plantId+":"+Day),Builders<BsonDocument>.Update.Set("closed",true),cancellationToken:token);
    public async Task<object> Refill(Guid actor,string waterId,GardenLocation location,CancellationToken token)
    {
        Location(location);var water=await resolve(waterId,location.Latitude,location.Longitude,token);
        if((water.Category!="River"&&water.Category!="Stream")||WorldscapePlaceCatalogue.DistanceToPlace(water,location.Latitude,location.Longitude)>30)
            throw new ArgumentException("Move near a mapped river or stream to refill your can.");
        await EnsureGardener(actor,token);
        await Transaction(async(s,t)=>{var g=await Gardener(s,actor,t);g["water"]=GardenRules.WaterCapacity;await SaveGardener(s,g,t);return true;},token);
        return await Wallet(actor,token);
    }
    public async Task<object> MatureReward(Guid actor,string plantId,GardenLocation location,CancellationToken token)
    {
        Location(location);
        await EnsureGardener(actor,token);
        bool awarded=false;
        try{await Transaction(async(s,t)=>{
            awarded=false;
            var plant=await Plants.Find(s,Id(plantId)).SingleOrDefaultAsync(t)??throw new ArgumentException("Seed not found.");Near(plant,location);
            var species=SpeciesFor(plant);
            if(Progress(plant)<1)throw new InvalidOperationException("This plant is still growing.");
            bool helper=plant["owner"]==Actor(actor)||await Care.Find(s,Builders<BsonDocument>.Filter.Eq("avatar",Actor(actor))&
                Builders<BsonDocument>.Filter.Eq("plant",plantId)&(Builders<BsonDocument>.Filter.Eq("water",true)|Builders<BsonDocument>.Filter.Eq("love",true))).AnyAsync(t);
            if(!helper)throw new InvalidOperationException("Help this plant before claiming its thank-you reward.");
            string claim="mature:"+Actor(actor)+":"+plantId;
            if(await Care.Find(s,Id(claim)).AnyAsync(t))return true;
            await Care.InsertOneAsync(s,new BsonDocument{{"_id",claim},{"avatar",Actor(actor)},{"plant",plantId},{"matureClaim",true},{"startedUtc",DateTime.UtcNow}},cancellationToken:t);
            var g=await Gardener(s,actor,t);var seeds=g["seeds"].AsBsonDocument;seeds[species.Id]=seeds[species.Id].AsInt32+1;await SaveGardener(s,g,t);
            await Reward(s,actor,Guid.NewGuid().ToString("D"),plantId,species.MatureKarma,"Thank-you from mature "+species.Name+" at "+plant["parkName"].AsString,t);awarded=true;return true;
        },token);}
        catch(MongoWriteException ex)when(ex.WriteError.Category==ServerErrorCategory.DuplicateKey)
        {if(!await Care.Find(Id("mature:"+Actor(actor)+":"+plantId)).AnyAsync(token))throw;awarded=false;}
        var delivery=await Settle(actor,token);var species=SpeciesFor(await Plants.Find(Id(plantId)).SingleAsync(token));
        return new{Message=awarded?"Thank you for helping me grow! Take this seed and share the love.":"Thank you for visiting again! You have already received my gift.",Karma=awarded?species.MatureKarma:0,Seed=species.Id,SeedAwarded=awarded,QuestId=species.QuestId,PendingKarma=delivery.Pending,RewardMessage=delivery.Message};
    }
    private static SeedSpecies SpeciesFor(BsonDocument p)=>new(){Id=p["species"].AsString,Name=p["speciesName"].AsString,GrowthDays=p["growthDays"].AsInt32,CareRequired=p["careRequired"].AsInt32,MatureKarma=p["matureKarma"].AsInt32,QuestId=p["questId"].IsBsonNull?null:p["questId"].AsString};
    private static double Progress(BsonDocument p)=>GardenRules.Progress(SpeciesFor(p),p["plantedUtc"].ToUniversalTime(),DateTime.UtcNow,p["water"].AsInt32,p["love"].AsInt32);
    private static object View(BsonDocument p)=>new{Id=p["_id"].AsString,OwnerId=p["owner"].AsString,SpeciesId=p["species"].AsString,
        Name=p["speciesName"].AsString,ParkName=p["parkName"].AsString,Latitude=p["latitude"].ToDouble(),Longitude=p["longitude"].ToDouble(),
        PlantedUtc=p["plantedUtc"].ToUniversalTime(),Water=p["water"].AsInt32,Love=p["love"].AsInt32,Progress=Progress(p),Stage=GardenRules.Stage(Progress(p))};
    public async Task<object> Nearby(double latitude,double longitude,CancellationToken token)
    {
        WorldscapePlaceCatalogue.ValidateCoordinates(latitude,longitude);
        await indexes.Value;
        var f=Builders<BsonDocument>.Filter;
        var rows=await Plants.Find(f.NearSphere("location",new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(longitude,latitude)),2000)).Limit(500).ToListAsync(token);
        return rows.Select(p=>new{Plant=p,Distance=WorldscapePlaceCatalogue.Distance(latitude,longitude,p["latitude"].ToDouble(),p["longitude"].ToDouble())})
            .Where(p=>p.Distance<=2000).OrderBy(p=>p.Distance).Select(p=>View(p.Plant)).ToArray();
    }
    public async Task<object> History(Guid actor,int offset,CancellationToken token)
    {
        if(offset<0)throw new ArgumentException("Invalid history page.");var delivery=await Settle(actor,token);
        var cf=Builders<BsonDocument>.Filter;
        var helped=await Care.Find(cf.Eq("avatar",Actor(actor))&(cf.Eq("water",true)|cf.Eq("love",true)|cf.Eq("matureClaim",true))).ToListAsync(token);
        var f=Builders<BsonDocument>.Filter;
        var filter=f.Eq("owner",Actor(actor))|f.In("_id",helped.Select(h=>h["plant"].AsString).Distinct());
        long total=await Plants.CountDocumentsAsync(filter,cancellationToken:token);
        var rows=await Plants.Find(filter).Sort(Builders<BsonDocument>.Sort.Descending("plantedUtc")).Skip(offset).Limit(50).ToListAsync(token);
        var rewards=await Rewards.Find(f.Eq("avatar",Actor(actor))&f.In("plant",rows.Select(p=>p["_id"].AsString))).ToListAsync(token);
        return new{Items=rows.Select(p=>new{Plant=View(p),Karma=rewards.Where(r=>r["plant"]==p["_id"]).Sum(r=>r["amount"].AsInt32),
            PendingKarma=rewards.Where(r=>r["plant"]==p["_id"]&&!r["credited"].AsBoolean).Sum(r=>r["amount"].AsInt32),
            CareHistory=helped.Where(v=>v["plant"]==p["_id"]).OrderByDescending(v=>v["startedUtc"]).Select(v=>new{VisitedUtc=v["startedUtc"].ToUniversalTime(),Watered=v.GetValue("water",false).AsBoolean,Loved=v.GetValue("love",false).AsBoolean,MatureReward=v.GetValue("matureClaim",false).AsBoolean})}),Total=total,NextOffset=offset+50<total?(int?)(offset+50):null,RewardMessage=delivery.Message};
    }
    private async Task<int> Pending(Guid actor,CancellationToken token)
    {var rows=await Rewards.Find(Builders<BsonDocument>.Filter.Eq("avatar",Actor(actor))&Builders<BsonDocument>.Filter.Eq("credited",false)).Project(Builders<BsonDocument>.Projection.Include("amount")).ToListAsync(token);return rows.Sum(r=>r["amount"].AsInt32);}
    public async Task<GardenRewardDelivery> Settle(Guid actor,CancellationToken token)
    {
        await indexes.Value;
        string lease=Guid.NewGuid().ToString("D");var f=Builders<BsonDocument>.Filter;
        // Cross-replica ownership is held while the Core ledger/audit is credited. Six minutes
        // exceeds the bounded provider timeout; renew before each additional outbox row.
        var locked=await Gardeners.FindOneAndUpdateAsync(Id(Actor(actor))&(f.Exists("creditLeaseUntil",false)|f.Lt("creditLeaseUntil",DateTime.UtcNow)),
            Builders<BsonDocument>.Update.Set("creditLease",lease).Set("creditLeaseUntil",DateTime.UtcNow.AddMinutes(6)),cancellationToken:token);
        if(locked==null)return new(){Pending=await Pending(actor,token),Message="Gardening rewards are being processed."};
        using var renewalCancellation=new CancellationTokenSource();
        var renewal=RenewCreditLease(actor,lease,renewalCancellation.Token);
        try
        {
        var pending=await Rewards.Find(Builders<BsonDocument>.Filter.Eq("avatar",Actor(actor))&Builders<BsonDocument>.Filter.Eq("credited",false)).Sort(Builders<BsonDocument>.Sort.Ascending("at")).Limit(100).ToListAsync(token);
        foreach(var reward in pending){
            var renewed=await Gardeners.UpdateOneAsync(Id(Actor(actor))&f.Eq("creditLease",lease),Builders<BsonDocument>.Update.Set("creditLeaseUntil",DateTime.UtcNow.AddMinutes(6)),cancellationToken:token);
            if(renewed.MatchedCount!=1)throw new InvalidOperationException("Gardening reward credit ownership changed; the outbox remains pending.");
            var result=await credit(actor,Guid.Parse(reward["_id"].AsString),Guid.Parse(reward["plant"].AsString),
                reward["amount"].AsInt32,reward["description"].AsString,reward["at"].ToUniversalTime());
            if(renewal.IsCompleted)await renewal;
            if(result.IsError)return new(){Pending=await Pending(actor,token),Message="Your gardening actions are saved; Karma delivery is pending: "+result.Message};
            await Rewards.UpdateOneAsync(Id(reward["_id"].AsString),Builders<BsonDocument>.Update.Set("credited",true),cancellationToken:token);
        }
        return new(){Pending=await Pending(actor,token)};
        }
        finally{
            renewalCancellation.Cancel();
            try{await renewal;}catch(OperationCanceledException)when(renewalCancellation.IsCancellationRequested){}
            await Gardeners.UpdateOneAsync(Id(Actor(actor))&f.Eq("creditLease",lease),Builders<BsonDocument>.Update.Unset("creditLease").Unset("creditLeaseUntil"),cancellationToken:CancellationToken.None);
        }
    }
    private async Task RenewCreditLease(Guid actor,string lease,CancellationToken token)
    {
        var f=Builders<BsonDocument>.Filter;
        while(true){
            await Task.Delay(TimeSpan.FromSeconds(30),token);
            var renewed=await Gardeners.UpdateOneAsync(Id(Actor(actor))&f.Eq("creditLease",lease)&f.Gt("creditLeaseUntil",DateTime.UtcNow),
                Builders<BsonDocument>.Update.Set("creditLeaseUntil",DateTime.UtcNow.AddMinutes(6)),cancellationToken:token);
            if(renewed.MatchedCount!=1)throw new InvalidOperationException("Gardening credit lease expired; its durable outbox remains pending.");
        }
    }
    public async Task<int> RetryPending(CancellationToken token)
    {
        await indexes.Value;
        var actors=await Rewards.Aggregate().Match(Builders<BsonDocument>.Filter.Eq("credited",false)).Group(new BsonDocument("_id","$avatar")).Limit(20).ToListAsync(token);
        int pending=0;foreach(var actor in actors){var delivery=await Settle(Guid.Parse(actor["_id"].AsString),token);if(delivery.Pending>0)pending++;}return pending;
    }
}
