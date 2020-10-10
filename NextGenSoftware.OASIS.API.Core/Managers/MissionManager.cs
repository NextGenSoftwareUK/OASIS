
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core
{
    public class MissionManager : OASISManager
    {
        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public MissionManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
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

        public bool CompleteMission(Mission mission)
        {
            return true;
        }

        public bool DeleteMission(Mission mission)
        {
            return true;
        }

        public IMissionData GetAllCurrentMissionsForAvatar(IAvatar avatar)
        {
            return new MissionData();
        }
    }
}
