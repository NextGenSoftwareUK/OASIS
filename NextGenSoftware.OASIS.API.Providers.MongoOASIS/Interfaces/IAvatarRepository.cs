using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces
{
    public interface IAvatarRepository
    {
        Avatar Add(Avatar avatar);
        Task<Avatar> AddAsync(Avatar avatar);
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
        List<Avatar> GetAvatars();
        Task<List<Avatar>> GetAvatarsAsync();
        Avatar Update(Avatar avatar);
        Task<Avatar> UpdateAsync(Avatar avatar);
    }
}