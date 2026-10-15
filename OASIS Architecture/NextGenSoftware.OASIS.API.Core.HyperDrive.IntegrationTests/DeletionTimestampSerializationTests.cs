using System;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;
using Xunit;

public class DeletionTimestampSerializationTests
{
    public DeletionTimestampSerializationTests()
    {
        NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton.SerializerRegister.GetInstance().RegisterGuidBsonSerializer();
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExactDeletionDateRoundTripsRegardlessOfFieldOrder(bool reverse)
    {
        var exact=new DateTime(2026,10,14,1,2,3,DateTimeKind.Utc).AddTicks(1234567);
        var document=new HolonBase {DeletedDate=exact}.ToBsonDocument();
        Assert.True(document["DeletedDate"].IsBsonDateTime);
        Assert.Equal(exact.Ticks,document["DeletedDateExactUtcTicks"].AsInt64);
        if(reverse) document=new BsonDocument(document.Elements.Reverse());
        var loaded=BsonSerializer.Deserialize<HolonBase>(document);
        Assert.Equal(exact,loaded.DeletedDate);
        loaded.DeletedDate=exact.AddTicks(1);
        Assert.Equal(exact.AddTicks(1),BsonSerializer.Deserialize<HolonBase>(loaded.ToBson()).DeletedDate);
        loaded.DeletedDate=DateTime.MinValue;
        Assert.False(loaded.ToBsonDocument().Contains("DeletedDateExactUtcTicks"));
    }

    [Fact]
    public void LegacyDatesRemainReadableAndConflictingPrecisionIsRejected()
    {
        var date=new DateTime(2026,10,14,1,2,3,DateTimeKind.Utc);
        var legacy=new BsonDocument("DeletedDate",new BsonDateTime(date));
        Assert.Equal(date,BsonSerializer.Deserialize<HolonBase>(legacy).DeletedDate);
        legacy["DeletedDateExactUtcTicks"]=date.AddSeconds(1).Ticks;
        Assert.ThrowsAny<Exception>(()=>BsonSerializer.Deserialize<HolonBase>(legacy));
    }
}
