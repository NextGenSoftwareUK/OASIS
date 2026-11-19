using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Contracts.Interfaces;

/// <summary>Computes geographic routes independently of the map renderer.</summary>
public interface IOASISRoutingProvider
{
    Task<RoutingRoute> ComputeRouteAsync(RoutingCoordinate origin, RoutingCoordinate destination,
        RoutingType travelMode, CancellationToken cancellationToken = default);
}

/// <summary>A WGS84 coordinate expressed in decimal degrees.</summary>
public readonly record struct RoutingCoordinate
{
    public double Latitude { get; }
    public double Longitude { get; }

    public RoutingCoordinate(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        if (!double.IsFinite(longitude) || longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude));

        Latitude = latitude;
        Longitude = longitude;
    }
}

/// <summary>Distance in metres and ordered WGS84 geometry from origin to destination.</summary>
public sealed class RoutingRoute
{
    public double DistanceMeters { get; }
    public IReadOnlyList<RoutingCoordinate> Points { get; }

    public RoutingRoute(double distanceMeters, IEnumerable<RoutingCoordinate> points)
    {
        if (!double.IsFinite(distanceMeters) || distanceMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(distanceMeters));
        ArgumentNullException.ThrowIfNull(points);

        var geometry = points.ToArray();
        if (geometry.Length < 2)
            throw new ArgumentException("Route geometry requires at least two points.", nameof(points));

        DistanceMeters = distanceMeters;
        Points = Array.AsReadOnly(geometry);
    }
}
