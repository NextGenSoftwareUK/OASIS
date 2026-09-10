using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Directions;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geocoding;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.IntegrationTests
{
    /// <summary>
    /// Exercises the real routing and geocoding services the provider talks to.
    ///
    /// These reach the public OSRM and Nominatim instances, so they are opt-in: set
    /// GOMAP_RUN_INTEGRATION_TESTS=1 to run them. Without it they are inconclusive
    /// rather than passing, so a green run never implies network coverage that did
    /// not happen. Override GOMAP_OSRM_URL and GOMAP_NOMINATIM_URL to point at
    /// self-hosted instances.
    /// </summary>
    [TestClass]
    public class GOMapOASISIntegrationTests
    {
        private static readonly Geolocation StPauls = new Geolocation(51.5138, -0.0984);
        private static readonly Geolocation TowerOfLondon = new Geolocation(51.5081, -0.0759);

        private GOMapOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            if (Environment.GetEnvironmentVariable("GOMAP_RUN_INTEGRATION_TESTS") != "1")
            {
                Assert.Inconclusive(
                    "Skipped: set GOMAP_RUN_INTEGRATION_TESTS=1 to run these against the live "
                    + "routing and geocoding services.");
            }

            _provider = new GOMapOASIS();
            _provider.Initialize(null);
            _provider.SetOrigin(StPauls);

            string? osrm = Environment.GetEnvironmentVariable("GOMAP_OSRM_URL");
            if (!string.IsNullOrWhiteSpace(osrm))
                _provider.DirectionsAPI = new OSRMDirectionsProvider { BaseUrl = osrm! };

            string? nominatim = Environment.GetEnvironmentVariable("GOMAP_NOMINATIM_URL");
            if (!string.IsNullOrWhiteSpace(nominatim))
                _provider.GeocodingProvider = new NominatimGeocodingProvider { BaseUrl = nominatim! };
        }

        [TestMethod]
        public async Task GetDirectionsAsync_ShouldReturnARoutePassingNearBothEnds()
        {
            List<Geolocation> route = await _provider.DirectionsAPI.GetDirectionsAsync(
                StPauls, TowerOfLondon, RoutingType.Walking);

            Assert.IsTrue(route.Count >= 2, "Expected a multi-point route geometry.");

            // The route starts and ends on the nearest routable way, not exactly on
            // the requested coordinates, so allow a short snap distance.
            Assert.IsTrue(_provider.CalculateDistance(StPauls, route.First()) < 200f);
            Assert.IsTrue(_provider.CalculateDistance(TowerOfLondon, route.Last()) < 200f);
        }

        [TestMethod]
        public async Task GetDirectionsAsync_ShouldReturnALongerPathThanTheStraightLine()
        {
            List<Geolocation> route = await _provider.DirectionsAPI.GetDirectionsAsync(
                StPauls, TowerOfLondon, RoutingType.Walking);

            double travelled = 0.0;
            for (int i = 1; i < route.Count; i++)
                travelled += _provider.CalculateDistance(route[i - 1], route[i]);

            double straightLine = _provider.CalculateDistance(StPauls, TowerOfLondon);

            Assert.IsTrue(travelled >= straightLine,
                $"A walked route ({travelled}m) cannot be shorter than the straight line ({straightLine}m).");
        }

        [TestMethod]
        public async Task GetDirectionsAsync_ShouldDifferByRoutingProfile()
        {
            List<Geolocation> walking = await _provider.DirectionsAPI.GetDirectionsAsync(
                StPauls, TowerOfLondon, RoutingType.Walking);

            List<Geolocation> driving = await _provider.DirectionsAPI.GetDirectionsAsync(
                StPauls, TowerOfLondon, RoutingType.Driving);

            Assert.IsTrue(walking.Count > 0 && driving.Count > 0);
        }

        [TestMethod]
        public async Task CreateAndDrawRoutedPathAsync_ShouldQueueTheRealRouteGeometry()
        {
            Assert.IsTrue(await _provider.CreateAndDrawRoutedPathAsync(StPauls, TowerOfLondon));

            var command = _provider.DrainCommands()
                .Single(c => c.CommandType == Bridge.GOMapCommandType.DrawRoute);

            var waypoints = (List<Geolocation>)command.Parameters["waypoints"];

            Assert.IsTrue(waypoints.Count > 2, "Expected real geometry, not just the two endpoints.");
        }

        [TestMethod]
        public async Task POISearchAsync_ShouldFindAKnownLandmark()
        {
            ForwardGeocodingResponse<IPointOfInterest> response =
                await _provider.GeocodingProvider.POISearchAsync(
                    new POISearchRequest("Tower of London", StPauls, 5));

            Assert.IsTrue(response.Success);
            Assert.IsTrue(response.Data.Count > 0, "Expected at least one match.");

            IPointOfInterest nearest = response.Data.First();

            Assert.IsTrue(_provider.CalculateDistance(TowerOfLondon, nearest.Location) < 1000f,
                "The top result should be the actual Tower of London.");
        }

        [TestMethod]
        public async Task POISearchAsync_ShouldMeasureDistanceFromTheSearchPoint()
        {
            ForwardGeocodingResponse<IPointOfInterest> response =
                await _provider.GeocodingProvider.POISearchAsync(
                    new POISearchRequest("Tower of London", StPauls, 5));

            Assert.IsTrue(response.Data.All(p => p.Distance > 0f));
            Assert.IsTrue(response.Data.Zip(response.Data.Skip(1), (a, b) => a.Distance <= b.Distance).All(x => x),
                "Results should be ordered nearest first.");
        }

        [TestMethod]
        public async Task POISearchAsync_ForNonsense_ShouldSucceedWithNoResults()
        {
            ForwardGeocodingResponse<IPointOfInterest> response =
                await _provider.GeocodingProvider.POISearchAsync(
                    new POISearchRequest("zzqxwvunlikelyplacename12345"));

            Assert.IsTrue(response.Success);
            Assert.AreEqual(0, response.Data.Count);
        }

        [TestMethod]
        public async Task BatchPOISearchAsync_ShouldReturnResultsForEveryQuery()
        {
            BatchPOISearchRequest batch = new BatchPOISearchRequest(
                new[] { "Tower of London", "St Pauls Cathedral" }, StPauls, 3);

            ForwardGeocodingResponse<IPointOfInterest> response =
                await _provider.GeocodingProvider.BatchPOISearchAsync(batch);

            Assert.IsTrue(response.Success);
            Assert.IsTrue(response.Data.Count >= 2);
        }

        [TestMethod]
        public async Task DirectionsProvider_PointedAtADeadEndpoint_ShouldFailCleanly()
        {
            OSRMDirectionsProvider broken = new OSRMDirectionsProvider
            {
                BaseUrl = "https://localhost:1"
            };

            List<Geolocation> route = await broken.GetDirectionsAsync(StPauls, TowerOfLondon);

            Assert.AreEqual(0, route.Count);
            Assert.IsNotNull(broken.LastError);
        }
    }
}
