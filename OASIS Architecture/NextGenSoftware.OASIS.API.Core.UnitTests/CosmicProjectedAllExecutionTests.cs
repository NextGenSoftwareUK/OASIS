using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicProjectedAllTests
{
    private sealed class NativeAll(ProviderType identity) : ICosmicQueryProjectedAllStorageProvider
    {
        public ProviderType QueryProviderType => identity;
        public CosmicQueryCapabilities QueryCapabilities => Rows;
        public CosmicProjectionCapabilities ProjectionCapabilities => Columns;
        public CosmicProjectionCompositionCapabilities CompositionCapabilities => Profile(Enabled);
        public bool Enabled = true, Loaded = true;
        public int Calls, Count, CounterexampleRows;
        public long Counterexamples;
        public string? Failure;
        public Action? DuringRead;
        public CosmicProjectionCompositionExecutionLimits? Seen;
        public Task<OASISResult<CosmicProjectedAllPacket>> ExecuteCosmicProjectedAllAsync<T, TResult>(
            CosmicProjectedAllPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
            CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++; Seen = limits; DuringRead?.Invoke();
            if (Failure != null) return Task.FromResult(new OASISResult<CosmicProjectedAllPacket> { IsError = true, ErrorCode = Failure });
            var input = Enumerable.Range(1, Count).Select(index => new CosmicProjectionInputRow(
                Guid.Parse($"00000000-0000-0000-0000-{index:D12}"), true, Guid.Empty, new object[] { index<=CounterexampleRows ? 0 : index }, Array.Empty<object>())).ToArray();
            var proof = new CosmicProjectionCompositionPacket(input, Array.Empty<CosmicProjectionStageProof>(),
                input.Select(row => new CosmicProjectionCompositionRow(row.Id, row.Values.ToArray())).ToArray(), Count, 0);
            return Task.FromResult(new OASISResult<CosmicProjectedAllPacket>(new(proof, Counterexamples)) { IsLoaded = Loaded });
        }
        public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T, TResult>(
            CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
            CancellationToken cancellationToken = default) where T : IHolon => throw new Exception("No composition row fallback.");
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon => throw new Exception("No entity row fallback.");
    }

    private static CosmicProjectedAllPlan<Holon, int> ExecutionPlan(int budget, params ProviderType[] providers)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(providers).Result)
            .Select(row => row.Version).All(value => value > 0).Result;
        var plan = CosmicProjectedAllPlanner.Plan(query, providers[0], providers.Select(provider =>
            new CosmicProjectionCompositionTarget(provider, Rows, Columns, Profile())).ToArray(),
            new CosmicProjectionCompositionOptions(budget, 100000, 10000, 100000));
        Assert.False(plan.IsError, plan.Message);
        return plan.Result;
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(150, 0, true)]
    [InlineData(150, 1, false)]
    public async Task NativeReductionIsNotLimitedByTheDefaultRowPage(int count, int counterexamples, bool expected)
    {
        var adapter = new NativeAll(ProviderType.MongoDBOASIS) { Count = count, Counterexamples = counterexamples, CounterexampleRows = counterexamples };
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(ExecutionPlan(200, adapter.QueryProviderType), new[] { adapter });
        Assert.False(result.IsError, result.Message);
        Assert.True(result.IsLoaded);
        Assert.Equal(expected, Assert.Single(result.Result.Providers).Value);
        Assert.Equal(1, adapter.Calls);
    }

    [Theory]
    [InlineData(0,1)]
    [InlineData(1,0)]
    public async Task WithinRangeNativeCounterexamplesMustAgreeWithTheCompleteProof(int actual,int claimed)
    {
        var adapter=new NativeAll(ProviderType.SQLLiteDBOASIS){Count=1,Counterexamples=claimed,CounterexampleRows=actual};
        var result=await CosmicProjectedAllExecutor.ExecuteAsync(ExecutionPlan(2,adapter.QueryProviderType),new[]{adapter});
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task EveryActualCapabilityIsAdmittedBeforeAnyNativeDispatch()
    {
        var first = new NativeAll(ProviderType.MongoDBOASIS);
        var second = new NativeAll(ProviderType.Neo4jOASIS) { Enabled = false };
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(ExecutionPlan(2, first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
        Assert.Equal(0, first.Calls); Assert.Equal(0, second.Calls); Assert.Null(result.Result);
    }

    [Fact]
    public async Task LaterProviderReceivesOnlyRemainingWorkAndCannotPublishPartialSuccess()
    {
        var first = new NativeAll(ProviderType.MongoDBOASIS) { Count = 2 };
        var second = new NativeAll(ProviderType.Neo4jOASIS) { Count = 1 };
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(ExecutionPlan(2, first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.Equal("CQL_RESULT_LIMIT", result.ErrorCode);
        Assert.Equal(0, second.Seen!.MaximumInputRows);
        Assert.True(second.Seen.MaximumInputPayloadBytes < first.Seen!.MaximumInputPayloadBytes);
        Assert.True(second.Seen.MaximumProofPayloadBytes < first.Seen.MaximumProofPayloadBytes);
        Assert.Equal(first.Seen.MaximumStageRows - 2, second.Seen.MaximumStageRows);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 2)]
    public async Task MissingAcknowledgementOrImpossibleCountIsRejected(bool loaded, int counterexamples)
    {
        var adapter = new NativeAll(ProviderType.SQLLiteDBOASIS) { Count = 1, Loaded = loaded, Counterexamples = counterexamples };
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(ExecutionPlan(2, adapter.QueryProviderType), new[] { adapter });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode); Assert.Null(result.Result);
    }

    [Fact]
    public async Task LiveRevocationBeforeLaterDispatchCannotReturnAnEarlierValue()
    {
        var first = new NativeAll(ProviderType.MongoDBOASIS);
        var second = new NativeAll(ProviderType.Neo4jOASIS);
        first.DuringRead = () => second.Enabled = false;
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(ExecutionPlan(2, first.QueryProviderType, second.QueryProviderType), new[] { first, second });
        Assert.Equal("CQL_CAPABILITY_UNSUPPORTED", result.ErrorCode);
        Assert.Equal(1, first.Calls); Assert.Equal(0, second.Calls); Assert.Null(result.Result);
    }

    [Fact]
    public async Task CancellationAfterNativeReadDiscardsTheAcknowledgedPacket()
    {
        using var cancelled = new CancellationTokenSource();
        var adapter = new NativeAll(ProviderType.SQLLiteDBOASIS) { DuringRead = cancelled.Cancel };
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(ExecutionPlan(2, adapter.QueryProviderType), new[] { adapter }, cancelled.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode); Assert.Null(result.Result);
    }
}
