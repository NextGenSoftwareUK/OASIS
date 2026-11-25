using System;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge;
using NextGenSoftware.OASIS.Providers.Shared.Maps;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Geo;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Routing;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS
{
    /// <summary>
    /// GO Map OASIS Provider - places OASIS avatars, holons, quests, OAPPs, GeoNFTs
    /// and geo hot spots on real-world maps for Our World and One World.
    ///
    /// GO Map is a Unity Asset Store package, so its runtime types only exist inside a
    /// Unity player. The map session (camera, pins, placements, routes, world-space
    /// projection) is the shared <see cref="MapProviderBase"/>: headless, every change
    /// is queued for a Unity client to drain; bound to a live GO Map component with
    /// <see cref="BindGOMap"/>, every change is applied to it through
    /// <see cref="GOMapCommandSink"/> and a rejected call fails the operation.
    ///
    /// Directions come from OSRM and place search from Nominatim (both keyless,
    /// configurable to self-hosted instances).
    /// </summary>
    public sealed class GOMapOASIS : MapProviderBase
    {
        public GOMapOASIS() : this(null)
        {
        }

        /// <summary>Creates the provider and, when given one, binds it to a live GO Map component.</summary>
        public GOMapOASIS(object goMapInstance)
            : this(OsrmRoutingService.PublicDemoBaseUrl, NominatimPlaceSearchService.PublicBaseUrl)
        {
            if (goMapInstance != null) BindGOMap(goMapInstance);
        }

        public GOMapOASIS(string osrmBaseUrl, string nominatimBaseUrl, HttpClient client = null)
            : base(MapProviderType.GoMap, "GO Map",
                   "GO Map OASIS Provider - real-world geo-spatial and AR mapping for OASIS avatars, "
                   + "holons, quests, OAPPs, GeoNFTs and geo hot spots.",
                   new OsrmRoutingService(osrmBaseUrl, client), new NominatimPlaceSearchService(nominatimBaseUrl, client))
        {
        }

        /// <summary>Reflective dispatcher onto the live GO Map component.</summary>
        public GOMapUnityBridge Bridge { get; } = new GOMapUnityBridge();

        /// <summary>The live GO Map component, or null when headless.</summary>
        public object GOMapInstance => Bridge.Instance;

        /// <summary>
        /// Starts the session against a live GO Map component. Its location manager's
        /// current location, when it has one, becomes the world origin.
        /// </summary>
        public void BindGOMap(object goMapInstance)
        {
            if (goMapInstance == null) throw new ArgumentNullException(nameof(goMapInstance));

            Bridge.Bind(goMapInstance);
            Initialize(new GOMapCommandSink(Bridge));

            double latitude = Bridge.GetValueOrDefault("locationManager.currentLocation.latitude", double.NaN);
            double longitude = Bridge.GetValueOrDefault("locationManager.currentLocation.longitude", double.NaN);
            Geolocation location = new Geolocation(latitude, longitude);
            if (GeoMath.IsValid(location)) SetCurrentLocation(location);
        }

        /// <summary>Initial compass bearing from one coordinate to another, 0-360 degrees.</summary>
        public double CalculateBearing(Geolocation from, Geolocation to)
        {
            if (!GeoMath.IsValid(from) || !GeoMath.IsValid(to)) throw new ArgumentException("Valid WGS84 coordinates are required.");
            return GeoMath.BearingDegrees(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
        }

        public bool IsWithinRange(Geolocation from, Geolocation to, double radiusMetres)
        {
            if (radiusMetres < 0) throw new ArgumentOutOfRangeException(nameof(radiusMetres));
            return GeoMath.IsValid(from) && GeoMath.IsValid(to) && GeoMath.HaversineMetres(from, to) <= radiusMetres;
        }

        /// <summary>The slippy-map tile a coordinate falls in at the current zoom.</summary>
        public bool TryGetTile(Geolocation location, out int tileX, out int tileY, out int zoom)
        {
            tileX = tileY = 0;
            zoom = (int)Math.Round(Camera.Zoom);
            if (!GeoMath.IsValid(location)) return false;

            WebMercator.LatLonToTile(location.Latitude, location.Longitude, zoom, out tileX, out tileY);
            return true;
        }

        protected override void OnShutdown() => Bridge.Unbind();
    }
}
