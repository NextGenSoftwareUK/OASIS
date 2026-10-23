using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicScalarExecutorTests
{
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());

    private class RowOnly(ProviderType provider) : ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType => provider;
        public CosmicQueryCapabilities QueryCapabilities => Rows;
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => throw new InvalidOperationException("A scalar must never enumerate the row API.");
    }

    private sealed class Scalar(ProviderType provider) : RowOnly(provider), ICosmicQueryScalarStorageProvider
    {
        public CosmicScalarCapabilities ScalarCapabilities { get; set; } = new(CosmicScalarOperation.Count, CosmicScalarOperation.LongCount);
        public long Count = 10001;
        public int Calls;
        public bool Loaded = true;
        public string? Failure;
        public Action? DuringRead;
        public Task<OASISResult<TResult>> ExecuteCosmicScalarAsync<T, TResult>(CosmicScalarPlan<T, TResult> plan,
            CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            DuringRead?.Invoke();
            if (Failure != null) return Task.FromResult(new OASISResult<TResult> { IsError = true, ErrorCode = Failure, Message = "controlled native failure" });
            var result = CosmicScalarResults.FromCount(plan.Query, Count);
            result.IsLoaded = Loaded && !result.IsError;
            return Task.FromResult(result);
        }
    }

    private static CosmicScalarPlan<Holon, long> Plan(params ProviderType[] providers)
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(providers).Result).LongCount().Result;
        var plan = CosmicScalarPlanner.Plan(source, providers[0], providers.Select(provider =>
            new CosmicScalarTarget(provider, Rows, new CosmicScalarCapabilities(CosmicScalarOperation.LongCount))).ToArray());
        Assert.False(plan.IsError, plan.Message);
        return plan.Result;
    }

    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task LaterUnsupportedAdapterRejectsBeforeAnyEarlierProviderDispatch(bool rowOnly)
    {
        var first = new Scalar(ProviderType.MongoDBOASIS);
        ICosmicQueryStorageProvider second = rowOnly ? new RowOnly(ProviderType.Neo4jOASIS)
            : new Scalar(ProviderType.Neo4jOASIS) { ScalarCapabilities = new CosmicScalarCapabilities() };
        var result = await CosmicScalarExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
        Assert.Null(result.Result); Assert.Equal(0, first.Calls);
        if (second is Scalar scalar) Assert.Equal(0, scalar.Calls);
    }

    [Fact]
    public async Task CountsRemainPerProviderAndAreNotTruncatedToTheRowPublicationLimit()
    {
        var first = new Scalar(ProviderType.MongoDBOASIS);
        var second = new Scalar(ProviderType.Neo4jOASIS);
        var result = await CosmicScalarExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType),
            new ICosmicQueryStorageProvider[] { first, second });
        Assert.False(result.IsError, result.Message); Assert.True(result.IsLoaded);
        Assert.Equal(new[] { first.QueryProviderType, second.QueryProviderType }, result.Result.Providers.Select(value => value.Provider));
        Assert.Equal(new long[] { 10001, 10001 }, result.Result.Providers.Select(value => value.Value));
        Assert.Equal(1, first.Calls); Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task LaterProviderFailureDoesNotPublishAnEarlierScalar()
    {
        var first = new Scalar(ProviderType.MongoDBOASIS);
        var second = new Scalar(ProviderType.Neo4jOASIS) { Failure = "CONTROLLED_NATIVE_FAILURE" };
        var result = await CosmicScalarExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType),
            new ICosmicQueryStorageProvider[] { first, second });
        Assert.Equal("CONTROLLED_NATIVE_FAILURE", result.ErrorCode);
        Assert.False(result.IsLoaded); Assert.Null(result.Result);
        Assert.Equal(1, first.Calls); Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task CancellationAfterNativeCompletionStopsLaterDispatchAndPublishesNoPartialValue()
    {
        using var cancellation = new CancellationTokenSource();
        var first = new Scalar(ProviderType.MongoDBOASIS) { DuringRead = cancellation.Cancel };
        var second = new Scalar(ProviderType.Neo4jOASIS);
        var result = await CosmicScalarExecutor.ExecuteAsync(Plan(first.QueryProviderType, second.QueryProviderType),
            new ICosmicQueryStorageProvider[] { first, second }, cancellation.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode); Assert.Null(result.Result);
        Assert.Equal(1, first.Calls); Assert.Equal(0, second.Calls);
    }

    [Fact]
    public async Task AnUnacknowledgedDefaultValueIsNotAValidEmptyCount()
    {
        var adapter = new Scalar(ProviderType.MongoDBOASIS) { Count = 0, Loaded = false };
        var result = await CosmicScalarExecutor.ExecuteAsync(Plan(adapter.QueryProviderType), new[] { adapter });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode); Assert.Null(result.Result);
        Assert.False(result.IsLoaded); Assert.Equal(1, adapter.Calls);
    }
}
