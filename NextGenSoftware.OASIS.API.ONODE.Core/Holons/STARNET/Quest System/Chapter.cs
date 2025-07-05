using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class Chapter : QuestBase, IChapter
    {
        public Chapter() : base("ChapterDNAJSON")
        {
            this.HolonType = HolonType.Chapter;
        }

        public string ChapterDisplayName { get; set; } = "Chapter"; //Can be things like Act, Phase, Stage etc.

        public new string Status
        {
            get
            {
                return $"{ChapterDisplayName} {CurrentSubQuestNumber}/{Quests.Count}";
            }
        }

    }
}