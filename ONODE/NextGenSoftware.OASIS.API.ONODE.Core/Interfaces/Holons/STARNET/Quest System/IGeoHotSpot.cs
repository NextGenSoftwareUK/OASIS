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
        /// <summary>Embedded audio bytes when type is <see cref="GeoHotSpotType.Audio"/>; clients should prefer this over <see cref="AudioUrl"/> when non-null and non-empty.</summary>
        byte[] AudioData { get; set; }
        /// <summary>Audio URL when type is <see cref="GeoHotSpotType.Audio"/> (used when <see cref="AudioData"/> is null or empty).</summary>
        string AudioUrl { get; set; }
        /// <summary>Embedded video bytes when type is <see cref="GeoHotSpotType.Video"/>; prefer over <see cref="VideoUrl"/> when non-null and non-empty.</summary>
        byte[] VideoData { get; set; }
        /// <summary>Video URL when type is <see cref="GeoHotSpotType.Video"/> (used when <see cref="VideoData"/> is null or empty).</summary>
        string VideoUrl { get; set; }
        /// <summary>Text body when type is <see cref="GeoHotSpotType.Text"/>.</summary>
        string TextContent { get; set; }
        /// <summary>Website URL when type is <see cref="GeoHotSpotType.WebsiteLink"/>.</summary>
        string WebsiteUrl { get; set; }
    }
}