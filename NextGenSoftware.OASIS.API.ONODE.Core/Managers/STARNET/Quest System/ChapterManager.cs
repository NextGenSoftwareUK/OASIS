using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class ChapterManager : QuestManagerBase<Chapter, DownloadedChapter, InstalledChapter, ChapterDNA>
    public class ChapterManager : QuestManagerBase<Chapter, DownloadedChapter, InstalledChapter, STARNETDNA>
    {
        public ChapterManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, 
            OASISDNA,
            typeof(ChapterType),
            HolonType.Chapter,
            HolonType.InstalledChapter,
            "Chapter",
            "ChapterId",
            "ChapterName",
            "ChapterType",
            "ochapter",
            "oasis_chapters",
            "ChapterDNA.json",
            "ChapterDNAJSON")
        { }

        public ChapterManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(ChapterType),
            HolonType.Chapter,
            HolonType.InstalledChapter,
            "Chapter",
            "ChapterId",
            "ChapterName",
            "ChapterType",
            "ochapter",
            "oasis_chapters",
            "ChapterDNA.json",
            "ChapterDNAJSON")
        { }
    }
}