using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class MongoSerializerOwnershipTests
{
    [Fact]
    public void NativePriceBsonContractDoesNotDependOnCreatingAnOutboxFirst()
    {
        var id=Guid.NewGuid();var time=new DateTime(2026,10,17,1,2,3,DateTimeKind.Utc).AddTicks(1234567);
        var quote=new NextGenSoftware.OASIS.API.Core.Services.Subscriptions.NativeFeePriceQuote(id,"SuiOASIS","fixture","MIST","fixture",9,
            1.234567890123456789m,"synthetic","fixture","fixture",time,time,time,time.AddMinutes(5));
        var raw=quote.ToBsonDocument();
        Assert.Equal(BsonType.Binary,raw["OperationId"].BsonType);
        Assert.Equal(BsonBinarySubType.UuidStandard,raw["OperationId"].AsBsonBinaryData.SubType);
        Assert.Equal(BsonType.Decimal128,raw["UsdPerWholeUnit"].BsonType);
        Assert.Equal(BsonType.String,raw["BoundAtUtc"].BsonType);
        Assert.Equal(quote,BsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Core.Services.Subscriptions.NativeFeePriceQuote>(raw));
        var valuation=quote.ValueOriginalReceipt("serialization-only","1000000000");
        var fee=valuation.ToBsonDocument();
        Assert.Equal(BsonBinarySubType.UuidStandard,fee["OperationId"].AsBsonBinaryData.SubType);
        Assert.Equal(BsonType.Decimal128,fee["NetCostUsd"].BsonType);Assert.Equal(BsonType.String,fee["ValuedAtUtc"].BsonType);
        Assert.Equal(valuation,BsonSerializer.Deserialize<NextGenSoftware.OASIS.API.Core.Services.Subscriptions.NativeFeeValuation>(fee));
    }
    [Fact]
    public void ProviderConstructionDoesNotReplaceAnAlreadyResolvedGlobalObjectSerializer()
    {
        var original=BsonSerializer.LookupSerializer<object>();
        for(var index=0;index<3;index++)new MongoDBOASIS("mongodb://127.0.0.1:1","owned_no_connection");
        Assert.Same(original,BsonSerializer.LookupSerializer<object>());
    }

    [Fact]
    public void ConcurrentProviderConstructionDoesNotReregisterProcessGlobalSerializers()
    {
        var original=BsonSerializer.LookupSerializer<object>();
        Parallel.For(0,32,_=>new MongoDBOASIS("mongodb://127.0.0.1:1","owned_no_connection"));
        Assert.Same(original,BsonSerializer.LookupSerializer<object>());
    }

    [Fact]
    public void OasisObjectMembersRetainConfiguredAllowedTypesWithoutGlobalReplacement()
    {
        var original=BsonSerializer.LookupSerializer<object>();
        new MongoDBOASIS("mongodb://127.0.0.1:1","owned_no_connection");
        var encoded=new OwnedCarrier {Value=new RefreshToken {Token="synthetic-roundtrip"}}.ToBson();
        var restored=BsonSerializer.Deserialize<OwnedCarrier>(encoded);
        Assert.Equal("synthetic-roundtrip",Assert.IsType<RefreshToken>(restored.Value).Token);
        Assert.Same(original,BsonSerializer.LookupSerializer<object>());
    }

    public sealed class OwnedCarrier {public object Value {get;set;}=null!;}
}
