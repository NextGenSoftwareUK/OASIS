using Moq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicChainIndexedExecutionTests
{
    private CosmicIndexedReadTariffPolicy Tariffs(int maximum = 100) => new(source, maximum,
        CosmicIndexedReadTariffPolicy.Required.Select(op => new CosmicIndexedReadTariff(op,
            op == CosmicIndexedReadOperation.IndexSqlSnapshot ? 0.01m : 0.002m, "indexed-test-v1")));

    [Theory]
    [InlineData("rows")] [InlineData("empty")] [InlineData("actor")] [InlineData("missing")]
    [InlineData("begin")] [InlineData("receipt")] [InlineData("transport")] [InlineData("budget")]
    [InlineData("unmetered")] [InlineData("cancel-after-begin")]
    [InlineData("unit-budget")] [InlineData("price-budget")] [InlineData("attempt-budget")]
    public async Task PreparedIndexedReadsOwnEachDurablePhysicalAttempt(string mode)
    {
        var f = Fixture(mode == "empty" ? "empty" : "rows");
        var reader = new BillingReader(source, f.Reader.Result, mode);
        var begins = new List<UsageProviderAttempt>(); var receipts = new List<UsageProviderReceipt>();
        using var cancellation = new CancellationTokenSource();
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        store.Setup(s => s.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()))
            .Returns((string id, string owner, UsageProviderAttempt attempt, CancellationToken ct) => {
                begins.Add(attempt);
                Assert.Equal(reader.PhysicalCalls, receipts.Count); // Begin precedes the next physical operation.
                if (mode == "begin") throw new IOException("begin persistence unknown");
                if (mode == "cancel-after-begin") cancellation.Cancel();
                return Task.CompletedTask;
            });
        store.Setup(s => s.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
            .Returns((string id, string owner, UsageProviderReceipt receipt, CancellationToken ct) => {
                receipt.Validate();
                if (mode == "receipt") throw new IOException("receipt persistence unknown");
                receipts.Add(receipt); return Task.CompletedTask;
            });
        var operation = new UsageOutboxOperation { Id = "indexed-billing", ExecutionOwner = "owner",
            UserId = (mode == "actor" ? Guid.NewGuid() : actor).ToString("D"), Policy = new UsageEndpointPolicy { RequiresProviderMeasurement=true,
                ReservedUnits=mode=="unit-budget"?1:100, EstimatedCostUsd=mode=="price-budget"?0:1,
                MaximumProviderAttempts=mode=="attempt-budget"?1:100 } };
        var handle = new UsageExecutionHandle(operation, store.Object, new Web4UsageLedgerOptions(),
            new SubscriptionAuthorizationResult(), Mock.Of<IUsageReservationAuthority>());
        var prepared = new PreparedStorageExecution(handle, "indexed-billing", new Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType, string>(),
            Array.Empty<NextGenSoftware.OASIS.API.Core.Enums.ProviderType>(), null, null, false,
            mode == "missing" ? Array.Empty<CosmicIndexedReadTariffPolicy>() : new[] { Tariffs(mode == "budget" ? 4 : 100) });
        var result = await prepared.RunAsync(() => CosmicIndexedQueryExecutor.ExecuteAsync(Plan(), reader,
            Mock.Of<ICosmicChainQuerySourceObserver>(), cancellationToken: cancellation.Token));
        if (mode is "rows" or "empty")
        {
            Assert.False(result.IsError, result.Message + result.Exception);
            Assert.Equal(mode == "empty" ? 4 : 7, begins.Count);
            Assert.Equal(begins.Count, receipts.Count);
            Assert.Equal(mode == "empty" ? 0.016m : 0.022m, receipts.Sum(r => r.ActualCostUsd));
            Assert.Single(receipts.Where(r => r.Provider == source.ExecutionProvider.ToString()));
            Assert.All(receipts, receipt => {
                Assert.Contains(Tariffs().BindingHash, receipt.Model);
                Assert.Equal("CQL-indexed-physical-read-service-tariff", receipt.CostSource);
                Assert.Equal(receipt.ClientRequestId, receipt.ProviderRequestId);
                Assert.Equal(1, receipt.Units);
                Assert.Contains(begins, begin => begin.ClientRequestId == receipt.ClientRequestId && begin.Provider == receipt.Provider && begin.Model == receipt.Model);
            });
        }
        else
        {
            Assert.True(result.IsError); Assert.Null(result.Result);
            if (mode is "actor" or "missing" or "unit-budget" or "price-budget" or "attempt-budget") { Assert.Equal(0, reader.Calls); Assert.Empty(begins); Assert.False(prepared.ProviderExecutionPossible); }
            if (mode is "unit-budget" or "price-budget" or "attempt-budget") Assert.Equal("CQL_INDEX_RESERVATION_REQUIRED",result.ErrorCode);
            if (mode == "actor") Assert.Equal("CQL_ACTOR_MISMATCH", result.ErrorCode);
            if (mode == "missing") Assert.Equal("CQL_INDEXED_BILLING_UNSUPPORTED", result.ErrorCode);
            if (mode is "begin" or "cancel-after-begin") { Assert.Equal(0, reader.PhysicalCalls); Assert.Single(begins); Assert.Empty(receipts); }
            if (mode == "receipt") { Assert.Equal(1, reader.PhysicalCalls); Assert.Single(begins); Assert.Empty(receipts); }
            if (mode == "transport") { Assert.Equal(2, reader.PhysicalCalls); Assert.Equal(2, begins.Count); Assert.Single(receipts); }
            if (mode == "budget") { Assert.Equal("CQL_QUERY_LIMIT", result.ErrorCode); Assert.Equal(4, reader.PhysicalCalls); Assert.Equal(4, begins.Count); }
            if (mode == "unmetered") { Assert.True(prepared.ProviderExecutionPossible); Assert.Equal("CQL_INDEXED_CAPTURE_INCOMPLETE", result.ErrorCode); Assert.Empty(begins); }
        }
        store.Verify(s => s.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Exactly(begins.Count));
        store.Verify(s => s.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()), Times.Exactly(mode == "receipt" ? 1 : receipts.Count));
        store.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("missing")] [InlineData("duplicate")] [InlineData("negative")] [InlineData("history")]
    public void IndexedTariffsRejectIncompleteOrUnenrolledOperationPolicies(string mode)
    {
        var tariffs = Tariffs().Tariffs.ToArray();
        if (mode == "missing") tariffs = tariffs.Skip(1).ToArray();
        if (mode == "duplicate") tariffs[1] = tariffs[0];
        if (mode == "negative") tariffs[0] = tariffs[0] with { CostUsd = -1 };
        if (mode == "history") tariffs[0] = tariffs[0] with { Operation = CosmicIndexedReadOperation.SourceHistory };
        Assert.Throws<ArgumentException>(() => new CosmicIndexedReadTariffPolicy(source, 100, tariffs));
    }

    private sealed class BillingReader(CosmicChainQuerySource source, OASISResult<CosmicChainIndexRows> page, string mode) : ICosmicChainQueryIndexReader
    {
        public CosmicChainQuerySource Source => source;
        public CosmicQueryCapabilities IndexedRowCapabilities => Capabilities;
        public int Calls, PhysicalCalls;
        public async Task<OASISResult<CosmicChainIndexRows>> QueryRowsAsync<T>(CosmicQueryPlan<T> plan,
            ICosmicChainQuerySourceObserver observer, int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
        {
            Calls++;
            if (mode == "unmetered") return page;
            await CosmicIndexedReadAttempts.ExecuteIndexSnapshotAsync(source, () => Task.FromResult(++PhysicalCalls), cancellationToken);
            async Task Rpc(CosmicIndexedReadOperation operation)
            {
                await CosmicIndexedReadAttempts.ExecuteSourceRpcAsync(source.SourceProvider, operation, () => {
                    PhysicalCalls++;
                    if (mode == "transport") throw new IOException("unknown RPC outcome");
                    return Task.FromResult(true);
                }, cancellationToken);
            }
            foreach (var row in page.Result.Rows)
            {
                await Rpc(CosmicIndexedReadOperation.SourceGenesis);
                await Rpc(CosmicIndexedReadOperation.SourceTransaction);
                await Rpc(CosmicIndexedReadOperation.SourceBlock);
            }
            await Rpc(CosmicIndexedReadOperation.SourceGenesis);
            await Rpc(CosmicIndexedReadOperation.SourceSlot);
            await Rpc(CosmicIndexedReadOperation.SourceBlock);
            return page;
        }
    }
}
