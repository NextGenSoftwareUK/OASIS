using NextGenSoftware.OASIS.API.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS
{
    public interface IAvatarRepository
    {
        Task<Avatar> Add(Avatar user);
        Task<Avatar> Update(Avatar user);
        Task Delete(string id);
        Task<Avatar> GetAvatar(string id);
        Task<List<Avatar>> GetAvatars();
    }

}
