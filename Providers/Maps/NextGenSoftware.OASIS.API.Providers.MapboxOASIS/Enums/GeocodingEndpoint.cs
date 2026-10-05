using System.ComponentModel;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums
{
    public enum GeocodingEndpoint
    {
        [Description("mapbox.places")]
        MapboxPlaces,
        [Description("mapbox.places-permanent")]
        MapboxPlacesPermanent
    }
}