using System;
using NextGenSoftware.OASIS.API.ONODE.BLL.Holons;
using NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Managers
{
    public class MissionManager : OASISManager, IMissionManager
    {
        public MissionManager() : base()
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
