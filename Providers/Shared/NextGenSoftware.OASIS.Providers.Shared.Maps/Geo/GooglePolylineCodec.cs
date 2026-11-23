using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Geo
{
    /// <summary>
    /// Google's Encoded Polyline Algorithm Format (1e5 precision), as returned by the
    /// Routes API in <c>routes[].polyline.encodedPolyline</c> and accepted by OSRM's
    /// <c>polyline</c> geometry format.
    /// </summary>
    public static class GooglePolylineCodec
    {
        public static List<Geolocation> Decode(string encoded, int precision = 5)
        {
            if (encoded == null) throw new ArgumentNullException(nameof(encoded));
            double factor = Math.Pow(10, precision);
            var points = new List<Geolocation>();
            int index = 0;
            long latitude = 0, longitude = 0;

            while (index < encoded.Length)
            {
                latitude += ReadValue(encoded, ref index);
                if (index >= encoded.Length)
                    throw new InvalidDataException("Encoded polyline ends after a latitude with no longitude.");
                longitude += ReadValue(encoded, ref index);

                var point = new Geolocation(latitude / factor, longitude / factor);
                if (!GeoMath.IsValid(point))
                    throw new InvalidDataException("Encoded polyline decodes to an invalid WGS84 coordinate.");
                points.Add(point);
            }

            return points;
        }

        public static string Encode(IEnumerable<Geolocation> points, int precision = 5)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            double factor = Math.Pow(10, precision);
            var builder = new StringBuilder();
            long previousLatitude = 0, previousLongitude = 0;

            foreach (Geolocation point in points)
            {
                if (!GeoMath.IsValid(point)) throw new ArgumentException("Every point must be a valid WGS84 coordinate.", nameof(points));
                long latitude = (long)Math.Round(point.Latitude * factor, MidpointRounding.AwayFromZero);
                long longitude = (long)Math.Round(point.Longitude * factor, MidpointRounding.AwayFromZero);
                WriteValue(builder, latitude - previousLatitude);
                WriteValue(builder, longitude - previousLongitude);
                previousLatitude = latitude;
                previousLongitude = longitude;
            }

            return builder.ToString();
        }

        private static long ReadValue(string encoded, ref int index)
        {
            long result = 0;
            int shift = 0;
            int chunk;

            do
            {
                if (index >= encoded.Length) throw new InvalidDataException("Encoded polyline is truncated mid-value.");
                chunk = encoded[index++] - 63;
                if (chunk < 0 || chunk > 63) throw new InvalidDataException("Encoded polyline contains a character outside the format.");
                if (shift > 60) throw new InvalidDataException("Encoded polyline value overflows.");
                result |= (long)(chunk & 0x1F) << shift;
                shift += 5;
            }
            while (chunk >= 0x20);

            return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
        }

        private static void WriteValue(StringBuilder builder, long value)
        {
            long shifted = value < 0 ? ~(value << 1) : value << 1;

            while (shifted >= 0x20)
            {
                builder.Append((char)((0x20 | (shifted & 0x1F)) + 63));
                shifted >>= 5;
            }

            builder.Append((char)(shifted + 63));
        }
    }
}
