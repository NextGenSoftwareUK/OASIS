using System;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Models
{
    /// <summary>A marker dropped at a coordinate, optionally tied to a client object.</summary>
    public sealed class MapPin
    {
        public MapPin(Geolocation location, object target)
        {
            Location = location ?? throw new ArgumentNullException(nameof(location));
            Target = target;
        }

        public Guid Id { get; } = Guid.NewGuid();
        public Geolocation Location { get; }
        public object Target { get; }
        public DateTime DroppedUtc { get; } = DateTime.UtcNow;
    }
}
