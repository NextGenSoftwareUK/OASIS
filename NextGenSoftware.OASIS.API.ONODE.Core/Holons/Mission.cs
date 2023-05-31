using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Holons
{
    public class Mission : Holon, IMission
    {
        public Mission()
        {
            this.HolonType = HolonType.Mission; 
        }

        public List<Quest> Quests = new List<Quest>();
    }
}