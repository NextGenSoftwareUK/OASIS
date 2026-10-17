using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class HostedHyperDriveFanOutDispatcherTests
{
    [Fact]
    public async Task SuccessfulItemIsCompletedUnderItsLease()
    {
        var item = NewItem(1);
        var store = new RecordingStore(item);
        var dispatcher = new HostedHyperDriveFanOutDispatcher(store, new StubHandler(true), "worker-1",
            new FixedClock());

        var result = await dispatcher.DispatchOnceAsync();

        Assert.False(result.IsError, result.Message);
        Assert.Equal(1, result.Result.CompletedCount);
        Assert.Equal(item.OperationId, Assert.Single(store.Completed));
        Assert.Empty(store.Failed);
    }

    [Fact]
    public async Task FailedItemIsDurablyDeferredWithBoundedBackoff()
    {
        var item = NewItem(4);
        var clock = new FixedClock();
        var store = new RecordingStore(item);
        var dispatcher = new HostedHyperDriveFanOutDispatcher(store,
            new StubHandler(false, "PROVIDER_DOWN"), "worker-1", clock);

        var result = await dispatcher.DispatchOnceAsync();

        Assert.False(result.IsError, result.Message);
        Assert.Equal(1, result.Result.DeferredCount);
        var failed = Assert.Single(store.Failed);
        Assert.Equal("PROVIDER_DOWN", failed.Code);
        Assert.Equal(clock.UtcNow.AddSeconds(8), failed.NextAttemptUtc);
        Assert.Empty(store.Completed);
    }

    [Fact]
    public async Task FailedHeadStopsOrderedBatchAndReleasesEveryLaterClaim()
    {
        var first = NewItem(1);
        var second = NewItem(1);
        var store = new RecordingStore(first, second);
        var handler = new StubHandler(false, "PROVIDER_DOWN");
        var dispatcher = new HostedHyperDriveFanOutDispatcher(store, handler, "worker-1", new FixedClock());

        var result = await dispatcher.DispatchOnceAsync();

        Assert.False(result.IsError, result.Message);
        Assert.Single(store.Failed);
        Assert.Equal(second.OperationId, Assert.Single(store.ReleasedItems));
        Assert.Empty(store.Completed);
    }

    [Fact]
    public async Task LostCompletionLeaseStopsBatchWithVisibleError()
    {
        var store = new RecordingStore(NewItem(1)) { LoseLeaseOnComplete = true };
        var dispatcher = new HostedHyperDriveFanOutDispatcher(store, new StubHandler(true), "worker-1",
            new FixedClock());

        var result = await dispatcher.DispatchOnceAsync();

        Assert.True(result.IsError);
        Assert.Equal("LEASE_LOST", result.ErrorCode);
        Assert.Equal(1, store.ReleaseCount);
    }

    [Fact]
    public async Task BusyGlobalLeaseReturnsNoWorkWithoutReleasingAnotherWorker()
    {
        var store = new RecordingStore(NewItem(1)) { LeaseAcquired = false };
        var dispatcher = new HostedHyperDriveFanOutDispatcher(store, new StubHandler(true), "worker-2",
            new FixedClock());

        var result = await dispatcher.DispatchOnceAsync();

        Assert.False(result.IsError, result.Message);
        Assert.Equal(0, result.Result.ClaimedCount);
        Assert.Equal(0, store.ReleaseCount);
    }

    [Fact]
    public async Task LongProviderCallRenewsItsGlobalAndItemLease()
    {
        var store = new RecordingStore(NewItem(1));
        var dispatcher = new HostedHyperDriveFanOutDispatcher(store,
            new DelayedHandler(TimeSpan.FromMilliseconds(250)), "worker-1");

        var result = await dispatcher.DispatchOnceAsync(leaseDuration: TimeSpan.FromMilliseconds(300));

        Assert.False(result.IsError, result.Message);
        Assert.True(store.RenewCount >= 1);
        Assert.Single(store.Completed);
    }

    private static HostedSyncFanOutItem NewItem(int attempts) => new()
    {
        OperationId = Guid.NewGuid(), AvatarId = Guid.NewGuid(), EntityId = Guid.NewGuid(),
        EntityType = "holon", Kind = SyncOperationKind.Upsert, VersionId = Guid.NewGuid(),
        PayloadJson = "{}", AttemptCount = attempts
    };

    private sealed class FixedClock : IHyperDriveClock
    {
        public DateTime UtcNow { get; } = new(2026, 9, 23, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class StubHandler : IHostedHyperDriveFanOutHandler
    {
        private readonly bool _success;
        private readonly string _code;
        public StubHandler(bool success, string code = null) { _success = success; _code = code; }
        public Task<OASISResult<bool>> ApplyAsync(HostedSyncFanOutItem item, CancellationToken cancellationToken) =>
            Task.FromResult(_success ? new OASISResult<bool>(true) : new OASISResult<bool>
            { IsError = true, ErrorCode = _code, Message = "failed" });
    }

    private sealed class DelayedHandler : IHostedHyperDriveFanOutHandler
    {
        private readonly TimeSpan _delay;
        public DelayedHandler(TimeSpan delay) => _delay = delay;
        public async Task<OASISResult<bool>> ApplyAsync(HostedSyncFanOutItem item,
            CancellationToken cancellationToken)
        {
            await Task.Delay(_delay, cancellationToken);
            return new OASISResult<bool>(true);
        }
    }

    private sealed class RecordingStore : IHostedHyperDriveFanOutStore
    {
        private readonly IReadOnlyList<HostedSyncFanOutItem> _items;
        public bool LoseLeaseOnComplete { get; set; }
        public bool LeaseAcquired { get; set; } = true;
        public List<Guid> Completed { get; } = new();
        public int ReleaseCount { get; private set; }
        public int RenewCount { get; private set; }
        public List<(Guid OperationId, string Code, DateTime NextAttemptUtc)> Failed { get; } = new();
        public List<Guid> ReleasedItems { get; } = new();
        public RecordingStore(params HostedSyncFanOutItem[] items) => _items = items;
        public Task<OASISResult<HostedFanOutClaim>> ClaimFanOutAsync(string workerId,
            int maximumCount, DateTime leaseUntilUtc, CancellationToken cancellationToken) =>
            Task.FromResult(new OASISResult<HostedFanOutClaim>(new HostedFanOutClaim
            { LeaseAcquired = LeaseAcquired, Items = LeaseAcquired ? _items : Array.Empty<HostedSyncFanOutItem>() }));
        public Task<OASISResult<bool>> CompleteFanOutAsync(Guid operationId, string workerId,
            CancellationToken cancellationToken)
        {
            if (LoseLeaseOnComplete) return Task.FromResult(new OASISResult<bool>
            { IsError = true, ErrorCode = "LEASE_LOST" });
            Completed.Add(operationId);
            return Task.FromResult(new OASISResult<bool>(true));
        }
        public Task<OASISResult<bool>> RenewFanOutLeaseAsync(Guid operationId, string workerId,
            DateTime leaseUntilUtc, CancellationToken cancellationToken)
        {
            RenewCount++;
            return Task.FromResult(new OASISResult<bool>(true));
        }
        public Task<OASISResult<bool>> ReleaseFanOutItemsAsync(IReadOnlyList<Guid> operationIds,
            string workerId, CancellationToken cancellationToken)
        {
            ReleasedItems.AddRange(operationIds);
            return Task.FromResult(new OASISResult<bool>(true));
        }
        public Task<OASISResult<bool>> ReleaseFanOutWorkerAsync(string workerId,
            CancellationToken cancellationToken)
        {
            ReleaseCount++;
            return Task.FromResult(new OASISResult<bool>(true));
        }
        public Task<OASISResult<bool>> FailFanOutAsync(Guid operationId, string workerId, string errorCode,
            string message, DateTime nextAttemptUtc, CancellationToken cancellationToken)
        {
            Failed.Add((operationId, errorCode, nextAttemptUtc));
            return Task.FromResult(new OASISResult<bool>(true));
        }
    }
}
