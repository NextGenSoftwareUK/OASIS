using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public class Chapter : QuestBase, IChapter
    {
        public Chapter()
        {
            this.HolonType = HolonType.Chapter;
        }

        public string ChapterDisplayName { get; set; } = "Chapter"; //Can be things like Act, Phase, Stage etc.

        public new string Status
        {
            get
            {
                return $"{ChapterDisplayName} {CurrentSubQuestNumber}/{SubQuests.Count}";
            }
        }

    }
}