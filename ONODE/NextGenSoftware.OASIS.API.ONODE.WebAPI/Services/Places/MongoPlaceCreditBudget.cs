using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Places;

/// <summary>One majority-acknowledged daily pool shared by all replicas and key rotations.</summary>
public sealed class MongoPlaceCreditBudget
{
    public const int DailyLimit=1000;
    private readonly IMongoCollection<BsonDocument> ledger;
    private readonly string pool;
    public MongoPlaceCreditBudget(IMongoCollection<BsonDocument> ledger,string pool="our-world-places")
    {this.ledger=ledger.WithWriteConcern(WriteConcern.WMajority);this.pool=pool;}
    public string CurrentId=>"geoapify:"+pool+":"+DateTime.UtcNow.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
    public async Task Reserve(int credits,CancellationToken token)
    {
        if(credits<1||credits>DailyLimit)throw new ArgumentOutOfRangeException(nameof(credits));
        var identity=Builders<BsonDocument>.Filter.Eq("_id",CurrentId);
        try
        {
            await ledger.UpdateOneAsync(identity,Builders<BsonDocument>.Update.SetOnInsert("used",0),new UpdateOptions{IsUpsert=true},token);
        }
        catch(MongoWriteException ex)when(ex.WriteError.Category==ServerErrorCategory.DuplicateKey) { /* Another replica created today's counter. */ }
        var result=await ledger.FindOneAndUpdateAsync(identity & Builders<BsonDocument>.Filter.Lte("used",DailyLimit-credits),
            Builders<BsonDocument>.Update.Inc("used",credits).Set("updatedUtc",DateTime.UtcNow),
            new FindOneAndUpdateOptions<BsonDocument>{ReturnDocument=ReturnDocument.After},token);
        if(result==null)throw new InvalidOperationException("The app's free daily place-data allowance has been reached. No paid requests will be made.");
    }
}
