using System;
using NextGenSoftware.OASIS.API.ONODE.BLL.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.BLL.Interfaces
{
    public interface IMissionManager : IOASISManager
    {
        bool CompleteMission(Guid missionId);
        bool CreateMission(Mission mission);
        bool DeleteMission(Guid missionId);
        IMissionData GetAllCurrentMissionsForAvatar(Guid avatarId);
        bool UpdateMission(Mission mission);
    }
}