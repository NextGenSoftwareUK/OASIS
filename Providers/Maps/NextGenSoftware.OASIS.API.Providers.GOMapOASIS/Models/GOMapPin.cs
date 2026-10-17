using System;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Models
{
    /// <summary>A marker dropped on the map, tracked so it can be removed again.</summary>
    public class GOMapPin
    {
        public Guid Id { get; set; }
        public Geolocation Location { get; set; }

        /// <summary>
        /// The client-side object the pin represents - a Unity GameObject when the
        /// provider is bound to a live GO Map, or any caller-supplied token when it
        /// is running headless.
        /// </summary>
        public object Target { get; set; }

        public DateTime DroppedAt { get; set; }

        public GOMapPin()
        {
            Id = Guid.NewGuid();
            DroppedAt = DateTime.UtcNow;
        }

        public GOMapPin(Geolocation location, object target) : this()
        {
            Location = location;
            Target = target;
        }
    }
}
