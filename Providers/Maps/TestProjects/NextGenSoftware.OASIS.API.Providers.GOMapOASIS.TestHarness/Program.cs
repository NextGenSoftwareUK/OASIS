using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geocoding;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.TestHarness
{
    /// <summary>
    /// Walks through the GOMap OASIS provider end to end.
    ///
    /// The offline part - projection, geodesy, placements, the command queue - always
    /// runs. The routing and geocoding part only runs with --live, since it reaches
    /// the public OSRM and Nominatim services.
    /// </summary>
    public static class Program
    {
        private static readonly Geolocation StPauls = new Geolocation(51.5138, -0.0984);
        private static readonly Geolocation TowerOfLondon = new Geolocation(51.5081, -0.0759);

        public static async Task Main(string[] args)
        {
            bool live = args.Any(a => string.Equals(a, "--live", StringComparison.OrdinalIgnoreCase));

            Console.WriteLine("GOMap OASIS Provider Test Harness");
            Console.WriteLine("=================================");
            Console.WriteLine();

            GOMapOASIS provider = new GOMapOASIS();
            provider.Initialize(null);
            provider.SetOrigin(StPauls);

            Console.WriteLine($"Provider     : {provider.MapProviderName} ({provider.MapProviderType})");
            Console.WriteLine($"Initialised  : {provider.IsInitialized}");
            Console.WriteLine($"Bound to GO Map: {provider.Bridge.IsBound} (headless when false)");
            Console.WriteLine($"Origin       : {provider.Origin}");
            Console.WriteLine();

            ShowGeodesy(provider);
            ShowProjection(provider);
            ShowCamera(provider);
            ShowPlacements(provider);
            ShowCommandQueue(provider);

            if (live)
                await ShowLiveServicesAsync(provider);
            else
                Console.WriteLine("Routing and geocoding skipped. Re-run with --live to exercise them.");

            Console.WriteLine();
            Console.WriteLine("Done.");
        }

        private static void ShowGeodesy(GOMapOASIS provider)
        {
            Console.WriteLine("Geodesy");
            Console.WriteLine("-------");
            Console.WriteLine($"  St Paul's -> Tower : {provider.CalculateDistance(StPauls, TowerOfLondon):F1} m");
            Console.WriteLine($"  Bearing            : {provider.CalculateBearing(StPauls, TowerOfLondon):F1} degrees");
            Console.WriteLine($"  Within 1km         : {provider.IsWithinRange(StPauls, TowerOfLondon, 1000)}");
            Console.WriteLine($"  Within 3km         : {provider.IsWithinRange(StPauls, TowerOfLondon, 3000)}");
            Console.WriteLine();
        }

        private static void ShowProjection(GOMapOASIS provider)
        {
            Console.WriteLine("Projection into GO Map world space");
            Console.WriteLine("----------------------------------");

            GOMapWorldPosition? position =
                provider.ConvertToWorldPosition(TowerOfLondon.Latitude, TowerOfLondon.Longitude);

            Console.WriteLine($"  Tower of London    : {position}");

            if (position != null && provider.ConvertWorldPositionToLatLong(position) is Geolocation back)
                Console.WriteLine($"  Converted back     : {back}");

            if (provider.TryGetTile(StPauls, out int x, out int y, out int zoom))
                Console.WriteLine($"  Tile at zoom {zoom}   : {x}, {y}");

            Console.WriteLine();
        }

        private static void ShowCamera(GOMapOASIS provider)
        {
            Console.WriteLine("Camera");
            Console.WriteLine("------");
            Console.WriteLine($"  Start              : zoom {provider.Camera.Zoom}, centre {provider.Camera.Centre}");

            provider.ZoomMapIn(2f);
            provider.PanMapUp(500f);
            provider.PanMapRight(250f);
            provider.UpdateOrbitValue(45f);

            Console.WriteLine($"  After pan and zoom : zoom {provider.Camera.Zoom}, centre {provider.Camera.Centre}");
            Console.WriteLine($"  Orbit              : {provider.Camera.Orbit} degrees");
            Console.WriteLine();
        }

        private static void ShowPlacements(GOMapOASIS provider)
        {
            Console.WriteLine("Placements");
            Console.WriteLine("----------");

            provider.PlaceQuestOnMap("The Ravens of the Tower", TowerOfLondon.Latitude, TowerOfLondon.Longitude);
            provider.PlaceGeoNFTOnMap("Whispering Gallery NFT", StPauls.Latitude, StPauls.Longitude);
            provider.PlaceGeoHotSpotOnMap("Millennium Bridge", 51.5095, -0.0985);
            provider.PlaceOAPPOnMap("Our World London", StPauls.Latitude, StPauls.Longitude);
            provider.DropPin(StPauls, "avatar-marker");

            foreach (GOMapPlacement placement in provider.Placements.OrderBy(p => p.Kind.ToString()))
                Console.WriteLine($"  {placement.Kind,-12} {placement.Entity} @ {placement.Location}");

            Console.WriteLine($"  Pins dropped       : {provider.Pins.Count}");

            IReadOnlyCollection<GOMapPlacement> near = provider.GetPlacementsNear(StPauls, 500.0);
            Console.WriteLine($"  Within 500m of St Paul's: {near.Count}");
            Console.WriteLine();
        }

        private static void ShowCommandQueue(GOMapOASIS provider)
        {
            Console.WriteLine("Command queue (what a Unity client would drain and apply)");
            Console.WriteLine("--------------------------------------------------------");

            foreach (GOMapCommand command in provider.DrainCommands())
                Console.WriteLine($"  {command.CommandType,-20} applied to live map: {command.AppliedToLiveMap}");

            Console.WriteLine();
        }

        private static async Task ShowLiveServicesAsync(GOMapOASIS provider)
        {
            Console.WriteLine("Routing (OSRM)");
            Console.WriteLine("--------------");

            List<Geolocation> route = await provider.DirectionsAPI.GetDirectionsAsync(
                StPauls, TowerOfLondon, RoutingType.Walking);

            if (route.Count == 0)
            {
                Console.WriteLine("  No route returned.");
            }
            else
            {
                double travelled = 0.0;
                for (int i = 1; i < route.Count; i++)
                    travelled += provider.CalculateDistance(route[i - 1], route[i]);

                Console.WriteLine($"  Waypoints          : {route.Count}");
                Console.WriteLine($"  Walked distance    : {travelled:F0} m");
                Console.WriteLine($"  Straight line      : {provider.CalculateDistance(StPauls, TowerOfLondon):F0} m");
            }

            Console.WriteLine();
            Console.WriteLine("Geocoding (Nominatim)");
            Console.WriteLine("---------------------");

            ForwardGeocodingResponse<IPointOfInterest> results =
                await provider.GeocodingProvider.POISearchAsync(
                    new POISearchRequest("Tower of London", StPauls, 3));

            foreach (IPointOfInterest poi in results.Data)
                Console.WriteLine($"  {poi.Name} [{poi.Category}] {poi.Distance:F0}m away");

            if (results.Data.Count == 0)
                Console.WriteLine("  No results.");

            Console.WriteLine();
        }
    }
}
