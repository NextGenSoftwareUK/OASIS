using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
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