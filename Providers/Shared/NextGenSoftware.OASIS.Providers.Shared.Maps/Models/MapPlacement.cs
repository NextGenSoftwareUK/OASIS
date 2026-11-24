using System;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Models
{
    /// <summary>An OASIS entity placed on the map at a coordinate.</summary>
    public sealed class MapPlacement
    {
        public MapPlacement(MapPlacementKind kind, object entity, Geolocation location)
        {
            Kind = kind;
            Entity = entity ?? throw new ArgumentNullException(nameof(entity));
            Location = location ?? throw new ArgumentNullException(nameof(location));
        }

        public Guid Id { get; } = Guid.NewGuid();
        public MapPlacementKind Kind { get; }
        public object Entity { get; }
        public Geolocation Location { get; }
        public DateTime PlacedUtc { get; } = DateTime.UtcNow;
    }
}
