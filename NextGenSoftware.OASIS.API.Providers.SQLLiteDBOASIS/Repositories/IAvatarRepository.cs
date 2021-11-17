using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Repositories{

    public interface IAvatarRepository
    {
        OASISResult<IAvatar> Add(IAvatar avatar);
        Task<OASISResult<IAvatar>> AddAsync(IAvatar avatar);
        OASISResult<IAvatarDetail> Add(IAvatarDetail avatar);
        Task<OASISResult<IAvatarDetail>> AddAsync(IAvatarDetail avatar);
        OASISResult<bool> Delete(Guid id, bool softDelete = true);
        OASISResult<bool> Delete(string userName, bool softDelete = true);
        OASISResult<bool> DeleteByEmail(string avatarEmail, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAsync(Guid id, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAsync(string userName, bool softDelete = true);
        Task<OASISResult<bool>> DeleteByEmailAsync(string avatarEmail, bool softDelete = true);
        OASISResult<IAvatar> GetAvatar(Guid id);
        OASISResult<IAvatar> GetAvatar(string username);
        OASISResult<IAvatar> GetAvatar(string username, string password);
        Task<OASISResult<IAvatar>> GetAvatarAsync(Guid id);
        Task<OASISResult<IAvatar>> GetAvatarAsync(string username);
        OASISResult<IAvatar> GetAvatarByEmail(string avatarEmail);
        Task<OASISResult<IAvatar>> GetAvatarByEmailAsync(string avatarEmail);
        Task<OASISResult<IAvatar>> GetAvatarAsync(string username, string password);
        OASISResult<IEnumerable<IAvatar>> GetAvatars();
        Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsAsync();
        OASISResult<IAvatarDetail> GetAvatarDetail(Guid id);
        OASISResult<IAvatarDetail> GetAvatarDetail(string username);
        OASISResult<IAvatarDetail> GetAvatarDetailByEmail(string avatarEmail);
        Task<OASISResult<IAvatarDetail>> GetAvatarDetailAsync(Guid id);
        Task<OASISResult<IAvatarDetail>> GetAvatarDetailAsync(string username);
        Task<OASISResult<IAvatarDetail>> GetAvatarDetailByEmailAsync(string avatarEmail);
        Task<OASISResult<IEnumerable<IAvatarDetail>>> GetAvatarDetailsAsync();
        OASISResult<IEnumerable<IAvatarDetail>> GetAvatarDetails();
        OASISResult<IAvatar> Update(IAvatar avatar);
        Task<OASISResult<IAvatar>> UpdateAsync(IAvatar avatar);
        OASISResult<IAvatarDetail> Update(IAvatarDetail avatar);
        Task<OASISResult<IAvatarDetail>> UpdateAsync(IAvatarDetail avatar);
    }
}