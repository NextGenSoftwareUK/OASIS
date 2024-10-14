using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class Mission : TaskBase, IMission
    {
        public Mission()
        {
            this.HolonType = HolonType.Mission; 
        }

        public IEnumerable<IQuest> Quests = new List<IQuest>();
        public IEnumerable<IChapter> Chapters = new List<IChapter>(); //optional (large collection of quests can be broken into chapters.)
    }
}