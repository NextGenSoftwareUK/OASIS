using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces
{
    public interface IAvatarDetailRepository
    {
        OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0);

        OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0);

        OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0);

        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0);

        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0);

        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0);

        OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0);

        Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0);

        OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail AvatarDetailEntity);

        Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail AvatarDetailEntity);
    }
}
