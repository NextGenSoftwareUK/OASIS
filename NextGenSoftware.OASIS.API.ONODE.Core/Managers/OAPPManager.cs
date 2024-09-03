using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public class OAPPManager : OASISManager//, INFTManager
    {
        private static OAPPManager _instance = null;

        public OAPPManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null) : base(OASISStorageProvider, avatarId, OASISDNA)
        {

        }

        public OAPPManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {

        }

        public async Task<OASISResult<List<IOAPP>>> ListOAPPsCreatedByAvatarAsync(Guid avatarId)
        {
            return new OASISResult<List<IOAPP>>();
        }

        public OASISResult<List<IOAPP>> ListOAPPsCreatedByAvatar(Guid avatarId)
        {
            return new OASISResult<List<IOAPP>>();
        }

        public async Task<OASISResult<List<IOAPP>>> ListOAPPsInstalledByAvatarAsync(Guid avatarId)
        {
            return new OASISResult<List<IOAPP>>();
        }

        public OASISResult<List<IOAPP>> ListOAPPsInstalledByAvatar(Guid avatarId)
        {
            return new OASISResult<List<IOAPP>>();
        }

        public async Task<OASISResult<List<IOAPP>>> ListAllOAPPsAsync()
        {
            return new OASISResult<List<IOAPP>>();
        }

        public OASISResult<List<IOAPP>> ListAllOAPPs()
        {
            return new OASISResult<List<IOAPP>>();
        }

        public async Task<OASISResult<IOAPP>> LoadOAPPAsync(Guid OAPPId)
        {
            return new OASISResult<IOAPP>();
        }

        public OASISResult<IOAPP> LoadOAPP(Guid OAPPId)
        {
            return new OASISResult<IOAPP>();
        }

        public async Task<OASISResult<IOAPP>> SaveOAPPAsync()
        {
            return new OASISResult<IOAPP>();
        }

        public OASISResult<IOAPP> SaveOAPP()
        {
            return new OASISResult<IOAPP>();
        }
    }
}