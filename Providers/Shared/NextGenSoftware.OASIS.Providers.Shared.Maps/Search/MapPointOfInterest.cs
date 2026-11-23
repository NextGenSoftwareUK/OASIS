using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Search
{
    public sealed class MapPointOfInterest : IPointOfInterest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public Geolocation Location { get; set; }
        public string Category { get; set; }

        /// <summary>Metres from the search's bias point, or 0 when the search had none.</summary>
        public float Distance { get; set; }

        /// <summary>The service that returned the result, e.g. "google-places".</summary>
        public string Source { get; set; }

        public override string ToString() => Location == null ? Name : Name + " (" + Location + ")";
    }
}
