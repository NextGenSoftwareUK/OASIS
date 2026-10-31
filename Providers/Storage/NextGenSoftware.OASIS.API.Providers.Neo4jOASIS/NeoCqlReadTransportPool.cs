using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

// A driver is shared between successive reads, never concurrent read owners.
// Closing one cancelled transport cannot interrupt another read or a write.
internal sealed class NeoCqlReadTransportPool : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly Func<IDriver> factory;
    private readonly Channel<Slot> available;
    private readonly HashSet<Slot> slots = new();
    private readonly CancellationTokenSource shutdown = new();
    private Task closing;

    internal NeoCqlReadTransportPool(Func<IDriver> factory, int maximumOwners = 16)
    {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        if (maximumOwners < 1) throw new ArgumentOutOfRangeException(nameof(maximumOwners));
        available = Channel.CreateBounded<Slot>(maximumOwners);
        for (var index = 0; index < maximumOwners; index++)
        {
            var slot = new Slot(factory);
            slots.Add(slot);
            available.Writer.TryWrite(slot);
        }
    }

    internal async Task<Lease> AcquireAsync(CancellationToken cancellationToken)
    {
        CancellationTokenSource linked;
        lock (gate)
        {
            if (closing != null) throw new ObjectDisposedException(nameof(NeoCqlReadTransportPool));
            linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, shutdown.Token);
        }
        Slot slot = null;
        try
        {
            slot = await available.Reader.ReadAsync(linked.Token).ConfigureAwait(false);
            return new Lease(this, slot, linked);
        }
        catch
        {
            linked.Dispose();
            if (slot != null)
            {
                await slot.CloseAsync().ConfigureAwait(false);
                await ReturnAsync(slot).ConfigureAwait(false);
            }
            throw;
        }
    }

    private async ValueTask ReturnAsync(Slot slot)
    {
        lock (gate)
        {
            if (closing == null)
            {
                if (slot.IsClosed)
                {
                    slots.Remove(slot);
                    slot = new Slot(factory);
                    slots.Add(slot);
                }
                if (!available.Writer.TryWrite(slot))
                    throw new InvalidOperationException("A CQL read transport was returned more than once.");
                return;
            }
            slots.Remove(slot);
        }
        await slot.CloseAsync().ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        lock (gate)
        {
            if (closing != null) return new ValueTask(closing);
            available.Writer.TryComplete();
            var owned = slots.ToArray();
            // Queue while holding the gate, so closing is published before any
            // driver callback can return a lease. The task is retained/joined.
            closing = Task.Run(async () =>
            {
                try
                {
                    shutdown.Cancel();
                    await Task.WhenAll(owned.Select(slot => slot.CloseAsync())).ConfigureAwait(false);
                }
                finally { shutdown.Dispose(); }
            });
            return new ValueTask(closing);
        }
    }

    internal sealed class Lease : IAsyncDisposable
    {
        private readonly NeoCqlReadTransportPool pool;
        private readonly Slot slot;
        private readonly CancellationTokenSource cancellation;
        private readonly CancellationTokenRegistration registration;
        private int disposed;
        internal IDriver Driver { get; }

        internal Lease(NeoCqlReadTransportPool pool, Slot slot, CancellationTokenSource cancellation)
        {
            this.pool = pool;
            this.slot = slot;
            this.cancellation = cancellation;
            Driver = slot.Driver;
            registration = cancellation.Token.Register(() => slot.CloseAsync());
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0) return;
            try
            {
                await registration.DisposeAsync().ConfigureAwait(false);
                if (cancellation.IsCancellationRequested || slot.IsClosed)
                    await slot.CloseAsync().ConfigureAwait(false);
            }
            finally
            {
                cancellation.Dispose();
                await pool.ReturnAsync(slot).ConfigureAwait(false);
            }
        }
    }

    internal sealed class Slot
    {
        private readonly object gate = new();
        private readonly Func<IDriver> factory;
        private IDriver driver;
        private Task closing;
        internal Slot(Func<IDriver> factory) => this.factory = factory;
        internal bool IsClosed { get { lock (gate) return closing != null; } }
        internal IDriver Driver
        {
            get
            {
                lock (gate)
                {
                    if (closing != null) throw new ObjectDisposedException("CQL read transport");
                    return driver ??= factory() ?? throw new InvalidOperationException("CQL driver factory returned null.");
                }
            }
        }
        internal Task CloseAsync()
        {
            lock (gate) return closing ??= Task.Run(() => CloseDriverAsync(driver));
        }
        private static async Task CloseDriverAsync(IDriver driver)
        {
            if (driver != null) await driver.CloseAsync().ConfigureAwait(false);
        }
    }
}
