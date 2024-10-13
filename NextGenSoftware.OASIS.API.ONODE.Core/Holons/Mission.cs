using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class Mission : Holon, IMission
    {
        public Mission()
        {
            this.HolonType = HolonType.Mission; 
        }

        //public IList<IQuest> Quests = new List<IQuest>();

        //public DateTime StartedOn { get; set; }
        //public Guid StartedBy { get; set; }
        //public DateTime CompletedOn { get; set; }
        //public Guid CompletedBy { get; set; }
    }
}