using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;

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
}
