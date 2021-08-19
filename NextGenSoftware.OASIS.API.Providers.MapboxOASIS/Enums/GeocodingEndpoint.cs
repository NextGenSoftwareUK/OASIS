using System.ComponentModel;

namespace Enums
{
    public enum GeocodingEndpoint
    {
        [Description("mapbox.places")]
        MapboxPlaces,
        [Description("mapbox.places-permanent")]
        MapboxPlacesPermanent
    }
}