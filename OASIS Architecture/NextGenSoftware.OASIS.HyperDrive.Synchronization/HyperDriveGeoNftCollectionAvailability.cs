using System;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization
{
    /// <summary>
    /// Private, avatar-scoped view of authoritative GeoNFT collection availability. The hosted
    /// authority owns quotas, ownership and counters; an Edge client may only let an already
    /// evaluated cooldown expire as local UTC advances.
    /// </summary>
    public sealed class HyperDriveGeoNftCollectionAvailabilityProjection
    {
        public Guid AvatarId { get; set; }
        public IReadOnlyList<HyperDriveGeoNftCollectionAvailabilityItem> Items { get; set; } =
            Array.Empty<HyperDriveGeoNftCollectionAvailabilityItem>();

        public HyperDriveGeoNftCollectionAvailabilityItem GetAt(Guid geoNftId, DateTime nowUtc)
        {
            if (geoNftId == Guid.Empty) throw new ArgumentException("A GeoNFT id is required.", nameof(geoNftId));
            if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Availability must be evaluated with UTC.", nameof(nowUtc));
            var item = Items?.SingleOrDefault(candidate => candidate.GeoNftId == geoNftId);
            return item?.At(nowUtc);
        }
    }

    public sealed class HyperDriveGeoNftCollectionAvailabilityItem
    {
        public Guid GeoNftId { get; set; }
        public bool CanCollect { get; set; }
        public string Reason { get; set; }
        public DateTime? NextCollectAtUtc { get; set; }
        public long PlayerCollectionCount { get; set; }
        public long GlobalCollectionCount { get; set; }

        public HyperDriveGeoNftCollectionAvailabilityItem At(DateTime nowUtc)
        {
            if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Availability must be evaluated with UTC.", nameof(nowUtc));
            // GeoSpatialSpawnPolicy only sets NextCollectAtUtc when cooldown is the sole blocker.
            // Quota/ownership failures have no timestamp and must never be guessed away by Edge.
            bool cooldownExpired = !CanCollect && NextCollectAtUtc.HasValue && nowUtc >= NextCollectAtUtc.Value.ToUniversalTime();
            return new HyperDriveGeoNftCollectionAvailabilityItem
            {
                GeoNftId = GeoNftId,
                CanCollect = CanCollect || cooldownExpired,
                Reason = cooldownExpired ? null : Reason,
                NextCollectAtUtc = cooldownExpired ? null : NextCollectAtUtc,
                PlayerCollectionCount = PlayerCollectionCount,
                GlobalCollectionCount = GlobalCollectionCount
            };
        }
    }
}
