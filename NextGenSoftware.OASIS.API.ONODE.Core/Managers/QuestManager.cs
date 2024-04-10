using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class QuestManager : OASISManager, IQuestManager
    {
        public QuestManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public QuestManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
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
