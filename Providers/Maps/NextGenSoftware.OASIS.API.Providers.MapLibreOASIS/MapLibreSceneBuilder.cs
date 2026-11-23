using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.Maps;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;

namespace NextGenSoftware.OASIS.API.Providers.MapLibreOASIS
{
    /// <summary>
    /// Builds MapLibre style-spec (v8) sources and layers from a map session: routes as
    /// LineString features, pins and placements as Point features (RFC 7946, so
    /// coordinates are [longitude, latitude]). A client adds the sources with
    /// <c>map.addSource</c> and the layers with <c>map.addLayer</c>, and centres the
    /// camera with <c>map.jumpTo</c> using <see cref="BuildCamera"/>.
    /// </summary>
    public sealed class MapLibreSceneBuilder
    {
        public const string RoutesSourceId = "oasis-routes";
        public const string PointsSourceId = "oasis-points";

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = false };
        private readonly MapProviderBase _provider;

        public MapLibreSceneBuilder(MapProviderBase provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public string RouteColour { get; set; } = "#3b82f6";
        public double RouteWidth { get; set; } = 5;
        public string PinColour { get; set; } = "#ef4444";
        public string PlacementColour { get; set; } = "#10b981";

        /// <summary>The routes source: <c>{"type":"geojson","data":FeatureCollection}</c>.</summary>
        public string BuildRoutesSource() => Serialize(GeoJsonSource(_provider.Routes.Select(RouteFeature)));

        /// <summary>The points source with every pin and placement.</summary>
        public string BuildPointsSource()
        {
            IEnumerable<object> pins = _provider.Pins.Select(p => PointFeature(p.Location, new Dictionary<string, object>
            {
                ["kind"] = "pin",
                ["id"] = p.Id.ToString()
            }));

            IEnumerable<object> placements = _provider.Placements.Select(p => PointFeature(p.Location, new Dictionary<string, object>
            {
                ["kind"] = p.Kind.ToString(),
                ["id"] = p.Id.ToString()
            }));

            return Serialize(GeoJsonSource(pins.Concat(placements)));
        }

        /// <summary>A style-spec layer array drawing routes as lines and points as circles.</summary>
        public string BuildLayers() => Serialize(new object[]
        {
            new
            {
                id = "oasis-routes-line",
                type = "line",
                source = RoutesSourceId,
                layout = new Dictionary<string, object> { ["line-join"] = "round", ["line-cap"] = "round" },
                paint = new Dictionary<string, object> { ["line-color"] = RouteColour, ["line-width"] = RouteWidth }
            },
            new
            {
                id = "oasis-points-circle",
                type = "circle",
                source = PointsSourceId,
                paint = new Dictionary<string, object>
                {
                    ["circle-radius"] = 7,
                    ["circle-color"] = new object[] { "match", new object[] { "get", "kind" }, "pin", PinColour, PlacementColour },
                    ["circle-stroke-width"] = 2,
                    ["circle-stroke-color"] = "#ffffff"
                }
            }
        });

        /// <summary>Camera options for <c>map.jumpTo</c>: centre [lon, lat], zoom and bearing.</summary>
        public string BuildCamera()
        {
            MapCameraState camera = _provider.Camera;
            return Serialize(new
            {
                center = new[] { camera.Centre.Longitude, camera.Centre.Latitude },
                zoom = camera.Zoom,
                bearing = camera.Bearing
            });
        }

        private static object GeoJsonSource(IEnumerable<object> features) => new
        {
            type = "geojson",
            data = new { type = "FeatureCollection", features = features.ToArray() }
        };

        private static object RouteFeature(MapRoute route) => new
        {
            type = "Feature",
            geometry = new
            {
                type = "LineString",
                coordinates = route.Geometry.Select(LonLat).ToArray()
            },
            properties = new Dictionary<string, object>
            {
                ["id"] = route.Id.ToString(),
                ["source"] = route.Source,
                ["routingType"] = route.RoutingType.ToString(),
                ["distanceMetres"] = route.DistanceMetres,
                ["durationSeconds"] = route.DurationSeconds
            }
        };

        private static object PointFeature(Geolocation location, Dictionary<string, object> properties) => new
        {
            type = "Feature",
            geometry = new { type = "Point", coordinates = LonLat(location) },
            properties
        };

        private static double[] LonLat(Geolocation point) => new[] { point.Longitude, point.Latitude };

        private static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);
    }
}
