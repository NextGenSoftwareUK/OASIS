using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicGroupedExecutorTests
{
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
    private static CosmicProjectionCapabilities Names => new(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
    private class RowOnly(ProviderType provider) : ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType => provider;
        public CosmicQueryCapabilities QueryCapabilities => Rows;
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => throw new InvalidOperationException("Grouping must not hydrate rows.");
    }
    private sealed class Grouped(ProviderType provider) : RowOnly(provider), ICosmicQueryGroupedStorageProvider
    {
        public CosmicProjectionCapabilities KeyCapabilities { get; set; } = Names;
        public CosmicGroupedCapabilities GroupedCapabilities => new(KeyCapabilities, new CosmicScalarCapabilities(CosmicScalarOperation.Count));
        public CosmicGroupedRow[] Data = { new(1, new object[] { "coffee" }, "coffee") };
        public bool Loaded = true;
        public int Calls;
        public int LastGroupLimit;
        public int LastPayloadLimit;
        public Action? DuringRead;
        public Task<OASISResult<IReadOnlyList<CosmicGroupedRow>>> ExecuteCosmicGroupedAsync<T, TKey, TResult>(CosmicGroupedPlan<T, TKey, TResult> plan,
            int maximumResults = 100, int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit,
            CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++; LastGroupLimit = maximumResults; LastPayloadLimit = maximumPayloadBytes; DuringRead?.Invoke();
            return Task.FromResult(new OASISResult<IReadOnlyList<CosmicGroupedRow>>(Data) { IsLoaded = Loaded });
        }
    }
    public class Snapshot
    {
        public static int Calls;
        public Snapshot() { Calls++; }
        public string Label { get; set; }
    }
    private static CosmicGroupedPlan<Holon, string, Snapshot> Plan(params ProviderType[] providers)
        => PlanWithLimit(1000, providers);
    private static CosmicGroupedPlan<Holon, string, Snapshot> PlanWithLimit(int groups, params ProviderType[] providers)
        => PlanWithOptions(groups, CosmicProjectionResults.MaximumPayloadLimit, providers);
    private static CosmicGroupedPlan<Holon, string, Snapshot> PlanWithOptions(int groups, int bytes, params ProviderType[] providers)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(providers).Result)
            .GroupBy(h => h.Name, CosmicGroupingOptions.Create(groups, bytes).Result)
            .Select(g => new Snapshot { Label = g.Key }).Result;
        return CosmicGroupedPlanner.Plan(query, providers[0], providers.Select(p => new CosmicGroupedTarget(p, Rows,
            new CosmicGroupedCapabilities(Names, new CosmicScalarCapabilities(CosmicScalarOperation.Count)))).ToArray()).Result;
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task EveryActualAdapterIsAdmittedBeforeFirstDispatch(bool rowOnly)
    {
        var first = new Grouped(ProviderType.MongoDBOASIS);
        ICosmicQueryStorageProvider second = rowOnly ? new RowOnly(ProviderType.Neo4jOASIS)
            : new Grouped(ProviderType.Neo4jOASIS) { KeyCapabilities = new(Array.Empty<CosmicProjectionFieldCapability>(), Array.Empty<Type>()) };
        var result = await CosmicGroupedExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
        Assert.Equal(0, first.Calls);
    }

    [Fact]
    public async Task LaterInvalidBufferPreventsAnyDtoConstructionOrPublication()
    {
        var first = new Grouped(ProviderType.MongoDBOASIS);
        var second = new Grouped(ProviderType.Neo4jOASIS) { Data = new[] { new CosmicGroupedRow(1, new object[] { "coffee" }, 12L) } };
        Snapshot.Calls = 0;
        var result = await CosmicGroupedExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.True(result.IsError); Assert.Null(result.Result); Assert.False(result.IsLoaded);
        Assert.Equal(0, Snapshot.Calls); Assert.Equal(1, first.Calls); Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task SuccessfulResultsRetainProviderProvenanceAndDetachedDtos()
    {
        var first = new Grouped(ProviderType.MongoDBOASIS);
        var second = new Grouped(ProviderType.Neo4jOASIS);
        var result = await CosmicGroupedExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded);
        Assert.Equal(new[] { first.QueryProviderType, second.QueryProviderType }, result.Result.Providers.Select(p => p.Provider));
        Assert.Equal("coffee", result.Result.Providers[0].Rows.Single().Label);
        Assert.NotSame(result.Result.Providers[0].Rows.Single(), result.Result.Providers[1].Rows.Single());
    }

    [Fact]
    public async Task GlobalLimitsAndUnacknowledgedResultsAreNotPartialSuccess()
    {
        var first = new Grouped(ProviderType.MongoDBOASIS);
        var second = new Grouped(ProviderType.Neo4jOASIS);
        Snapshot.Calls = 0;
        var limited = await CosmicGroupedExecutor.ExecuteAsync(PlanWithLimit(1, first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.Equal("CQL_RESULT_LIMIT", limited.ErrorCode); Assert.Null(limited.Result); Assert.Equal(0, Snapshot.Calls);
        first.Loaded = false;
        var unacknowledged = await CosmicGroupedExecutor.ExecuteAsync(Plan(first.QueryProviderType), new[] { first });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", unacknowledged.ErrorCode);
    }

    [Fact]
    public async Task CancellationStopsLaterDispatchAndAllDtoConstruction()
    {
        using var cancellation = new CancellationTokenSource();
        var first = new Grouped(ProviderType.MongoDBOASIS) { DuringRead = cancellation.Cancel };
        var second = new Grouped(ProviderType.Neo4jOASIS);
        Snapshot.Calls = 0;
        var result = await CosmicGroupedExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType), new[] { first, second }, cancellationToken: cancellation.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode); Assert.Null(result.Result);
        Assert.Equal(0, second.Calls); Assert.Equal(0, Snapshot.Calls);
    }
    [Theory]
    [InlineData(NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2, true)]
    [InlineData(NextGenSoftware.OASIS.API.DNA.HyperDriveModes.Legacy, false)]
    public async Task GenericManagerOwnsV2AdmissionBeforeAdapterDispatch(string mode, bool supported)
    {
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA
        { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = mode } };
        var manager = new NextGenSoftware.OASIS.API.Core.Managers.HolonManager(null, dna,
            new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null, dna));
        var adapter = new Grouped(ProviderType.SQLLiteDBOASIS);
        var result = await manager.ExecuteGroupedQueryAsync(Plan(adapter.QueryProviderType), new[] { adapter });
        Assert.Equal(!supported, result.IsError);
        Assert.Equal(supported ? 1 : 0, adapter.Calls);
        if (!supported) Assert.Equal("CQL_V2_REQUIRED", result.ErrorCode);
        else Assert.Equal("coffee", result.Result.Providers.Single().Rows.Single().Label);
    }

    [Fact]
    public async Task ExhaustedPublicationBudgetDoesNotSkipALaterEmptyProvider()
    {
        var first = new Grouped(ProviderType.MongoDBOASIS);
        var second = new Grouped(ProviderType.Neo4jOASIS) { Data = Array.Empty<CosmicGroupedRow>() };
        var result = await CosmicGroupedExecutor.ExecuteAsync(PlanWithOptions(1, 76, first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(1, second.Calls);
        Assert.Equal(0, second.LastGroupLimit);
        Assert.Equal(0, second.LastPayloadLimit);
        Assert.Empty(result.Result.Providers[1].Rows);
    }

    [Fact]
    public async Task MissingOrDuplicateAdapterIdentityCannotDispatchEarlierTargets()
    {
        var first = new Grouped(ProviderType.MongoDBOASIS);
        var missing = await CosmicGroupedExecutor.ExecuteAsync(Plan(first.QueryProviderType, ProviderType.Neo4jOASIS), new[] { first });
        Assert.Equal("CQL_PLAN_TARGET_UNAVAILABLE", missing.ErrorCode);
        Assert.Equal(0, first.Calls);
        var duplicate = await CosmicGroupedExecutor.ExecuteAsync(Plan(first.QueryProviderType), new[] { first, first });
        Assert.Equal("CQL_EXECUTION_INVALID", duplicate.ErrorCode);
        Assert.Equal(0, first.Calls);
    }

    [Fact]
    public async Task PayloadLimitIsGlobalBeforeAnyProviderConstructsADto()
    {
        var first = new Grouped(ProviderType.MongoDBOASIS);
        var second = new Grouped(ProviderType.Neo4jOASIS);
        Snapshot.Calls = 0;
        var result = await CosmicGroupedExecutor.ExecuteAsync(PlanWithOptions(1000, 100, first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.Equal("CQL_RESULT_LIMIT", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.Equal(0, Snapshot.Calls);
        Assert.Equal(24, second.LastPayloadLimit);
    }

}
