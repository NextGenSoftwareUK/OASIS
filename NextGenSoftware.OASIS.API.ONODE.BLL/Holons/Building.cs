using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Holons
{
    public class Building : Holon, IBuilding
    {
        public Building()
        {
            this.HolonType = HolonType.Building; 
        }
    }
}
