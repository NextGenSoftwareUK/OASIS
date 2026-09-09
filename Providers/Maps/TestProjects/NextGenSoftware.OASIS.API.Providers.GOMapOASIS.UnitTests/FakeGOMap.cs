namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.UnitTests
{
    /// <summary>
    /// Stands in for the Unity GOMap component. It mirrors the member names and
    /// shapes the bridge reflects over - lower-cased methods, a locationManager with
    /// a currentLocation - so the forwarding path can be tested without Unity.
    /// </summary>
    public class FakeGOMap
    {
        public double Zoom { get; private set; } = 16.0;
        public int DroppedPins { get; private set; }
        public int RemovedPins { get; private set; }
        public object? LastSelected { get; private set; }

        public FakeGOMapLocationManager locationManager { get; } = new FakeGOMapLocationManager();

        public void setZoom(double zoom) => Zoom = zoom;

        public void panMap(double bearing, double distance) { }

        public void dropPin(object coordinate, object gameObject) => DroppedPins++;

        public void removePin(object gameObject) => RemovedPins++;

        public void selectObject(object target) => LastSelected = target;

        public void centerMap(object coordinate) { }
    }
}
