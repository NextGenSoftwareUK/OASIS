using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedFilterAdmissionTests
{
    private static readonly CosmicQueryComparisonOperator[] All = System.Enum.GetValues<CosmicQueryComparisonOperator>();
    private const CosmicQueryFeatures Logic = CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Not |
        CosmicQueryFeatures.NullLiteral | CosmicQueryFeatures.BooleanConstant | CosmicQueryFeatures.FieldComparison;
    private static CosmicProjectionCapabilities Fields => new(new[] {
        new CosmicProjectionFieldCapability("Name", typeof(string)), new CosmicProjectionFieldCapability("Version", typeof(int)) }, Array.Empty<Type>());
    private static CosmicGroupedCapabilities Profile(CosmicGroupedFilteringCapabilities? filter) => new(Fields,
        new CosmicScalarCapabilities(new[] { CosmicScalarOperation.LongCount }, new[] {
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, Fields),
            new CosmicScalarSelectionCapability(CosmicScalarOperation.Average, Fields) }), null, false, filter);
    private static CosmicGroupedFilteringCapabilities Counts(CosmicQueryFeatures features = Logic) => new(features,
        Array.Empty<CosmicQueryMetadataCapability>(), new[] { new CosmicGroupedAggregateFilteringCapability(
            CosmicScalarOperation.LongCount, new CosmicQueryMetadataCapability(typeof(long), All)) });

    [Fact]
    public void FilterPermissionIsIndependentForKeysOperationsAndResultTypes()
    {
        var selected = CosmicQuery<Holon>.Create().GroupBy(h => h.Name)
            .Select(g => new { Name = g.Key, Rows = g.LongCount(), Total = g.Sum(h => h.Version), Mean = g.Average(h => h.Version) }).Result;
        Assert.False(Profile(Counts()).Admit(selected.Where(row => row.Rows > 1).Result).IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile(Counts()).Admit(selected.Where(row => row.Name == "x").Result).ErrorCode);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile(Counts()).Admit(selected.Where(row => row.Total > 1).Result).ErrorCode);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile(Counts()).Admit(selected.Where(row => row.Mean > 1m).Result).ErrorCode);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile(null).Admit(selected.Where(row => row.Rows > 1).Result).ErrorCode);
    }

    [Fact]
    public void FeaturesMembershipMixedLiteralsAndExactOperatorsMustAllBeDeclared()
    {
        var names = new[] { "coffee" };
        var selected = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => new { Name = g.Key, Rows = g.LongCount() }).Result;
        var stringOnly = new CosmicGroupedFilteringCapabilities(Logic | CosmicQueryFeatures.Membership | CosmicQueryFeatures.StringStartsWith,
            new[] { new CosmicQueryMetadataCapability(typeof(string), All, new[] { CosmicQueryStringOperator.StartsWith }) },
            Array.Empty<CosmicGroupedAggregateFilteringCapability>());
        Assert.False(Profile(stringOnly).Admit(selected.Where(row => row.Name.StartsWith("c", StringComparison.Ordinal)).Result).IsError);
        Assert.True(Profile(stringOnly).Admit(selected.Where(row => names.Contains(row.Name)).Result).IsError);
        var noFeature = new CosmicGroupedFilteringCapabilities(Logic,
            new[] { new CosmicQueryMetadataCapability(typeof(string), All, new[] { CosmicQueryStringOperator.StartsWith }) },
            Array.Empty<CosmicGroupedAggregateFilteringCapability>());
        Assert.True(Profile(noFeature).Admit(selected.Where(row => row.Name.StartsWith("c", StringComparison.Ordinal)).Result).IsError);
        Assert.True(Profile(Counts(Logic | CosmicQueryFeatures.MixedNumericComparison)).Admit(selected.Where(row => row.Rows > 1.5m).Result).IsError);
        var equality = new CosmicGroupedFilteringCapabilities(Logic, Array.Empty<CosmicQueryMetadataCapability>(),
            new[] { new CosmicGroupedAggregateFilteringCapability(CosmicScalarOperation.LongCount,
                new CosmicQueryMetadataCapability(typeof(long), new[] { CosmicQueryComparisonOperator.Equal })) });
        Assert.True(Profile(equality).Admit(selected.Where(row => row.Rows > 1).Result).IsError);
        Assert.False(Profile(equality).Admit(selected.Where(row => row.Rows == 1).Result).IsError);
        var mixed = new CosmicGroupedFilteringCapabilities(Logic | CosmicQueryFeatures.MixedNumericComparison,
            Array.Empty<CosmicQueryMetadataCapability>(), new[] { new CosmicGroupedAggregateFilteringCapability(
                CosmicScalarOperation.LongCount, new CosmicQueryMetadataCapability(typeof(long), All, supportsMixedNumericComparison: true)) });
        Assert.False(Profile(mixed).Admit(selected.Where(row => row.Rows > 1.5m).Result).IsError);
        var membership = new CosmicGroupedFilteringCapabilities(Logic | CosmicQueryFeatures.Membership,
            new[] { new CosmicQueryMetadataCapability(typeof(string), All, supportsMembership: true) },
            Array.Empty<CosmicGroupedAggregateFilteringCapability>());
        Assert.False(Profile(membership).Admit(selected.Where(row => names.Contains(row.Name)).Result).IsError);
    }

    [Fact]
    public void ActualAdapterCannotBorrowClaimedFilterPermission()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).Where(rows => rows > 1).Result;
        var rows = new CosmicQueryCapabilities(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
        var plan = CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicGroupedTarget(ProviderType.MongoDBOASIS, rows, Profile(Counts())) });
        Assert.False(plan.IsError, plan.Message);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicGroupedPlanner.Admit(plan.Result, ProviderType.MongoDBOASIS, rows, Profile(null)).ErrorCode);
    }

    [Fact]
    public void CapabilityInputsAreSnapshottedBoundedAndRejectImpossibleAggregateDomains()
    {
        var keys = new[] { new CosmicQueryMetadataCapability(typeof(string), All) };
        var aggregates = new[] { new CosmicGroupedAggregateFilteringCapability(CosmicScalarOperation.LongCount,
            new CosmicQueryMetadataCapability(typeof(long), All)) };
        var profile = new CosmicGroupedFilteringCapabilities(Logic, keys, aggregates);
        keys[0] = new CosmicQueryMetadataCapability(typeof(int), All); aggregates[0] = null!;
        Assert.Equal(typeof(string), profile.Keys[0].ValueType); Assert.NotNull(profile.Aggregates[0]);
        Assert.Throws<ArgumentException>(() => new CosmicGroupedFilteringCapabilities(CosmicQueryFeatures.GraphExpansion, keys, Array.Empty<CosmicGroupedAggregateFilteringCapability>()));
        Assert.Throws<ArgumentException>(() => new CosmicGroupedAggregateFilteringCapability(CosmicScalarOperation.Average,
            new CosmicQueryMetadataCapability(typeof(long), All)));
        Assert.Throws<ArgumentException>(() => new CosmicGroupedFilteringCapabilities(Logic, new[] { keys[0], keys[0] }, Array.Empty<CosmicGroupedAggregateFilteringCapability>()));
    }

    [Fact]
    public void RepeatedWhereClausesRequireAndSupport()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.LongCount()).Where(rows => rows > 1).Where(rows => rows < 10).Result;
        Assert.True(Profile(Counts(CosmicQueryFeatures.None)).Admit(query).IsError);
        Assert.False(Profile(Counts()).Admit(query).IsError);
    }
}
