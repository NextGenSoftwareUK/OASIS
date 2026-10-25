using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicProjectionCompositionPlanningTests
{
    private const CosmicSelectedStageFeatures AllStages = CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging;
    private const CosmicQueryFeatures Logic = CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Not | CosmicQueryFeatures.NullLiteral | CosmicQueryFeatures.BooleanConstant;
    private static readonly CosmicQueryComparisonOperator[] Comparisons = Enum.GetValues<CosmicQueryComparisonOperator>();
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.Paging, Array.Empty<CosmicQueryFieldCapability>());
    private static CosmicProjectionCapabilities Columns => new(new[] {
        new CosmicProjectionFieldCapability("Name", typeof(string)), new CosmicProjectionFieldCapability("Version", typeof(int)) }, Array.Empty<Type>());
    private static CosmicProjectionCompositionCapabilities Profile(CosmicSelectedStageFeatures stages = AllStages,
        CosmicQueryFeatures features = Logic, Type[]? filters = null, Type[]? ordering = null)
        => new(stages, features, (filters ?? new[] { typeof(string), typeof(int) })
            .Select(type => new CosmicQueryMetadataCapability(type, Comparisons)).ToArray(), ordering ?? new[] { typeof(string), typeof(int) });
    private static CosmicProjectionCompositionTarget Target(ProviderType provider, CosmicProjectionCompositionCapabilities? profile = null)
        => new(provider, Rows, Columns, profile ?? Profile());

    [Fact]
    public void GraphSourcesCannotBorrowFlatCompositionProofPermissions()
    {
        var query = CosmicQuery<Holon>.Create().WithChildren(CosmicQueryGraphOptions.Create().Result).Select(h => h.Name).Take(1).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile().Admit(query).ErrorCode);
    }

    [Fact]
    public void ExplicitCompositionPlanPreservesSourceStagesAndInputBudgets()
    {
        var source = CosmicQuery<Holon>.Create().Skip(2).Take(3).Result;
        var query = source.Select(h => new { Label = h.Name, Revision = h.Version })
            .Take(2).Where(row => row.Revision > 1).OrderBy(row => row.Label).Result;
        var options = new CosmicProjectionCompositionOptions(7, 1000, 50);
        var plan = CosmicProjectionCompositionPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { Target(ProviderType.MongoDBOASIS) }, options);
        Assert.False(plan.IsError, plan.Message);
        Assert.Same(query, plan.Result.Query);
        Assert.Same(source, plan.Result.SourcePlan.Query);
        Assert.Same(options, plan.Result.Options);
        Assert.Equal(7, plan.Result.Options.MaximumInputRows);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Columns.Admit(query).ErrorCode);
    }

    [Theory]
    [InlineData(CosmicSelectedStageFeatures.Filter)]
    [InlineData(CosmicSelectedStageFeatures.Ordering)]
    [InlineData(CosmicSelectedStageFeatures.Paging)]
    public void EachStageRequiresItsOwnPermission(CosmicSelectedStageFeatures missing)
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Version).Where(value => value > 1).OrderBy(value => value).Take(1).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile(AllStages & ~missing).Admit(query).ErrorCode);
    }

    [Fact]
    public void SelectedFilterAndOrderingDomainsAreIndependentAndExact()
    {
        var selected = CosmicQuery<Holon>.Create().Select(h => h.Version);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile(filters: new[] { typeof(long) }).Admit(selected.Where(value => value > 1).Result).ErrorCode);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile(ordering: new[] { typeof(long) }).Admit(selected.OrderBy(value => value).Result).ErrorCode);
        Assert.False(Profile(filters: Array.Empty<Type>()).Admit(selected.OrderBy(value => value).Result).IsError);
    }

    [Fact]
    public void ExactFilterOperatorsCannotBeBorrowedFromAnotherDomain()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Version).Where(value => value > 1).Result;
        var onlyEqual = new CosmicProjectionCompositionCapabilities(AllStages, Logic,
            new[] { new CosmicQueryMetadataCapability(typeof(int), new[] { CosmicQueryComparisonOperator.Equal }) }, new[] { typeof(int) });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", onlyEqual.Admit(query).ErrorCode);
    }

    [Fact]
    public void StringOperationsRequireFeatureAndExactDomainOperatorPermission()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Where(value => value.StartsWith("c", StringComparison.Ordinal)).Result;
        var features = Logic | CosmicQueryFeatures.StringStartsWith;
        var allowed = new CosmicProjectionCompositionCapabilities(AllStages, features,
            new[] { new CosmicQueryMetadataCapability(typeof(string), Comparisons, new[] { CosmicQueryStringOperator.StartsWith }) }, new[] { typeof(string) });
        Assert.False(allowed.Admit(query).IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile(features: features).Admit(query).ErrorCode);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile().Admit(query).ErrorCode);
    }

    [Fact]
    public void MembershipNeedsFeatureAndPerDomainEnrollment()
    {
        var names = new[] { "coffee" };
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Where(value => names.Contains(value)).Result;
        var features = Logic | CosmicQueryFeatures.Membership;
        var allowed = new CosmicProjectionCompositionCapabilities(AllStages, features,
            new[] { new CosmicQueryMetadataCapability(typeof(string), Comparisons, supportsMembership: true) }, new[] { typeof(string) });
        Assert.False(allowed.Admit(query).IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Profile(features: features).Admit(query).ErrorCode);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public void ActualProviderReadmissionCannotBorrowPlannedPermissions(ProviderType provider)
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Result;
        var plan = CosmicProjectionCompositionPlanner.Plan(query, provider, new[] { Target(provider) }).Result;
        Assert.False(CosmicProjectionCompositionPlanner.Admit(plan, provider, Rows, Columns, Profile()).IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionCompositionPlanner.Admit(plan, provider,
            Rows, Columns, Profile(CosmicSelectedStageFeatures.None)).ErrorCode);
        var other = provider == ProviderType.Neo4jOASIS ? ProviderType.SQLLiteDBOASIS : ProviderType.Neo4jOASIS;
        Assert.Equal("CQL_PLAN_INVALID", CosmicProjectionCompositionPlanner.Admit(plan, other, Rows, Columns, Profile()).ErrorCode);
    }

    [Fact]
    public void AllSelectedTargetsMustAdmitEveryStageBeforeExecution()
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).Select(h => h.Name).Take(1).Result;
        var targets = new[] { Target(ProviderType.MongoDBOASIS), Target(ProviderType.SQLLiteDBOASIS, Profile(CosmicSelectedStageFeatures.None)) };
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionCompositionPlanner.Plan(query, ProviderType.MongoDBOASIS, targets).ErrorCode);
    }

    [Fact]
    public void SourceAndProjectionPermissionsRemainRequired()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Result;
        var noColumns = new CosmicProjectionCapabilities(Array.Empty<CosmicProjectionFieldCapability>(), Array.Empty<Type>());
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionCompositionPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { new CosmicProjectionCompositionTarget(ProviderType.MongoDBOASIS, Rows, noColumns, Profile()) }).ErrorCode);
        var sourceFilter = CosmicQuery<Holon>.Create().Where(h => h.Version > 1).Select(h => h.Name).Take(1).Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionCompositionPlanner.Plan(sourceFilter, ProviderType.MongoDBOASIS,
            new[] { Target(ProviderType.MongoDBOASIS) }).ErrorCode);
    }

    [Fact]
    public void CapabilityArraysAndTargetListsAreFrozenBeforeCallerMutation()
    {
        var domains = new[] { new CosmicQueryMetadataCapability(typeof(int), Comparisons) };
        var sorts = new[] { typeof(int) };
        var profile = new CosmicProjectionCompositionCapabilities(AllStages, Logic, domains, sorts);
        domains[0] = new CosmicQueryMetadataCapability(typeof(long), Comparisons); sorts[0] = typeof(long);
        var query = CosmicQuery<Holon>.Create().Select(h => h.Version).Where(value => value > 1).OrderBy(value => value).Result;
        var targets = new[] { Target(ProviderType.MongoDBOASIS, profile) };
        var plan = CosmicProjectionCompositionPlanner.Plan(query, ProviderType.MongoDBOASIS, targets).Result;
        targets[0] = Target(ProviderType.SQLLiteDBOASIS);
        Assert.Equal(ProviderType.MongoDBOASIS, plan.Targets.Single().Provider);
        Assert.Equal(typeof(int), profile.FilterDomains.Single().ValueType);
        Assert.Equal(typeof(int), profile.OrderingDomains.Single());
    }

    [Fact]
    public void InvalidCapabilityDeclarationsAndEmptyPipelinesReject()
    {
        Assert.Throws<ArgumentException>(() => Profile((CosmicSelectedStageFeatures)128));
        Assert.Throws<ArgumentException>(() => Profile(features: CosmicQueryFeatures.Paging));
        Assert.Throws<ArgumentException>(() => Profile(filters: new[] { typeof(int), typeof(int) }));
        Assert.Throws<ArgumentException>(() => Profile(ordering: new[] { typeof(object) }));
        Assert.True(Profile().Admit(CosmicQuery<Holon>.Create().Select(h => h.Name).Result).IsError);
    }

    [Theory]
    [InlineData(0, 100, 10)] [InlineData(10001, 100, 10)] [InlineData(1, 0, 10)]
    [InlineData(1, 33554433, 10)] [InlineData(1, 100, 0)] [InlineData(1, 100, 320001)]
    public void InputAndCumulativeStageWorkBoundsCannotBeUpgraded(int rows, int bytes, int work)
        => Assert.Throws<ArgumentException>(() => new CosmicProjectionCompositionOptions(rows, bytes, work));
}
