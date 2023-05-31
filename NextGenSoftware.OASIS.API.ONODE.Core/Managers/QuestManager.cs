using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.BLL.Holons;
using NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Managers
{
    public class QuestManager : OASISManager, IQuestManager
    {
        public QuestManager(OASISDNA OASISDNA = null) : base(OASISDNA)
        {

        }

        public QuestManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
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
