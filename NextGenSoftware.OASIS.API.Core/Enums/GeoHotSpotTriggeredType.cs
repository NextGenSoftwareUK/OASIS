
namespace NextGenSoftware.OASIS.API.Core.Enums
{
    public enum GeoHotSpotTriggeredType
    {
        WhenArrivedAtGeoLocation,
        WhenLookingAtObjectOrSprite, //Need to look for 3 seconds to trigger it.
        WhenObjectOrSpriteIsTouched, //If in AR Mode this means you need to reach out with your hand and touch it, otherwise it means tapping on it on the map.
    }
}