using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicProjectedAllTests
{
    private const CosmicSelectedStageFeatures Stages = CosmicSelectedStageFeatures.Filter | CosmicSelectedStageFeatures.Ordering | CosmicSelectedStageFeatures.Paging;
    private const CosmicQueryFeatures Logic = CosmicQueryFeatures.And | CosmicQueryFeatures.Or | CosmicQueryFeatures.Not | CosmicQueryFeatures.NullLiteral | CosmicQueryFeatures.BooleanConstant;
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.Paging, Array.Empty<CosmicQueryFieldCapability>());
    private static CosmicProjectionCapabilities Columns => new(new[] {
        new CosmicProjectionFieldCapability("Name", typeof(string)), new CosmicProjectionFieldCapability("Version", typeof(int)) }, Array.Empty<Type>());
    private static CosmicProjectionCompositionCapabilities Profile(bool all = true, Type[]? domains = null)
        => new(Stages, Logic, (domains ?? new[] { typeof(string), typeof(int) })
            .Select(type => new CosmicQueryMetadataCapability(type, Enum.GetValues<CosmicQueryComparisonOperator>())).ToArray(),
            new[] { typeof(string), typeof(int) }, 32, all);
    private static CosmicProjectionCompositionTarget Target(bool all = true)
        => new(ProviderType.SQLLiteDBOASIS, Rows, Columns, Profile(all));

    [Fact]
    public void PlainScalarAllCapturesWithoutInventingASelectedFilterStage()
    {
        var projection = CosmicQuery<Holon>.Create().Select(row => row.Version).Result;
        var query = projection.All(value => value > 2);
        Assert.False(query.IsError, query.Message);
        Assert.Same(projection, query.Result.Projection);
        Assert.Empty(projection.ResultStages);
        Assert.Equal("Value", Assert.Single(Assert.IsType<CosmicQueryComparison>(query.Result.Predicate).Path.Segments));
        Assert.False(Profile().AdmitAll(query.Result).IsError);
    }

    [Fact]
    public void TerminalKeepsBothSourceAndSelectedResultWindows()
    {
        var source = CosmicQuery<Holon>.Create().Skip(2).Take(10).Result;
        var projection = source.Select(row => new { Label = row.Name, Revision = row.Version })
            .Take(5).Where(row => row.Revision > 0).OrderBy(row => row.Label).Skip(1).Take(2).Result;
        var query = projection.All(row => row.Revision > 3);
        Assert.False(query.IsError, query.Message);
        Assert.Same(projection, query.Result.Projection);
        Assert.Same(source, query.Result.Projection.Source);
        Assert.Equal(5, projection.ResultStages.Count);
        Assert.Equal("Revision", Assert.Single(Assert.IsType<CosmicQueryComparison>(query.Result.Predicate).Path.Segments));
        Assert.False(Profile().AdmitAll(query.Result).IsError);
    }

    [Fact]
    public void TerminalDoesNotConsumeASelectedStageOrExecuteCapturedCode()
    {
        var projection = CosmicQuery<Holon>.Create().Select(row => row.Version);
        for (var index = 0; index < 32; index++) projection = projection.Take(1);
        var query = projection.All(value => true);
        Assert.False(query.IsError, query.Message);
        Assert.Equal(32, query.Result.Projection.ResultStages.Count);
        Assert.False(Profile().AdmitAll(query.Result).IsError);
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED", projection.All(value => Callback(value)).ErrorCode);
    }
    private static bool Callback(int value) => throw new InvalidOperationException("Predicate callbacks must not run.");

    [Fact]
    public void CapturedLiteralIsFrozenAndArbitraryCallbackIsRejected()
    {
        var value = 2;
        var projection = CosmicQuery<Holon>.Create().Select(row => row.Version);
        var query = projection.All(selected => selected > value);
        value = 99;
        Assert.Equal(2L, Assert.IsType<CosmicQueryComparison>(query.Result.Predicate).Literal.Value);
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED", projection.All(selected => Callback(selected)).ErrorCode);
    }

    [Fact]
    public void FailedCompositionAndMissingPredicateDoNotBecomeVacuousTrue()
    {
        var failed = new OASISResult<CosmicProjectionQuery<Holon, int>> { IsError = true, ErrorCode = "original", Message = "original failure" };
        Assert.Equal("original", failed.All(value => Callback(value)).ErrorCode);
        var projection = CosmicQuery<Holon>.Create().Select(row => row.Version).Result;
        Assert.Equal("CQL_QUERY_INVALID", projection.All(null!).ErrorCode);
    }

    [Fact]
    public void ExistingCompositionPermissionDoesNotGrantProjectedAll()
    {
        var query = CosmicQuery<Holon>.Create().Select(row => row.Version).All(value => value > 0).Result;
        var oldConstructor = new CosmicProjectionCompositionCapabilities(Stages, Logic,
            new[] { new CosmicQueryMetadataCapability(typeof(int), Enum.GetValues<CosmicQueryComparisonOperator>()) }, new[] { typeof(int) });
        Assert.False(oldConstructor.SupportsAll);
        Assert.True(oldConstructor.AdmitAll(query).IsError);
        Assert.True(Profile(domains: new[] { typeof(string) }).AdmitAll(query).IsError);
    }

    [Fact]
    public void PlannerAndLiveAdmissionKeepIndependentPermissionAndWorkBounds()
    {
        var query = CosmicQuery<Holon>.Create().Select(row => row.Version).All(value => value > 0).Result;
        Assert.True(CosmicProjectedAllPlanner.Plan(query, ProviderType.SQLLiteDBOASIS, new[] { Target(false) }).IsError);
        var options = new CosmicProjectionCompositionOptions(2, 500, 10, 600);
        var plan = CosmicProjectedAllPlanner.Plan(query, ProviderType.SQLLiteDBOASIS, new[] { Target() }, options);
        Assert.False(plan.IsError, plan.Message);
        Assert.Same(options, plan.Result.Options);
        var limits = new CosmicProjectionCompositionExecutionLimits(2, 500, 10, 600, 1, 128);
        Assert.False(CosmicProjectedAllPlanner.AdmitExecution(plan.Result, ProviderType.SQLLiteDBOASIS, Rows, Columns, Profile(), limits).IsError);
        Assert.True(CosmicProjectedAllPlanner.AdmitExecution(plan.Result, ProviderType.SQLLiteDBOASIS, Rows, Columns, Profile(false), limits).IsError);
        var upgraded = new CosmicProjectionCompositionExecutionLimits(3, 500, 10, 600, 1, 128);
        Assert.Equal("CQL_EXECUTION_INVALID", CosmicProjectedAllPlanner.AdmitExecution(plan.Result, ProviderType.SQLLiteDBOASIS, Rows, Columns, Profile(), upgraded).ErrorCode);
    }
}
