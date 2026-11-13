using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AccessLog
{
    /// <summary>One recorded read. Holds ids only, never holon content.</summary>
    public class AccessLogEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; }
        public Guid AvatarId { get; set; }
        public string AppId { get; set; }
        public string Operation { get; set; }
        /// <summary>Type of the holons read (e.g. Holon or a custom type); "Mixed" when a bulk read returned several.</summary>
        public string HolonType { get; set; }
        /// <summary>Holon ids returned, capped at MaxHolonIdsPerEvent.</summary>
        public List<Guid> HolonIds { get; set; } = new List<Guid>();
        /// <summary>How many holons the read returned in total (may exceed HolonIds.Count).</summary>
        public int TotalCount { get; set; }
        public string IpAddress { get; set; }
    }

    public interface IAccessLogStore
    {
        Task InsertAsync(IReadOnlyList<AccessLogEvent> batch, CancellationToken cancellation);
        Task<IReadOnlyList<AccessLogEvent>> QueryByHolonAsync(Guid holonId, int limit, int offset, CancellationToken cancellation);
        Task<IReadOnlyList<AccessLogEvent>> QueryByAvatarAsync(Guid avatarId, int limit, int offset, CancellationToken cancellation);
    }

    /// <summary>
    /// Records reads without ever slowing or failing them. With AccessLog.Enabled false <see cref="Record"/> returns at the
    /// first line: nothing is queued and the store is never touched. When enabled, events go into a bounded in-memory queue
    /// (full = dropped and counted) and a background loop writes them in batches.
    /// </summary>
    public class AccessLogService
    {
        private readonly Func<AccessLogSettings> _settings;
        private readonly IAccessLogStore _store;
        private readonly Func<DateTime> _clock;
        private Channel<AccessLogEvent> _queue;
        private readonly object _gate = new object();
        private long _dropped;
        private long _failedBatches;

        public AccessLogService(Func<AccessLogSettings> settings, IAccessLogStore store, Func<DateTime> clock = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        public long DroppedEvents => Interlocked.Read(ref _dropped);
        public long FailedBatches => Interlocked.Read(ref _failedBatches);
        /// <summary>Per-app choice (true = record, false = do not) that overrides AccessLog.Enabled for that app; null follows the DNA. Read from memory only.</summary>
        public AccessLogOverrides Overrides { get; } = new AccessLogOverrides();
        public bool IsEnabled => _settings()?.Enabled == true || Overrides.AnyEnabled;

        /// <summary>Queues one read. Never throws, never blocks.</summary>
        public void Record(Guid avatarId, string appId, string operation, IReadOnlyCollection<(Guid Id, string Type)> holons, string ip = null)
        {
            try
            {
                var s = _settings();
                var appChoice = appId == null ? null : Overrides.For(appId);
                if (!(appChoice ?? (s != null && s.Enabled))) return; // zero overhead and zero writes when off (unless this app opted in)
                s ??= new AccessLogSettings();
                if (holons == null || holons.Count == 0 || avatarId == Guid.Empty) return;
                if (s.Apps != null && s.Apps.Count > 0 && !s.Apps.Contains(appId ?? "", StringComparer.OrdinalIgnoreCase)) return;
                if (s.Operations != null && s.Operations.Count > 0 && !s.Operations.Contains(operation ?? "", StringComparer.OrdinalIgnoreCase)) return;
                var selected = holons;
                if (s.HolonTypes != null && s.HolonTypes.Count > 0)
                {
                    selected = holons.Where(h => s.HolonTypes.Contains(h.Type ?? "", StringComparer.OrdinalIgnoreCase)).ToList();
                    if (selected.Count == 0) return;
                }
                var cap = s.MaxHolonIdsPerEvent <= 0 ? 200 : s.MaxHolonIdsPerEvent;
                var types = selected.Select(h => h.Type ?? "").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var evt = new AccessLogEvent
                {
                    Timestamp = _clock(),
                    AvatarId = avatarId,
                    AppId = appId,
                    Operation = operation,
                    HolonType = types.Count == 1 ? types[0] : "Mixed",
                    HolonIds = selected.Take(cap).Select(h => h.Id).ToList(),
                    TotalCount = selected.Count,
                    IpAddress = s.CaptureIp ? ip : null
                };
                if (!Queue(s).Writer.TryWrite(evt)) Interlocked.Increment(ref _dropped);
            }
            catch (Exception) { Interlocked.Increment(ref _dropped); }
        }

        private Channel<AccessLogEvent> Queue(AccessLogSettings s)
        {
            lock (_gate)
            {
                return _queue ??= Channel.CreateBounded<AccessLogEvent>(new BoundedChannelOptions(s.QueueCapacity <= 0 ? 10000 : s.QueueCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait, // TryWrite then returns false when full (DropWrite would drop silently and report success)
                    SingleReader = true,
                    SingleWriter = false
                });
            }
        }

        /// <summary>Writes whatever is queued, in batches. A failing store loses that batch (counted) but never throws.</summary>
        public async Task<int> FlushAsync(CancellationToken cancellation = default)
        {
            var s = _settings();
            Channel<AccessLogEvent> queue;
            lock (_gate) queue = _queue;
            if (queue == null) return 0;
            var size = s == null || s.BatchSize <= 0 ? 500 : s.BatchSize;
            var written = 0;
            while (!cancellation.IsCancellationRequested)
            {
                var batch = new List<AccessLogEvent>(size);
                while (batch.Count < size && queue.Reader.TryRead(out var e)) batch.Add(e);
                if (batch.Count == 0) break;
                try { await _store.InsertAsync(batch, cancellation).ConfigureAwait(false); written += batch.Count; }
                catch (Exception) { Interlocked.Increment(ref _failedBatches); }
            }
            return written;
        }

        /// <summary>Only the holon's owner or a Wizard (app admin) may see who read a holon.</summary>
        public static bool CanViewHolonLog(bool isWizard, Guid callerId, Guid holonCreatorId)
            => callerId != Guid.Empty && (isWizard || holonCreatorId == callerId);

        /// <summary>An avatar may see its own reads; a Wizard (app admin) may see any avatar's.</summary>
        public static bool CanViewAvatarLog(bool isWizard, Guid callerId, Guid targetAvatarId)
            => callerId != Guid.Empty && (isWizard || targetAvatarId == callerId);

        public Task<IReadOnlyList<AccessLogEvent>> ForHolonAsync(Guid holonId, int limit, int offset, CancellationToken c = default)
            => _store.QueryByHolonAsync(holonId, Clamp(limit), Math.Max(0, offset), c);

        public Task<IReadOnlyList<AccessLogEvent>> ForAvatarAsync(Guid avatarId, int limit, int offset, CancellationToken c = default)
            => _store.QueryByAvatarAsync(avatarId, Clamp(limit), Math.Max(0, offset), c);

        private static int Clamp(int limit) => limit <= 0 ? 50 : Math.Min(limit, 200);
    }

    /// <summary>In-memory per-app on/off choices, refreshed periodically from the app accounts so a read never touches the database.</summary>
    public sealed class AccessLogOverrides
    {
        private volatile System.Collections.Generic.Dictionary<string, bool> _map = new System.Collections.Generic.Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private volatile bool _any;
        public bool AnyEnabled => _any;
        public bool? For(string appId) => _map.TryGetValue(appId, out var v) ? v : (bool?)null;
        public void Replace(System.Collections.Generic.IEnumerable<(string AppId, bool? Choice)> choices)
        {
            var map = new System.Collections.Generic.Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in choices) if (c.Choice.HasValue && !string.IsNullOrEmpty(c.AppId)) map[c.AppId] = c.Choice.Value;
            _map = map; _any = map.Values.Any(v => v);
        }
    }
}
