using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS
{
    /// <summary>
    /// Drawing operations. Each records a command for the GO Map client to apply and,
    /// when bound to a live instance, forwards the same call to it.
    /// </summary>
    public partial class GOMapOASIS
    {
        public bool Draw3DObjectOnMap(object obj, float x, float y)
        {
            if (!IsInitialized || obj == null) return false;

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.Draw3DObjectOnMap)
            {
                Target = obj,
                Location = WorldOffsetToLocation(x, y)
            }.With("x", x).With("y", y));

            command.AppliedToLiveMap = Bridge.TryInvoke("placeObject", new object[] { obj, x, y }, out _);
            return true;
        }

        public bool Draw2DSpriteOnMap(object sprite, float x, float y)
        {
            if (!IsInitialized || sprite == null) return false;

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.Draw2DSpriteOnMap)
            {
                Target = sprite,
                Location = WorldOffsetToLocation(x, y)
            }.With("x", x).With("y", y));

            command.AppliedToLiveMap = Bridge.TryInvoke("placeSprite", new object[] { sprite, x, y }, out _);
            return true;
        }

        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        {
            if (!IsInitialized || sprite == null) return false;

            // HUD coordinates are screen space, not world space, so no projection applies.
            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.Draw2DSpriteOnHUD)
            {
                Target = sprite
            }.With("x", x).With("y", y));

            command.AppliedToLiveMap = Bridge.TryInvoke("drawOnHUD", new object[] { sprite, x, y }, out _);
            return true;
        }

        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY)
        {
            if (!IsInitialized) return false;

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.DrawRoute)
                .With("startX", startX).With("startY", startY)
                .With("endX", endX).With("endY", endY)
                .With("waypoints", new List<Geolocation>
                {
                    WorldOffsetToLocation(startX, startY),
                    WorldOffsetToLocation(endX, endY)
                }));

            command.AppliedToLiveMap = Bridge.TryInvoke(
                "drawRoute", new object[] { startX, startY, endX, endY }, out _);

            return true;
        }

        public bool CreateAndDrawRouteOnMapBetweenHolons(object fromHolon, object toHolon)
        {
            if (!IsInitialized || fromHolon == null || toHolon == null) return false;

            Geolocation from = ExtractLocation(fromHolon);
            Geolocation to = ExtractLocation(toHolon);

            if (from == null || to == null) return false;

            return DrawRouteBetween(new List<Geolocation> { from, to }, fromHolon, toHolon);
        }

        public bool CreateAndDrawRouteOnMapBeweenPoints(object points)
        {
            if (!IsInitialized || points == null) return false;

            List<Geolocation> waypoints = ExtractLocations(points);
            if (waypoints.Count < 2) return false;

            return DrawRouteBetween(waypoints, null, null);
        }

        /// <summary>
        /// Asks the directions service for the real road or footpath geometry between
        /// two points and draws that, rather than a straight line.
        /// </summary>
        public async System.Threading.Tasks.Task<bool> CreateAndDrawRoutedPathAsync(
            Geolocation from, Geolocation to, RoutingType routingType = RoutingType.Walking)
        {
            if (!IsInitialized || from == null || to == null || DirectionsAPI == null) return false;

            List<Geolocation> waypoints = await DirectionsAPI.GetDirectionsAsync(from, to, routingType);

            if (waypoints == null || waypoints.Count < 2)
                waypoints = new List<Geolocation> { from, to };

            return DrawRouteBetween(waypoints, null, null);
        }

        private bool DrawRouteBetween(List<Geolocation> waypoints, object fromEntity, object toEntity)
        {
            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.DrawRoute)
            {
                Location = waypoints[0]
            }
            .With("waypoints", waypoints)
            .With("from", fromEntity)
            .With("to", toEntity));

            object[] coordinates = waypoints
                .Select(w => Bridge.CreateCoordinate(w.Latitude, w.Longitude))
                .ToArray();

            if (coordinates.Length > 0 && coordinates.All(c => c != null))
                command.AppliedToLiveMap = Bridge.TryInvoke("drawRoute", new object[] { coordinates }, out _);

            return true;
        }

        /// <summary>
        /// Reads a coordinate off an arbitrary OASIS entity. Holons, quests, OAPPs and
        /// GeoNFTs all carry latitude and longitude, but under several different
        /// property names and through different interfaces across the codebase, so
        /// this accepts a Geolocation directly or discovers the pair reflectively
        /// rather than forcing a dependency on any one of those types.
        /// </summary>
        private static Geolocation ExtractLocation(object entity)
        {
            if (entity == null) return null;
            if (entity is Geolocation location) return location;

            double latitude = ReadCoordinate(entity, "Latitude", "Lat", "latitude");
            double longitude = ReadCoordinate(entity, "Longitude", "Long", "Lon", "Lng", "longitude");

            if (double.IsNaN(latitude) || double.IsNaN(longitude))
            {
                // Some entities nest the pair under a location-like member.
                foreach (string nested in new[] { "Geolocation", "Location", "Coordinates", "Position" })
                {
                    object inner = ReadMember(entity, nested);
                    if (inner != null && !ReferenceEquals(inner, entity))
                    {
                        Geolocation resolved = ExtractLocation(inner);
                        if (resolved != null) return resolved;
                    }
                }

                return null;
            }

            return new Geolocation(latitude, longitude);
        }

        private static List<Geolocation> ExtractLocations(object points)
        {
            List<Geolocation> waypoints = new List<Geolocation>();

            if (points is IEnumerable sequence && !(points is string))
            {
                foreach (object item in sequence)
                {
                    Geolocation location = ExtractLocation(item);
                    if (location != null) waypoints.Add(location);
                }

                return waypoints;
            }

            // A single object that itself exposes a waypoint collection.
            foreach (string name in new[] { "Points", "Waypoints", "MapPoints", "Coordinates" })
            {
                object inner = ReadMember(points, name);
                if (inner is IEnumerable innerSequence)
                {
                    foreach (object item in innerSequence)
                    {
                        Geolocation location = ExtractLocation(item);
                        if (location != null) waypoints.Add(location);
                    }

                    if (waypoints.Count > 0) return waypoints;
                }
            }

            return waypoints;
        }

        private static double ReadCoordinate(object entity, params string[] names)
        {
            foreach (string name in names)
            {
                object value = ReadMember(entity, name);
                if (value == null) continue;

                try
                {
                    return Convert.ToDouble(value);
                }
                catch (Exception)
                {
                    // Not a numeric member under that name - keep looking.
                }
            }

            return double.NaN;
        }

        private static object ReadMember(object entity, string name)
        {
            const System.Reflection.BindingFlags flags =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.FlattenHierarchy;

            Type type = entity.GetType();

            try
            {
                System.Reflection.PropertyInfo property = type.GetProperty(name, flags);
                if (property != null && property.CanRead) return property.GetValue(entity);

                System.Reflection.FieldInfo field = type.GetField(name, flags);
                return field?.GetValue(entity);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
