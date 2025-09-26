using System;
using System.Linq;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class QuestBase : TaskBase, IQuestBase
    {
        public QuestBase(string STARNETDNAJSONName = "QuestBaseDNAJSON") : base(STARNETDNAJSONName)
        {

        }

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
                    if (Quests != null && Quests.Count > 0)
                        return Quests.OrderBy(x => x.Order).FirstOrDefault(x => x.CompletedOn == DateTime.MinValue);
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
                return $"Quest {CurrentSubQuestNumber}/{Quests.Count}";
            }
        }

        [CustomOASISProperty()]
        public IList<IQuest> Quests { get; set; } //TODO: Dont think is needed now because it is stored in the Dependencies.
    }
}