using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces
{
    public interface IQuestManager : IOASISManager
    {
        bool CompleteQuest(Guid questId);
        bool CreateQuest(IQuest quest);
        bool DeleteQuest(Guid questId);
        IQuest FindNearestQuestOnMap();
        List<IQuest> GetAllCurrentQuestsForAvatar(Guid avatarId);
        bool HighlightQuestOnMap(Guid questId);
        bool UpdateQuest(IQuest quest);
    }
}