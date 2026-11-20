using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AccessLog;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class AccessLogShutdownAcknowledgementTests
{
    private sealed class VisibleBeforeAcknowledgementStore : IAccessLogStore
    {
        public TaskCompletionSource<bool> Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Acknowledge { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken WriteToken { get; private set; }
        public List<AccessLogEvent> Visible { get; } = new();
        public int Calls { get; private set; }
        public int Acknowledged { get; private set; }

        public async Task InsertAsync(IReadOnlyList<AccessLogEvent> batch, CancellationToken token)
        {
            Calls++;
            WriteToken = token;
            // MongoDB can expose a committed event before the client's insert
            // acknowledgement completes. Hold exactly that boundary.
            Visible.AddRange(batch);
            Entered.TrySetResult(true);
            await Acknowledge.Task.WaitAsync(token);
            Acknowledged++;
        }

        public Task<IReadOnlyList<AccessLogEvent>> QueryByAvatarAsync(Guid id, int limit, int offset, CancellationToken token)
            => Task.FromResult<IReadOnlyList<AccessLogEvent>>(Visible.Where(e => e.AvatarId == id).Skip(offset).Take(limit).ToArray());
        public Task<IReadOnlyList<AccessLogEvent>> QueryByHolonAsync(Guid id, int limit, int offset, CancellationToken token)
            => Task.FromResult<IReadOnlyList<AccessLogEvent>>(Visible.Where(e => e.HolonIds.Contains(id)).Skip(offset).Take(limit).ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PeriodicWriteAcknowledgementUsesShutdownBudgetInsteadOfTimerCancellation(bool budgetCancelled)
    {
        var settings = new AccessLogSettings { Enabled = true, FlushIntervalSeconds = 1 };
        var store = new VisibleBeforeAcknowledgementStore();
        var service = new AccessLogService(() => settings, store);
        var actor = Guid.NewGuid(); var holon = Guid.NewGuid();
        service.Record(actor, "acknowledgement-test", "load", new[] { (holon, "Holon") });
        settings.Enabled = false;
        using var worker = new AccessLogFlushHostedService(service, () => settings,
            NullLogger<AccessLogFlushHostedService>.Instance);
        try
        {
            await worker.StartAsync(CancellationToken.None);
            await store.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(holon, Assert.Single(Assert.Single(store.Visible).HolonIds));
            using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var stopping = worker.StopAsync(budget.Token);
            if (budgetCancelled)
            {
                budget.Cancel();
                Assert.True(store.WriteToken.IsCancellationRequested);
            }
            else
            {
                Assert.False(store.WriteToken.IsCancellationRequested,
                    "Stopping the periodic timer must not cancel an accepted write acknowledgement.");
                Assert.False(stopping.IsCompleted);
                store.Acknowledge.TrySetResult(true);
            }
            await stopping.WaitAsync(TimeSpan.FromSeconds(5));
            if (worker.ExecuteTask != null)
                await worker.ExecuteTask.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(1, store.Calls);
            Assert.Equal(budgetCancelled ? 0 : 1, store.Acknowledged);
            Assert.Equal(budgetCancelled ? 1 : 0, service.FailedBatches);
            Assert.Equal(0, service.DroppedEvents);
            Assert.Single(store.Visible);
        }
        finally
        {
            store.Acknowledge.TrySetResult(true);
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await worker.StopAsync(cleanup.Token);
        }
    }
}
