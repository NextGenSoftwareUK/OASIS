using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.UnitTests
{
    /// <summary>
    /// The reflective bridge, exercised against a stand-in that mimics the shape of
    /// the GO Map component. This is what proves the provider really does forward to
    /// a live instance when one is present, without needing Unity in the test run.
    /// </summary>
    [TestClass]
    public class GOMapUnityBridgeTests
    {
        private static readonly Geolocation StPauls = new Geolocation(51.5138, -0.0984);

        [TestMethod]
        public void UnboundBridge_ShouldReportNotBoundAndInvokeNothing()
        {
            GOMapUnityBridge bridge = new GOMapUnityBridge();

            Assert.IsFalse(bridge.IsBound);
            Assert.IsFalse(bridge.TryInvoke("dropPin", new object[] { null!, null! }, out _));
        }

        [TestMethod]
        public void TryInvoke_ShouldCallAMatchingMethodOnTheBoundInstance()
        {
            FakeGOMap map = new FakeGOMap();
            GOMapUnityBridge bridge = new GOMapUnityBridge();
            bridge.Bind(map);

            Assert.IsTrue(bridge.TryInvoke("setZoom", new object[] { 12.0 }, out _));
            Assert.AreEqual(12.0, map.Zoom, 0.001);
        }

        [TestMethod]
        public void TryInvoke_ForAMethodThatDoesNotExist_ShouldFailAndRecordWhy()
        {
            GOMapUnityBridge bridge = new GOMapUnityBridge();
            bridge.Bind(new FakeGOMap());

            Assert.IsFalse(bridge.TryInvoke("noSuchMethod", new object[0], out _));
            Assert.IsNotNull(bridge.LastError);
        }

        [TestMethod]
        public void TryGetValue_ShouldFollowADottedMemberPath()
        {
            GOMapUnityBridge bridge = new GOMapUnityBridge();
            bridge.Bind(new FakeGOMap());

            Assert.AreEqual(51.5138,
                bridge.GetValueOrDefault("locationManager.currentLocation.latitude", double.NaN),
                0.00001);
        }

        [TestMethod]
        public void GetValueOrDefault_ForAMissingPath_ShouldReturnTheFallback()
        {
            GOMapUnityBridge bridge = new GOMapUnityBridge();
            bridge.Bind(new FakeGOMap());

            Assert.AreEqual(-1.0, bridge.GetValueOrDefault("locationManager.nothingHere", -1.0), 0.001);
        }

        [TestMethod]
        public void Unbind_ShouldReturnTheBridgeToUnbound()
        {
            GOMapUnityBridge bridge = new GOMapUnityBridge();
            bridge.Bind(new FakeGOMap());
            bridge.Unbind();

            Assert.IsFalse(bridge.IsBound);
        }

        [TestMethod]
        public void ProviderBoundToAnInstance_ShouldForwardAndSaySo()
        {
            FakeGOMap map = new FakeGOMap();
            GOMapOASIS provider = new GOMapOASIS(map);
            provider.SetOrigin(StPauls);

            Assert.IsTrue(provider.ZoomMapIn(1f));

            Assert.AreEqual(map.Zoom, provider.Camera.Zoom, 0.001);
            Assert.AreEqual(0, provider.PendingCommandCount, "A bound provider applies commands instead of queueing them.");
        }

        [TestMethod]
        public void ProviderBoundToAnInstance_ShouldForwardPinsAndSelection()
        {
            FakeGOMap map = new FakeGOMap();
            GOMapOASIS provider = new GOMapOASIS(map);
            object marker = new object();

            Assert.IsTrue(provider.DropPin(StPauls, marker));
            Assert.IsTrue(provider.RemovePin(marker));
            Assert.IsTrue(provider.SelectHolonOnMap("holon"));

            Assert.AreEqual(1, map.DroppedPins);
            Assert.AreEqual(1, map.RemovedPins);
            Assert.AreEqual("holon", map.LastSelected);
        }

        [TestMethod]
        public void ProviderBoundToAnInstance_ShouldFailWhenTheComponentLacksTheMethod()
        {
            GOMapOASIS provider = new GOMapOASIS(new FakeGOMap());

            Assert.IsFalse(provider.PlaceQuestOnMap("quest", StPauls.Latitude, StPauls.Longitude));
            Assert.AreEqual(0, provider.Placements.Count, "A placement the scene rejected must not be recorded.");
            Assert.IsNotNull(provider.LastError);
            StringAssert.Contains(provider.Bridge.LastError, "placeQuest");
        }

        [TestMethod]
        public void ProviderBoundToAnInstance_ShouldTakeItsOriginFromTheLocationManager()
        {
            GOMapOASIS provider = new GOMapOASIS(new FakeGOMap());

            Assert.IsNotNull(provider.Origin);
            Assert.AreEqual(StPauls.Latitude, provider.Origin!.Latitude, 0.00001);
        }

        [TestMethod]
        public void Shutdown_ShouldUnbindAndDeinitialise()
        {
            GOMapOASIS provider = new GOMapOASIS(new FakeGOMap());
            provider.Shutdown();

            Assert.IsFalse(provider.IsInitialized);
            Assert.IsNull(provider.GOMapInstance);
        }
    }
}
