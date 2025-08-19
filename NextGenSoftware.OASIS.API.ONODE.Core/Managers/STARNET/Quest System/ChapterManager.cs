using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class ChapterManager : QuestManagerBase<Chapter, DownloadedChapter, InstalledChapter, ChapterDNA>
    public class ChapterManager : QuestManagerBase<Chapter, DownloadedChapter, InstalledChapter, STARNETDNA>
    {
        public ChapterManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(ChapterType),
            HolonType.Chapter,
            HolonType.InstalledChapter,
            "Chapter",
            //"ChapterId",
            "STARNETHolonId",
            "ChapterName",
            "ChapterType",
            "ochapter",
            "oasis_chapters",
            "ChapterDNA.json",
            "ChapterDNAJSON")
        { }

        public ChapterManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(ChapterType),
            HolonType.Chapter,
            HolonType.InstalledChapter,
            "Chapter",
            //"ChapterId",
            "STARNETHolonId",
            "ChapterName",
            "ChapterType",
            "ochapter",
            "oasis_chapters",
            "ChapterDNA.json",
            "ChapterDNAJSON")
        { }
    }
}