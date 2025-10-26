using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers
{
    public interface IMissionManager : ISTARNETManagerBase<Mission, DownloadedMission, InstalledMission, MissionDNA>
    {
        //bool CompleteMission(Guid missionId);
        //bool CreateMission(Mission mission);
        //bool DeleteMission(Guid missionId);
        //IList<IMission> GetAllCurrentMissionsForAvatar(Guid avatarId);
        //bool UpdateMission(Mission mission);
    }
}