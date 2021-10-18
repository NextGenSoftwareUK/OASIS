using NextGenSoftware.OASIS.API.ONODE.BLL.Holons;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Managers
{
    public class QuestManager : OASISManager, IQuestManager
    {
        public QuestManager() : base()
        {

        }

        public bool CreateQuest(Quest quest)
        {
            return true;
        }

        public bool UpdateQuest(Quest quest)
        {
            return true;
        }

        public bool CompleteQuest(Guid questId)
        {
            return true;
        }

        public bool DeleteQuest(Guid questId)
        {
            return true;
        }

        public bool HighlightQuestOnMap(Guid questId)
        {
            return true;
        }

        public Quest FindNearestQuestOnMap()
        {
            return new Quest();
        }

        public List<Quest> GetAllCurrentQuestsForAvatar(Guid avatarId)
        {
            return new List<Quest>();
        }
    }
}
