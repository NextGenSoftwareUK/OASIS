using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllNativeTests
{
    // One private in-memory database per disposed context. No fixture files,
    // provider-row mocks or retained shared databases are involved.
    private sealed class Native(DataContext context) : ICosmicQueryProjectedAllStorageProvider
    {
        private readonly HolonRepository repository = new(context, () => false);
        public ProviderType QueryProviderType => ProviderType.SQLLiteDBOASIS;
        public CosmicQueryCapabilities QueryCapabilities => SQLLiteDBOASIS.CqlCapabilities;
        public CosmicProjectionCapabilities ProjectionCapabilities => SQLLiteDBOASIS.CqlProjectionCapabilities;
        public CosmicProjectionCompositionCapabilities CompositionCapabilities => SQLLiteDBOASIS.CqlCompositionCapabilities;
        public Task<OASISResult<CosmicProjectedAllPacket>> ExecuteCosmicProjectedAllAsync<T, TResult>(
            CosmicProjectedAllPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
            CancellationToken cancellationToken = default) where T : IHolon
            => repository.ExecuteCosmicProjectedAllAsync(plan, limits, cancellationToken);
        public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T, TResult>(
            CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
            CancellationToken cancellationToken = default) where T : IHolon
            => repository.ExecuteCosmicProjectionCompositionAsync(plan, limits, cancellationToken);
        public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
            int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
            => throw new Exception("Projected All cannot hydrate rows.");
    }

    private static CosmicQuery<Holon> Source => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result);
    private static CosmicProjectedAllPlan<Holon, TResult> Plan<TResult>(ICosmicQueryProjectedAllStorageProvider native, CosmicProjectedAllQuery<Holon, TResult> query,
        CosmicProjectionCompositionOptions? options = null)
    {
        var result = CosmicProjectedAllPlanner.Plan(query, native.QueryProviderType,
            new[] { new CosmicProjectionCompositionTarget(native.QueryProviderType, native.QueryCapabilities,
                native.ProjectionCapabilities, native.CompositionCapabilities) }, options);
        Assert.False(result.IsError, result.Message); return result.Result;
    }
    private static async Task<DataContext> Database(int count = 3)
    {
        var context = new DataContext("Data Source=:memory:");
        await context.Database.OpenConnectionAsync();
        await Sql(context, "CREATE TABLE Holon(Id TEXT,Name TEXT,Description TEXT,Version INTEGER,IsPublic INTEGER,CreatedByAvatarId TEXT);"
            + "WITH RECURSIVE n(x) AS (SELECT 1 WHERE " + count + ">0 UNION ALL SELECT x+1 FROM n WHERE x<" + count + ")"
            + " INSERT INTO Holon SELECT printf('00000000-0000-0000-0000-%012d',x),'coffee','root',x,1,NULL FROM n;");
        return context;
    }
    private static async Task Sql(DataContext context, string text)
    {
        using var command = context.Database.GetDbConnection().CreateCommand(); command.CommandText = text;
        await command.ExecuteNonQueryAsync();
    }
    private static async Task<bool> Read<TResult>(ICosmicQueryProjectedAllStorageProvider native, CosmicProjectedAllQuery<Holon, TResult> query)
    {
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(Plan(native, query), new[] { native });
        Assert.False(result.IsError, result.Message + result.Exception); Assert.True(result.IsLoaded);
        return Assert.Single(result.Result.Providers).Value;
    }

    [Theory]
    [InlineData(0, true)] [InlineData(150, false)]
    public async Task NativeAllReadsBeyondThePublicRowPageAndProvesEmptyInput(int count, bool expected)
    {
        using var context = await Database(count); var native = new Native(context);
        Assert.Equal(expected, await Read(native, Source.Select(row => row.Version).All(value => value < 150).Result));
        Assert.Null(context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task NativeAllRunsAfterBothSourceAndSelectedWindows()
    {
        using var context = await Database(); var native = new Native(context);
        var projection = Source.OrderBy(row => row.Version).Skip(1).Take(2)
            .Select(row => new { Revision = row.Version, Label = row.Name })
            .Where(row => row.Revision > 1).OrderByDescending(row => row.Revision).Skip(1).Take(1);
        Assert.True(await Read(native, projection.All(row => row.Revision == 2 && row.Label.StartsWith("co", StringComparison.Ordinal)).Result));
        Assert.False(await Read(native, projection.All(row => row.Revision == 3).Result));
        Assert.True(await Read(native, Source.Select(row => row.Version).Take(0).All(value => false).Result));
    }

    [Fact]
    public async Task ThirtyTwoSelectedStagesLeaveRoomForTheNativeTerminal()
    {
        using var context = await Database(); var native = new Native(context);
        var projection = Source.Select(row => row.Version);
        for (var index = 0; index < 32; index++) projection = projection.Take(1);
        Assert.True(await Read(native, projection.All(value => value == 1).Result));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LaterSelectedFiltersCannotHideMalformedSourceValues(bool empty)
    {
        using var context = await Database(); var native = new Native(context);
        await Sql(context, "UPDATE Holon SET Version='bad' WHERE Version=3");
        var projection = Source.Select(row => row.Version);
        projection = empty ? projection.Take(0) : projection.Where(value => value < 0);
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(Plan(native, projection.All(value => true).Result), new[] { native });
        Assert.Equal("CQL_DATA_INVALID", result.ErrorCode); Assert.Null(result.Result);
        Assert.Null(context.Database.CurrentTransaction);
    }

    [Theory]
    [InlineData(2, 100, false)] [InlineData(3, 2, false)] [InlineData(3, 3, true)]
    public async Task CompleteInputAndTerminalScanRespectExactWorkBoundaries(int input, int stage, bool succeeds)
    {
        using var context = await Database(); var native = new Native(context);
        var query = Source.Select(row => row.Version).All(value => value > 0).Result;
        var options = new CosmicProjectionCompositionOptions(input, 10000, stage, 10000);
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(Plan(native, query, options), new[] { native });
        Assert.Equal(succeeds, !result.IsError);
        if (!succeeds) { Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode); Assert.Null(result.Result); }
        else Assert.True(Assert.Single(result.Result.Providers).Value);
    }

    [Fact]
    public async Task NativeAllCancellationReleasesTheSnapshotForReuse()
    {
        using var context = await Database(); var native = new Native(context);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var query = Source.Select(row => row.Version).All(value => value > 0).Result;
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(Plan(native, query), new[] { native }, cancelled.Token);
        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode); Assert.Null(context.Database.CurrentTransaction);
        Assert.True(await Read(native, query));
    }

    [Fact]
    public async Task NullComparisonsAndNativeReductionNeverConstructSelectedDtos()
    {
        using var context = await Database(); var native = new Native(context);
        await Sql(context, "UPDATE Holon SET Name=NULL WHERE Version=1");
        Assert.True(await Read(native, Source.Select(row => row.Name).All(value => value == null || value == "coffee").Result));
        Assert.False(await Read(native, Source.Select(row => row.Name).All(value => value != null).Result));
        var query = Source.Select(row => new NeverConstructed { Revision = row.Version }).All(row => row.Revision > 0).Result;
        Assert.True(await Read(native, query));
    }
    public sealed class NeverConstructed
    {
        public NeverConstructed() => throw new Exception("Projected All must not construct selected DTOs.");
        public int Revision { get; set; }
    }

    [Fact]
    public async Task CancellationInsideActualSqlReleasesTheOwnerAndAllowsReuse()
    {
        using var context = await Database(); var native = new Native(context);
        using var cancellation = new CancellationTokenSource(); var entered = 0;
        var connection = (SqliteConnection)context.Database.GetDbConnection();
        connection.CreateFunction<long, long>("all_observe", value =>
        { if (Interlocked.Increment(ref entered) == 1) cancellation.Cancel(); return value; }, isDeterministic: false);
        await Sql(context, "ALTER TABLE Holon RENAME TO AllInput; CREATE VIEW Holon AS SELECT Id,Name,Description,"
            + "all_observe(Version) AS Version,IsPublic,CreatedByAvatarId FROM AllInput");
        var query = Source.Select(row => row.Version).All(value => value > 0).Result;
        var result = await CosmicProjectedAllExecutor.ExecuteAsync(Plan(native, query), new[] { native }, cancellation.Token);
        Assert.True(entered > 0); Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.Null(result.Result); Assert.Null(context.Database.CurrentTransaction);
        Assert.True(await Read(native, query));
    }

    [Fact]
    public async Task ActivatedShippingAdapterExecutesNativeAllAgainstItsSavedRecord()
    {
        const string connectionString = "Data Source=cql-projected-all-shipping;Mode=Memory;Cache=Shared";
        // Keep this named in-memory database alive only for the shipping adapter's
        // independent connections. Disposal removes the entire owned fixture.
        using var keeper = new SqliteConnection(connectionString); await keeper.OpenAsync();
        using var provider = new SQLLiteDBOASIS(connectionString);
        var activated = await provider.ActivateProviderAsync();
        Assert.False(activated.IsError, activated.Message + activated.Exception);
        var row = new Holon { Name = "coffee" };
        var saved = await provider.SaveHolonAsync(row, saveChildren: false);
        Assert.False(saved.IsError, saved.Message + saved.Exception);
        var id = row.Id;
        var source = Source.Where(value => value.Id == id).Select(value => value.Name);
        Assert.True(await Read(provider, source.All(value => value == "coffee").Result));
        Assert.False(await Read(provider, source.All(value => value == "tea").Result));
    }

    [Fact]
    public async Task ShippingAdapterRequiresActivationBeforeNativeEntry()
    {
        using var provider = new SQLLiteDBOASIS("Data Source=:memory:");
        Assert.True(provider.CompositionCapabilities.SupportsAll);
        var query = Source.Select(row => row.Version).All(value => true).Result;
        using var context = await Database(0); var native = new Native(context);
        var plan = Plan(native, query);
        var result = await provider.ExecuteCosmicProjectedAllAsync(plan,
            new CosmicProjectionCompositionExecutionLimits(1000,1048576,32000,1048576,10000,1048576));
        Assert.Equal("CQL_PROVIDER_NOT_READY", result.ErrorCode);
    }
}
