using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Models
{
    /// <summary>Where the map is centred and how it is viewed.</summary>
    public sealed class MapCameraState
    {
        public const double MinZoom = 0.0;
        public const double MaxZoom = 22.0;

        public Geolocation Centre { get; set; } = new Geolocation(0.0, 0.0);
        public double Zoom { get; set; } = 16.0;

        /// <summary>Rotation of the view about the centre, in degrees clockwise from north.</summary>
        public double Bearing { get; set; }

        public MapCameraState Clone() => new MapCameraState
        {
            Centre = new Geolocation(Centre.Latitude, Centre.Longitude),
            Zoom = Zoom,
            Bearing = Bearing
        };
    }
}
