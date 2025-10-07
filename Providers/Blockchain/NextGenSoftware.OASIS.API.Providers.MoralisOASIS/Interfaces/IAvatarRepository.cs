using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avatar = NextGenSoftware.OASIS.API.Providers.MoralisOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MoralisOASIS.Entities.AvatarDetail;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS.Interfaces
{
    public interface IAvatarRepository
    {
        Avatar Add(Avatar avatar);
        Task<Avatar> AddAsync(Avatar avatar);
        AvatarDetail Add(AvatarDetail avatar);
        Task<AvatarDetail> AddAsync(AvatarDetail avatar);
        bool Delete(Guid id, bool softDelete = true);
        bool Delete(string providerKey, bool softDelete = true);
        Task<bool> DeleteAsync(Guid id, bool softDelete = true);
        Task<bool> DeleteAsync(string providerKey, bool softDelete = true);
        Avatar GetAvatar(Guid id);
        Avatar GetAvatar(string username);
        Avatar GetAvatar(string username, string password);
        Task<Avatar> GetAvatarAsync(Guid id);
        Task<Avatar> GetAvatarAsync(string username);
        Task<Avatar> GetAvatarAsync(string username, string password);
        AvatarDetail GetAvatarDetail(Guid id);
        AvatarDetail GetAvatarDetail(string username);
        //AvatarDetail GetAvatarDetail(string username, string password);
        Task<AvatarDetail> GetAvatarDetailAsync(Guid id);
        Task<AvatarDetail> GetAvatarDetailAsync(string username);
        //Task<AvatarDetail> GetAvatarDetailAsync(string username, string password);
        List<Avatar> GetAvatars();
        Task<List<Avatar>> GetAvatarsAsync();
        Task<IEnumerable<AvatarDetail>> GetAvatarDetailsAsync();
        IEnumerable<AvatarDetail> GetAvatarDetails();
        Avatar Update(Avatar avatar);
        Task<Avatar> UpdateAsync(Avatar avatar);
        AvatarDetail Update(AvatarDetail avatar);
        Task<AvatarDetail> UpdateAsync(AvatarDetail avatar);

        //Task<AvatarThumbnail> GetAvatarThumbnailByIdAsync(Guid id);
        //AvatarThumbnail GetAvatarThumbnailById(Guid id);
    }
}