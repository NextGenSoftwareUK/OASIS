using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static NextGenSoftware.OASIS.API.Core.AvatarManager;

namespace NextGenSoftware.OASIS.API.Core
{
    // This interface is responsbile for persisting data/state to storage, this could be a local DB or other local 
    // storage or through a distributed/decentralised provider such as IPFS or Holochain (these two implementations 
    // will be implemented soon (IPFSOASIS & HoloOASIS).
    public interface IOASISStorage : IOASISProvider
    {
        Task<IAvatar> LoadAvatarAsync(string providerKey);
        Task<IAvatar> LoadAvatarAsync(Guid Id);
        Task<IAvatar> LoadAvatarAsync(string username, string password);

        //TODO: Add NonAsync methods for rest...
        IAvatar LoadAvatar(string username, string password);
        IAvatar LoadAvatar(string username);
        IAvatar LoadAvatar(Guid id);

        IEnumerable<IAvatar> LoadAllAvatars();
        Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync();

        //Task<bool> SaveAvatarAsync(IAvatar Avatar);
        Task<IAvatar> SaveAvatarAsync(IAvatar Avatar);
        IAvatar SaveAvatar(IAvatar Avatar);

        Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(API.Core.IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc);
        Task<KarmaAkashicRecord> SubtractKarmaFromAvatarAsync(API.Core.IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc);


        Task<ISearchResults> SearchAsync(string searchTerm);

        event StorageProviderError StorageProviderError;

        //TODO: Lots more to come! ;-)
    }
}
