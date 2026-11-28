using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    /// <summary>JSON filters accepted by POST /api/map/search.</summary>
    public sealed class MapSearchBody
    {
        public string? Query { get; set; }
        public LocationType? Type { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public double? RadiusKm { get; set; }
    }
}
