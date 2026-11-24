using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Geo
{
    /// <summary>
    /// Reads a coordinate off an OASIS entity. Holons, quests, OAPPs, GeoNFTs and hot
    /// spots carry latitude and longitude under different member names and interfaces,
    /// and <c>IOASISMapProvider</c> passes them as <see cref="object"/>, so the pair is
    /// discovered by name rather than tying the map layer to every entity type.
    /// </summary>
    public static class MapEntityLocator
    {
        private static readonly string[] LatitudeNames = { "Latitude", "Lat" };
        private static readonly string[] LongitudeNames = { "Longitude", "Long", "Lon", "Lng" };
        private static readonly string[] NestedNames = { "Geolocation", "Location", "Coordinates", "Position", "GeoLocation" };
        private static readonly string[] CollectionNames = { "Points", "Waypoints", "MapPoints", "Coordinates" };

        /// <summary>Returns the entity's coordinate, or null when it carries none.</summary>
        public static Geolocation Locate(object entity) => Locate(entity, 0);

        /// <summary>Reads every locatable item of a sequence, or of a member holding one.</summary>
        public static List<Geolocation> LocateAll(object points)
        {
            var waypoints = new List<Geolocation>();
            if (points == null) return waypoints;

            if (points is IEnumerable sequence && !(points is string))
            {
                AddAll(sequence, waypoints);
                return waypoints;
            }

            foreach (string name in CollectionNames)
            {
                if (ReadMember(points, name) is IEnumerable inner && !(inner is string))
                {
                    AddAll(inner, waypoints);
                    if (waypoints.Count > 0) return waypoints;
                }
            }

            return waypoints;
        }

        private static void AddAll(IEnumerable sequence, List<Geolocation> into)
        {
            foreach (object item in sequence)
            {
                Geolocation location = Locate(item);
                if (location == null)
                    throw new ArgumentException("Every waypoint must carry a latitude and longitude.");
                into.Add(location);
            }
        }

        private static Geolocation Locate(object entity, int depth)
        {
            if (entity == null || depth > 3) return null;
            if (entity is Geolocation location) return GeoMath.IsValid(location) ? location : null;

            double latitude = ReadNumber(entity, LatitudeNames);
            double longitude = ReadNumber(entity, LongitudeNames);

            if (!double.IsNaN(latitude) && !double.IsNaN(longitude))
            {
                var found = new Geolocation(latitude, longitude);
                return GeoMath.IsValid(found) ? found : null;
            }

            foreach (string name in NestedNames)
            {
                object inner = ReadMember(entity, name);
                if (inner == null || ReferenceEquals(inner, entity)) continue;
                Geolocation nested = Locate(inner, depth + 1);
                if (nested != null) return nested;
            }

            return null;
        }

        private static double ReadNumber(object entity, string[] names)
        {
            foreach (string name in names)
            {
                switch (ReadMember(entity, name))
                {
                    case double d: return d;
                    case float f: return f;
                    case decimal m: return (double)m;
                    case int i: return i;
                    case long l: return l;
                    case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed):
                        return parsed;
                }
            }

            return double.NaN;
        }

        private static object ReadMember(object entity, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy;
            Type type = entity.GetType();

            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null && property.CanRead && property.GetIndexParameters().Length == 0)
                return property.GetValue(entity);

            return type.GetField(name, flags)?.GetValue(entity);
        }
    }
}
