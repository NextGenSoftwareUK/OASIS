using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Interfaces
{
    public interface IAvatarRepository
    {
       // Avatar Add(AvatarModel avatar);
        //Task<Avatar> AddAsync(Avatar avatar);
        bool Delete(long id, bool softDelete = true);
        bool Delete(string providerKey, bool softDelete = true);
        Task<bool> DeleteAsync(long id, bool softDelete = true);
        Task<bool> DeleteAsync(string providerKey, bool softDelete = true);
        Avatar GetAvatar(long id);
        Avatar GetAvatar(string username);
        Avatar GetAvatar(string username, string password);
        Task<Avatar> GetAvatarAsync(long id);
        Task<Avatar> GetAvatarAsync(string username);
        Task<Avatar> GetAvatarAsync(string username, string password);
        List<Avatar> GetAvatars();
        Task<List<Avatar>> GetAvatarsAsync();
     //   Avatar Update(AvatarModel avatar);
        Task<Avatar> UpdateAsync(Avatar avatar);
    }
}