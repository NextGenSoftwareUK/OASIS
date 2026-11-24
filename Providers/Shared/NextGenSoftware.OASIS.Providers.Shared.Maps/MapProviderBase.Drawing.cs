using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Geo;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Http;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps
{
    public abstract partial class MapProviderBase
    {
        /// <summary>Draws an object x metres east and y metres north of the origin.</summary>
        public bool Draw3DObjectOnMap(object obj, float x, float y) => DrawAtOffset(MapCommandType.Draw3DObject, obj, x, y);

        public bool Draw2DSpriteOnMap(object sprite, float x, float y) => DrawAtOffset(MapCommandType.Draw2DSpriteOnMap, sprite, x, y);

        /// <summary>Draws a sprite at screen coordinates; no map projection applies.</summary>
        public bool Draw2DSpriteOnHUD(object sprite, float x, float y)
        {
            if (!Ready()) return false;
            if (sprite == null) return Fail("A sprite is required.");
            if (!float.IsFinite(x) || !float.IsFinite(y)) return Fail("HUD coordinates must be finite.");

            return Dispatch(new MapCommand(MapCommandType.Draw2DSpriteOnHUD) { Target = sprite }.With("x", x).With("y", y));
        }

        /// <summary>
        /// Draws a straight segment between two world offsets (metres east, north of the
        /// origin). This is a drawing primitive, not routing: use the route methods for
        /// paths that follow roads or footpaths.
        /// </summary>
        public bool DrawRouteOnMap(float startX, float startY, float endX, float endY)
        {
            if (!Ready()) return false;

            Geolocation start = FromWorldOffset(new MapWorldPosition(startX, 0, startY));
            Geolocation end = start == null ? null : FromWorldOffset(new MapWorldPosition(endX, 0, endY));
            if (start == null || end == null) return false;

            return Dispatch(new MapCommand(MapCommandType.DrawRoute) { Location = start }
                .With("waypoints", new List<Geolocation> { start, end })
                .With("routed", false));
        }

        /// <summary>Routes between two located entities with the vendor's directions service and draws the result.</summary>
        public bool CreateAndDrawRouteOnMapBetweenHolons(object fromHolon, object toHolon)
        {
            if (!Ready()) return false;

            Geolocation from = MapEntityLocator.Locate(fromHolon);
            Geolocation to = MapEntityLocator.Locate(toHolon);
            if (from == null || to == null) return Fail("Both entities must carry a valid latitude and longitude.");

            return RunRoute(new List<Geolocation> { from, to });
        }

        /// <summary>Routes through every waypoint in order and draws the combined path.</summary>
        public bool CreateAndDrawRouteOnMapBeweenPoints(object points)
        {
            if (!Ready()) return false;

            List<Geolocation> waypoints;
            try
            {
                waypoints = MapEntityLocator.LocateAll(points);
            }
            catch (ArgumentException ex)
            {
                return Fail(ex.Message);
            }

            if (waypoints.Count < 2) return Fail("At least two located waypoints are required.");
            return RunRoute(waypoints);
        }

        /// <summary>
        /// Computes a real route through the waypoints, one leg per consecutive pair,
        /// records it and draws it. Any failed leg fails the whole route.
        /// </summary>
        public async Task<OASISResult<MapRoute>> CreateAndDrawRouteAsync(IReadOnlyList<Geolocation> waypoints,
            RoutingType routingType, CancellationToken cancellationToken = default)
        {
            if (!IsInitialized) return MapResults.Error<MapRoute>(MapProviderName + " has not been initialised; call Initialize first.");
            if (waypoints == null || waypoints.Count < 2) return MapResults.Error<MapRoute>("At least two waypoints are required.");

            var geometry = new List<Geolocation>();
            double distance = 0, duration = 0;

            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                OASISResult<MapRoute> leg = await Routing
                    .GetRouteAsync(waypoints[i], waypoints[i + 1], routingType, cancellationToken).ConfigureAwait(false);
                if (leg.IsError) return MapResults.Error<MapRoute>("Leg " + (i + 1) + " failed: " + leg.Message);

                int skip = geometry.Count > 0 && SamePoint(geometry[geometry.Count - 1], leg.Result.Geometry[0]) ? 1 : 0;
                for (int p = skip; p < leg.Result.Geometry.Count; p++) geometry.Add(leg.Result.Geometry[p]);
                distance += leg.Result.DistanceMetres;
                duration += leg.Result.DurationSeconds;
            }

            var route = new MapRoute(geometry, distance, duration, routingType, Routing.Name);
            var command = new MapCommand(MapCommandType.DrawRoute) { Location = geometry[0] }
                .With("routeId", route.Id)
                .With("waypoints", geometry)
                .With("distanceMetres", distance)
                .With("durationSeconds", duration)
                .With("routed", true);

            if (!Dispatch(command)) return MapResults.Error<MapRoute>(LastError);

            _routes[route.Id] = route;
            return MapResults.Ok(route);
        }

        public bool RemoveRoute(Guid routeId) => _routes.TryRemove(routeId, out _);

        private bool RunRoute(List<Geolocation> waypoints)
        {
            // The contract is synchronous; the request runs on the thread pool so a caller
            // holding a synchronisation context (Unity, UI) cannot deadlock on it.
            OASISResult<MapRoute> result = Task.Run(() => CreateAndDrawRouteAsync(waypoints, DefaultRoutingType))
                .GetAwaiter().GetResult();

            if (result.IsError) return Fail(result.Message);
            LastError = null;
            return true;
        }

        private bool DrawAtOffset(MapCommandType type, object target, float x, float y)
        {
            if (!Ready()) return false;
            if (target == null) return Fail("Something to draw is required.");
            if (!float.IsFinite(x) || !float.IsFinite(y)) return Fail("World offsets must be finite.");

            Geolocation location = FromWorldOffset(new MapWorldPosition(x, 0, y));
            if (location == null) return false;

            return Dispatch(new MapCommand(type) { Target = target, Location = location }.With("x", x).With("y", y));
        }

        private static bool SamePoint(Geolocation a, Geolocation b) =>
            Math.Abs(a.Latitude - b.Latitude) < 1e-9 && Math.Abs(a.Longitude - b.Longitude) < 1e-9;
    }
}
