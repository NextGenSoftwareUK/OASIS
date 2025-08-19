using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    //public class MissionManager : QuestManagerBase<Mission, DownloadedMission, InstalledMission, MissionDNA>
    public class MissionManager : QuestManagerBase<Mission, DownloadedMission, InstalledMission, STARNETDNA>
    {
        public MissionManager(Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(avatarId,
            STARDNA,
            OASISDNA,
            typeof(MissionType),
            HolonType.Mission,
            HolonType.InstalledMission,
            "Mission",
            //"MissionId",
            "STARNETHolonId",
            "MissionName",
            "MissionType",
            "omission",
            "oasis_missions",
            "MissionDNA.json",
            "MissionDNAJSON")
        { }

        public MissionManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, STARDNA STARDNA, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            STARDNA,
            OASISDNA,
            typeof(MissionType),
            HolonType.Mission,
            HolonType.InstalledMission,
            "Mission",
            //"MissionId",
            "STARNETHolonId",
            "MissionName",
            "MissionType",
            "omission",
            "oasis_missions",
            "MissionDNA.json",
            "MissionDNAJSON")
        { }
    }
}