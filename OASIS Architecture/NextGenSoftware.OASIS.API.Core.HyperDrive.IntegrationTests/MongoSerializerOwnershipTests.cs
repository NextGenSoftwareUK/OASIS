using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class MongoSerializerOwnershipTests
{
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
