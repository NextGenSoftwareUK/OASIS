using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteOASIS.Interfaces
{
    public interface IAvtarDetailRepository
    {
        //Task<List<AvatarDetailEntity>> GetAvtarDetails();
        //Task<AvatarDetailEntity> GetAvtarDetailById(Guid avtarDetailId);
        //Task<AvatarDetailEntity> CreateAvtarDetail(AvatarDetailEntity request);
        //Task<AvatarDetailEntity> UpdateAvtarDetail(AvatarDetailEntity request);
        //Task<bool> DeleteAvtarDetailById(Guid avtarDetailId);


        List<AvatarDetailEntity> LoadAvatarDetail(Guid id, int version = 0);

        List<AvatarDetailEntity> LoadAvatarDetailByEmail(string avatarEmail, int version = 0);

        List<AvatarDetailEntity> LoadAvatarDetailByUsername(string avatarUsername, int version = 0);

        Task<List<AvatarDetailEntity>> LoadAvatarDetailAsync(Guid id, int version = 0);

        Task<List<AvatarDetailEntity>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0);

        Task<List<AvatarDetailEntity>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0);

        List<AvatarDetailEntity> LoadAllAvatarDetails(int version = 0);

        Task<List<AvatarDetailEntity>> LoadAllAvatarDetailsAsync(int version = 0);

        AvatarDetailEntity SaveAvatarDetail(AvatarDetailEntity AvatarDetailEntity);

        Task<AvatarDetailEntity> SaveAvatarDetailAsync(AvatarDetailEntity AvatarDetailEntity);
    }
}
