using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class Mission : QuestBase, IMission, IQuestBase, ISTARHolon
    {
        public Mission()
        {
            this.HolonType = HolonType.Mission; 
        }

        [CustomOASISProperty]
        public IList<IChapter> Chapters { get; set; } = new List<IChapter>(); //optional (large collection of quests can be broken into chapters.)
    }
}