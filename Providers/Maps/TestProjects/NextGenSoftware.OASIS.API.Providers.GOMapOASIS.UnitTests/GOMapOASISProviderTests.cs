using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.UnitTests
{
    /// <summary>
    /// Provider behaviour with no Unity and no network. Everything asserted here is
    /// work the provider does itself, so these are real tests rather than smoke
    /// checks that pass because nothing was exercised.
    /// </summary>
    [TestClass]
    public class GOMapOASISProviderTests
    {
        // St Paul's Cathedral and the Tower of London: 1.68km apart on the ground.
        private static readonly Geolocation StPauls = new Geolocation(51.5138, -0.0984);
        private static readonly Geolocation TowerOfLondon = new Geolocation(51.5081, -0.0759);

        private GOMapOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new GOMapOASIS();
            _provider.Initialize(null);
            _provider.SetOrigin(StPauls);
        }

        [TestMethod]
        public void MapProviderType_ShouldBeGoMap()
        {
            Assert.AreEqual(MapProviderType.GoMap, _provider.MapProviderType);
        }

        [TestMethod]
        public void MapProviderName_ShouldNotBeEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.MapProviderName));
        }

        [TestMethod]
        public void MapProviderDescription_ShouldNotBeEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(_provider.MapProviderDescription));
        }

        [TestMethod]
        public void NewProvider_ShouldNotBeInitialised()
        {
            Assert.IsFalse(new GOMapOASIS().IsInitialized);
        }

        [TestMethod]
        public void Initialize_WithoutAnInstance_ShouldStillInitialiseHeadless()
        {
            Assert.IsTrue(_provider.IsInitialized);
            Assert.IsFalse(_provider.Bridge.IsBound);
            Assert.IsNull(_provider.GOMapInstance);
        }

        [TestMethod]
        public void DirectionsAndGeocodingProviders_ShouldBeWiredUpByDefault()
        {
            Assert.IsNotNull(_provider.DirectionsAPI);
            Assert.IsNotNull(_provider.GeocodingProvider);
        }

        #region Distance

        [TestMethod]
        public void CalculateDistance_ShouldMatchKnownGroundDistance()
        {
            float metres = _provider.CalculateDistance(StPauls, TowerOfLondon);

            // Great-circle distance between these two coordinates is 1681m; the
            // tolerance allows for the spherical earth radius the model uses.
            Assert.IsTrue(Math.Abs(metres - 1681.0) < 10.0,
                $"Expected about 1681m between St Paul's and the Tower, got {metres}m.");
        }

        [TestMethod]
        public void CalculateDistance_ShouldBeSymmetric()
        {
            Assert.AreEqual(
                _provider.CalculateDistance(StPauls, TowerOfLondon),
                _provider.CalculateDistance(TowerOfLondon, StPauls),
                0.001f);
        }

        [TestMethod]
        public void CalculateDistance_ToItself_ShouldBeZero()
        {
            Assert.AreEqual(0f, _provider.CalculateDistance(StPauls, StPauls), 0.001f);
        }

        [TestMethod]
        public void CalculateDistance_WithInvalidCoordinates_ShouldBeZero()
        {
            Assert.AreEqual(0f, _provider.CalculateDistance(new Geolocation(200.0, 0.0), StPauls), 0.001f);
        }

        [TestMethod]
        public void IsWithinRange_ShouldRespectTheRadius()
        {
            Assert.IsTrue(_provider.IsWithinRange(StPauls, TowerOfLondon, 3000.0));
            Assert.IsFalse(_provider.IsWithinRange(StPauls, TowerOfLondon, 500.0));
        }

        [TestMethod]
        public void CalculateBearing_ShouldPointRoughlySouthEast()
        {
            double bearing = _provider.CalculateBearing(StPauls, TowerOfLondon);

            Assert.IsTrue(bearing > 90.0 && bearing < 180.0,
                $"Expected a south-easterly bearing, got {bearing} degrees.");
        }

        #endregion

        #region Coordinate conversion

        [TestMethod]
        public void ConvertToWorldPosition_AtTheOrigin_ShouldBeZero()
        {
            GOMapWorldPosition position = _provider.ConvertToWorldPosition(StPauls.Latitude, StPauls.Longitude);

            Assert.IsNotNull(position);
            Assert.AreEqual(0.0, position!.X, 0.001);
            Assert.AreEqual(0.0, position.Z, 0.001);
        }

        [TestMethod]
        public void ConvertToWorldPosition_ThenBack_ShouldRoundTrip()
        {
            GOMapWorldPosition position =
                _provider.ConvertToWorldPosition(TowerOfLondon.Latitude, TowerOfLondon.Longitude)!;

            Geolocation back = (Geolocation)_provider.ConvertWorldPositionToLatLong(position)!;

            Assert.AreEqual(TowerOfLondon.Latitude, back.Latitude, 0.00001);
            Assert.AreEqual(TowerOfLondon.Longitude, back.Longitude, 0.00001);
        }

        [TestMethod]
        public void ConvertToWorldPosition_ShouldBeConsistentWithGroundDistance()
        {
            GOMapWorldPosition position =
                _provider.ConvertToWorldPosition(TowerOfLondon.Latitude, TowerOfLondon.Longitude)!;

            double planar = Math.Sqrt(position.X * position.X + position.Z * position.Z);
            double ground = _provider.CalculateDistance(StPauls, TowerOfLondon);

            // Over a couple of kilometres the projection and the great circle should
            // agree closely; 1% catches a wrong scale factor without being brittle.
            Assert.IsTrue(Math.Abs(planar - ground) / ground < 0.01,
                $"World-space distance {planar}m disagrees with ground distance {ground}m.");
        }

        [TestMethod]
        public void ConvertWorldPositionToLatLong_ShouldAcceptAnXYZObject()
        {
            GOMapWorldPosition position =
                _provider.ConvertToWorldPosition(TowerOfLondon.Latitude, TowerOfLondon.Longitude)!;

            // Stands in for a UnityEngine.Vector3, which is read reflectively.
            object vector = new { x = position.X, y = 0.0, z = position.Z };
            Geolocation back = (Geolocation)_provider.ConvertWorldPositionToLatLong(vector)!;

            Assert.AreEqual(TowerOfLondon.Latitude, back.Latitude, 0.00001);
        }

        [TestMethod]
        public void ConvertToWorldPosition_BeforeTheOriginIsSet_ShouldReturnNull()
        {
            GOMapOASIS unpositioned = new GOMapOASIS();
            unpositioned.Initialize(null);

            Assert.IsNull(unpositioned.ConvertToWorldPosition(StPauls.Latitude, StPauls.Longitude));
        }

        [TestMethod]
        public void TryGetTile_ShouldReturnATileInRangeForTheZoom()
        {
            Assert.IsTrue(_provider.TryGetTile(StPauls, out int x, out int y, out int zoom));

            int max = (int)Math.Pow(2, zoom);
            Assert.IsTrue(x >= 0 && x < max);
            Assert.IsTrue(y >= 0 && y < max);
        }

        #endregion

        #region Camera

        [TestMethod]
        public void ZoomMapIn_ShouldRaiseTheZoomAndQueueACommand()
        {
            double before = _provider.Camera.Zoom;

            Assert.IsTrue(_provider.ZoomMapIn(2f));
            Assert.AreEqual(before + 2.0, _provider.Camera.Zoom, 0.001);
            Assert.IsTrue(_provider.PendingCommands.Any(c => c.CommandType == GOMapCommandType.Zoom));
        }

        [TestMethod]
        public void ZoomMapOut_ShouldNotFallBelowTheMinimum()
        {
            _provider.ZoomMapOut(500f);
            Assert.AreEqual(GOMapCameraState.MinZoom, _provider.Camera.Zoom, 0.001);
        }

        [TestMethod]
        public void ZoomMapIn_ShouldNotExceedTheMaximum()
        {
            _provider.ZoomMapIn(500f);
            Assert.AreEqual(GOMapCameraState.MaxZoom, _provider.Camera.Zoom, 0.001);
        }

        [TestMethod]
        public void PanMapUp_ShouldMoveTheCentreNorth()
        {
            double before = _provider.Camera.Centre.Latitude;

            Assert.IsTrue(_provider.PanMapUp(100f));
            Assert.IsTrue(_provider.Camera.Centre.Latitude > before);
        }

        [TestMethod]
        public void PanMapDown_ShouldMoveTheCentreSouth()
        {
            double before = _provider.Camera.Centre.Latitude;

            Assert.IsTrue(_provider.PanMapDown(100f));
            Assert.IsTrue(_provider.Camera.Centre.Latitude < before);
        }

        [TestMethod]
        public void PanMapRight_ShouldMoveTheCentreEast()
        {
            double before = _provider.Camera.Centre.Longitude;

            Assert.IsTrue(_provider.PanMapRight(100f));
            Assert.IsTrue(_provider.Camera.Centre.Longitude > before);
        }

        [TestMethod]
        public void PanMapLeft_ShouldMoveTheCentreWest()
        {
            double before = _provider.Camera.Centre.Longitude;

            Assert.IsTrue(_provider.PanMapLeft(100f));
            Assert.IsTrue(_provider.Camera.Centre.Longitude < before);
        }

        [TestMethod]
        public void PanMapUp_ShouldMoveRoughlyTheDistanceAsked()
        {
            Geolocation before = _provider.Camera.Centre;

            _provider.PanMapUp(250f);

            double moved = _provider.CalculateDistance(before, _provider.Camera.Centre);
            Assert.AreEqual(250.0, moved, 1.0);
        }

        [TestMethod]
        public void UpdateOrbitValue_ShouldNormaliseToZeroThreeSixty()
        {
            Assert.IsTrue(_provider.UpdateOrbitValue(-90f));
            Assert.AreEqual(270.0, _provider.Camera.Orbit, 0.001);
        }

        #endregion

        #region Pins and placements

        [TestMethod]
        public void DropPin_ShouldRecordThePin()
        {
            object marker = new object();

            Assert.IsTrue(_provider.DropPin(StPauls, marker));
            Assert.AreEqual(1, _provider.Pins.Count);
        }

        [TestMethod]
        public void DropPin_WithAnInvalidCoordinate_ShouldFail()
        {
            Assert.IsFalse(_provider.DropPin(new Geolocation(91.0, 0.0), new object()));
            Assert.AreEqual(0, _provider.Pins.Count);
        }

        [TestMethod]
        public void RemovePin_ShouldRemoveThePinItWasGiven()
        {
            object marker = new object();
            _provider.DropPin(StPauls, marker);

            Assert.IsTrue(_provider.RemovePin(marker));
            Assert.AreEqual(0, _provider.Pins.Count);
        }

        [TestMethod]
        public void RemovePin_ForSomethingNeverDropped_ShouldFail()
        {
            Assert.IsFalse(_provider.RemovePin(new object()));
        }

        [TestMethod]
        public void GetPinsNear_ShouldOnlyReturnPinsInsideTheRadius()
        {
            _provider.DropPin(StPauls, new object());
            _provider.DropPin(TowerOfLondon, new object());

            Assert.AreEqual(1, _provider.GetPinsNear(StPauls, 500.0).Count);
            Assert.AreEqual(2, _provider.GetPinsNear(StPauls, 5000.0).Count);
        }

        [TestMethod]
        public void PlaceQuestOnMap_ShouldRecordAQuestPlacement()
        {
            Assert.IsTrue(_provider.PlaceQuestOnMap("quest", StPauls.Latitude, StPauls.Longitude));

            Assert.AreEqual(1, _provider.GetPlacements(GOMapPlacementKind.Quest).Count);
        }

        [TestMethod]
        public void PlaceGeoNFTOnMap_ShouldRecordAGeoNFTPlacement()
        {
            Assert.IsTrue(_provider.PlaceGeoNFTOnMap("nft", StPauls.Latitude, StPauls.Longitude));

            Assert.AreEqual(1, _provider.GetPlacements(GOMapPlacementKind.GeoNFT).Count);
        }

        [TestMethod]
        public void PlaceOnMap_WithAnInvalidCoordinate_ShouldFail()
        {
            Assert.IsFalse(_provider.PlaceOAPPOnMap("oapp", 999.0, 0.0));
            Assert.AreEqual(0, _provider.Placements.Count);
        }

        [TestMethod]
        public void PlaceOnMap_WithANullEntity_ShouldFail()
        {
            Assert.IsFalse(_provider.PlaceGeoHotSpotOnMap(null, StPauls.Latitude, StPauls.Longitude));
        }

        [TestMethod]
        public void GetPlacementsNear_ShouldOrderByDistance()
        {
            _provider.PlaceQuestOnMap("far", TowerOfLondon.Latitude, TowerOfLondon.Longitude);
            _provider.PlaceQuestOnMap("near", StPauls.Latitude, StPauls.Longitude);

            var near = _provider.GetPlacementsNear(StPauls, 5000.0).ToList();

            Assert.AreEqual(2, near.Count);
            Assert.AreEqual("near", near[0].Entity);
        }

        [TestMethod]
        public void RemovePlacement_ShouldRemoveIt()
        {
            _provider.PlaceQuestOnMap("quest", StPauls.Latitude, StPauls.Longitude);
            Guid id = _provider.Placements.First().Id;

            Assert.IsTrue(_provider.RemovePlacement(id));
            Assert.AreEqual(0, _provider.Placements.Count);
        }

        #endregion

        #region Command queue

        [TestMethod]
        public void DrainCommands_ShouldEmptyTheQueue()
        {
            _provider.DropPin(StPauls, new object());
            _provider.ZoomMapIn(1f);

            Assert.AreEqual(2, _provider.DrainCommands().Count);
            Assert.AreEqual(0, _provider.PendingCommands.Count);
        }

        [TestMethod]
        public void CommandsFromAHeadlessProvider_ShouldNotClaimToHaveReachedUnity()
        {
            _provider.DropPin(StPauls, new object());

            Assert.IsFalse(_provider.DrainCommands().Single().AppliedToLiveMap);
        }

        [TestMethod]
        public void ClearCommands_ShouldDiscardEverythingQueued()
        {
            _provider.ZoomMapIn(1f);
            _provider.ClearCommands();

            Assert.AreEqual(0, _provider.PendingCommands.Count);
        }

        [TestMethod]
        public void SelectHolonOnMap_ShouldRecordTheSelection()
        {
            Assert.IsTrue(_provider.SelectHolonOnMap("holon"));
            Assert.AreEqual("holon", _provider.SelectedHolon);
        }

        [TestMethod]
        public void HighlightBuildingOnMap_ShouldRecordTheHighlight()
        {
            Assert.IsTrue(_provider.HighlightBuildingOnMap("building"));
            Assert.AreEqual("building", _provider.HighlightedBuilding);
        }

        [TestMethod]
        public void ZoomToHolonOnMap_ShouldCentreOnTheHolon()
        {
            Assert.IsTrue(_provider.ZoomToHolonOnMap(TowerOfLondon));

            Assert.AreEqual(TowerOfLondon.Latitude, _provider.Camera.Centre.Latitude, 0.00001);
        }

        [TestMethod]
        public void CreateAndDrawRouteOnMapBetweenHolons_ShouldQueueARouteWithBothEnds()
        {
            Assert.IsTrue(_provider.CreateAndDrawRouteOnMapBetweenHolons(StPauls, TowerOfLondon));

            GOMapCommand route = _provider.DrainCommands()
                .Single(c => c.CommandType == GOMapCommandType.DrawRoute);

            Assert.AreEqual(2, ((System.Collections.Generic.List<Geolocation>)route.Parameters["waypoints"]).Count);
        }

        [TestMethod]
        public void CreateAndDrawRouteOnMapBeweenPoints_ShouldAcceptACollection()
        {
            Assert.IsTrue(_provider.CreateAndDrawRouteOnMapBeweenPoints(
                new[] { StPauls, TowerOfLondon }));
        }

        [TestMethod]
        public void CreateAndDrawRouteOnMapBeweenPoints_WithOnePoint_ShouldFail()
        {
            Assert.IsFalse(_provider.CreateAndDrawRouteOnMapBeweenPoints(new[] { StPauls }));
        }

        #endregion

        #region Location

        [TestMethod]
        public async Task WaitForOriginSet_ShouldCompleteOnceTheOriginIsKnown()
        {
            Task waiting = _provider.WaitForOriginSet();

            Assert.AreSame(waiting, await Task.WhenAny(waiting, Task.Delay(1000)));
        }

        [TestMethod]
        public void SetCurrentLocation_ShouldBeReadBackByGetCurrentLocation()
        {
            _provider.SetCurrentLocation(TowerOfLondon);

            Assert.AreEqual(TowerOfLondon.Latitude, _provider.GetCurrentLocation()!.Latitude, 0.00001);
        }

        [TestMethod]
        public void OperationsBeforeInitialisation_ShouldAllFailRatherThanThrow()
        {
            GOMapOASIS uninitialised = new GOMapOASIS();

            Assert.IsFalse(uninitialised.ZoomMapIn(1f));
            Assert.IsFalse(uninitialised.DropPin(StPauls, new object()));
            Assert.IsFalse(uninitialised.SelectHolonOnMap("holon"));
            Assert.IsNull(uninitialised.GetCurrentLocation());
        }

        #endregion
    }
}
