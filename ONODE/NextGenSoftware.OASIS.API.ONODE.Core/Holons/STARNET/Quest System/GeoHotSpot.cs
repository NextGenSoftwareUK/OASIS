using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class GeoHotSpot : STARNETHolon, IGeoHotSpot
    {
        public GeoHotSpot() : base("GeoHotSpotDNAJSON")
        {
            this.HolonType = HolonType.GeoHotSpot;
        }

        [CustomOASISProperty]
        public GeoHotSpotTriggeredType TriggerType { get; set; } = GeoHotSpotTriggeredType.WhenArrivedAtGeoLocation;

        [CustomOASISProperty]
        public int TimeInSecondsNeedToBeAtLocationToTriggerHotSpot { get; set; } = 3; //Optional (only applicable if TriggerType is WhenAtGeoLocationForXSeconds).

        [CustomOASISProperty]
        public double Lat { get; set; }

        [CustomOASISProperty]
        public double Long { get; set; }

        [CustomOASISProperty]
        public int HotSpotRadiusInMetres { get; set; } = 10; //The user/avatar needs to be within this radius to trigger the hotspot.

        [CustomOASISProperty]
        public byte[] Object3D { get; set; } //If TriggerType is WhenLookingAtObjectOrImageInARMode or WhenObjectOrImageIsTouchedInARMode then this will appear once they enter AR Mode otherwise it will appear on the map.

        [CustomOASISProperty]
        public Uri Object3DURI { get; set; } //If TriggerType is WhenLookingAtObjectOrImageInARMode or WhenObjectOrImageIsTouchedInARMode then this will appear once they enter AR Mode otherwise it will appear on the map.

        [CustomOASISProperty]
        public byte[] Image2D { get; set; } //If TriggerType is WhenLookingAtObjectOrImageInARMode or WhenObjectOrImageIsTouchedInARMode then this will appear once they enter AR Mode otherwise it will appear on the map.

        [CustomOASISProperty]
        public Uri Image2DURI { get; set; } //If TriggerType is WhenLookingAtObjectOrImageInARMode or WhenObjectOrImageIsTouchedInARMode then this will appear once they enter AR Mode otherwise it will appear on the map.


        [CustomOASISProperty]
        public int TimeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot { get; set; } = 3;

        [CustomOASISProperty]
        public IList<IInventoryItem> Rewards {get; set;} //The item that is rewarded once the hotspot has been triggered.

        [CustomOASISProperty()]
        public IList<string> RewardIds { get; set; }
    }
}