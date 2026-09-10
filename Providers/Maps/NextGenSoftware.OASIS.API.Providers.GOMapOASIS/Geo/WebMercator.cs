using System;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geo
{
    /// <summary>
    /// Spherical Web Mercator (EPSG:3857) projection - the same projection GO Map
    /// uses to lay its slippy-map tiles out in Unity world space.
    ///
    /// GO Map places tiles on the XZ plane with Y as altitude, one Unity unit per
    /// metre by default, measured from whatever coordinate the map origin was set
    /// to. Everything here works in projected metres so the provider can produce
    /// world positions identical to GO Map's own without referencing UnityEngine.
    /// </summary>
    public static class WebMercator
    {
        /// <summary>Half the circumference of the earth at the equator, in metres.</summary>
        public const double OriginShift = 20037508.342789244;

        /// <summary>
        /// Web Mercator cannot represent the poles, so latitude is clamped to the
        /// square-aspect limit every slippy-map implementation uses.
        /// </summary>
        public const double MaxLatitude = 85.05112877980659;

        public const double EarthRadiusMetres = 6378137.0;

        public static void LatLonToMetres(double latitude, double longitude, out double x, out double y)
        {
            double lat = Clamp(latitude, -MaxLatitude, MaxLatitude);
            x = longitude * OriginShift / 180.0;
            y = Math.Log(Math.Tan((90.0 + lat) * Math.PI / 360.0)) / (Math.PI / 180.0);
            y = y * OriginShift / 180.0;
        }

        public static void MetresToLatLon(double x, double y, out double latitude, out double longitude)
        {
            longitude = x / OriginShift * 180.0;
            double lat = y / OriginShift * 180.0;
            latitude = 180.0 / Math.PI * (2.0 * Math.Atan(Math.Exp(lat * Math.PI / 180.0)) - Math.PI / 2.0);
        }

        /// <summary>Ground resolution in metres per pixel at a given latitude and zoom.</summary>
        public static double Resolution(double latitude, int zoom, int tileSize = 256)
        {
            double lat = Clamp(latitude, -MaxLatitude, MaxLatitude);
            return Math.Cos(lat * Math.PI / 180.0) * 2.0 * Math.PI * EarthRadiusMetres
                   / (tileSize * Math.Pow(2.0, zoom));
        }

        /// <summary>The slippy-map tile containing a coordinate, as GO Map requests them.</summary>
        public static void LatLonToTile(double latitude, double longitude, int zoom, out int tileX, out int tileY)
        {
            double lat = Clamp(latitude, -MaxLatitude, MaxLatitude);
            double n = Math.Pow(2.0, zoom);
            tileX = (int)Math.Floor((longitude + 180.0) / 360.0 * n);
            double latRad = lat * Math.PI / 180.0;
            tileY = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);

            int max = (int)n - 1;
            tileX = (int)Clamp(tileX, 0, max);
            tileY = (int)Clamp(tileY, 0, max);
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>Round-trips a coordinate through the projection, for verification.</summary>
        public static Geolocation Reproject(Geolocation location)
        {
            LatLonToMetres(location.Latitude, location.Longitude, out double x, out double y);
            MetresToLatLon(x, y, out double lat, out double lon);
            return new Geolocation(lat, lon);
        }
    }
}
