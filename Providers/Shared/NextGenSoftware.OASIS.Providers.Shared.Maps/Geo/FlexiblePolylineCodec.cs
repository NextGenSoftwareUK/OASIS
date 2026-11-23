using System;
using System.Collections.Generic;
using System.IO;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Geo
{
    /// <summary>
    /// Decoder for HERE's Flexible Polyline format (version 1), returned by HERE Routing
    /// API v8 in <c>routes[].sections[].polyline</c>. Specification:
    /// https://github.com/heremaps/flexible-polyline. A third dimension, when present,
    /// is read and discarded because OASIS route geometry is two-dimensional.
    /// </summary>
    public static class FlexiblePolylineCodec
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        private static readonly int[] DecodingTable = BuildDecodingTable();

        public static List<Geolocation> Decode(string encoded)
        {
            if (encoded == null) throw new ArgumentNullException(nameof(encoded));
            int index = 0;

            if (ReadUnsigned(encoded, ref index) != 1)
                throw new InvalidDataException("Only flexible polyline format version 1 is supported.");

            long header = ReadUnsigned(encoded, ref index);
            int precision = (int)(header & 15);
            int thirdDimension = (int)((header >> 4) & 7);
            if (thirdDimension == 4 || thirdDimension == 5)
                throw new InvalidDataException("Flexible polyline uses a reserved third-dimension type.");

            double factor = Math.Pow(10, precision);
            var points = new List<Geolocation>();
            long latitude = 0, longitude = 0;

            while (index < encoded.Length)
            {
                latitude += ReadSigned(encoded, ref index);
                longitude += ReadSigned(encoded, ref index);
                if (thirdDimension != 0) ReadSigned(encoded, ref index);

                var point = new Geolocation(latitude / factor, longitude / factor);
                if (!GeoMath.IsValid(point))
                    throw new InvalidDataException("Flexible polyline decodes to an invalid WGS84 coordinate.");
                points.Add(point);
            }

            return points;
        }

        private static long ReadSigned(string encoded, ref int index)
        {
            long value = ReadUnsigned(encoded, ref index);
            return (value & 1) != 0 ? -((value + 1) >> 1) : value >> 1;
        }

        private static long ReadUnsigned(string encoded, ref int index)
        {
            long result = 0;
            int shift = 0;

            while (true)
            {
                if (index >= encoded.Length) throw new InvalidDataException("Flexible polyline is truncated mid-value.");
                char character = encoded[index++];
                int value = character < 128 ? DecodingTable[character] : -1;
                if (value < 0) throw new InvalidDataException("Flexible polyline contains a character outside its alphabet.");
                if (shift > 60) throw new InvalidDataException("Flexible polyline value overflows.");

                result |= (long)(value & 0x1F) << shift;
                if ((value & 0x20) == 0) return result;
                shift += 5;
            }
        }

        private static int[] BuildDecodingTable()
        {
            var table = new int[128];
            for (int i = 0; i < table.Length; i++) table[i] = -1;
            for (int i = 0; i < Alphabet.Length; i++) table[Alphabet[i]] = i;
            return table;
        }
    }
}
