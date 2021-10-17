//using System;
//using System.Collections.Generic;
//using NextGenSoftware.OASIS.API.Core.Interfaces;

//namespace NextGenSoftware.OASIS.API.ONODE.BLL.Managers
//{
//    public class QuestManager : OASISManager
//    {
//        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
//        public QuestManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
//        {

//        }

//        public bool CreateQuest(Quest quest)
//        {
//            return true;
//        }

//        public bool UpdateQuest(Quest quest)
//        {
//            return true;
//        }

//        public bool CompleteQuest(Guid questId)
//        {
//            return true;
//        }

//        public bool DeleteQuest(Guid questId)
//        {
//            return true;
//        }

//        public bool HighlightQuestOnMap(Guid questId)
//        {
//            return true;
//        }

//        public Quest FindNearestQuestOnMap()
//        {
//            return new Quest();
//        }

//        public List<Quest> GetAllCurrentQuestsForAvatar(Guid avatarId)
//        {
//            return new List<Quest>();
//        }
//    }
//}
