using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicProjectionPlanningTests
{
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.Paging,
        Array.Empty<CosmicQueryFieldCapability>());
    private static CosmicProjectionCapabilities Names => new(
        new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
    private static CosmicProjectionCapabilities None => new(Array.Empty<CosmicProjectionFieldCapability>(), Array.Empty<Type>());

    [Fact]
    public void ProjectionNeedsItsOwnFieldPermissionAndExactDomain()
    {
        var query = CosmicQuery<Holon>.Create().Select(holon => holon.Name).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicProjectionTarget(ProviderType.MongoDBOASIS, Rows, None) }).ErrorCode);
        var wrongType = new CosmicProjectionCapabilities(
            new[] { new CosmicProjectionFieldCapability("Name", typeof(Guid)) }, Array.Empty<Type>());
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicProjectionTarget(ProviderType.MongoDBOASIS, Rows, wrongType) }).ErrorCode);
        Assert.False(CosmicProjectionPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicProjectionTarget(ProviderType.MongoDBOASIS, Rows, Names) }).IsError);
    }

    [Fact]
    public void ActualAdapterCannotBorrowProjectionOrSourcePermissions()
    {
        var query = CosmicQuery<Holon>.Create().Take(1).Select(holon => holon.Name).Result;
        var plan = CosmicProjectionPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicProjectionTarget(ProviderType.MongoDBOASIS, Rows, Names) }).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionPlanner.Admit(plan, ProviderType.MongoDBOASIS, Rows, None).ErrorCode);
        Assert.Equal("CQL_PLAN_INVALID", CosmicProjectionPlanner.Admit(plan, ProviderType.Neo4jOASIS, Rows, Names).ErrorCode);
        var unpaged = new CosmicQueryCapabilities(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionPlanner.Admit(plan, ProviderType.MongoDBOASIS, unpaged, Names).ErrorCode);
    }

    [Fact]
    public void MetadataReadDomainsAreExplicitIndependentOfFilterOperators()
    {
        var query = CosmicQuery<Holon>.Create().Select(holon => CosmicQueryMetadata.Value<long?>(holon, "stock")).Result;
        var metadata = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), new[] { typeof(long?) });
        Assert.False(CosmicProjectionPlanner.Plan(query, ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicProjectionTarget(ProviderType.SQLLiteDBOASIS, Rows, metadata) }).IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionPlanner.Plan(query, ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicProjectionTarget(ProviderType.SQLLiteDBOASIS, Rows, Names) }).ErrorCode);
    }

    [Fact]
    public void CapabilitiesSnapshotInputsAndBoundProjectionWidth()
    {
        var fields = new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) };
        var capability = new CosmicProjectionCapabilities(fields, Array.Empty<Type>(), maximumColumns: 1);
        fields[0] = new CosmicProjectionFieldCapability("Id", typeof(Guid));
        var query = CosmicQuery<Holon>.Create().Select(holon => new { holon.Name, holon.Id }).Result;
        Assert.Equal("Name", capability.CanonicalFields[0].Name);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionPlanner.Plan(query, ProviderType.Neo4jOASIS,
            new[] { new CosmicProjectionTarget(ProviderType.Neo4jOASIS, Rows, capability) }).ErrorCode);
        Assert.Throws<ArgumentException>(() => new CosmicProjectionCapabilities(fields, Array.Empty<Type>(), maximumColumns: 33));
    }
}
