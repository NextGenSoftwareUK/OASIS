using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Models
{
    /// <summary>A routed path returned by a directions service.</summary>
    public sealed class MapRoute
    {
        public MapRoute(IReadOnlyList<Geolocation> geometry, double distanceMetres, double durationSeconds,
            RoutingType routingType, string source)
        {
            if (geometry == null || geometry.Count < 2)
                throw new ArgumentException("A route needs at least two points.", nameof(geometry));
            if (!double.IsFinite(distanceMetres) || distanceMetres < 0) throw new ArgumentOutOfRangeException(nameof(distanceMetres));
            if (!double.IsFinite(durationSeconds) || durationSeconds < 0) throw new ArgumentOutOfRangeException(nameof(durationSeconds));

            Geometry = geometry;
            DistanceMetres = distanceMetres;
            DurationSeconds = durationSeconds;
            RoutingType = routingType;
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public Guid Id { get; } = Guid.NewGuid();
        public IReadOnlyList<Geolocation> Geometry { get; }
        public double DistanceMetres { get; }
        public double DurationSeconds { get; }
        public RoutingType RoutingType { get; }

        /// <summary>The service that computed the route, e.g. "google-routes".</summary>
        public string Source { get; }
    }
}
