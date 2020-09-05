using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS
{
    public interface IAvatarRepository
    {
        Task<IAvatar> Add(IAvatar user);
        Task<IAvatar> Update(IAvatar user);
        Task Delete(string id);
        Task<IAvatar> GetAvatar(string id);
        Task<IEnumerable<IAvatar>> GetAvatars();
    }

}
