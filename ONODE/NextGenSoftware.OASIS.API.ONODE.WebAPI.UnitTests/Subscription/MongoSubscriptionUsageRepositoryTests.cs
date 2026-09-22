using System.Reflection;
using FluentAssertions;
using MongoDB.Bson.Serialization;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

public class MongoSubscriptionUsageRepositoryTests
{
    [Fact]
    public void UsageBucket_MapsExactlyOneMongoId()
    {
        var classMap = BsonClassMap.LookupClassMap(typeof(SubscriptionUsageBucket));
        classMap.AllMemberMaps.Count(member => member.ElementName == "_id").Should().Be(1);
        classMap.IdMemberMap.MemberName.Should().Be("Id");
    }

    [Fact]
    public void UsageEventProjection_MapsExactlyOneMongoId()
    {
        var document = typeof(MongoSubscriptionUsageRepository).GetNestedType("UsageEventDocument", BindingFlags.NonPublic)!;
        var classMap = BsonClassMap.LookupClassMap(document);
        classMap.AllMemberMaps.Count(member => member.ElementName == "_id").Should().Be(1);
        classMap.IdMemberMap.MemberName.Should().Be("Id");
    }
}
