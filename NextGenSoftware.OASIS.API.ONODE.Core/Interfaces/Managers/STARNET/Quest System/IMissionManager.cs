using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers.Base;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface IMissionManager : ISTARNETManagerBase<Mission, DownloadedMission, InstalledMission>
    {
        //bool CompleteMission(Guid missionId);
        //bool CreateMission(Mission mission);
        //bool DeleteMission(Guid missionId);
        //IList<IMission> GetAllCurrentMissionsForAvatar(Guid avatarId);
        //bool UpdateMission(Mission mission);
    }
}