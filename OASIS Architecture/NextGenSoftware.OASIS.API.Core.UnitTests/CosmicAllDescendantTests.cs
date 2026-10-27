using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicAllDescendantTests
{
    [Fact]
    public void CapturesIndependentBoundedTraversalAfterSourceWindow()
    {
        var bounds = CosmicQueryTraversalOptions.Create(2, 4).Result;
        var original = bounds;
        var source = CosmicQuery<Holon>.Create().OrderBy(row => row.Name).Skip(1).Take(1).Result;
        var query = source.All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "leaf", bounds));
        bounds = CosmicQueryTraversalOptions.Create(1, 1).Result;
        Assert.False(query.IsError, query.Message);
        Assert.Same(source, query.Result.Source);
        var descendant = Assert.IsType<CosmicQueryAnyDescendant>(Assert.Single(query.Result.AllPredicate.Predicates));
        Assert.Same(original, descendant.Traversal);
        Assert.Equal("leaf", Assert.IsType<CosmicQueryComparison>(descendant.Predicate).Literal.Value);
    }

    [Fact]
    public void ChildCapabilityDoesNotGrantDescendantScalarExecution()
    {
        var bounds = CosmicQueryTraversalOptions.Create(2, 4).Result;
        var query = CosmicQuery<Holon>.Create().All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == "leaf", bounds)).Result;
        var childOnly = new CosmicScalarCapabilities(new[] { CosmicScalarOperation.All }, Array.Empty<CosmicScalarSelectionCapability>(), true);
        Assert.True(childOnly.Admit(query).IsError);
        var descendants = new CosmicScalarCapabilities(new[] { CosmicScalarOperation.All }, Array.Empty<CosmicScalarSelectionCapability>(), false, true);
        Assert.False(descendants.Admit(query).IsError);
    }

    [Fact]
    public void NullBoundsAreRejectedBeforeExecution()
    {
        var query = CosmicQuery<Holon>.Create().All(row => CosmicQueryRelations.AnyDescendant(row, child => true, null!));
        Assert.True(query.IsError);
        Assert.Equal("CQL_EXPRESSION_INVALID", query.ErrorCode);
    }

    [Fact]
    public void MarkerCannotInvokeAClrCallback()
    {
        var calls = 0;
        Assert.Throws<NotSupportedException>(() => CosmicQueryRelations.AnyDescendant(new Holon(), child => { calls++; return true; },
            CosmicQueryTraversalOptions.Create().Result));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ExistingRowGrammarDoesNotAcquireDescendantMarkers()
    {
        var bounds = CosmicQueryTraversalOptions.Create().Result;
        var compiled = CosmicQueryExpression.Compile<Holon>(row => CosmicQueryRelations.AnyDescendant(row, child => true, bounds));
        Assert.True(compiled.IsError);
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED", compiled.ErrorCode);
    }

    [Fact]
    public void DescendantInsideChildRequiresIndependentScalarAdmission()
    {
        var bounds = CosmicQueryTraversalOptions.Create().Result;
        var query = CosmicQuery<Holon>.Create().All(row => row.Children.Any(child =>
            CosmicQueryRelations.AnyDescendant(child, descendant => descendant.Name == "leaf", bounds)));
        Assert.False(query.IsError, query.Message);
        var ordinary = new CosmicScalarCapabilities(new[] { CosmicScalarOperation.All }, Array.Empty<CosmicScalarSelectionCapability>(), true, true);
        Assert.True(ordinary.Admit(query.Result).IsError);
        var nested = new CosmicScalarCapabilities(new[] { CosmicScalarOperation.All }, Array.Empty<CosmicScalarSelectionCapability>(), true, true, true);
        Assert.False(nested.Admit(query.Result).IsError);
    }

    [Fact]
    public void DescendantPredicateCannotCaptureOuterRootFields()
    {
        var bounds = CosmicQueryTraversalOptions.Create().Result;
        var query = CosmicQuery<Holon>.Create().All(row => CosmicQueryRelations.AnyDescendant(row, child => child.Name == row.Name, bounds));
        Assert.True(query.IsError);
        Assert.StartsWith("CQL_", query.ErrorCode);
    }
    [Fact]
    public void DescendantPredicateRelationshipRemainsExplicitUntilItsOwnOwnerExists()
    {
        var bounds = CosmicQueryTraversalOptions.Create().Result;
        var query = CosmicQuery<Holon>.Create().All(row => CosmicQueryRelations.AnyDescendant(row,
            child => child.Children.Any(grandchild => grandchild.Name == "leaf"), bounds));
        Assert.True(query.IsError);
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED", query.ErrorCode);
    }

    [Fact]
    public void ChildDescendantCaptureIsFrozen()
    {
        var bounds = CosmicQueryTraversalOptions.Create(2, 3).Result;
        var name = "original";
        var query = CosmicQuery<Holon>.Create().All(row => row.Children.Any(child =>
            CosmicQueryRelations.AnyDescendant(child, descendant => descendant.Name == name, bounds)));
        name = "changed";
        Assert.False(query.IsError, query.Message);
        var child = Assert.IsType<CosmicQueryAnyChild>(Assert.Single(query.Result.AllPredicate.Predicates));
        var descendant = Assert.IsType<CosmicQueryAnyDescendant>(child.Predicate);
        Assert.Equal("original", Assert.IsType<CosmicQueryComparison>(descendant.Predicate).Literal.Value);
    }

}
