using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class Building : Holon, IBuilding
    {
        public Building()
        {
            this.HolonType = HolonType.Building; 
        }
    }
}
