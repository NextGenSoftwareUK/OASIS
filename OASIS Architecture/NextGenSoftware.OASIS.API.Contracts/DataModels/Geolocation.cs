using System;
using System.Globalization;

namespace NextGenSoftware.OASIS.API.Contracts.Interfaces
{
    /// <summary>
    /// Geolocation data model for coordinates
    /// Enhanced with Unity-specific functionality for cross-platform compatibility
    /// </summary>
    public class Geolocation
    {
        // Semi-axes of WGS-84 geoidal reference
        private const double WGS84_a = 6378137.0; // Major semiaxis [m]
        private const double WGS84_b = 6356752.3; // Minor semiaxis [m]
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        // Static field for temporary player position (Unity-specific)
        public static Geolocation TempPlayerPosition = new Geolocation(74.277282, 31.544004);
        
        public Geolocation() { }
        
        public Geolocation(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
        
        // Constructor with longitude first (for Unity compatibility)
        public Geolocation(double longitude, double latitude, bool isLongitudeFirst = false)
        {
            if (isLongitudeFirst)
            {
                Longitude = longitude;
                Latitude = latitude;
            }
            else
            {
                Latitude = latitude;
                Longitude = longitude;
            }
        }
        
        public override string ToString()
        {
            return $"{Longitude.ToString(CultureInfo.InvariantCulture)},{Latitude.ToString(CultureInfo.InvariantCulture)}";
        }
        
        // Extension methods for Unity compatibility
        public BoundingBox GetBoundingBox(double halfSideInKm)
        {
            // Bounding box surrounding the point at given coordinates,
            // assuming local approximation of Earth surface as a sphere
            // of radius given by WGS84
            var lat = Deg2rad(Latitude);
            var lon = Deg2rad(Longitude);
            var halfSide = 1000 * halfSideInKm;

            // Radius of Earth at given latitude
            var radius = WGS84EarthRadius(lat);
            // Radius of the parallel at given latitude
            var pradius = radius * Math.Cos(lat);

            var latMin = lat - halfSide / radius;
            var latMax = lat + halfSide / radius;
            var lonMin = lon - halfSide / pradius;
            var lonMax = lon + halfSide / pradius;

            return new BoundingBox
            {
                MinPoint = new Geolocation { Latitude = Rad2deg(latMin), Longitude = Rad2deg(lonMin) },
                MaxPoint = new Geolocation { Latitude = Rad2deg(latMax), Longitude = Rad2deg(lonMax) }
            };
        }
        
        public double[] ToLatLonArray() => new[] { Latitude, Longitude };
        public double[] ToLonLatArray() => new[] { Longitude, Latitude };
        
        public float DistanceTo(Geolocation other)
        {
            var rhRad = ToRadianGeolocation();
            var lhRad = other.ToRadianGeolocation();

            double distanceLongitude = lhRad.Longitude - rhRad.Longitude;
            double distanceLatitude = lhRad.Latitude - rhRad.Latitude;

            double haversine = Math.Pow(Math.Sin(distanceLatitude / 2), 2) +
                Math.Cos(rhRad.Latitude) *
                Math.Cos(lhRad.Latitude) *
                Math.Pow(Math.Sin(distanceLongitude / 2), 2);

            double rawDistance = 2 * Math.Asin(Math.Sqrt(haversine));

            return (float)(rawDistance * 6371f);
        }
        
        private Geolocation ToRadianGeolocation()
        {
            return new Geolocation(Deg2rad(Longitude), Deg2rad(Latitude), true);
        }
        
        // degrees to radians
        private static double Deg2rad(double degrees)
        {
            return Math.PI * degrees / 180.0;
        }

        // radians to degrees
        private static double Rad2deg(double radians)
        {
            return 180.0 * radians / Math.PI;
        }

        // Earth radius at a given latitude, according to the WGS-84 ellipsoid [m]
        private static double WGS84EarthRadius(double lat)
        {
            // http://en.wikipedia.org/wiki/Earth_radius
            var An = WGS84_a * WGS84_a * Math.Cos(lat);
            var Bn = WGS84_b * WGS84_b * Math.Sin(lat);
            var Ad = WGS84_a * Math.Cos(lat);
            var Bd = WGS84_b * Math.Sin(lat);
            return Math.Sqrt((An * An + Bn * Bn) / (Ad * Ad + Bd * Bd));
        }
    }
}

