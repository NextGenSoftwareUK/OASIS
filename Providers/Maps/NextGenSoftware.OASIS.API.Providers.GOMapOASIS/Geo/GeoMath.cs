using System;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geo
{
    /// <summary>
    /// Great-circle geodesy. GO Map's GOCoordinate.DistanceFromOtherGPSCoordinate
    /// is a haversine over a spherical earth, so this reproduces it exactly rather
    /// than approximating with the projected-metre distance (which stretches badly
    /// away from the equator).
    /// </summary>
    public static class GeoMath
    {
        public const double EarthRadiusMetres = 6371000.0;

        public static double HaversineMetres(double lat1, double lon1, double lat2, double lon2)
        {
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0)
                     + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                     * Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);

            return 2.0 * EarthRadiusMetres * Math.Asin(Math.Min(1.0, Math.Sqrt(a)));
        }

        public static double HaversineMetres(Geolocation from, Geolocation to)
        {
            if (from == null || to == null) return 0.0;
            return HaversineMetres(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
        }

        /// <summary>Initial bearing from one coordinate to another, in degrees clockwise from north.</summary>
        public static double BearingDegrees(double lat1, double lon1, double lat2, double lon2)
        {
            double dLon = ToRadians(lon2 - lon1);
            double y = Math.Sin(dLon) * Math.Cos(ToRadians(lat2));
            double x = Math.Cos(ToRadians(lat1)) * Math.Sin(ToRadians(lat2))
                     - Math.Sin(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Cos(dLon);

            return (ToDegrees(Math.Atan2(y, x)) + 360.0) % 360.0;
        }

        /// <summary>The coordinate reached by travelling a distance along a bearing.</summary>
        public static Geolocation Offset(Geolocation origin, double distanceMetres, double bearingDegrees)
        {
            if (origin == null) return null;

            double angular = distanceMetres / EarthRadiusMetres;
            double bearing = ToRadians(bearingDegrees);
            double lat1 = ToRadians(origin.Latitude);
            double lon1 = ToRadians(origin.Longitude);

            double lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(angular)
                                  + Math.Cos(lat1) * Math.Sin(angular) * Math.Cos(bearing));

            double lon2 = lon1 + Math.Atan2(Math.Sin(bearing) * Math.Sin(angular) * Math.Cos(lat1),
                                            Math.Cos(angular) - Math.Sin(lat1) * Math.Sin(lat2));

            return new Geolocation(ToDegrees(lat2), ((ToDegrees(lon2) + 540.0) % 360.0) - 180.0);
        }

        public static bool IsValid(Geolocation location)
        {
            return location != null
                && location.Latitude >= -90.0 && location.Latitude <= 90.0
                && location.Longitude >= -180.0 && location.Longitude <= 180.0;
        }

        public static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
        public static double ToDegrees(double radians) => radians * 180.0 / Math.PI;
    }
}
