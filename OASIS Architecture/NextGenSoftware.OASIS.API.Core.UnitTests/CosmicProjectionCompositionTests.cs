using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicProjectionCompositionTests
{
    [Fact]
    public void ResultStagesPreserveOriginalSourcePolicyGraphAndWindowInExactOrder()
    {
        var source = CosmicQuery<Holon>.Create()
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
            .WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth: 2).Result)
            .OrderBy(h => h.Name).Skip(2).Take(3).Result;
        var original = source.Select(h => new { Label = h.Name, Revision = h.Version }).Result;
        var selected = original.Take(2).Where(row => row.Revision > 1)
            .OrderBy(row => row.Label).ThenByDescending(row => row.Revision).Skip(1).Result;

        Assert.Same(source, selected.Source);
        Assert.Empty(original.ResultStages);
        Assert.Equal(4, selected.ResultStages.Count);
        Assert.Equal(CosmicSelectedWindowKind.Take, Assert.IsType<CosmicSelectedWindowStage>(selected.ResultStages[0]).Kind);
        Assert.IsType<CosmicSelectedFilterStage>(selected.ResultStages[1]);
        var ordering = Assert.IsType<CosmicSelectedOrderStage>(selected.ResultStages[2]);
        Assert.Equal(new[] { 0, 1 }, ordering.Orderings.Select(item => item.ColumnIndex));
        Assert.True(ordering.Orderings[1].Descending);
        // ThenBy extends the existing sort; Skip remains a distinct later stage.
        Assert.Equal(CosmicSelectedWindowKind.Skip, Assert.IsType<CosmicSelectedWindowStage>(selected.ResultStages.Last()).Kind);
    }

    [Fact]
    public void ScalarFilterFreezesLiteralWithoutMutatingEarlierSelection()
    {
        var threshold = 7;
        var original = CosmicQuery<Holon>.Create().Select(h => h.Version).Result;
        var filtered = original.Where(value => value >= threshold).Result;
        threshold = 99;
        var predicate = Assert.IsType<CosmicQueryComparison>(Assert.IsType<CosmicSelectedFilterStage>(filtered.ResultStages[0]).Predicate);
        Assert.Equal(7L, predicate.Literal.Value);
        Assert.Empty(original.ResultStages);
        Assert.Equal("Value", predicate.Path.Segments.Single());
    }

    [Fact]
    public void RuntimeRecordBindsExactSelectedAliasAndRejectsWrongDomain()
    {
        var query = CosmicQuery<Holon>.Create().SelectColumns(new[]
        {
            CosmicProjectionSelection<Holon>.Create("stock", h => CosmicQueryMetadata.Value<long>(h, "stock")).Result
        });
        Assert.False(query.Where(row => (long)row["stock"] > 7L).OrderBy(row => (long)row["stock"]).IsError);
        Assert.True(query.Where(row => (int)row["stock"] > 7).IsError);
        Assert.True(query.Where(row => (long)row["missing"] > 7L).IsError);
    }

    [Fact]
    public void CapturingCompositionInvokesNoDtoGetterConstructorSetterOrArbitraryCode()
    {
        Explosive.Calls = 0;
        var query = CosmicQuery<Holon>.Create().Select(h => new Explosive { Label = h.Name }).Result;
        Assert.False(query.Where(row => row.Label == "coffee").OrderBy(row => row.Label).IsError);
        Assert.True(query.Where(row => row.Label.Length > 1).IsError);
        Assert.True(query.Where(row => Dangerous(row.Label)).IsError);
        Assert.Equal(0, Explosive.Calls);
    }

    [Fact]
    public void NewOrderByRetainsEarlierSortAndWindowRatherThanRewritingThem()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => new { h.Name, h.Version })
            .OrderBy(row => row.Name).Take(2).OrderByDescending(row => row.Version).Result;
        Assert.Equal(3, query.ResultStages.Count);
        Assert.Equal(0, Assert.IsType<CosmicSelectedOrderStage>(query.ResultStages[0]).Orderings.Single().ColumnIndex);
        Assert.Equal(1, Assert.IsType<CosmicSelectedOrderStage>(query.ResultStages[2]).Orderings.Single().ColumnIndex);
        Assert.Equal(2, Assert.IsType<CosmicSelectedWindowStage>(query.ResultStages[1]).Count);
    }

    [Theory]
    [InlineData(-1)] [InlineData(10001)]
    public void SelectedWindowsRejectOutOfContractBounds(int count)
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name);
        Assert.True(query.Skip(count).IsError);
        Assert.True(query.Take(count).IsError);
    }

    [Fact]
    public void ZeroWindowsAreRetainedAndCannotEraseLaterProtectedStages()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(0).Where(value => value == "coffee").Skip(0).Result;
        Assert.Equal(3, query.ResultStages.Count);
        Assert.Equal(0, Assert.IsType<CosmicSelectedWindowStage>(query.ResultStages[0]).Count);
        Assert.Equal(0, Assert.IsType<CosmicSelectedWindowStage>(query.ResultStages[2]).Count);
    }

    [Fact]
    public void StageBoundAndReadOnlySnapshotsPreventUnboundedOrMutablePlans()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name);
        for (var index = 0; index < 32; index++) query = query.Take(1);
        Assert.False(query.IsError);
        Assert.Equal("CQL_QUERY_LIMIT", query.Take(1).ErrorCode);
        Assert.Throws<NotSupportedException>(() => ((IList<CosmicSelectedStage>)query.Result.ResultStages).Clear());
    }

    [Fact]
    public void ThenByRequiresOneCurrentSortAndRejectsLossyConvertedKeys()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Version);
        Assert.True(query.ThenBy(value => value).IsError);
        Assert.True(query.OrderBy(value => (long)value).IsError);
        Assert.True(query.OrderBy(value => value).Take(1).ThenBy(value => value).IsError);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public void OrdinaryProjectionPermissionsCannotAuthorizeNewNativeSubqueries(ProviderType provider)
    {
        var query = CosmicQuery<Holon>.Create().Select(h => h.Name).Take(1).Result;
        var rows = new CosmicQueryCapabilities(CosmicQueryFeatures.Paging, Array.Empty<CosmicQueryFieldCapability>());
        var projection = new CosmicProjectionCapabilities(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
        var planned = CosmicProjectionPlanner.Plan(query, provider, new[] { new CosmicProjectionTarget(provider, rows, projection) });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", planned.ErrorCode);
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", CosmicProjectionResults.Materialize(query, new[] { new CosmicProjectionRow("coffee") }).ErrorCode);
    }

    [Fact]
    public void FailedIntentPropagatesThroughEveryCompositionOperation()
    {
        var failed = new OASISResult<CosmicProjectionQuery<Holon, string>> { IsError = true, ErrorCode = "original", Message = "original failure" };
        Assert.Equal("original", failed.Where(value => value == "coffee").OrderBy(value => value).ThenByDescending(value => value).Skip(1).Take(1).ErrorCode);
    }

    private static bool Dangerous(string value) { Explosive.Calls++; throw new Exception(); }
    public sealed class Explosive
    {
        public static int Calls;
        public Explosive() { Calls++; throw new Exception(); }
        public string Label { get { Calls++; throw new Exception(); } set { Calls++; throw new Exception(); } }
    }
}
