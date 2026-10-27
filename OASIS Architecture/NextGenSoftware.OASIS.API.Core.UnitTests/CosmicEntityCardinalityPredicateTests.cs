using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicEntityCardinalityPredicateTests
{
    private static CosmicScalarCapabilities Counts(bool enrolled, bool children = false) => new(
        new[] { CosmicScalarOperation.Count, CosmicScalarOperation.LongCount, CosmicScalarOperation.Any },
        Array.Empty<CosmicScalarSelectionCapability>(), children, false, false, false, enrolled);

    [Fact]
    public void TerminalCapturePreservesSourceWindowAndFreezesItsOwnParameters()
    {
        var source = CosmicQuery<Holon>.Create().OrderBy(row => row.Name).Skip(2).Take(3).Result;
        var name = "captured";
        var captured = source.Count(row => row.Name == name);
        name = "changed";
        Assert.False(captured.IsError, captured.Message);
        Assert.Same(source, captured.Result.Source);
        Assert.Equal(2, source.Offset);
        Assert.Equal(3, source.Limit);
        Assert.Empty(source.Predicates);
        Assert.Null(captured.Result.AllPredicate);
        var terminal = captured.Result.CardinalityPredicate;
        Assert.Same(terminal, captured.Result.TerminalPredicate);
        Assert.Equal(0, terminal.Offset);
        Assert.Null(terminal.Limit);
        Assert.Equal("captured", Assert.IsType<CosmicQueryComparison>(Assert.Single(terminal.Predicates)).Literal.Value);
    }

    [Fact]
    public void PostWindowCardinalityRequiresIndependentPermissionWithoutGrantingAll()
    {
        var query = CosmicQuery<Holon>.Create().Take(1).Count(row => row.Name == "coffee").Result;
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Counts(false).Admit(query).ErrorCode);
        Assert.False(Counts(true).Admit(query).IsError);
        Assert.False(Counts(true).Supports(CosmicScalarOperation.All));
        Assert.False(Counts(false).Admit(query.Source.Count().Result).IsError);
    }

    [Fact]
    public void AdmissionChecksTheTerminalReadPathsIndependentlyOfSourcePaths()
    {
        var source = CosmicQuery<Holon>.Create().Take(1).Result;
        var query = source.LongCount(row => row.Name == "coffee").Result;
        var sourceOnly = new CosmicQueryCapabilities(CosmicQueryFeatures.Paging, Array.Empty<CosmicQueryFieldCapability>());
        var plan = CosmicScalarPlanner.Plan(query, ProviderType.SQLLiteDBOASIS,
            new[] { new CosmicScalarTarget(ProviderType.SQLLiteDBOASIS, sourceOnly, Counts(true)) });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", plan.ErrorCode);
    }

    [Fact]
    public void ChildPredicatesRetainTheirExplicitTraversalPermission()
    {
        var query = CosmicQuery<Holon>.Create().Take(1)
            .Count(row => row.Children.Any(child => child.Name == "coffee")).Result;
        Assert.NotNull(query);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", Counts(true).Admit(query).ErrorCode);
        Assert.False(Counts(true, children: true).Admit(query).IsError);
    }

    [Fact]
    public void InvalidInputsAndFailedSourceIntentsCannotBecomeEmptySuccess()
    {
        var failed = CosmicQuery<Holon>.Create().Take(-1);
        Assert.True(failed.IsError);
        Assert.Equal(failed.ErrorCode, failed.Count(row => true).ErrorCode);
        var source = CosmicQuery<Holon>.Create();
        Assert.Equal("CQL_QUERY_INVALID", source.Any((Expression<Func<Holon, bool>>)null!).ErrorCode);
        Assert.Equal("CQL_QUERY_INVALID", ((CosmicQuery<Holon>)null!).LongCount(row => true).ErrorCode);
    }
}
