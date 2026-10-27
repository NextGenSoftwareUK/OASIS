using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

// Red Q23 acceptance: these must compile to storage predicates, never enumerate
// an object's currently loaded Children collection or execute a user callback.
public class CosmicAllTraversalTests
{
    [Fact]
    public void ChildAnyCompilesWithoutEnumeratingTheNavigation()
    {
        var compiled = CosmicQueryExpression.CompileAll<Holon>(row => row.Children.Any(child => child.Name == "public child"));

        Assert.False(compiled.IsError, compiled.ErrorCode + ": " + compiled.Message);
        var child = Assert.IsType<CosmicQueryAnyChild>(compiled.Result);
        var comparison = Assert.IsType<CosmicQueryComparison>(child.Predicate);
        Assert.Equal("Name", Assert.Single(comparison.Path.Segments));
        Assert.Equal("public child", comparison.Literal.Value);
    }

    [Fact]
    public void ChildLiteralIsFrozenAtConstruction()
    {
        var name = "original";
        var terminal = CosmicQueryExpression.CompileAll<Holon>(row => row.Children.Any(child => child.Name == name));
        name = "changed";

        Assert.False(terminal.IsError, terminal.ErrorCode + ": " + terminal.Message);
        var child = Assert.IsType<CosmicQueryAnyChild>(terminal.Result);
        Assert.Equal("original", Assert.IsType<CosmicQueryComparison>(child.Predicate).Literal.Value);
    }

    [Fact]
    public void ExistingRowGrammarDoesNotEnableUnimplementedRelationshipShapes()
    {
        var compiled = CosmicQueryExpression.Compile<Holon>(row => row.Children.Any(child => child.Name == "child"));
        Assert.True(compiled.IsError);
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED", compiled.ErrorCode);
    }

    [Fact]
    public void CorrelatedChildCallbackIsRejectedWithoutExecution()
    {
        var terminal = CosmicQuery<Holon>.Create().All(row => row.Children.Any(child => child.Name == row.Name));

        Assert.True(terminal.IsError);
        Assert.StartsWith("CQL_", terminal.ErrorCode);
    }
    [Fact]
    public void AllChildPredicateRetainsItsSourceWindow()
    {
        var source = CosmicQuery<Holon>.Create().OrderBy(row => row.Name).Skip(1).Take(1).Result;
        var query = source.All(row => row.Children.Any(child => child.Name == "public child"));
        Assert.False(query.IsError, query.Message);
        Assert.Same(source, query.Result.Source);
        Assert.Empty(source.Predicates);
        Assert.IsType<CosmicQueryAnyChild>(Assert.Single(query.Result.AllPredicate.Predicates));
    }

    [Fact]
    public void ScalarAllAndRowChildSupportDoNotGrantPostWindowChildSupport()
    {
        var query = CosmicQuery<Holon>.Create().All(row => row.Children.Any(child => child.Name == "child")).Result;
        Assert.True(new CosmicScalarCapabilities(CosmicScalarOperation.All).Admit(query).IsError);
        var capable = new CosmicScalarCapabilities(new[] { CosmicScalarOperation.All },
            Array.Empty<CosmicScalarSelectionCapability>(), supportsAllChildren: true);
        Assert.False(capable.Admit(query).IsError);
    }

    [Fact]
    public void ChildPredicateCarriesSourceVisibility()
    {
        var access = CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var source = CosmicQuery<Holon>.Create().WithReadAccess(access).Result;
        var query = source.All(row => row.Children.Any(child => child.Name == "child")).Result;
        Assert.Same(access, query.AllPredicate.ReadAccess);
    }

    [Fact]
    public void NestedChildTraversalRemainsAnExplicitGap()
    {
        var query = CosmicQuery<Holon>.Create().All(row => row.Children.Any(child =>
            child.Children.Any(grandchild => grandchild.Name == "nested")));
        Assert.True(query.IsError);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", query.ErrorCode);
    }

}
