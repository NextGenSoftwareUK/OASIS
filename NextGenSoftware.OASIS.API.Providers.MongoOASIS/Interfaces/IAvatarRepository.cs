using NextGenSoftware.OASIS.API.Core.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces
{
    public interface IAvatarRepository
    {
        Avatar Add(Avatar avatar);
        Task<Avatar> AddAsync(Avatar avatar);
        AvatarDetail Add(AvatarDetail avatar);
        Task<AvatarDetail> AddAsync(AvatarDetail avatar);
        OASISResult<bool> Delete(Guid id, bool softDelete = true);
        OASISResult<bool> Delete(string providerKey, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAsync(Guid id, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAsync(string providerKey, bool softDelete = true);
        Avatar GetAvatar(Guid id);
        Avatar GetAvatar(string username);
        Avatar GetAvatar(string username, string password);
        Task<Avatar> GetAvatarAsync(Guid id);
        Task<Avatar> GetAvatarAsync(string username);
        Task<Avatar> GetAvatarAsync(string username, string password);
        OASISResult<AvatarDetail> GetAvatarDetail(Guid id);
        OASISResult<AvatarDetail> GetAvatarDetail(string username);
        Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(Guid id);
        Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(string username);
        List<Avatar> GetAvatars();
        Task<List<Avatar>> GetAvatarsAsync();
        Task<IEnumerable<AvatarDetail>> GetAvatarDetailsAsync();
        IEnumerable<AvatarDetail> GetAvatarDetails();
        Avatar Update(Avatar avatar);
        Task<Avatar> UpdateAsync(Avatar avatar);
        AvatarDetail Update(AvatarDetail avatar);
        Task<AvatarDetail> UpdateAsync(AvatarDetail avatar);
    }
}