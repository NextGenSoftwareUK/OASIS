using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IGeoHotSpot : ISTARNETHolon
    {
        GeoHotSpotTriggeredType TriggerType { get; set; }
        double Lat { get; set; }
        double Long { get; set; }
        int HotSpotRadiusInMetres { get; set; }
        int TimeInSecondsNeedToBeAtLocationToTriggerHotSpot { get; set; } //Optional (only applicable if TriggerType is WhenAtGeoLocationForXSeconds).
        int TimeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot { get; set; }
        byte[] Object3D { get; set; } //If TriggerType is WhenLookingAtObjectOrSpriteInARMode or WhenObjectOrSpriteIsTouchedInARMode then this will appear once they enter AR Mode otherwise it will appear on the map.
        byte[] Image2D { get; set; } //If TriggerType is WhenLookingAtObjectOrSpriteInARMode or WhenObjectOrSpriteIsTouchedInARMode then this will appear once they enter AR Mode otherwise it will appear on the map.
        IList<IInventoryItem> Rewards { get; set; } //The item that is rewarded once the hotspot has been triggered.
    }
}