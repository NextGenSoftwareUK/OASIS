using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Models
{
    /// <summary>
    /// Where the map is currently looking. Pans and zooms mutate this, so a client
    /// attaching mid-session can restore the exact view rather than starting at the
    /// map origin.
    /// </summary>
    public class GOMapCameraState
    {
        /// <summary>GO Map's own zoom range.</summary>
        public const int MinZoom = 1;
        public const int MaxZoom = 21;

        public Geolocation Centre { get; set; }
        public double Zoom { get; set; }

        /// <summary>Orbit angle in degrees, as driven by GO Map's GOOrbit component.</summary>
        public double Orbit { get; set; }

        public GOMapCameraState()
        {
            Centre = new Geolocation(0.0, 0.0);
            Zoom = 16.0;
            Orbit = 0.0;
        }
    }
}
