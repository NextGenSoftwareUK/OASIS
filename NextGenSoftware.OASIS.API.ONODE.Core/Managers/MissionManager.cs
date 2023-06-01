using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class MissionManager : OASISManager, IMissionManager
    {
        public MissionManager(OASISDNA OASISDNA = null) : base(OASISDNA)
        {

        }

        public MissionManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
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
