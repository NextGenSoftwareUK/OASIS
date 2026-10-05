using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface IQuestBase : ITaskBase
    {
        public Guid ParentMissionId { get; set; }
        public int Order { get; set; } //The order that the quest's appear and need to be completed in (stages). Each stage/sub-quest can have 1 or more nfts and/or 1 or more hotspots assigned. Once they are all collected/visited/completed then that sub-quest is complete. Once all sub-quests are complete then the parent quest is complete and so on. Once all quests are complete then the mission is complete.
        public IQuest CurrentSubQuest { get; }

        public int CurrentSubQuestNumber { get; }
        public string SubQuestStatus { get; }
        public QuestStatus Status { get; set; }
        public QuestType Type { get; set; }
        public QuestDifficulty Difficulty { get; set; }
        public long RewardKarma { get; set; }
        public long RewardXP { get; set; }
        public List<string> Requirements { get; set; }
        public string CompletionNotes { get; set; }
        public IList<IQuest> Quests { get; set; } //TODO: Dont think is needed now because it is stored in the Dependencies.
    }
}