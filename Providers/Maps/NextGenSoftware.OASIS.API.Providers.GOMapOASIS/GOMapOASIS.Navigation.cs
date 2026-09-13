using System;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Bridge;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geo;
using NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Models;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS
{
    /// <summary>
    /// Camera movement. Pans and zooms move the provider's own camera state - so the
    /// map centre is always known and a reconnecting client can restore the exact
    /// view - as well as being queued for and forwarded to the GO Map client.
    /// </summary>
    public partial class GOMapOASIS
    {
        /// <summary>
        /// How far one unit of pan moves the map, in metres. GO Map pans in world
        /// units, and the provider's default scale is one unit per metre.
        /// </summary>
        public double MetresPerPanUnit { get; set; } = 1.0;

        public bool PanMapUp(float value) => Pan(0.0, value);

        public bool PanMapDown(float value) => Pan(180.0, value);

        public bool PanMapRight(float value) => Pan(90.0, value);

        public bool PanMapLeft(float value) => Pan(270.0, value);

        public bool ZoomMapIn(float value) => Zoom(value);

        public bool ZoomMapOut(float value) => Zoom(-value);

        public bool UpdateOrbitValue(float value)
        {
            if (!IsInitialized) return false;

            lock (_cameraLock)
            {
                Camera.Orbit = ((value % 360.0f) + 360.0) % 360.0;
            }

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.SetOrbit)
                .With("value", value)
                .With("orbit", Camera.Orbit));

            command.AppliedToLiveMap = Bridge.TryInvoke("UpdateValue", new object[] { value }, out _);
            return true;
        }

        /// <summary>Centres the map on a coordinate without changing the zoom.</summary>
        public bool CentreMapOn(Geolocation location)
        {
            if (!IsInitialized || !GeoMath.IsValid(location)) return false;

            lock (_cameraLock)
            {
                Camera.Centre = new Geolocation(location.Latitude, location.Longitude);
            }

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.ZoomToLocation)
            {
                Location = location
            }.With("zoom", Camera.Zoom));

            object coordinate = Bridge.CreateCoordinate(location.Latitude, location.Longitude);
            if (coordinate != null)
                command.AppliedToLiveMap = Bridge.TryInvoke("centerMap", new object[] { coordinate }, out _);

            return true;
        }

        private bool Pan(double bearingDegrees, float value)
        {
            if (!IsInitialized) return false;

            // A pan of zero is a no-op, and a negative pan is the opposite direction.
            if (Math.Abs(value) < double.Epsilon) return true;

            double distance = Math.Abs(value) * MetresPerPanUnit;
            double bearing = value < 0.0f ? (bearingDegrees + 180.0) % 360.0 : bearingDegrees;

            Geolocation centre;

            lock (_cameraLock)
            {
                Geolocation from = Camera.Centre ?? Origin ?? new Geolocation(0.0, 0.0);
                centre = GeoMath.Offset(from, distance, bearing);
                Camera.Centre = centre;
            }

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.Pan)
            {
                Location = centre
            }
            .With("value", value)
            .With("bearing", bearing)
            .With("distanceMetres", distance));

            command.AppliedToLiveMap = Bridge.TryInvoke("panMap", new object[] { bearing, distance }, out _);
            return true;
        }

        private bool Zoom(float delta)
        {
            if (!IsInitialized) return false;

            double zoom;

            lock (_cameraLock)
            {
                zoom = Camera.Zoom + delta;

                if (zoom < GOMapCameraState.MinZoom) zoom = GOMapCameraState.MinZoom;
                if (zoom > GOMapCameraState.MaxZoom) zoom = GOMapCameraState.MaxZoom;

                Camera.Zoom = zoom;
            }

            GOMapCommand command = Enqueue(new GOMapCommand(GOMapCommandType.Zoom)
            {
                Location = Camera.Centre
            }
            .With("delta", delta)
            .With("zoom", zoom));

            command.AppliedToLiveMap = Bridge.TryInvoke("setZoom", new object[] { zoom }, out _);
            return true;
        }
    }
}
