using System;
using System.Linq;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class QuestBase : TaskBase, IQuestBase
    {
        [CustomOASISProperty()]
        public Guid ParentMissionId { get; set; }

        [CustomOASISProperty()]
        public int Order { get; set; } //The order that the quest's appear and need to be completed in (stages). Each stage/sub-quest can have 1 or more nfts and/or 1 or more hotspots assigned. Once they are all collected/visited/completed then that sub-quest is complete. Once all sub-quests are complete then the parent quest is complete and so on. Once all quests are complete then the mission is complete.


        public IQuest CurrentSubQuest
        {
            get
            {
                if (CompletedOn != DateTime.MinValue)
                {
                    if (SubQuests != null && SubQuests.Count > 0)
                        return SubQuests.OrderBy(x => x.Order).FirstOrDefault(x => x.CompletedOn == DateTime.MinValue);
                }

                return null;
            }
        }

        public int CurrentSubQuestNumber
        {
            get
            {
                if (CurrentSubQuest != null)
                    return CurrentSubQuest.Order;
                
                else return 0;
            }
        }

        public string Status
        {
            get
            {
                return $"Quest {CurrentSubQuestNumber}/{SubQuests.Count}";
            }
        }

        [CustomOASISProperty()]
        public IList<IQuest> SubQuests { get; set; }
    }
}