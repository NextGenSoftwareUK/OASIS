using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Interfaces
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