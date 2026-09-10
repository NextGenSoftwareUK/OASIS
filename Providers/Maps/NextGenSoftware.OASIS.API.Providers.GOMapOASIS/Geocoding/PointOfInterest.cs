using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.Geocoding
{
    /// <summary>A place returned by a geocoding search.</summary>
    public class PointOfInterest : IPointOfInterest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Geolocation Location { get; set; }
        public string Category { get; set; }

        /// <summary>Metres from the search point, when the search supplied one.</summary>
        public float Distance { get; set; }

        public override string ToString()
        {
            return Location != null ? Name + " (" + Location + ")" : Name;
        }
    }
}
