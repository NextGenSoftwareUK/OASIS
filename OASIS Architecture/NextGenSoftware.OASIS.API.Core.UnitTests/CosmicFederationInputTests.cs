using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicFederationInputTests
{
    private static readonly CosmicQueryCapabilities Capability = new(CosmicQueryFeatures.Paging,
        new CosmicQueryFieldCapability("Name", typeof(string), CosmicQueryComparisonOperator.Equal));

    private sealed class Adapter(ProviderType provider) : ICosmicQueryStorageProvider
    {
        public ProviderType QueryProviderType => provider;
        public CosmicQueryCapabilities QueryCapabilities { get; set; } = Capability;
        public int Calls;
        public int Budget;
        public int Offset;
        public int? Limit;
        public Func<CancellationToken, Task<OASISResult<IReadOnlyList<IHolon>>>> Handler =
            _ => Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(Array.Empty<IHolon>()));
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            Budget = maximumResults;
            Offset = plan.Query.Offset;
            Limit = plan.Query.Limit;
            Assert.Equal(provider, Assert.Single(plan.Targets).Provider);
            return Handler(cancellationToken);
        }
    }

    private static CosmicFederationPlan<Holon> Plan(CosmicFederationOptions? options = null,
        CosmicQuery<Holon>? query = null)
        => CosmicFederationPlanner.Plan(query ?? CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible),
            options ?? CosmicFederationOptions.Create().Result, ProviderType.MongoDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.MongoDBOASIS, Capability),
                new CosmicQueryTarget(ProviderType.SQLLiteDBOASIS, Capability) }).Result;

    private static OASISResult<IReadOnlyList<IHolon>> Rows(params IHolon[] rows) => new(rows);

    [Fact]
    public async Task CompleteInputUsesOrdinaryExecutorOncePerTargetAndDetachesRows()
    {
        var row = new Holon { Id = Guid.NewGuid(), Name = "before" };
        var mongo = new Adapter(ProviderType.MongoDBOASIS) { Handler = _ => Task.FromResult(Rows(row)) };
        var sqlite = new Adapter(ProviderType.SQLLiteDBOASIS);
        var result = await CosmicFederationInputCapture.CaptureAsync(Plan(), new[] { mongo, sqlite });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(2, result.Result.Providers.Count);
        Assert.Equal(1, mongo.Calls);
        Assert.Equal(1, sqlite.Calls);
        Assert.Equal(1000, mongo.Budget);
        row.Name = "after";
        Assert.Equal("before", Assert.Single(result.Result.Providers[0].Rows).Name);
        Assert.Empty(result.Result.Providers[1].Rows);
        result.Result.Budget.Checkpoint();
    }

    [Fact]
    public async Task LaterUnavailableOrIncapableAdapterRejectsBeforeAnyRead()
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).Where(row => row.Name == "allowed").Result;
        var mongo = new Adapter(ProviderType.MongoDBOASIS);
        var missing = await CosmicFederationInputCapture.CaptureAsync(Plan(query: source), new[] { mongo });
        Assert.Equal("CQL_PLAN_TARGET_UNAVAILABLE", missing.ErrorCode);
        var sqlite = new Adapter(ProviderType.SQLLiteDBOASIS)
        { QueryCapabilities = new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>()) };
        var incapable = await CosmicFederationInputCapture.CaptureAsync(Plan(query: source), new[] { mongo, sqlite });
        Assert.True(incapable.IsError);
        Assert.Null(incapable.Result);
        Assert.Equal(0, mongo.Calls);
        Assert.Equal(0, sqlite.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CumulativeRowsAndBytesRejectWithoutPartialInput(bool bytes)
    {
        var options = bytes ? CosmicFederationOptions.Create(maximumInputBytes: 1).Result
            : CosmicFederationOptions.Create(maximumInputRows: 1).Result;
        var mongo = new Adapter(ProviderType.MongoDBOASIS)
        { Handler = _ => Task.FromResult(Rows(new Holon { Id = Guid.NewGuid() })) };
        var sqlite = new Adapter(ProviderType.SQLLiteDBOASIS)
        { Handler = _ => Task.FromResult(Rows(new Holon { Id = Guid.NewGuid() })) };
        var result = await CosmicFederationInputCapture.CaptureAsync(Plan(options), new[] { mongo, sqlite });
        Assert.Equal("CQL_FEDERATION_INPUT_LIMIT", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.Equal(1, mongo.Calls);
        Assert.Equal(bytes ? 0 : 1, sqlite.Calls);
    }

    [Fact]
    public async Task LaterNativeFailurePreservesErrorAndNeverRetriesOrReturnsEarlierInput()
    {
        var mongo = new Adapter(ProviderType.MongoDBOASIS);
        var sqlite = new Adapter(ProviderType.SQLLiteDBOASIS)
        { Handler = _ => Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>
            { IsError = true, ErrorCode = "NATIVE_FAILURE", Message = "Rejected" }) };
        var result = await CosmicFederationInputCapture.CaptureAsync(Plan(), new[] { mongo, sqlite });
        Assert.Equal("NATIVE_FAILURE", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.Equal(1, mongo.Calls);
        Assert.Equal(1, sqlite.Calls);
    }

    [Fact]
    public async Task NativeWarningCannotBeUpgradedIntoCompleteInput()
    {
        var mongo = new Adapter(ProviderType.MongoDBOASIS)
        { Handler = _ => Task.FromResult(new OASISResult<IReadOnlyList<IHolon>>(Array.Empty<IHolon>()) { IsWarning = true }) };
        var sqlite = new Adapter(ProviderType.SQLLiteDBOASIS);
        var result = await CosmicFederationInputCapture.CaptureAsync(Plan(), new[] { mongo, sqlite });
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID", result.ErrorCode);
        Assert.Null(result.Result);
        Assert.Equal(0, sqlite.Calls);
    }

    [Fact]
    public async Task CumulativeDeadlineCancelsTheNativeTokenAndStopsLaterDispatch()
    {
        var options = CosmicFederationOptions.Create(maximumDurationMilliseconds: 200).Result;
        var cancelled = false;
        var mongo = new Adapter(ProviderType.MongoDBOASIS)
        { Handler = async token => { try { await Task.Delay(10000, token); }
            catch (OperationCanceledException) { cancelled = token.IsCancellationRequested; throw; }
            return Rows(); } };
        var sqlite = new Adapter(ProviderType.SQLLiteDBOASIS);
        var result = await CosmicFederationInputCapture.CaptureAsync(Plan(options), new[] { mongo, sqlite });
        Assert.Equal("CQL_FEDERATION_DEADLINE", result.ErrorCode);
        Assert.True(cancelled);
        Assert.Null(result.Result);
        Assert.Equal(0, sqlite.Calls);
    }

    [Fact]
    public async Task CallerCancellationRejectsBeforeAnyDispatch()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var mongo = new Adapter(ProviderType.MongoDBOASIS);
        var result = await CosmicFederationInputCapture.CaptureAsync(Plan(), new[] { mongo }, cancellation.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.Equal(0, mongo.Calls);
    }

    [Fact]
    public async Task InputKeepsReplicaCopiesAndPerSourceWindowsSeparateFromGlobalPaging()
    {
        var id = Guid.NewGuid();
        var mongo = new Adapter(ProviderType.MongoDBOASIS)
        { Handler = _ => Task.FromResult(Rows(new Holon { Id = id, Name = "mongo" })) };
        var sqlite = new Adapter(ProviderType.SQLLiteDBOASIS)
        { Handler = _ => Task.FromResult(Rows(new Holon { Id = id, Name = "sqlite" })) };
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.AllEligible).Skip(2).Take(3).Result;
        var options = CosmicFederationOptions.Create(skip: 8, take: 1).Result;
        var result = await CosmicFederationInputCapture.CaptureAsync(Plan(options, source), new[] { mongo, sqlite });
        Assert.False(result.IsError, result.Message);
        Assert.Equal(new[] { "mongo", "sqlite" }, result.Result.Providers.Select(group => Assert.Single(group.Rows).Name));
        Assert.Equal(2, mongo.Offset);
        Assert.Equal(2, sqlite.Offset);
        Assert.Equal(3, mongo.Limit);
        Assert.Equal(3, sqlite.Limit);
    }

    [Fact]
    public async Task NullAndDuplicateRegistriesRejectWithoutDispatch()
    {
        var mongo = new Adapter(ProviderType.MongoDBOASIS);
        var absent = await CosmicFederationInputCapture.CaptureAsync(Plan(), null!);
        Assert.Equal("CQL_FEDERATION_EXECUTION_INVALID", absent.ErrorCode);
        var duplicate = await CosmicFederationInputCapture.CaptureAsync(Plan(), new[] { mongo, mongo });
        Assert.Equal("CQL_FEDERATION_EXECUTION_INVALID", duplicate.ErrorCode);
        Assert.Equal(0, mongo.Calls);
    }
}
