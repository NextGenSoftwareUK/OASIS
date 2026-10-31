using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    private sealed class SelectedReader(CosmicChainQuerySource source, CosmicChainIndexRows page, string mode)
        : ICosmicChainQueryIndexProjectionReader
    {
        public int Calls;
        public CosmicChainQuerySource Source => source;
        public CosmicQueryCapabilities IndexedRowCapabilities => Capabilities;
        public CosmicProjectionCapabilities IndexedProjectionCapabilities => new(new[] {
            new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>());
        public Task<OASISResult<CosmicChainIndexRows>> QueryRowsAsync<T>(CosmicQueryPlan<T> plan,
            ICosmicChainQuerySourceObserver observer, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => throw new InvalidOperationException("Selected execution must dispatch native selected cells exactly once.");
        public Task<OASISResult<CosmicChainIndexProjectionRows>> QueryProjectionAsync<T, TResult>(CosmicProjectionPlan<T, TResult> plan,
            ICosmicChainQuerySourceObserver observer, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            return Task.FromResult(new OASISResult<CosmicChainIndexProjectionRows>(new(page,
                page.Rows.Select(row => new CosmicProjectionRow(mode == "forged" ? "forged" : row.Name)))) {
                    IsLoaded = mode != "incomplete", IsWarning = mode == "warning" });
        }
    }

    [Theory]
    [InlineData("rows", null)] [InlineData("empty", null)]
    [InlineData("forged", "CQL_SOURCE_RECORD_MISMATCH")]
    [InlineData("hidden", "CQL_VISIBILITY_VIOLATION")]
    [InlineData("mismatch", "CQL_SOURCE_RECORD_MISMATCH")]
    [InlineData("warning", "CQL_PROVIDER_RESULT_INVALID")]
    [InlineData("incomplete", "CQL_PROVIDER_RESULT_INVALID")]
    public async Task IndexedProjectionUsesTheSharedProofOwnerBeforePublishingSelectedValues(string mode, string? error)
    {
        var fixture = Fixture(mode);
        var reader = new SelectedReader(source, fixture.Reader.Result.Result, mode);
        var selected = Plan().Query.Select(row => row.Name);
        var plan = CosmicProjectionPlanner.Plan(selected.Result, source.SourceProvider, new[] {
            new CosmicProjectionTarget(source.SourceProvider, reader.IndexedRowCapabilities, reader.IndexedProjectionCapabilities, source) });
        Assert.False(plan.IsError, plan.Message);
        var result = await CosmicIndexedProjectionExecutor.ExecuteAsync(plan.Result, reader, Mock.Of<ICosmicChainQuerySourceObserver>());
        Assert.Equal(1, reader.Calls);
        Assert.Equal(error, result.ErrorCode);
        if(error != null) { Assert.True(result.IsError); Assert.Null(result.Result); }
        else { Assert.False(result.IsError, result.Message); Assert.Equal(mode == "empty" ? 0 : 1, result.Result.Rows.Count); }
    }

    [Fact]
    public async Task IndexedProjectionCannotBypassMissingPreparedTariffAdmission()
    {
        var fixture = Fixture("rows");
        var reader = new SelectedReader(source, fixture.Reader.Result.Result, "rows");
        var selected = Plan().Query.Select(row => row.Name);
        var plan = CosmicProjectionPlanner.Plan(selected.Result, source.SourceProvider, new[] {
            new CosmicProjectionTarget(source.SourceProvider, reader.IndexedRowCapabilities, reader.IndexedProjectionCapabilities, source) });
        Assert.False(plan.IsError, plan.Message);
        var handle = new UsageExecutionHandle(new UsageOutboxOperation { Id = "indexed-projection", UserId = actor.ToString("D"),
            Policy = new UsageEndpointPolicy { RequiresProviderMeasurement = true, ReservedUnits = 100, EstimatedCostUsd = 1, MaximumProviderAttempts = 100 } },
            Mock.Of<IUsageOutboxStore>(), new(), new(), Mock.Of<IUsageReservationAuthority>());
        var prepared = new PreparedStorageExecution(handle, "indexed-projection", new Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType, string>(),
            Array.Empty<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>());
        var result = await prepared.RunAsync(() => CosmicIndexedProjectionExecutor.ExecuteAsync(plan.Result, reader, Mock.Of<ICosmicChainQuerySourceObserver>()));
        Assert.True(result.IsError);
        Assert.Equal("CQL_INDEXED_BILLING_UNSUPPORTED", result.ErrorCode);
        Assert.Equal(0, reader.Calls);
    }
}
