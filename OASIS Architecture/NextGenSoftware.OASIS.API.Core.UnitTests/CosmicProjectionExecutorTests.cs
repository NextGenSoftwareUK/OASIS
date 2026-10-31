using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicProjectionExecutorTests
{
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
    private static CosmicProjectionCapabilities Names => new(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
    private class RowOnly(ProviderType provider) : ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType => provider;
        public CosmicQueryCapabilities QueryCapabilities => Rows;
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => throw new InvalidOperationException("Projection must not hydrate rows.");
    }
    private sealed class Projection(ProviderType provider) : RowOnly(provider), ICosmicQueryProjectionStorageProvider
    {
        public CosmicProjectionCapabilities ProjectionCapabilities { get; set; } = Names;
        public CosmicProjectionRow[] Data = { new("coffee") };
        public bool Loaded = true;
        public int Calls;
        public Action? DuringRead;
        public Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicProjectionAsync<T, TResult>(CosmicProjectionPlan<T, TResult> plan,
            int maximumResults = 100, int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit,
            CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++; DuringRead?.Invoke();
            return Task.FromResult(new OASISResult<IReadOnlyList<CosmicProjectionRow>>(Data) { IsLoaded = Loaded });
        }
    }
    public class Snapshot
    {
        public static int Calls;
        public Snapshot() { Calls++; }
        public string Label { get; set; }
    }
    private static CosmicProjectionPlan<Holon, Snapshot> Plan(params ProviderType[] providers)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(providers).Result).Select(h => new Snapshot { Label = h.Name }).Result;
        return CosmicProjectionPlanner.Plan(query, providers[0], providers.Select(p => new CosmicProjectionTarget(p, Rows, Names)).ToArray()).Result;
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task EveryActualAdapterIsAdmittedBeforeFirstDispatch(bool rowOnly)
    {
        var first = new Projection(ProviderType.MongoDBOASIS);
        ICosmicQueryStorageProvider second = rowOnly ? new RowOnly(ProviderType.Neo4jOASIS)
            : new Projection(ProviderType.Neo4jOASIS) { ProjectionCapabilities = new(Array.Empty<CosmicProjectionFieldCapability>(), Array.Empty<Type>()) };
        var result = await CosmicProjectionExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
        Assert.Equal(0, first.Calls);
    }

    [Fact]
    public async Task LaterInvalidBufferPreventsAnyDtoConstructionOrPublication()
    {
        var first = new Projection(ProviderType.MongoDBOASIS);
        var second = new Projection(ProviderType.Neo4jOASIS) { Data = new[] { new CosmicProjectionRow(12L) } };
        Snapshot.Calls = 0;
        var result = await CosmicProjectionExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.True(result.IsError); Assert.Null(result.Result); Assert.False(result.IsLoaded);
        Assert.Equal(0, Snapshot.Calls); Assert.Equal(1, first.Calls); Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task SuccessfulResultsRetainProviderProvenanceAndDetachedDtos()
    {
        var first = new Projection(ProviderType.MongoDBOASIS);
        var second = new Projection(ProviderType.Neo4jOASIS);
        var result = await CosmicProjectionExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded);
        Assert.Equal(new[] { first.QueryProviderType, second.QueryProviderType }, result.Result.Providers.Select(p => p.Provider));
        Assert.Equal("coffee", result.Result.Providers[0].Rows.Single().Label);
        Assert.NotSame(result.Result.Providers[0].Rows.Single(), result.Result.Providers[1].Rows.Single());
    }

    [Fact]
    public async Task GlobalLimitsAndUnacknowledgedResultsAreNotPartialSuccess()
    {
        var first = new Projection(ProviderType.MongoDBOASIS);
        var second = new Projection(ProviderType.Neo4jOASIS);
        Snapshot.Calls = 0;
        var limited = await CosmicProjectionExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType), new[] { first, second }, maximumResults: 1);
        Assert.Equal("CQL_RESULT_LIMIT", limited.ErrorCode); Assert.Null(limited.Result); Assert.Equal(0, Snapshot.Calls);
        first.Loaded = false;
        var unacknowledged = await CosmicProjectionExecutor.ExecuteAsync(Plan(first.QueryProviderType), new[] { first });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", unacknowledged.ErrorCode);
    }

    [Fact]
    public async Task CancellationStopsLaterDispatchAndAllDtoConstruction()
    {
        using var cancellation = new CancellationTokenSource();
        var first = new Projection(ProviderType.MongoDBOASIS) { DuringRead = cancellation.Cancel };
        var second = new Projection(ProviderType.Neo4jOASIS);
        Snapshot.Calls = 0;
        var result = await CosmicProjectionExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType), new[] { first, second }, cancellationToken: cancellation.Token);
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
        var adapter = new Projection(ProviderType.SQLLiteDBOASIS);
        var result = await manager.ExecuteProjectionQueryAsync(Plan(adapter.QueryProviderType), new[] { adapter });
        Assert.Equal(!supported, result.IsError);
        Assert.Equal(supported ? 1 : 0, adapter.Calls);
        if (!supported) Assert.Equal("CQL_V2_REQUIRED", result.ErrorCode);
        else Assert.Equal("coffee", result.Result.Providers.Single().Rows.Single().Label);
    }

}
