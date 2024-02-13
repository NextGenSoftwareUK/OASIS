using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces
{
    public interface IAvatarRepository
    {
        OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true);
        OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true);

        OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true);

        Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true);

        OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true);

        Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true);

        Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true);

        Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true);

        OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0);

        OASISResult<IAvatar> LoadAvatar(string username, int version = 0);

        Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0);

        Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0);

        OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0);

        OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0);

        Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0);

        Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0);

        Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0);

        OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0);

        OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0);

        Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0);

        Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0);

        OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0);
        OASISResult<IAvatar> SaveAvatar(IAvatar avatar);
        Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar);
    }
}