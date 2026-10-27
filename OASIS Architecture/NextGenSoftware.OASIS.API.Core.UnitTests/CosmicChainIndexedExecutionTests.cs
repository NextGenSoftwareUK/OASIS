using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicChainIndexedExecutionTests
{
    private readonly Guid actor = Guid.NewGuid();
    private readonly CosmicChainQueryPosition position = new(1, "block");
    private readonly CosmicChainQuerySource source = new(ProviderType.SolanaOASIS, "network",
        ProviderType.SQLLiteDBOASIS, Guid.NewGuid(), 2, new string('A', 64), 0);
    private static CosmicQueryCapabilities Capabilities => new(CosmicQueryFeatures.Ordering | CosmicQueryFeatures.Or | CosmicQueryFeatures.And, new[] {
        new CosmicQueryFieldCapability("Id", typeof(Guid), new[] { CosmicQueryComparisonOperator.Equal },
            Array.Empty<CosmicQueryStringOperator>(), true),
        new CosmicQueryFieldCapability("IsPublic", typeof(bool), CosmicQueryComparisonOperator.Equal),
        new CosmicQueryFieldCapability("CreatedByAvatarId", typeof(Guid), CosmicQueryComparisonOperator.Equal) });

    private CosmicQueryPlan<Holon> Plan(bool access = true)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(source.SourceProvider).Result)
            .WithIndexedSource(new(source, position, 0, TimeSpan.FromMinutes(1))).Result;
        if (access) query = query.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var planned = CosmicQueryPlanner.Plan(query, source.SourceProvider,
            new[] { new CosmicQueryTarget(source.SourceProvider, Capabilities, source) });
        Assert.False(planned.IsError, planned.Message);
        return planned.Result;
    }

    private (Reader Reader, Holon Row) Fixture(string mode)
    {
        var row = new Holon { Id = Guid.NewGuid(), Version = 1, VersionId = Guid.NewGuid(),
            Name = "original", CreatedByAvatarId = mode == "hidden" ? Guid.NewGuid() : actor };
        if (mode == "deleted") row.DeletedDate = DateTime.UtcNow;
        var identity = new CosmicChainQueryRecordIdentity(mode == "mismatch" ? Guid.NewGuid() : row.Id,
            row.Version, row.VersionId, row.IsDeleted, new string('B', 64));
        var receipt = new CosmicChainQueryRecordReceipt(source, position, "commit", identity);
        var rows = mode is "empty" or "stale-empty" ? Array.Empty<IHolon>() : mode is "duplicate" or "budget"
            ? new IHolon[] { row, row } : new IHolon[] { row };
        var receipts = rows.Select(_ => receipt).ToArray();
        var page = new CosmicChainIndexRows(new(source, position, 0),
            new(source.SourceProvider, source.NetworkIdentity, position,
                DateTimeOffset.UtcNow.AddMinutes(mode == "stale-empty" ? -10 : 0)), rows, receipts);
        var reader = new Reader(source, new(page) { IsLoaded = mode != "incomplete", IsWarning = mode == "warning" });
        return (reader, row);
    }

    [Theory]
    [InlineData("rows", null)] [InlineData("empty", null)]
    [InlineData("mismatch", "CQL_SOURCE_RECORD_MISMATCH")]
    [InlineData("deleted", "CQL_VISIBILITY_VIOLATION")] [InlineData("hidden", "CQL_VISIBILITY_VIOLATION")]
    [InlineData("incomplete", "CQL_PROVIDER_RESULT_INVALID")] [InlineData("warning", "CQL_PROVIDER_RESULT_INVALID")]
    [InlineData("duplicate", "CQL_PROVIDER_RESULT_INVALID")] [InlineData("budget", "CQL_QUERY_LIMIT")]
    public async Task PublishesOnlyCompleteBoundVisibleDetachedPages(string mode, string? error)
    {
        var f = Fixture(mode);
        var result = await CosmicIndexedQueryExecutor.ExecuteAsync(Plan(), f.Reader,
            Mock.Of<ICosmicChainQuerySourceObserver>(), maximumResults: mode == "budget" ? 1 : 100);
        Assert.Equal(error, result.ErrorCode);
        Assert.Equal(error != null, result.IsError);
        Assert.Equal(1, f.Reader.Calls);
        if (error != null) { Assert.Null(result.Result); return; }
        Assert.Equal(source.ExecutionProvider, result.Result.Checkpoint.Source.ExecutionProvider);
        if (mode == "rows")
        {
            var detached = Assert.Single(result.Result.Rows);
            Assert.NotSame(f.Row, detached);
            f.Row.Name = "changed after completion";
            Assert.Equal("original", detached.Name);
            Assert.Equal(detached.Id, Assert.Single(result.Result.Receipts).Record.Id);
        }
        else Assert.Empty(result.Result.Rows);
    }

    [Fact]
    public async Task StaleSourceHeadCannotPublishEvenAnEmptyPage()
    {
        var f = Fixture("stale-empty");
        var result = await CosmicIndexedQueryExecutor.ExecuteAsync(Plan(), f.Reader, Mock.Of<ICosmicChainQuerySourceObserver>());
        Assert.True(result.IsError);
        Assert.Null(result.Result);
    }

    [Theory]
    [InlineData("access")] [InlineData("binding")] [InlineData("cancel")]
    public async Task PreflightFailuresPerformNoReaderIo(string mode)
    {
        var f = Fixture("rows");
        if (mode == "binding") f.Reader.Source = new(source.SourceProvider, source.NetworkIdentity,
            source.ExecutionProvider, Guid.NewGuid(), 2, source.CatalogueHash, 0);
        var result = await CosmicIndexedQueryExecutor.ExecuteAsync(Plan(mode != "access"), f.Reader,
            Mock.Of<ICosmicChainQuerySourceObserver>(), cancellationToken: new CancellationToken(mode == "cancel"));
        Assert.True(result.IsError);
        Assert.Equal(0, f.Reader.Calls);
    }

    [Fact]
    public async Task PreparedBillingCannotBeBypassedByStandaloneIndexedOwner()
    {
        var f = Fixture("rows");
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var handle = new UsageExecutionHandle(new UsageOutboxOperation { Id = "indexed-owner", ExecutionOwner = "owner",
            Policy = new UsageEndpointPolicy() }, store.Object, new Web4UsageLedgerOptions(),
            new SubscriptionAuthorizationResult(), Mock.Of<IUsageReservationAuthority>());
        var prepared = new PreparedStorageExecution(handle, "indexed-owner", new Dictionary<ProviderType, string>(), Array.Empty<ProviderType>());
        var result = await prepared.RunAsync(() => CosmicIndexedQueryExecutor.ExecuteAsync(Plan(), f.Reader,
            Mock.Of<ICosmicChainQuerySourceObserver>()));
        Assert.Equal("CQL_INDEXED_BILLING_UNSUPPORTED", result.ErrorCode);
        Assert.Equal(0, f.Reader.Calls);
        store.VerifyNoOtherCalls();
    }

    private sealed class Reader(CosmicChainQuerySource source, OASISResult<CosmicChainIndexRows> result) : ICosmicChainQueryIndexReader
    {
        public CosmicChainQuerySource Source { get; set; } = source;
        public CosmicQueryCapabilities IndexedRowCapabilities => Capabilities;
        public int Calls;
        public Task<OASISResult<CosmicChainIndexRows>> QueryRowsAsync<T>(CosmicQueryPlan<T> plan,
            ICosmicChainQuerySourceObserver observer, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        { Calls++; return Task.FromResult(result); }
    }
}
