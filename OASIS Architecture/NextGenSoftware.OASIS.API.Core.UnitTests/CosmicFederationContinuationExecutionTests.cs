using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicFederationContinuationExecutionTests
{
    private static readonly ProviderType Mongo = ProviderType.MongoDBOASIS;
    private static readonly ProviderType Sqlite = ProviderType.SQLLiteDBOASIS;
    private static readonly CosmicQueryCapabilities Capabilities = new(CosmicQueryFeatures.Paging,
        new CosmicQueryFieldCapability("Name", typeof(string), CosmicQueryComparisonOperator.Equal));

    private sealed class Adapter(ProviderType provider) : ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType => provider;
        public CosmicQueryCapabilities QueryCapabilities => Capabilities;
        public int Calls;
        public int LastMaximum;
        public bool Fail;
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            LastMaximum = maximumResults;
            return Task.FromResult(Fail
                ? new OASISResult<IReadOnlyList<IHolon>> { IsError = true, ErrorCode = "NATIVE_FAILURE" }
                : new OASISResult<IReadOnlyList<IHolon>>(new[] { new Holon { Id = Guid.NewGuid(), Name = "coffee" } }));
        }
    }

    private static CosmicFederationPlan<Holon> Plan(CosmicFederationOptions options)
        => CosmicFederationPlanner.Plan(CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible),
            options, Mongo, new[] { new CosmicQueryTarget(Mongo, Capabilities),
                new CosmicQueryTarget(Sqlite, Capabilities) }).Result;

    [Fact]
    public async Task SuccessPublishesPostMergeAccountingAndNextReadConsumesIt()
    {
        var options = CosmicFederationOptions.Create(maximumInputRows: 6,
            maximumRowsPerProvider: 3, maximumProviderAttempts: 6).Result;
        var plan = Plan(options);
        var adapters = new[] { new Adapter(Mongo), new Adapter(Sqlite) };
        var first = await CosmicFederationExecutor.ExecuteAsync(plan, adapters);
        Assert.False(first.IsError, first.Message);
        var remaining = Assert.IsType<CosmicFederationRemainingBudget>(first.Result.RemainingBudget);
        Assert.Equal(4, remaining.InputRows);
        Assert.Equal(4, remaining.Attempts);
        Assert.All(remaining.ProviderRows.Values, value => Assert.Equal(2, value));
        Assert.True(remaining.Work < options.MaximumWork);
        Assert.True(remaining.OutputBytes < options.MaximumOutputBytes);
        var second = await CosmicFederationExecutor.ExecuteWithBudgetAsync(plan, adapters, remaining);
        Assert.False(second.IsError, second.Message);
        Assert.Equal(2, second.Result.RemainingBudget.InputRows);
        Assert.Equal(2, second.Result.RemainingBudget.Attempts);
        Assert.All(adapters, adapter => { Assert.Equal(2, adapter.Calls); Assert.Equal(2, adapter.LastMaximum); });
        Assert.True(second.Result.RemainingBudget.InputBytes < remaining.InputBytes);
        Assert.True(second.Result.RemainingBudget.Work < remaining.Work);
        Assert.True(second.Result.RemainingBudget.OutputBytes < remaining.OutputBytes);
    }

    [Theory]
    [InlineData("attempts")]
    [InlineData("target-rows")]
    [InlineData("duration")]
    [InlineData("input-bytes")]
    [InlineData("output-bytes")]
    [InlineData("work")]
    public async Task DepletedCompleteReadSetRejectsBeforeFirstProviderDispatch(string mode)
    {
        var options = CosmicFederationOptions.Create().Result;
        var budget = new CosmicFederationExecutionBudget(options, new[] { Mongo, Sqlite }, default);
        if (mode == "attempts")
            for (var index = 0; index < options.MaximumProviderAttempts - 1; index++) budget.ReserveAttempt(Mongo);
        if (mode == "target-rows") budget.ChargeInput(Sqlite, options.MaximumRowsPerProvider, 0);
        if (mode == "input-bytes") budget.ChargeInput(Mongo, 0, options.MaximumInputBytes);
        if (mode == "output-bytes") budget.ChargeOutput(options.MaximumOutputBytes);
        if (mode == "work") budget.ChargeWork(options.MaximumWork);
        var state = budget.CaptureRemaining();
        if (mode == "duration")
            state = CosmicFederationRemainingBudget.Restore(options, state.InputRows, state.InputBytes,
                state.OutputBytes, state.Work, state.Attempts, 0, state.ProviderRows).Result;
        var adapters = new[] { new Adapter(Mongo), new Adapter(Sqlite) };
        var result = await CosmicFederationExecutor.ExecuteWithBudgetAsync(Plan(options), adapters, state);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.All(adapters, adapter => Assert.Equal(0, adapter.Calls));
    }

    [Fact]
    public async Task NullRemainingAccountingCannotResetToFreshLimits()
    {
        var adapters = new[] { new Adapter(Mongo), new Adapter(Sqlite) };
        var result = await CosmicFederationExecutor.ExecuteWithBudgetAsync(
            Plan(CosmicFederationOptions.Create().Result), adapters, null!);
        Assert.Equal("CQL_FEDERATION_BUDGET_INVALID", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.All(adapters, adapter => Assert.Equal(0, adapter.Calls));
    }

    [Fact]
    public async Task NativeFailureCannotPublishRowsOrRemainingAccounting()
    {
        var options = CosmicFederationOptions.Create().Result;
        var first = new Adapter(Mongo);
        var second = new Adapter(Sqlite) { Fail = true };
        var result = await CosmicFederationExecutor.ExecuteAsync(Plan(options), new[] { first, second });
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public void RestoreRejectsTamperedAccountingWithoutThrowingOrCreatingGrant()
    {
        var options = CosmicFederationOptions.Create().Result;
        var result = CosmicFederationRemainingBudget.Restore(options, options.MaximumInputRows + 1,
            options.MaximumInputBytes, options.MaximumOutputBytes, options.MaximumWork,
            options.MaximumProviderAttempts, options.MaximumDurationMilliseconds,
            new Dictionary<ProviderType, int> { [Mongo] = options.MaximumRowsPerProvider });
        Assert.True(result.IsError);
        Assert.Equal("CQL_FEDERATION_BUDGET_INVALID", result.ErrorCode);
        Assert.Null(result.Result);
    }
}
