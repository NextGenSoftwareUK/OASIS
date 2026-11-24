using System;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Geo
{
    /// <summary>EPSG:3857 spherical Mercator, the projection used by Google, HERE and MapLibre tiles.</summary>
    public static class WebMercator
    {
        public const double OriginShift = 20037508.342789244;
        public const double MaxLatitude = 85.05112877980659;
        public const double EarthRadiusMetres = 6378137.0;

        public static void LatLonToMetres(double latitude, double longitude, out double x, out double y)
        {
            double lat = Math.Clamp(latitude, -MaxLatitude, MaxLatitude);
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

        public static void LatLonToTile(double latitude, double longitude, int zoom, out int tileX, out int tileY)
        {
            if (zoom < 0 || zoom > 30) throw new ArgumentOutOfRangeException(nameof(zoom));

            double lat = Math.Clamp(latitude, -MaxLatitude, MaxLatitude);
            double n = Math.Pow(2.0, zoom);
            double latRad = lat * Math.PI / 180.0;
            int max = (int)n - 1;

            tileX = Math.Clamp((int)Math.Floor((longitude + 180.0) / 360.0 * n), 0, max);
            tileY = Math.Clamp((int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n), 0, max);
        }
    }
}
