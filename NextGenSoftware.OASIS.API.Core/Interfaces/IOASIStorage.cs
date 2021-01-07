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

        IHolon LoadHolon(Guid id);
        IHolon LoadHolon(string providerKey);
        IHolon SaveHolon(IHolon holon);
        List<IHolon> SaveHolons(List<IHolon> holons);

        Task<IHolon> LoadHolonAsync(Guid id);
        Task<IHolon> LoadHolonAsync(string providerKey);
        Task<IHolon> SaveHolonAsync(IHolon holon);
        Task<List<IHolon>> SaveHolonsAsync(List<IHolon> holons);


        List<IHolon> LoadHolons(Guid id);
        List<IHolon> LoadHolons(string providerKey);
        Task<List<IHolon>> LoadHolonsAsync(Guid id);
        Task<List<IHolon>> LoadHolonsAsync(string providerKey);


        bool DeleteAvatar(Guid id, bool softDelete = true);
        Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true);

        Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(API.Core.IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc);
        Task<KarmaAkashicRecord> SubtractKarmaFromAvatarAsync(API.Core.IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc);


        Task<ISearchResults> SearchAsync(ISearchParams searchParams);

        event StorageProviderError StorageProviderError;

        //TODO: Lots more to come! ;-)
    }
}
