using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Geo;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps
{
    public abstract partial class MapProviderBase
    {
        public Geolocation GetCurrentLocation() => CurrentLocation;

        /// <summary>Completes once <see cref="SetOrigin"/> or the first <see cref="SetCurrentLocation"/> has run.</summary>
        public Task WaitForOriginSet() => _originSet.Task;

        /// <summary>Great-circle distance in metres.</summary>
        public float CalculateDistance(Geolocation location1, Geolocation location2)
        {
            if (!GeoMath.IsValid(location1)) throw new ArgumentException("A valid WGS84 coordinate is required.", nameof(location1));
            if (!GeoMath.IsValid(location2)) throw new ArgumentException("A valid WGS84 coordinate is required.", nameof(location2));
            return (float)GeoMath.HaversineMetres(location1, location2);
        }

        public float CalculateDistance(double lat1, double lon1, double lat2, double lon2) =>
            CalculateDistance(new Geolocation(lat1, lon1), new Geolocation(lat2, lon2));

        /// <summary>
        /// Converts a coordinate to metres east (X), up (Y) and north (Z) of the origin,
        /// in true ground metres. Returns null, with <see cref="LastError"/> set, when no
        /// origin has been set.
        /// </summary>
        public object ConvertLatLongToWorldPosition(double latitude, double longitude) =>
            ToWorldPosition(new Geolocation(latitude, longitude), 0.0);

        public MapWorldPosition ToWorldPosition(Geolocation location, double altitudeMetres)
        {
            Geolocation origin = Origin;
            if (origin == null) { Fail("No origin has been set; call SetOrigin or SetCurrentLocation first."); return null; }
            if (!GeoMath.IsValid(location)) { Fail("A valid WGS84 coordinate is required."); return null; }

            WebMercator.LatLonToMetres(origin.Latitude, origin.Longitude, out double originX, out double originY);
            WebMercator.LatLonToMetres(location.Latitude, location.Longitude, out double x, out double y);
            double scale = Math.Cos(GeoMath.ToRadians(origin.Latitude));

            LastError = null;
            return new MapWorldPosition((x - originX) * scale, altitudeMetres, (y - originY) * scale);
        }

        /// <summary>
        /// Converts a world position back to a coordinate. Accepts a
        /// <see cref="MapWorldPosition"/>, a double[3] or float[3] (x, y, z), or any object
        /// with x and z members such as a Unity Vector3.
        /// </summary>
        public object ConvertWorldPositionToLatLong(object worldPosition) => FromWorldOffset(worldPosition);

        private Geolocation FromWorldOffset(object worldPosition)
        {
            Geolocation origin = Origin;
            if (origin == null) { Fail("No origin has been set; call SetOrigin or SetCurrentLocation first."); return null; }
            if (!TryReadXZ(worldPosition, out double eastMetres, out double northMetres))
            {
                Fail("The world position must be a MapWorldPosition, a 3-element array or an object with x and z.");
                return null;
            }

            double scale = Math.Cos(GeoMath.ToRadians(origin.Latitude));
            WebMercator.LatLonToMetres(origin.Latitude, origin.Longitude, out double originX, out double originY);
            WebMercator.MetresToLatLon(originX + eastMetres / scale, originY + northMetres / scale, out double latitude, out double longitude);

            LastError = null;
            return new Geolocation(latitude, longitude);
        }

        private static bool TryReadXZ(object position, out double x, out double z)
        {
            x = z = 0.0;
            switch (position)
            {
                case MapWorldPosition p: x = p.X; z = p.Z; return true;
                case double[] d when d.Length == 3: x = d[0]; z = d[2]; return double.IsFinite(x) && double.IsFinite(z);
                case float[] f when f.Length == 3: x = f[0]; z = f[2]; return float.IsFinite(f[0]) && float.IsFinite(f[2]);
                case null: return false;
            }

            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase;
            Type type = position.GetType();
            object rawX = type.GetField("x", flags)?.GetValue(position) ?? type.GetProperty("x", flags)?.GetValue(position);
            object rawZ = type.GetField("z", flags)?.GetValue(position) ?? type.GetProperty("z", flags)?.GetValue(position);
            if (rawX == null || rawZ == null) return false;

            x = Convert.ToDouble(rawX, System.Globalization.CultureInfo.InvariantCulture);
            z = Convert.ToDouble(rawZ, System.Globalization.CultureInfo.InvariantCulture);
            return double.IsFinite(x) && double.IsFinite(z);
        }
    }
}
