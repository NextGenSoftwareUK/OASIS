using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryCompositionTests
{
    [Fact]
    public void ScopeKeepsConcreteIdentityOrderAndFreezesCallerArray()
    {
        var providers = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS };
        var result = CosmicQueryScope.Named(providers);
        Assert.False(result.IsError, result.Message);
        providers[0] = ProviderType.None;
        Assert.Equal(CosmicQueryScopeKind.NamedProviders, result.Result.Kind);
        Assert.Equal(new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS }, result.Result.Providers);
        Assert.Throws<NotSupportedException>(() => ((IList<ProviderType>)result.Result.Providers)[0] = ProviderType.None);
    }

    [Fact]
    public void InvalidScopesNeverBecomeAnImplicitAllProviderQuery()
    {
        ProviderType[][] invalid = { null!, Array.Empty<ProviderType>(), new[] { ProviderType.None }, new[] { ProviderType.All },
            new[] { ProviderType.Default }, new[] { (ProviderType)int.MaxValue }, new[] { ProviderType.MongoDBOASIS, ProviderType.MongoDBOASIS },
            new ProviderType[65] };
        foreach (var providers in invalid)
        {
            var result = CosmicQueryScope.Named(providers);
            Assert.True(result.IsError); Assert.Null(result.Result); Assert.Equal("CQL_SCOPE_INVALID", result.ErrorCode);
        }
    }

    [Fact]
    public void CompositionIsImmutableAndCapturesEachWhereAtCompositionTime()
    {
        var original = CosmicQuery<Holon>.Create();
        var minimum = 7;
        var first = original.Where(h => h.Version >= minimum);
        Assert.False(first.IsError, first.Message); minimum = 20;
        var second = first.Result.Where(h => h.Version < minimum);
        Assert.False(second.IsError, second.Message); minimum = 90;
        Assert.Empty(original.Predicates); Assert.Single(first.Result.Predicates);
        Assert.Equal(2, second.Result.Predicates.Count);
        Assert.Equal(7L, Assert.IsType<CosmicQueryComparison>(second.Result.Predicates[0]).Literal.Value);
        Assert.Equal(20L, Assert.IsType<CosmicQueryComparison>(second.Result.Predicates[1]).Literal.Value);
        Assert.Throws<NotSupportedException>(() => ((IList<CosmicQueryPredicate>)second.Result.Predicates).Clear());
        Assert.Equal(CosmicQueryScopeKind.CurrentDefault, original.Scope.Kind);
        var scoped = second.Result.WithScope(CosmicQueryScope.AllEligible);
        Assert.Equal(CosmicQueryScopeKind.AllEligible, scoped.Scope.Kind);
        Assert.Equal(CosmicQueryScopeKind.CurrentDefault, second.Result.Scope.Kind);
        Assert.Equal(second.Result.Predicates, scoped.Predicates);
    }

    [Fact]
    public void InvalidExpressionPreservesCompilerErrorWithoutMutatingQuery()
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        var invalid = query.Where(h => h.Name.StartsWith("prefix"));
        Assert.True(invalid.IsError); Assert.Equal("CQL_EXPRESSION_UNSUPPORTED", invalid.ErrorCode);
        Assert.NotNull(invalid.Exception); Assert.Null(invalid.Result); Assert.Empty(query.Predicates);
        var missing = query.Where(null!);
        Assert.True(missing.IsError); Assert.Equal("CQL_EXPRESSION_INVALID", missing.ErrorCode);
    }

    [Fact]
    public void QueryWideCompositionLimitCannotBeBypassedByChainingSmallPredicates()
    {
        var query = CosmicQuery<Holon>.Create();
        for (var i = 0; i < CosmicQuery<Holon>.MaximumPredicates; i++)
        {
            var next = query.Where(h => h.IsActive); Assert.False(next.IsError, next.Message); query = next.Result;
        }
        var rejected = query.Where(h => h.IsPublic);
        Assert.True(rejected.IsError); Assert.Equal("CQL_QUERY_LIMIT", rejected.ErrorCode);
        Assert.Equal(CosmicQuery<Holon>.MaximumPredicates, query.Predicates.Count);
    }
}
