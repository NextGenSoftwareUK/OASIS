using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces
{
    public interface IAvatarRepository
    {
        OASISResult<Avatar> Add(Avatar avatar);
        Task<OASISResult<Avatar>> AddAsync(Avatar avatar);
        OASISResult<AvatarDetail> Add(AvatarDetail avatar);
        Task<OASISResult<AvatarDetail>> AddAsync(AvatarDetail avatar);
        OASISResult<bool> Delete(Guid id, bool softDelete = true);
        OASISResult<bool> Delete(string providerKey, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAsync(Guid id, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAsync(string providerKey, bool softDelete = true);
        OASISResult<Avatar> GetAvatar(Guid id);
        OASISResult<Avatar> GetAvatar(string username);
        //OASISResult<Avatar> GetAvatar(string username, string password);
        Task<OASISResult<Avatar>> GetAvatarAsync(Guid id);
        Task<OASISResult<Avatar>> GetAvatarAsync(string username);
        //Task<OASISResult<Avatar>> GetAvatarAsync(string username, string password);
        OASISResult<AvatarDetail> GetAvatarDetail(Guid id);
        OASISResult<AvatarDetail> GetAvatarDetail(string username);
        Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(Guid id);
        Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(string username);
        OASISResult<IEnumerable<Avatar>> GetAvatars();
        Task<OASISResult<IEnumerable<Avatar>>> GetAvatarsAsync();
        Task<OASISResult<IEnumerable<AvatarDetail>>> GetAvatarDetailsAsync();
        OASISResult<IEnumerable<AvatarDetail>> GetAvatarDetails();
        OASISResult<Avatar> Update(Avatar avatar);
        Task<OASISResult<Avatar>> UpdateAsync(Avatar avatar);
        OASISResult<AvatarDetail> Update(AvatarDetail avatar);
        Task<OASISResult<AvatarDetail>> UpdateAsync(AvatarDetail avatar);
    }
}