using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class GeoHotSpot : Holon, IGeoHotSpot
    {
        public GeoHotSpot()
        {
            this.HolonType = HolonType.GeoHotSpot;
        }

        [CustomOASISProperty]
        public bool IsARHotSpot { get; set; } //If this is false then this will be triggered as soon as they reach the hotspot, otherwise it will only be triggered once they activate AR mode in that area unless they have to interact with a 3D object or enviroment (such as collecting a dino etc).

        [CustomOASISProperty]
        public GeoHotSpotTriggeredType TriggerType { get; set; } = GeoHotSpotTriggeredType.WhenLookingAtObjectOrSprite;

        [CustomOASISProperty]
        public string ThreeDObject { get; set; } //If IsARHotSpot is true then this will appear once they enter AR Mode otherwise it will appear on the map.

        [CustomOASISProperty]
        public string TwoDSprite { get; set; } //If IsARHotSpot is true then this will appear once they enter AR Mode otherwise it will appear on the map.

        [CustomOASISProperty]
        public IInventoryItem Reward {get; set;} //The item that is rewarded once the hotspot has been triggered.
    }
}