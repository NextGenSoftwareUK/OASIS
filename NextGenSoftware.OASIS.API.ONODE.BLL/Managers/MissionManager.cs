//using System;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Interfaces;

//namespace NextGenSoftware.OASIS.API.ONODE.BLL.Managers
//{
//    public class MissionManager : OASISManager
//    {
//        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
//        public MissionManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
//        {

//        }

//        public bool CreateMission(Mission mission)
//        {
//            return true;
//        }

//        public bool UpdateMission(Mission mission)
//        {
//            return true;
//        }

//        public bool CompleteMission(Guid missionId)
//        {
//            return true;
//        }

//        public bool DeleteMission(Guid missionId)
//        {
//            return true;
//        }

//        public IMissionData GetAllCurrentMissionsForAvatar(Guid avatarId)
//        {
//            return new MissionData();
//        }
//    }
//}
