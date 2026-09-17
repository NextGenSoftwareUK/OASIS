using System;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geo;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS
{
    /// <summary>
    /// Conversion between real-world coordinates and GO Map's Unity world space.
    ///
    /// GO Map projects to spherical Web Mercator and lays tiles on the XZ plane
    /// measured from the map origin, so a coordinate becomes a world position by
    /// projecting both it and the origin to metres and taking the difference. That
    /// is reproduced exactly here, which means the values returned agree with GO
    /// Map's own coordinateToWorldPosition without needing Unity to compute them.
    /// </summary>
    public partial class GOMapOASIS
    {
        /// <summary>
        /// Projects a coordinate into GO Map world space. Returns null until the
        /// origin is set, because the result would otherwise be meaningless - await
        /// <see cref="WaitForOriginSet"/> first.
        /// </summary>
        public object ConvertLatLongToWorldPosition(double latitude, double longitude)
        {
            return ConvertToWorldPosition(latitude, longitude, 0.0);
        }

        /// <summary>Strongly typed form of <see cref="ConvertLatLongToWorldPosition"/>.</summary>
        public GOMapWorldPosition ConvertToWorldPosition(double latitude, double longitude, double altitude = 0.0)
        {
            if (!IsInitialized || Origin == null) return null;

            Geolocation target = new Geolocation(latitude, longitude);
            if (!GeoMath.IsValid(target)) return null;

            WebMercator.LatLonToMetres(Origin.Latitude, Origin.Longitude, out double originX, out double originY);
            WebMercator.LatLonToMetres(latitude, longitude, out double x, out double y);

            // Mercator over-measures away from the equator; scaling by cos(latitude)
            // restores true ground metres, which is what GO Map places objects in.
            double scale = Math.Cos(GeoMath.ToRadians(Origin.Latitude)) * WorldUnitsPerMetre;

            return new GOMapWorldPosition(
                (x - originX) * scale,
                altitude * WorldUnitsPerMetre,
                (y - originY) * scale);
        }

        /// <summary>
        /// Turns a GO Map world position back into a coordinate. Accepts this
        /// provider's own <see cref="GOMapWorldPosition"/>, a double array, or any
        /// object exposing x/y/z members - which is how a UnityEngine.Vector3
        /// arrives when the caller is inside Unity.
        /// </summary>
        public object ConvertWorldPositionToLatLong(object worldPosition)
        {
            if (!IsInitialized || Origin == null || worldPosition == null) return null;

            if (!TryReadWorldPosition(worldPosition, out double x, out double z)) return null;

            double scale = Math.Cos(GeoMath.ToRadians(Origin.Latitude)) * WorldUnitsPerMetre;
            if (Math.Abs(scale) < double.Epsilon) return null;

            WebMercator.LatLonToMetres(Origin.Latitude, Origin.Longitude, out double originX, out double originY);
            WebMercator.MetresToLatLon(originX + x / scale, originY + z / scale,
                out double latitude, out double longitude);

            return new Geolocation(latitude, longitude);
        }

        /// <summary>The slippy-map tile a coordinate falls in at the current zoom.</summary>
        public bool TryGetTile(Geolocation location, out int tileX, out int tileY, out int zoom)
        {
            tileX = 0;
            tileY = 0;
            zoom = (int)Math.Round(Camera.Zoom);

            if (!GeoMath.IsValid(location)) return false;

            WebMercator.LatLonToTile(location.Latitude, location.Longitude, zoom, out tileX, out tileY);
            return true;
        }

        /// <summary>Converts a pan offset in world units back into a coordinate.</summary>
        private Geolocation WorldOffsetToLocation(double x, double z)
        {
            if (Origin == null) return null;

            return ConvertWorldPositionToLatLong(new GOMapWorldPosition(x, 0.0, z)) as Geolocation;
        }

        private static bool TryReadWorldPosition(object worldPosition, out double x, out double z)
        {
            x = 0.0;
            z = 0.0;

            if (worldPosition is GOMapWorldPosition typed)
            {
                x = typed.X;
                z = typed.Z;
                return true;
            }

            if (worldPosition is double[] array && array.Length >= 3)
            {
                x = array[0];
                z = array[2];
                return true;
            }

            if (worldPosition is float[] floats && floats.Length >= 3)
            {
                x = floats[0];
                z = floats[2];
                return true;
            }

            // A UnityEngine.Vector3, or anything else exposing x/y/z.
            double readX = ReadCoordinate(worldPosition, "x", "X");
            double readZ = ReadCoordinate(worldPosition, "z", "Z");

            if (double.IsNaN(readX) || double.IsNaN(readZ)) return false;

            x = readX;
            z = readZ;
            return true;
        }
    }
}
