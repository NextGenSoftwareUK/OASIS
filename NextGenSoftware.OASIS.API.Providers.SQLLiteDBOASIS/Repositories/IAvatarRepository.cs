using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Repositories{

    public interface IAvatarRepository
    {
        OASISResult<Avatar> Add(Avatar avatar);
        Task<OASISResult<Avatar>> AddAsync(Avatar avatar);
        OASISResult<AvatarDetail> Add(AvatarDetail avatar);
        Task<OASISResult<AvatarDetail>> AddAsync(AvatarDetail avatar);
        OASISResult<bool> Delete(Guid id, bool softDelete = true);
        OASISResult<bool> Delete(string userName, bool softDelete = true);
        OASISResult<bool> DeleteByEmail(string avatarEmail, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAsync(Guid id, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAsync(string userName, bool softDelete = true);
        Task<OASISResult<bool>> DeleteByEmailAsync(string avatarEmail, bool softDelete = true);
        OASISResult<Avatar> GetAvatar(Guid id);
        OASISResult<Avatar> GetAvatar(string username);
        OASISResult<Avatar> GetAvatar(string username, string password);
        Task<OASISResult<Avatar>> GetAvatarAsync(Guid id);
        Task<OASISResult<Avatar>> GetAvatarAsync(string username);
        OASISResult<Avatar> GetAvatarByEmail(string avatarEmail);
        Task<OASISResult<Avatar>> GetAvatarByEmailAsync(string avatarEmail);
        Task<OASISResult<Avatar>> GetAvatarAsync(string username, string password);
        OASISResult<List<Avatar>> GetAvatars();
        Task<OASISResult<List<Avatar>>> GetAvatarsAsync();
        OASISResult<AvatarDetail> GetAvatarDetail(Guid id);
        OASISResult<AvatarDetail> GetAvatarDetail(string username);
        OASISResult<AvatarDetail> GetAvatarDetailByEmail(string avatarEmail);
        Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(Guid id);
        Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(string username);
        Task<OASISResult<AvatarDetail>> GetAvatarDetailByEmailAsync(string avatarEmail);
        Task<OASISResult<IEnumerable<AvatarDetail>>> GetAvatarDetailsAsync();
        OASISResult<IEnumerable<AvatarDetail>> GetAvatarDetails();
        OASISResult<Avatar> Update(Avatar avatar);
        Task<OASISResult<Avatar>> UpdateAsync(Avatar avatar);
        OASISResult<AvatarDetail> Update(AvatarDetail avatar);
        Task<OASISResult<AvatarDetail>> UpdateAsync(AvatarDetail avatar);
    }
}