
namespace NextGenSoftware.OASIS.API.Core.Enums
{
    public enum GeoHotSpotType
    {
        Map,
        AR,
        VR,
        IR, //If in AR Mode this means you need to reach out with your hand and touch it, otherwise it means tapping on it on the map.
        /// <summary>Play streamed or file audio when the hotspot is triggered (see <c>AudioUrl</c> on the holon).</summary>
        Audio,
        /// <summary>Play video (stream or file) when triggered (see <c>VideoUrl</c>).</summary>
        Video,
        /// <summary>Show authored text (see <c>TextContent</c>).</summary>
        Text,
        /// <summary>Open a website / deep link when triggered (see <c>WebsiteUrl</c>).</summary>
        WebsiteLink
    }
}