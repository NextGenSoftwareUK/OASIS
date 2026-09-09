using System;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Models
{
    /// <summary>
    /// An OASIS entity anchored to a real-world coordinate on the map. This is the
    /// authoritative record: the Unity client renders from it, so a client that
    /// reconnects can rebuild the whole map from the provider's placements.
    /// </summary>
    public class GOMapPlacement
    {
        public Guid Id { get; set; }
        public GOMapPlacementKind Kind { get; set; }

        /// <summary>The entity itself - a GeoNFT, quest, OAPP, holon or hot spot.</summary>
        public object Entity { get; set; }

        public Geolocation Location { get; set; }
        public DateTime PlacedAt { get; set; }

        public GOMapPlacement()
        {
            Id = Guid.NewGuid();
            PlacedAt = DateTime.UtcNow;
        }

        public GOMapPlacement(GOMapPlacementKind kind, object entity, Geolocation location) : this()
        {
            Kind = kind;
            Entity = entity;
            Location = location;
        }
    }
}
