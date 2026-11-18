using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Purge
{
    public class HolonPurgeCycleReport
    {
        public bool MasterEnabled { get; set; }
        public int Scheduled { get; set; }
        public int Due { get; set; }
        public int Purged { get; set; }
        public int PurgedPartially { get; set; }
        public int HeldBack { get; set; }
        public int CancelledRestored { get; set; }
        public int CancelledPolicy { get; set; }
        public int AlreadyGone { get; set; }
        public int Failed { get; set; }
    }

    /// <summary>
    /// Opt-in purge of every saved version of a holon. All decisions live here and use only the three small interfaces,
    /// so the rules are unit tested with fakes and no database.
    /// </summary>
    public sealed class HolonPurgeService
    {
        public const int MaxBatch = 100;
        private const int MaxQueueDrain = 200;
        private readonly HolonPurgePolicy _policy;
        private readonly IHolonPurgeStore _store;
        private readonly IHolonPurgeBackend _backend;
        private readonly Func<DateTime> _clock;
        private readonly ConcurrentQueue<(Guid Id, Guid Owner, string AppId, DateTime Deleted, int Days)> _queue = new ConcurrentQueue<(Guid, Guid, string, DateTime, int)>();
        private readonly SemaphoreSlim _wake = new SemaphoreSlim(0, int.MaxValue);

        public HolonPurgeService(HolonPurgePolicy policy, IHolonPurgeStore store, IHolonPurgeBackend backend, Func<DateTime> clock = null)
        {
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _backend = backend ?? throw new ArgumentNullException(nameof(backend));
            _clock = clock ?? (() => DateTime.UtcNow);
        }

        public HolonPurgePolicy Policy => _policy;
        public int QueuedCount => _queue.Count;

        // ---- scheduling -------------------------------------------------------------------------------------------------

        /// <summary>Called from the delete path: in memory only, never touches the database, never throws.</summary>
        public void QueueDeleted(IHolon deleted, string appId)
        {
            if (deleted == null || deleted.Id == Guid.Empty) return;
            if (_policy.Decide(deleted.CreatedByAvatarId, appId) != HolonPurgeDecision.On) return;
            var days = _policy.EffectiveDays(deleted.CreatedByAvatarId, appId).Days; // fixed now, so a later change never shortens a promise already made
            _queue.Enqueue((deleted.Id, deleted.CreatedByAvatarId, appId, deleted.DeletedDate == DateTime.MinValue ? _clock() : deleted.DeletedDate.ToUniversalTime(), days));
            // Only an immediate purge (PurgeAfterDays 0) needs the worker woken early; later ones wait for the normal cycle.
            if (days <= 0) try { _wake.Release(); } catch (Exception) { }
        }

        public async Task WaitForWorkAsync(TimeSpan timeout, CancellationToken c)
        {
            await _wake.WaitAsync(timeout, c).ConfigureAwait(false);
            while (_wake.Wait(0)) { } // many deletions in a burst cause one cycle, not one each
        }

        /// <summary>Writes queued deletions as pending purges. Bounded per call; unwritten items are put back for the next cycle.</summary>
        public async Task<int> DrainQueueAsync(CancellationToken c)
        {
            var written = 0;
            for (var i = 0; i < MaxQueueDrain && _queue.TryDequeue(out var item); i++)
            {
                try
                {
                    await _store.SchedulePendingAsync(new HolonPurgeEntry
                    {
                        HolonId = item.Id, OwnerAvatarId = item.Owner, AppId = item.AppId, Status = "pending",
                        DeletedDateUtc = item.Deleted, PurgeAfterUtc = item.Deleted.AddDays(item.Days)
                    }, c).ConfigureAwait(false);
                    written++;
                }
                catch (Exception)
                {
                    _queue.Enqueue(item); // retry next cycle
                    break;
                }
            }
            return written;
        }

        // ---- explicit purge -----------------------------------------------------------------------------------------------

        public async Task<IReadOnlyList<HolonPurgeResult>> PurgeBatchAsync(IEnumerable<Guid> ids, HolonPurgeCaller caller, bool allowLive, CancellationToken c)
        {
            var list = (ids ?? Enumerable.Empty<Guid>()).Where(i => i != Guid.Empty).Distinct().Take(MaxBatch).ToList();
            var results = new List<HolonPurgeResult>();
            foreach (var id in list) results.Add(await PurgeAsync(id, caller, allowLive, c).ConfigureAwait(false));
            return results;
        }

        public async Task<HolonPurgeResult> PurgeAsync(Guid holonId, HolonPurgeCaller caller, bool allowLive, CancellationToken c)
        {
            var result = new HolonPurgeResult { HolonId = holonId };
            try
            {
                if (!_policy.MasterEnabled) return Fail(result, HolonPurgeStatus.Disabled, "Version purge is not enabled on this node.");
                if (holonId == Guid.Empty || caller == null || caller.AvatarId == Guid.Empty) return Fail(result, HolonPurgeStatus.Forbidden, "A holon and an authenticated avatar are required.");
                var subject = await _backend.GetSubjectAsync(holonId, c).ConfigureAwait(false);
                if (subject == null) return Fail(result, HolonPurgeStatus.Failed, "The storage provider cannot purge versions.");
                var entry = await _store.GetAsync(holonId, c).ConfigureAwait(false);
                if (!subject.Exists)
                {
                    if (entry?.Status == "purged" && entry.OwnerAvatarId == caller.AvatarId)
                    {
                        result.PurgedAtUtc = entry.PurgedAtUtc; result.VersionsRemoved = entry.VersionsRemoved;
                        return Fail(result, HolonPurgeStatus.AlreadyPurged, "This holon was already purged.");
                    }
                    return Fail(result, HolonPurgeStatus.NotFound, "Holon not found.");
                }
                if (!await _backend.CanDeleteAsync(subject, caller.AvatarId, c).ConfigureAwait(false))
                    return Fail(result, HolonPurgeStatus.Forbidden, "Only the holon's creator, or an avatar with a Delete grant on it, may purge it.");
                if (entry?.Hold == true)
                    return Fail(result, HolonPurgeStatus.OnHold, "A legal hold is set on this holon; release it before purging.");
                if (!subject.IsDeleted && !allowLive)
                    return Fail(result, HolonPurgeStatus.NotDeleted, "This holon has not been deleted. Delete it first, or pass allowLive to purge a live holon.");
                return await PurgeCoreAsync(result, subject, caller.AvatarId, "explicit", caller.AppId, c).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return Fail(result, HolonPurgeStatus.Failed, "The purge could not be completed: " + ex.Message);
            }
        }

        private async Task<HolonPurgeResult> PurgeCoreAsync(HolonPurgeResult result, HolonPurgeSubject subject, Guid byAvatar, string trigger, string appId, CancellationToken c)
        {
            var outcomes = await _backend.PurgeAsync(subject.Id, c).ConfigureAwait(false) ?? new List<HolonPurgeStoreOutcome>();
            result.Stores = outcomes.ToList();
            result.UnpurgedStores = result.Stores.Where(o => o.State == HolonPurgeStoreState.NotPurged).Select(o => o.Store).ToList();
            var primary = result.Stores.FirstOrDefault();
            result.BackupNote = BackupNote();
            if (primary == null || primary.State == HolonPurgeStoreState.NotPurged)
                return Fail(result, HolonPurgeStatus.Failed, "The primary store could not be purged; nothing was recorded as purged.");

            result.VersionsRemoved = primary.VersionsRemoved;
            result.PurgedAtUtc = _clock();
            result.Status = result.UnpurgedStores.Count == 0 ? HolonPurgeStatus.Purged : HolonPurgeStatus.PurgedPartially;
            result.Message = result.Status == HolonPurgeStatus.Purged
                ? "Every saved version was removed from every store OASIS can reach. " + result.BackupNote
                : "The primary store was purged, but these stores could not be: " + string.Join(", ", result.UnpurgedStores) + ". " + result.BackupNote;

            // The purge cannot be undone, so a bookkeeping failure only adds a warning; it never reports the purge as failed.
            try
            {
                await _store.MarkPurgedAsync(subject.Id, subject.CreatedByAvatarId, result.PurgedAtUtc.Value, result.VersionsRemoved, c).ConfigureAwait(false);
                await _store.WriteAuditAsync(new HolonPurgeAuditRecord
                {
                    HolonId = subject.Id, PurgedAtUtc = result.PurgedAtUtc.Value, ByAvatarId = byAvatar, Trigger = trigger, AppId = appId,
                    VersionsRemoved = result.VersionsRemoved,
                    StoresPurged = result.Stores.Where(o => o.State != HolonPurgeStoreState.NotPurged).Select(o => o.Store).ToList(),
                    StoresNotPurged = result.UnpurgedStores
                }, c).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                result.Message += " Warning: the purge record could not be written (" + ex.Message + ").";
            }
            return result;
        }

        private string BackupNote()
        {
            var days = _policy.Settings.BackupRetentionDays;
            return days > 0
                ? "Database backups are not purged by OASIS; they expire on the host's schedule, within " + days + " days."
                : "Database backups are outside OASIS control and are not purged; ask the database host for its backup retention window.";
        }

        private static HolonPurgeResult Fail(HolonPurgeResult result, HolonPurgeStatus status, string message)
        {
            result.Status = status; result.Message = message; return result;
        }

        // ---- legal hold ---------------------------------------------------------------------------------------------------

        public async Task<HolonPurgeResult> SetHoldAsync(Guid holonId, HolonPurgeCaller caller, bool hold, string reason, CancellationToken c)
        {
            var result = new HolonPurgeResult { HolonId = holonId };
            try
            {
                // A hold only ever protects data, so it is accepted even while the master switch is off.
                if (holonId == Guid.Empty || caller == null || caller.AvatarId == Guid.Empty) return Fail(result, HolonPurgeStatus.Forbidden, "A holon and an authenticated avatar are required.");
                var subject = await _backend.GetSubjectAsync(holonId, c).ConfigureAwait(false);
                if (subject == null) return Fail(result, HolonPurgeStatus.Failed, "The storage provider cannot purge versions.");
                if (!subject.Exists) return Fail(result, HolonPurgeStatus.NotFound, "Holon not found.");
                if (!await _backend.CanDeleteAsync(subject, caller.AvatarId, c).ConfigureAwait(false))
                    return Fail(result, HolonPurgeStatus.Forbidden, "Only the holon's creator, or an avatar with a Delete grant on it, may change its legal hold.");
                await _store.SetHoldAsync(holonId, subject.CreatedByAvatarId, hold, reason, caller.AvatarId, _clock(), c).ConfigureAwait(false);
                return Fail(result, hold ? HolonPurgeStatus.HoldSet : HolonPurgeStatus.HoldReleased, hold ? "Legal hold set. Nothing will be purged until it is released." : "Legal hold released.");
            }
            catch (Exception ex)
            {
                return Fail(result, HolonPurgeStatus.Failed, "The hold could not be changed: " + ex.Message);
            }
        }

        // ---- the worker cycle ---------------------------------------------------------------------------------------------

        /// <summary>One bounded pass: one query for due entries (at most BatchSize), then each is re-checked against the stored holon and the policy.</summary>
        public async Task<HolonPurgeCycleReport> RunCycleAsync(CancellationToken c)
        {
            var report = new HolonPurgeCycleReport { MasterEnabled = _policy.MasterEnabled };
            if (!report.MasterEnabled) return report;
            report.Scheduled = await DrainQueueAsync(c).ConfigureAwait(false);
            var batch = Math.Max(1, _policy.Settings.BatchSize);
            var due = await _store.ListDueAsync(_clock(), batch, c).ConfigureAwait(false);
            report.Due = due.Count;
            foreach (var entry in due.Take(batch))
            {
                c.ThrowIfCancellationRequested();
                try
                {
                    if (entry.Hold) { report.HeldBack++; continue; }
                    var subject = await _backend.GetSubjectAsync(entry.HolonId, c).ConfigureAwait(false);
                    if (subject == null) { report.Failed++; continue; }
                    if (!subject.Exists)
                    {
                        await _store.CancelAsync(entry.HolonId, "record no longer exists", c).ConfigureAwait(false);
                        report.AlreadyGone++; continue;
                    }
                    if (!subject.IsDeleted)
                    {
                        // Restored (or never deleted): the purge is cancelled and a live holon is never touched.
                        await _store.CancelAsync(entry.HolonId, "holon was restored", c).ConfigureAwait(false);
                        report.CancelledRestored++; continue;
                    }
                    if (_policy.Decide(entry.OwnerAvatarId, entry.AppId) != HolonPurgeDecision.On)
                    {
                        await _store.CancelAsync(entry.HolonId, "policy no longer applies", c).ConfigureAwait(false);
                        report.CancelledPolicy++; continue;
                    }
                    var result = await PurgeCoreAsync(new HolonPurgeResult { HolonId = entry.HolonId }, subject, Guid.Empty, "scheduled", entry.AppId, c).ConfigureAwait(false);
                    if (result.Status == HolonPurgeStatus.Purged) report.Purged++;
                    else if (result.Status == HolonPurgeStatus.PurgedPartially) report.PurgedPartially++;
                    else report.Failed++;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception) { report.Failed++; }
            }
            return report;
        }
    }
}
