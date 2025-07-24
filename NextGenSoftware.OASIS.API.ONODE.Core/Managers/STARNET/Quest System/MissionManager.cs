using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class MissionManager : QuestManagerBase<Mission, DownloadedMission, InstalledMission, MissionDNA>
    {
        public MissionManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId,
            OASISDNA,
            typeof(MissionType),
            HolonType.Mission,
            HolonType.InstalledMission,
            "Mission",
            "MissionId",
            "MissionName",
            "MissionType",
            "omission",
            "oasis_missions",
            "MissionDNA.json",
            "MissionDNAJSON")
        { }

        public MissionManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId,
            OASISDNA,
            typeof(MissionType),
            HolonType.Mission,
            HolonType.InstalledMission,
            "Mission",
            "MissionId",
            "MissionName",
            "MissionType",
            "omission",
            "oasis_missions",
            "MissionDNA.json",
            "MissionDNAJSON")
        { }
    }
}