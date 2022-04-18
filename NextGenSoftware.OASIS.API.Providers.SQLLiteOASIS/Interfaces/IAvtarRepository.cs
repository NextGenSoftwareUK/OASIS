using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Interfaces
{
    public interface IAvtarRepository
    {
        //Task<List<AvatarEntity>> GetAvtars();
        //Task<AvatarEntity> GetAvtarById(Guid avtarId);
        //Task<AvatarEntity> CreateAvtar(AvatarEntity request);
        //Task<AvatarEntity> UpdateAvtar(AvatarEntity request);
        //Task<bool> DeleteAvtrarById(Guid avtarId);

        bool DeleteAvatar(Guid id, bool softDelete = true);
        bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true);

        bool DeleteAvatarByUsername(string avatarUsername, bool softDelete = true);

        Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true);

        bool DeleteAvatar(string providerKey, bool softDelete = true);

        Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true);

        Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true);

        Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true);

        List<AvatarEntity> LoadAvatar(string username, string password, int version = 0);

        List<AvatarEntity> LoadAvatar(string username, int version = 0);

        Task<List<AvatarEntity>> LoadAvatarAsync(string username, string password, int version = 0);

        Task<List<AvatarEntity>> LoadAllAvatarsAsync(int version = 0);

        List<AvatarEntity> LoadAllAvatars(int version = 0);

        AvatarEntity LoadAvatarByUsername(string avatarUsername, int version = 0);

        Task<List<AvatarEntity>> LoadAvatarAsync(Guid Id, int version = 0);

        Task<List<AvatarEntity>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0);

        Task<List<AvatarEntity>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0);

        List<AvatarEntity> LoadAvatar(Guid Id, int version = 0);

        List<AvatarEntity> LoadAvatarByEmail(string avatarEmail, int version = 0);

        Task<List<AvatarEntity>> LoadAvatarAsync(string username, int version = 0);

        Task<List<AvatarEntity>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0);

        List<AvatarEntity> LoadAvatarForProviderKey(string providerKey, int version = 0);
        AvatarEntity SaveAvatar(AvatarEntity avatar);
        Task<AvatarEntity> SaveAvatarAsync(AvatarEntity Avatar);
    }
}