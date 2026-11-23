using System;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Geo;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps
{
    public abstract partial class MapProviderBase
    {
        /// <summary>Ground metres moved per unit of a pan value.</summary>
        public double MetresPerPanUnit { get; set; } = 1.0;

        public bool PanMapUp(float value) => Pan(0.0, value);
        public bool PanMapDown(float value) => Pan(180.0, value);
        public bool PanMapRight(float value) => Pan(90.0, value);
        public bool PanMapLeft(float value) => Pan(270.0, value);
        public bool ZoomMapIn(float value) => Zoom(value);
        public bool ZoomMapOut(float value) => Zoom(-value);

        /// <summary>Rotates the view to the given bearing in degrees.</summary>
        public bool UpdateOrbitValue(float value)
        {
            if (!Ready()) return false;
            if (!float.IsFinite(value)) return Fail("The orbit value must be a finite number of degrees.");

            MapCameraState next = Camera;
            next.Bearing = ((value % 360.0) + 360.0) % 360.0;
            return SetCamera(next);
        }

        /// <summary>Centres the map on a coordinate, optionally changing zoom.</summary>
        public bool CentreMapOn(Geolocation location, double? zoom = null)
        {
            if (!Ready()) return false;
            if (!GeoMath.IsValid(location)) return Fail("The centre must be a valid WGS84 coordinate.");

            MapCameraState next = Camera;
            next.Centre = new Geolocation(location.Latitude, location.Longitude);
            if (zoom.HasValue) next.Zoom = Math.Clamp(zoom.Value, MapCameraState.MinZoom, MapCameraState.MaxZoom);
            return SetCamera(next);
        }

        private bool Pan(double bearingDegrees, float value)
        {
            if (!Ready()) return false;
            if (!float.IsFinite(value)) return Fail("The pan value must be a finite number.");
            if (value == 0f) { LastError = null; return true; }

            MapCameraState next = Camera;
            double bearing = value < 0f ? (bearingDegrees + 180.0) % 360.0 : bearingDegrees;
            next.Centre = GeoMath.Offset(next.Centre, Math.Abs(value) * MetresPerPanUnit, bearing);
            return SetCamera(next);
        }

        private bool Zoom(float delta)
        {
            if (!Ready()) return false;
            if (!float.IsFinite(delta)) return Fail("The zoom value must be a finite number.");

            MapCameraState next = Camera;
            next.Zoom = Math.Clamp(next.Zoom + delta, MapCameraState.MinZoom, MapCameraState.MaxZoom);
            return SetCamera(next);
        }

        private bool SetCamera(MapCameraState next)
        {
            var command = new MapCommand(MapCommandType.SetCamera) { Location = next.Centre }
                .With("zoom", next.Zoom)
                .With("bearing", next.Bearing);

            if (!Dispatch(command)) return false;

            lock (_sync)
            {
                _camera.Centre = next.Centre;
                _camera.Zoom = next.Zoom;
                _camera.Bearing = next.Bearing;
            }

            return true;
        }
    }
}
