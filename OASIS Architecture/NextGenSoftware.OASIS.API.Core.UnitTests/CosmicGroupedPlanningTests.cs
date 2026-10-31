using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicGroupedPlanningTests
{
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.Paging, Array.Empty<CosmicQueryFieldCapability>());
    private static CosmicProjectionCapabilities Names => new(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
    private static CosmicProjectionCapabilities None => new(Array.Empty<CosmicProjectionFieldCapability>(), Array.Empty<Type>());
    private static CosmicGroupedCapabilities Counts => new(Names, new CosmicScalarCapabilities(CosmicScalarOperation.Count));
    private static CosmicGroupedTarget Target(CosmicGroupedCapabilities capabilities)
        => new(ProviderType.MongoDBOASIS, Rows, capabilities);

    [Fact]
    public void GroupPlanPreservesSourceAndSnapshotsTargets()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        var targets = new[] { Target(Counts) };
        var result = CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS, targets);
        Assert.False(result.IsError, result.Message);
        Assert.Same(query, result.Result.Query);
        Assert.Same(query.Source, result.Result.SourcePlan.Query);
        targets[0] = null!;
        Assert.NotNull(result.Result.Targets[0]);
    }

    [Fact]
    public void OmittedResultKeyStillRequiresEveryGroupingKeyPermission()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        var denied = new CosmicGroupedCapabilities(None, new CosmicScalarCapabilities(CosmicScalarOperation.Count));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS, new[] { Target(denied) }).ErrorCode);
    }

    [Fact]
    public void KeyReadPermissionCannotGrantAggregateOperation()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        var denied = new CosmicGroupedCapabilities(Names, new CosmicScalarCapabilities(CosmicScalarOperation.LongCount));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS, new[] { Target(denied) }).ErrorCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectedAggregateReadDomainIsIndependentlyAdmitted(bool wrongType)
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Sum(h => h.Version)).Result;
        var reads = new CosmicProjectionCapabilities(new[] { new CosmicProjectionFieldCapability("Version", wrongType ? typeof(long) : typeof(int)) }, Array.Empty<Type>());
        var profile = new CosmicGroupedCapabilities(Names, new CosmicScalarCapabilities(Array.Empty<CosmicScalarOperation>(),
            new[] { new CosmicScalarSelectionCapability(CosmicScalarOperation.Sum, reads) }));
        var result = CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS, new[] { Target(profile) });
        Assert.Equal(wrongType, result.IsError);
    }

    [Fact]
    public void ActualAdapterCannotBorrowAPlannedGroupingProfile()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        var plan = CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS, new[] { Target(Counts) }).Result;
        var denied = new CosmicGroupedCapabilities(None, new CosmicScalarCapabilities(CosmicScalarOperation.Count));
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicGroupedPlanner.Admit(plan, ProviderType.MongoDBOASIS, Rows, denied).ErrorCode);
        Assert.Equal("CQL_PLAN_INVALID", CosmicGroupedPlanner.Admit(plan, ProviderType.SQLLiteDBOASIS, Rows, Counts).ErrorCode);
    }

    [Fact]
    public void ActualSourceWindowPermissionCannotBeBorrowed()
    {
        var query = CosmicQuery<Holon>.Create().Take(1).GroupBy(h => h.Name).Select(g => g.Count()).Result;
        var plan = CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS, new[] { Target(Counts) }).Result;
        var noPaging = new CosmicQueryCapabilities(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
        Assert.True(CosmicGroupedPlanner.Admit(plan, ProviderType.MongoDBOASIS, noPaging, Counts).IsError);
    }

    [Fact]
    public void GroupAndColumnBoundsMustFitActualProfile()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name, CosmicGroupingOptions.Create(1001).Result).Select(g => g.Count()).Result;
        Assert.True(CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS, new[] { Target(Counts) }).IsError);
        var wide = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => new { g.Key, Count = g.Count() }).Result;
        var narrow = new CosmicGroupedCapabilities(Names, new CosmicScalarCapabilities(CosmicScalarOperation.Count), maximumOutputColumns: 1);
        Assert.True(CosmicGroupedPlanner.Plan(wide, ProviderType.MongoDBOASIS, new[] { Target(narrow) }).IsError);
    }

    [Fact]
    public void InvalidAndAmbiguousTargetSetsCannotPlan()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        Assert.True(CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS, null!).IsError);
        Assert.True(CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS, new[] { Target(Counts), Target(Counts) }).IsError);
    }
    [Fact]
    public void AllEligiblePlanningRejectsAnUnsupportedLaterTarget()
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).GroupBy(h => h.Name).Select(g => g.Count()).Result;
        var denied = new CosmicGroupedCapabilities(None, new CosmicScalarCapabilities(CosmicScalarOperation.Count));
        var targets = new[] { Target(Counts), new CosmicGroupedTarget(ProviderType.SQLLiteDBOASIS, Rows, denied) };
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS, targets).ErrorCode);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10001, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 33)]
    public void InvalidGroupedProfilesCannotDeclareCapabilities(int groups, int columns)
        => Assert.Throws<ArgumentException>(() => new CosmicGroupedCapabilities(Names,
            new CosmicScalarCapabilities(CosmicScalarOperation.Count), maximumGroups: groups, maximumOutputColumns: columns));

    [Fact]
    public void CompositeIdentityCannotHideAnUnselectedKeyComponent()
    {
        var query = CosmicQuery<Holon>.Create().GroupBy(h => new { h.Name, h.Version }).Select(g => g.Count()).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicGroupedPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { Target(Counts) }).ErrorCode);
    }

    [Fact]
    public void PayloadCeilingIsAdmittedSeparatelyFromGroupCount()
    {
        var profile = new CosmicGroupedCapabilities(Names, new CosmicScalarCapabilities(CosmicScalarOperation.Count), maximumPayloadBytes: 1024);
        var bounded = CosmicQuery<Holon>.Create().GroupBy(h => h.Name, CosmicGroupingOptions.Create(maximumPayloadBytes: 1024).Result).Select(g => g.Count()).Result;
        Assert.False(CosmicGroupedPlanner.Plan(bounded, ProviderType.MongoDBOASIS, new[] { Target(profile) }).IsError);
        var larger = CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Count()).Result;
        Assert.True(CosmicGroupedPlanner.Plan(larger, ProviderType.MongoDBOASIS, new[] { Target(profile) }).IsError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(33554433)]
    public void InvalidPayloadProfilesCannotBeDeclared(int bytes)
        => Assert.Throws<ArgumentException>(() => new CosmicGroupedCapabilities(Names,
            new CosmicScalarCapabilities(CosmicScalarOperation.Count), maximumPayloadBytes: bytes));

}
