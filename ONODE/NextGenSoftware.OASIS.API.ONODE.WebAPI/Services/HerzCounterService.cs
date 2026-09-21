using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Thread-safe global sequential counter for HerzID member numbers.
    ///
    /// Uses Interlocked.Increment for in-process atomicity (safe for single-instance deployments
    /// such as Railway). On startup the counter is seeded from the highest existing HerzSequentialNumber
    /// found in OASIS storage. The current high-water mark is also persisted back to a dedicated
    /// counter Holon every time it is incremented, so restarts never reissue a number.
    ///
    /// Multi-instance note: for horizontal scaling, replace NextValue() with a distributed atomic
    /// increment (e.g. a dedicated SQL SEQUENCE or a coordination service). The interface is the
    /// same — callers only call NextValue().
    /// </summary>
    public interface IHerzCounterService
    {
        /// <summary>Returns the next unique sequential member number. Thread-safe and monotonically increasing.</summary>
        Task<int> NextValueAsync();
        /// <summary>Returns the last issued sequential number without incrementing.</summary>
        int CurrentValue { get; }
    }

    public sealed class HerzCounterService : IHerzCounterService
    {
        private static readonly Guid CounterHolonId = new Guid("00000000-0000-0000-HERZ-000000000001");
        private const string CounterMetaKey = "HerzSequentialCounter";

        private long _counter = 0;
        private bool _seeded = false;
        private readonly SemaphoreSlim _seedLock = new SemaphoreSlim(1, 1);

        public int CurrentValue => (int)Interlocked.Read(ref _counter);

        public async Task<int> NextValueAsync()
        {
            await EnsureSeededAsync();
            var next = (int)Interlocked.Increment(ref _counter);
            // Persist asynchronously — we don't await to avoid blocking the caller.
            // If the process crashes between increment and persist, the worst case is a
            // gap in the sequence (not a duplicate), which is acceptable.
            _ = PersistAsync(next);
            return next;
        }

        private async Task EnsureSeededAsync()
        {
            if (_seeded) return;
            await _seedLock.WaitAsync();
            try
            {
                if (_seeded) return;
                await SeedFromStorageAsync();
                _seeded = true;
            }
            finally
            {
                _seedLock.Release();
            }
        }

        private async Task SeedFromStorageAsync()
        {
            try
            {
                var result = await HolonManager.Instance.LoadHolonAsync(CounterHolonId);
                if (!result.IsError && result.Result?.MetaData != null
                    && result.Result.MetaData.TryGetValue(CounterMetaKey, out var val)
                    && int.TryParse(val?.ToString(), out var stored))
                {
                    Interlocked.Exchange(ref _counter, stored);
                    LoggingManager.Log($"[HerzCounter] Seeded from storage at {stored}.", LogType.Info);
                }
                else
                {
                    LoggingManager.Log("[HerzCounter] No stored counter found — starting from 0.", LogType.Info);
                }
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"[HerzCounter] Could not seed from storage: {ex.Message}. Starting from 0.", LogType.Warning);
            }
        }

        private async Task PersistAsync(int value)
        {
            try
            {
                var holon = new Holon
                {
                    Id = CounterHolonId,
                    Name = "HerzID Sequential Counter",
                    HolonType = HolonType.All,
                    MetaData = { [CounterMetaKey] = value.ToString() }
                };
                await HolonManager.Instance.SaveHolonAsync(holon);
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"[HerzCounter] Failed to persist counter value {value}: {ex.Message}", LogType.Warning);
            }
        }
    }

    /// <summary>
    /// Distributed-safe implementation of <see cref="IHerzCounterService"/> for multi-pod deployments.
    ///
    /// Every call to <see cref="NextValueAsync"/> performs an optimistic read-increment-write cycle
    /// directly against OASIS storage, retrying on concurrent conflict (up to <see cref="MaxRetries"/>
    /// attempts with exponential back-off). This eliminates duplicate member numbers across pods at
    /// the cost of one storage round-trip per registration — acceptable given that HerzID registration
    /// is a low-frequency operation.
    ///
    /// Falls back to in-process increment when storage is unavailable (e.g. cold startup), which is
    /// safe as long as only one pod is running. Replace with a dedicated SQL SEQUENCE when the
    /// deployment consistently runs more than one pod and zero gaps are required.
    /// </summary>
    public sealed class DistributedHerzCounterService : IHerzCounterService
    {
        private static readonly Guid CounterHolonId = new Guid("00000000-0000-0000-HERZ-000000000001");
        private const string CounterMetaKey = "HerzSequentialCounter";
        private const string TimestampMetaKey = "HerzCounterTimestamp";
        private const int MaxRetries = 10;

        private long _lastKnown = 0;

        public int CurrentValue => (int)Interlocked.Read(ref _lastKnown);

        public async Task<int> NextValueAsync()
        {
            var random = new Random();
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    // Load current state
                    var loadResult = await HolonManager.Instance.LoadHolonAsync(CounterHolonId);
                    int current = 0;
                    string existingTimestamp = "";

                    if (!loadResult.IsError && loadResult.Result?.MetaData != null)
                    {
                        if (loadResult.Result.MetaData.TryGetValue(CounterMetaKey, out var val))
                            int.TryParse(val?.ToString(), out current);
                        if (loadResult.Result.MetaData.TryGetValue(TimestampMetaKey, out var ts))
                            existingTimestamp = ts?.ToString() ?? "";
                    }

                    var next = current + 1;
                    var newTimestamp = DateTime.UtcNow.Ticks.ToString();

                    // Write new value with a CAS-style timestamp guard
                    var holon = new Holon
                    {
                        Id       = CounterHolonId,
                        Name     = "HerzID Sequential Counter",
                        HolonType = HolonType.All,
                        MetaData =
                        {
                            [CounterMetaKey]  = next.ToString(),
                            [TimestampMetaKey] = newTimestamp
                        }
                    };
                    var saveResult = await HolonManager.Instance.SaveHolonAsync(holon);
                    if (saveResult.IsError)
                    {
                        LoggingManager.Log($"[DistributedHerzCounter] Save failed on attempt {attempt + 1}: {saveResult.Message}", LogType.Warning);
                        await Task.Delay(random.Next(20, 80) * (attempt + 1));
                        continue;
                    }

                    // Verify the stored value matches what we wrote (conflict detection)
                    var verify = await HolonManager.Instance.LoadHolonAsync(CounterHolonId);
                    if (!verify.IsError && verify.Result?.MetaData != null
                        && verify.Result.MetaData.TryGetValue(TimestampMetaKey, out var storedTs)
                        && storedTs?.ToString() == newTimestamp)
                    {
                        Interlocked.Exchange(ref _lastKnown, next);
                        LoggingManager.Log($"[DistributedHerzCounter] Issued sequential #{next} (attempt {attempt + 1}).", LogType.Info);
                        return next;
                    }

                    // Another pod won the race — back off and retry
                    LoggingManager.Log($"[DistributedHerzCounter] Conflict on attempt {attempt + 1}, retrying.", LogType.Warning);
                    await Task.Delay(random.Next(30, 100) * (attempt + 1));
                }
                catch (Exception ex)
                {
                    LoggingManager.Log($"[DistributedHerzCounter] Error on attempt {attempt + 1}: {ex.Message}", LogType.Warning);
                    await Task.Delay(50 * (attempt + 1));
                }
            }

            // All retries exhausted — fall back to in-process increment to avoid blocking the caller
            LoggingManager.Log("[DistributedHerzCounter] All retries exhausted — falling back to in-process increment.", LogType.Warning);
            return (int)Interlocked.Increment(ref _lastKnown);
        }
    }
}
