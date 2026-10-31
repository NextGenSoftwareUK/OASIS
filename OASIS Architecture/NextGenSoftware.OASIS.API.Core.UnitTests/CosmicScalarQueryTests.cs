using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicScalarQueryTests
{
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.Paging | CosmicQueryFeatures.Ordering,
        new CosmicQueryFieldCapability("Version", typeof(int), new[] { CosmicQueryComparisonOperator.Equal },
            Array.Empty<CosmicQueryStringOperator>(), supportsOrdering: true));
    private static CosmicScalarTarget Target(ProviderType provider, params CosmicScalarOperation[] operations)
        => new(provider, Rows, new CosmicScalarCapabilities(operations));

    [Fact]
    public void TerminalsRetainTheCompleteImmutableSourceWithoutChangingItsWindowOrAccess()
    {
        var graph = CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot, maxChildDepth: 2).Result;
        var access = CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var source = CosmicQuery<Holon>.Create().WithChildren(graph).WithReadAccess(access)
            .WhereAnyDescendant(child => child.Name == "match", CosmicQueryTraversalOptions.Create(2, 10).Result)
            .OrderBy(row => row.Version).Skip(3).Take(5).Result;

        var count = source.Count().Result;
        Assert.Same(source, count.Source);
        Assert.Same(source, source.LongCount().Result.Source);
        Assert.Same(source, source.Any().Result.Source);
        Assert.Same(access, count.Source.ReadAccess);
        Assert.Same(graph, count.Source.GraphOptions);
        Assert.Equal(3, count.Source.Offset);
        Assert.Equal(5, count.Source.Limit);
        Assert.Contains(count.Source.Predicates, predicate => predicate is CosmicQueryAnyDescendant);
    }

    [Fact]
    public void FluentTerminalPreservesTheOriginalConstructionFailure()
    {
        var failed = CosmicQuery<Holon>.Create().Take(-1);
        failed.MetaData["proof"] = "original";
        foreach (var code in new[] { failed.Count().ErrorCode, failed.LongCount().ErrorCode, failed.Any().ErrorCode })
            Assert.Equal(failed.ErrorCode, code);
        var count = failed.Count();
        Assert.True(count.IsError);
        Assert.Null(count.Result);
        Assert.Equal(failed.Message, count.Message);
        Assert.Equal("original", count.MetaData["proof"]);
        Assert.Equal("CQL_QUERY_INVALID", ((CosmicQuery<Holon>)null!).Count().ErrorCode);
        Assert.Equal("CQL_QUERY_INVALID", ((OASISResult<CosmicQuery<Holon>>)null!).Any().ErrorCode);
    }

    [Fact]
    public void ScalarAdmissionCannotBorrowRowPermissionOrAnotherAdaptersTerminalCapability()
    {
        var query = CosmicQuery<Holon>.Create().Count().Result;
        var admitted = CosmicScalarPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { Target(ProviderType.MongoDBOASIS, CosmicScalarOperation.Count) });
        Assert.False(admitted.IsError, admitted.Message);
        Assert.Same(query.Source, admitted.Result.SourcePlan.Query);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicScalarPlanner.Admit(admitted.Result,
            ProviderType.MongoDBOASIS, Rows, new CosmicScalarCapabilities(CosmicScalarOperation.LongCount)).ErrorCode);
        Assert.Equal("CQL_PLAN_INVALID", CosmicScalarPlanner.Admit(admitted.Result,
            ProviderType.Neo4jOASIS, Rows, new CosmicScalarCapabilities(CosmicScalarOperation.Count)).ErrorCode);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicScalarPlanner.Plan(query, ProviderType.MongoDBOASIS,
            new[] { Target(ProviderType.MongoDBOASIS) }).ErrorCode);

        var filtered = CosmicQuery<Holon>.Create().Where(row => row.Version == 1).Count().Result;
        var filteredPlan = CosmicScalarPlanner.Plan(filtered, ProviderType.MongoDBOASIS,
            new[] { Target(ProviderType.MongoDBOASIS, CosmicScalarOperation.Count) }).Result;
        var noFields = new CosmicQueryCapabilities(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicScalarPlanner.Admit(filteredPlan,
            ProviderType.MongoDBOASIS, noFields, new CosmicScalarCapabilities(CosmicScalarOperation.Count)).ErrorCode);
    }

    [Fact]
    public void EverySelectedProviderMustAdmitTheTerminalAndGlobalWindowsRemainExplicit()
    {
        var targets = new[] { Target(ProviderType.MongoDBOASIS, CosmicScalarOperation.Count), Target(ProviderType.Neo4jOASIS) };
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicScalarPlanner.Plan(source.Count().Result,
            ProviderType.MongoDBOASIS, targets).ErrorCode);
        targets[1] = Target(ProviderType.Neo4jOASIS, CosmicScalarOperation.Count);
        var plan = CosmicScalarPlanner.Plan(source.Count().Result, ProviderType.MongoDBOASIS, targets);
        Assert.False(plan.IsError, plan.Message);
        Assert.Equal(2, plan.Result.Targets.Count);
        targets[0] = targets[1];
        Assert.Equal(ProviderType.MongoDBOASIS, plan.Result.Targets[0].Provider);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicScalarPlanner.Plan(source.Take(1).Count().Result,
            ProviderType.MongoDBOASIS, new[] { Target(ProviderType.MongoDBOASIS, CosmicScalarOperation.Count),
                Target(ProviderType.Neo4jOASIS, CosmicScalarOperation.Count) }).ErrorCode);
    }

    [Fact]
    public void CapabilityDeclarationsAreImmutableAndRejectUndefinedOrAmbiguousOperations()
    {
        var operations = new[] { CosmicScalarOperation.Count };
        var capability = new CosmicScalarCapabilities(operations);
        operations[0] = CosmicScalarOperation.Any;
        Assert.True(capability.Supports(CosmicScalarOperation.Count));
        Assert.False(capability.Supports(CosmicScalarOperation.Any));
        Assert.Throws<ArgumentException>(() => new CosmicScalarCapabilities((CosmicScalarOperation)999));
        Assert.Throws<ArgumentException>(() => new CosmicScalarCapabilities(CosmicScalarOperation.Count, CosmicScalarOperation.Count));
    }

    [Theory]
    [InlineData(0L)] [InlineData(1L)] [InlineData(2147483647L)]
    public void CountAndAnyHaveExactTypedEmptyAndNonemptyOutcomes(long count)
    {
        var source = CosmicQuery<Holon>.Create();
        var result = CosmicScalarResults.FromCount(source.Count().Result, count);
        Assert.False(result.IsError); Assert.True(result.IsLoaded); Assert.Equal((int)count, result.Result);
        var any = CosmicScalarResults.FromCount(source.Any().Result, count);
        Assert.False(any.IsError); Assert.Equal(count != 0, any.Result);
    }

    [Theory]
    [InlineData(2147483648L)] [InlineData(9007199254740993L)] [InlineData(long.MaxValue)]
    public void CountOverflowIsExplicitWhileLongCountPreservesEveryIntegerBit(long count)
    {
        var source = CosmicQuery<Holon>.Create();
        var overflow = CosmicScalarResults.FromCount(source.Count().Result, count);
        Assert.True(overflow.IsError); Assert.False(overflow.IsLoaded);
        Assert.Equal("CQL_AGGREGATE_OVERFLOW", overflow.ErrorCode);
        var wide = CosmicScalarResults.FromCount(source.LongCount().Result, count);
        Assert.False(wide.IsError); Assert.True(wide.IsLoaded); Assert.Equal(count, wide.Result);
    }

    [Fact]
    public void NegativeNativeCountsNeverPublishAsAValidCountOrExistenceResult()
    {
        var source = CosmicQuery<Holon>.Create();
        Assert.Equal("CQL_DATA_INVALID", CosmicScalarResults.FromCount(source.Count().Result, -1).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID", CosmicScalarResults.FromCount(source.LongCount().Result, -1).ErrorCode);
        Assert.Equal("CQL_DATA_INVALID", CosmicScalarResults.FromCount(source.Any().Result, -1).ErrorCode);
    }
}
