
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core
{
    public class QuestManager : OASISManager
    {
        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public QuestManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
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

        public bool CompleteQuest(Quest quest)
        {
            return true;
        }

        public bool DeleteQuest(Quest quest)
        {
            return true;
        }

        public bool HighlightQuestOnMap(Quest quest)
        {
            return true;
        }

        public Quest FindNearestQuestOnMap()
        {
            return new Quest();
        }

        public List<Quest> GetAllCurrentQuestsForAvatar(Avatar avatar)
        {
            return new List<Quest>();
        }
    }
}
