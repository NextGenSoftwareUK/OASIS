using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class MissionManager : OASISManager, IMissionManager
    {
        public MissionManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public MissionManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public bool CreateMission(Mission mission)
        {
            return true;
        }

        public bool UpdateMission(Mission mission)
        {
            return true;
        }

        public bool CompleteMission(Guid missionId)
        {
            return true;
        }

        public bool DeleteMission(Guid missionId)
        {
            return true;
        }

        public IMissionData GetAllCurrentMissionsForAvatar(Guid avatarId)
        {
            return new MissionData();
        }
    }
}
