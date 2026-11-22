using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Purge
{
    public enum HolonPurgeStatus
    {
        /// <summary>Every saved version was removed from every store OASIS can reach.</summary>
        Purged,
        /// <summary>The primary store was purged but at least one other store could not be (see Stores / UnpurgedStores).</summary>
        PurgedPartially,
        /// <summary>No such holon (or the caller may not know about it).</summary>
        NotFound,
        /// <summary>Purged earlier; nothing left to remove.</summary>
        AlreadyPurged,
        /// <summary>The holon is still live. Delete it first (or pass allowLive).</summary>
        NotDeleted,
        /// <summary>A legal hold blocks the purge.</summary>
        OnHold,
        /// <summary>Only the holon's creator, or an avatar with a Delete grant on it, may purge it.</summary>
        Forbidden,
        /// <summary>Version purge is not enabled on this node (master switch).</summary>
        Disabled,
        /// <summary>The primary store could not be purged.</summary>
        Failed,
        /// <summary>Answer to a legal-hold request: the hold is now set.</summary>
        HoldSet,
        /// <summary>Answer to a legal-hold request: the hold is now released.</summary>
        HoldReleased
    }

    public class HolonPurgeResult
    {
        public Guid HolonId { get; set; }
        public HolonPurgeStatus Status { get; set; }
        public string Message { get; set; }
        /// <summary>Saved versions removed from the primary store.</summary>
        public int VersionsRemoved { get; set; }
        public DateTime? PurgedAtUtc { get; set; }
        /// <summary>Each store OASIS tried, with what it did. Never holon content.</summary>
        public List<HolonPurgeStoreOutcome> Stores { get; set; } = new List<HolonPurgeStoreOutcome>();
        /// <summary>Names of the stores that still hold a copy or could not be checked.</summary>
        public List<string> UnpurgedStores { get; set; } = new List<string>();
        /// <summary>What OASIS cannot do about database backups, with the configured retention window when known.</summary>
        public string BackupNote { get; set; }
    }

    /// <summary>One layer's explicit choice (an avatar's or an app's). Null fields inherit from the layer above.</summary>
    public class PurgeChoice
    {
        public bool? Enabled { get; set; }
        /// <summary>Days before purge, 0 to 3650 (0 = at deletion). Null = inherit.</summary>
        public int? Days { get; set; }
        public bool IsEmpty => !Enabled.HasValue && !Days.HasValue;
        public const int MaxDays = 3650;
        /// <summary>Null when valid; otherwise the message to show. Nothing may be saved when this is not null.</summary>
        public static string Validate(int? days) => days.HasValue && (days.Value < 0 || days.Value > MaxDays) ? "Days must be a whole number from 0 to " + MaxDays + " (0 purges at deletion)." : null;
    }

    public class HolonPurgeCaller
    {
        public Guid AvatarId { get; set; }
        /// <summary>Set by the host (header X-OASIS-App-Id); only recorded in the audit entry.</summary>
        public string AppId { get; set; }
    }

    /// <summary>One holon's purge bookkeeping: schedule, legal hold and the fact it was purged. Holds no holon content.</summary>
    public class HolonPurgeEntry
    {
        public Guid HolonId { get; set; }
        public Guid OwnerAvatarId { get; set; }
        public string AppId { get; set; }
        /// <summary>pending, purged or cancelled.</summary>
        public string Status { get; set; } = "none";
        public DateTime? DeletedDateUtc { get; set; }
        public DateTime? PurgeAfterUtc { get; set; }
        public bool Hold { get; set; }
        public string HoldReason { get; set; }
        public Guid HoldByAvatarId { get; set; }
        public DateTime? HoldAtUtc { get; set; }
        public DateTime? PurgedAtUtc { get; set; }
        public int VersionsRemoved { get; set; }
        public string CancelReason { get; set; }
    }

    /// <summary>The one non-reversible record written per purge. Insert-only; never contains holon content.</summary>
    public class HolonPurgeAuditRecord
    {
        public Guid HolonId { get; set; }
        public DateTime PurgedAtUtc { get; set; }
        /// <summary>The avatar that asked, or Guid.Empty for the scheduled worker.</summary>
        public Guid ByAvatarId { get; set; }
        public string Trigger { get; set; }
        public string AppId { get; set; }
        public int VersionsRemoved { get; set; }
        public List<string> StoresPurged { get; set; } = new List<string>();
        public List<string> StoresNotPurged { get; set; } = new List<string>();
    }

    public interface IHolonPurgeStore
    {
        Task<HolonPurgeEntry> GetAsync(Guid holonId, CancellationToken c);
        /// <summary>Creates or refreshes a pending purge. A legal hold already on the holon is kept.</summary>
        Task SchedulePendingAsync(HolonPurgeEntry entry, CancellationToken c);
        Task CancelAsync(Guid holonId, string reason, CancellationToken c);
        Task SetHoldAsync(Guid holonId, Guid ownerAvatarId, bool hold, string reason, Guid byAvatarId, DateTime nowUtc, CancellationToken c);
        /// <summary>ONE query: pending entries whose PurgeAfterUtc has passed, oldest first, at most <paramref name="limit"/>.</summary>
        Task<IReadOnlyList<HolonPurgeEntry>> ListDueAsync(DateTime nowUtc, int limit, CancellationToken c);
        Task MarkPurgedAsync(Guid holonId, Guid ownerAvatarId, DateTime purgedAtUtc, int versionsRemoved, CancellationToken c);
        Task WriteAuditAsync(HolonPurgeAuditRecord record, CancellationToken c);
        /// <summary>Per-avatar choices (true = on, false = off); an avatar with no record inherits. Small collection.</summary>
        Task<IReadOnlyDictionary<Guid, PurgeChoice>> ListAvatarChoicesAsync(CancellationToken c);
        Task<PurgeChoice> GetAvatarChoiceAsync(Guid avatarId, CancellationToken c);
        /// <summary>An empty choice clears the avatar's record (inherit everything).</summary>
        Task SetAvatarChoiceAsync(Guid avatarId, PurgeChoice choice, CancellationToken c);
    }

    /// <summary>What the service needs from the storage side. The production implementation sits over HolonManager and the providers.</summary>
    public interface IHolonPurgeBackend
    {
        /// <summary>Null when the primary provider cannot purge at all.</summary>
        Task<HolonPurgeSubject> GetSubjectAsync(Guid holonId, CancellationToken c);
        /// <summary>The creator, or an avatar with a Delete grant.</summary>
        Task<bool> CanDeleteAsync(HolonPurgeSubject subject, Guid avatarId, CancellationToken c);
        /// <summary>Purges every store; the first outcome is the primary store. Never throws.</summary>
        Task<IReadOnlyList<HolonPurgeStoreOutcome>> PurgeAsync(Guid holonId, CancellationToken c);
    }
}
