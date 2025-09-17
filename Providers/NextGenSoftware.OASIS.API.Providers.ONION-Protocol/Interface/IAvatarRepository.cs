using OASIS_Onion.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Avatar = OASIS_Onion.Model.Models.Avatar;

namespace OASIS_Onion.Repository.Interface
{
    public interface IAvatarRepository : IEntityRepository<Avatar>
    {
        Avatar Add(Avatar avatar);

        Task<Avatar> AddAsync(Avatar avatar);

        bool Delete(Guid id, bool softDelete = true);

        bool Delete(string providerKey, bool softDelete = true);

        Task<bool> DeleteAsync(Guid id, bool softDelete = true);

        Task<bool> DeleteAsync(string providerKey, bool softDelete = true);

        Avatar GetAvatar(Guid id);

        Avatar GetAvatar(string username);

        Avatar GetAvatar(Expression<Func<Avatar, bool>> expression);

        Avatar GetAvatar(string username, string password);

        Task<Avatar> GetAvatarAsync(Guid id);

        Task<Avatar> GetAvatarAsync(Expression<Func<Avatar, bool>> expression);

        bool Delete(Expression<Func<Avatar, bool>> expression, bool softDelete = true);

        Task<bool> DeleteAsync(Expression<Func<Avatar, bool>> expression, bool softDelete = true);

        Task<Avatar> GetAvatarAsync(string username);

        Task<Avatar> GetAvatarAsync(string username, string password);

        //Task<AvatarDetail> GetAvatarDetailAsync(string username, string password);
        List<Avatar> GetAvatars();

        Task<List<Avatar>> GetAvatarsAsync();

        Avatar Update(Avatar avatar);

        Task<Avatar> UpdateAsync(Avatar avatar);
    }
}