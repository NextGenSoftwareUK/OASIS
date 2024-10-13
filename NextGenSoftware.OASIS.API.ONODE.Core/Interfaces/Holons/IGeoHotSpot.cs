using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IGeoHotSpot : IHolon
    {
        bool IsARHotSpot { get; set; }
        string ThreeDObject { get; set; }
        string TwoDSprite { get; set; }
        GeoHotSpotTriggeredType TriggerType { get; set; }
    }
}