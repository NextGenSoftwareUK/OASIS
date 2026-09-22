using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

public class MongoSubscriptionUsageRepositoryTests
{
    [Fact]
    public void UsageAggregateDocument_MapsExactlyOneMongoId()
    {
        Type aggregateDocumentType = typeof(MongoSubscriptionUsageRepository).GetNestedType(
            "UsageAggregateDocument", BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("UsageAggregateDocument persistence type was not found.");

        BsonClassMap classMap = BsonClassMap.LookupClassMap(aggregateDocumentType);

        classMap.AllMemberMaps.Count(member => member.ElementName == "_id").Should().Be(1);
        classMap.IdMemberMap.Should().NotBeNull();
        classMap.IdMemberMap.MemberName.Should().Be("Id");
    }
}
