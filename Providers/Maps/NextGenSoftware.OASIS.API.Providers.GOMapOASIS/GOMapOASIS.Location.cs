using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geo;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS
{
    /// <summary>
    /// Location and distance. Distances are true great-circle metres, matching what
    /// GO Map's GOCoordinate.DistanceFromOtherGPSCoordinate returns, so Our World
    /// proximity checks agree whichever side of the wire they run on.
    /// </summary>
    public partial class GOMapOASIS
    {
        /// <summary>
        /// The device's current position: read live from the GO Map location manager
        /// when bound to one, otherwise the last position the client reported through
        /// <see cref="SetCurrentLocation"/>.
        /// </summary>
        public Geolocation GetCurrentLocation()
        {
            if (!IsInitialized) return null;

            if (Bridge.IsBound)
            {
                double latitude = Bridge.GetValueOrDefault("locationManager.currentLocation.latitude", double.NaN);
                double longitude = Bridge.GetValueOrDefault("locationManager.currentLocation.longitude", double.NaN);

                if (!double.IsNaN(latitude) && !double.IsNaN(longitude))
                {
                    Geolocation live = new Geolocation(latitude, longitude);
                    SetCurrentLocation(live);
                    return live;
                }
            }

            return CurrentLocation;
        }

        /// <summary>
        /// Completes once the map origin is known. GO Map cannot convert between
        /// coordinates and world positions before its first GPS fix, so callers that
        /// need conversions await this first.
        /// </summary>
        public Task WaitForOriginSet()
        {
            if (!IsInitialized) return Task.CompletedTask;

            if (Bridge.IsBound && Origin == null)
                ReadOriginFromLiveMap();

            return _originSet.Task;
        }

        public float CalculateDistance(Geolocation location1, Geolocation location2)
        {
            if (!GeoMath.IsValid(location1) || !GeoMath.IsValid(location2)) return 0f;

            return (float)GeoMath.HaversineMetres(location1, location2);
        }

        public float CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            return CalculateDistance(new Geolocation(lat1, lon1), new Geolocation(lat2, lon2));
        }

        /// <summary>Initial bearing between two coordinates, in degrees clockwise from north.</summary>
        public double CalculateBearing(Geolocation from, Geolocation to)
        {
            if (!GeoMath.IsValid(from) || !GeoMath.IsValid(to)) return 0.0;

            return GeoMath.BearingDegrees(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
        }

        /// <summary>
        /// True when two coordinates are within a radius of each other - the check
        /// behind quest and geo hot spot triggers.
        /// </summary>
        public bool IsWithinRange(Geolocation from, Geolocation to, double radiusMetres)
        {
            if (!GeoMath.IsValid(from) || !GeoMath.IsValid(to) || radiusMetres <= 0.0) return false;

            return GeoMath.HaversineMetres(from, to) <= radiusMetres;
        }
    }
}
