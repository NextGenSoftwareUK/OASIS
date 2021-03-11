using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using static NextGenSoftware.OASIS.API.Core.Managers.AvatarManager;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    // This interface is responsbile for persisting data/state to storage, this could be a local DB or other local 
    // storage or through a distributed/decentralised provider such as IPFS (IPFSOASIS Provider coming soon) or Holochain (HoloOASIS Provider implemented).
    public interface IOASISStorage : IOASISProvider
    {
        Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey);
        Task<IAvatar> LoadAvatarAsync(Guid Id);
        Task<IAvatar> LoadAvatarAsync(string username);
        Task<IAvatar> LoadAvatarAsync(string username, string password);

        IAvatar LoadAvatar(string username, string password);
        IAvatar LoadAvatar(string username);
        IAvatar LoadAvatar(Guid id);
        IAvatar LoadAvatarForProviderKey(string providerKey);

        IEnumerable<IAvatar> LoadAllAvatars();
        Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync();

        Task<IAvatar> SaveAvatarAsync(IAvatar Avatar);
        IAvatar SaveAvatar(IAvatar Avatar);

        bool DeleteAvatar(Guid id, bool softDelete = true);
        Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true);
        bool DeleteAvatar(string providerKey, bool softDelete = true);
        Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true);


        IHolon LoadHolon(Guid id, HolonType type = HolonType.Holon);
        IHolon LoadHolon(string providerKey, HolonType type = HolonType.Holon);
        IHolon SaveHolon(IHolon holon);
        IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons);

        Task<IHolon> LoadHolonAsync(Guid id, HolonType type = HolonType.Holon);
        Task<IHolon> LoadHolonAsync(string providerKey, HolonType type = HolonType.Holon);
        Task<IHolon> SaveHolonAsync(IHolon holon);
        Task<IEnumerable<IHolon>> SaveHolonsAsync(IEnumerable<IHolon> holons);


        IEnumerable<IHolon> LoadHolons(Guid id, HolonType type = HolonType.Holon);
        IEnumerable<IHolon> LoadHolons(string providerKey, HolonType type = HolonType.Holon);
        Task<IEnumerable<IHolon>> LoadHolonsAsync(Guid id, HolonType type = HolonType.Holon);
        Task<IEnumerable<IHolon>> LoadHolonsAsync(string providerKey, HolonType type = HolonType.Holon);

        bool DeleteHolon(Guid id, bool softDelete = true);
        Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true);
        bool DeleteHolon(string providerKey, bool softDelete = true);
        Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true);

        Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc);
        Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc);
        KarmaAkashicRecord AddKarmaToAvatar(IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc);
        KarmaAkashicRecord RemoveKarmaFromAvatar(IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc);


        Task<ISearchResults> SearchAsync(ISearchParams searchParams);

        event StorageProviderError StorageProviderError;

        //TODO: Lots more to come! ;-)
    }
}
