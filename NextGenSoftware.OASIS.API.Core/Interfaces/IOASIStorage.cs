using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.OASIS.API.Core.Managers.AvatarManager;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    // This interface is responsbile for persisting data/state to storage, this could be a local DB or other local 
    // storage or through a distributed/decentralised provider such as IPFS (IPFSOASIS Provider coming soon) or Holochain (HoloOASIS Provider implemented).
    public interface IOASISStorage : IOASISProvider
    {
        IAvatar LoadAvatarForProviderKey(string providerKey);
        Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey);
        IAvatar LoadAvatar(Guid id);
        Task<IAvatar> LoadAvatarAsync(Guid Id);
        IAvatar LoadAvatar(string username);
        Task<IAvatar> LoadAvatarAsync(string username);
        IAvatar LoadAvatar(string username, string password);
        Task<IAvatar> LoadAvatarAsync(string username, string password);
        IEnumerable<IAvatar> LoadAllAvatars();
        Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync();
        IAvatar SaveAvatar(IAvatar Avatar);
        Task<IAvatar> SaveAvatarAsync(IAvatar Avatar);
        bool DeleteAvatar(Guid id, bool softDelete = true);
        Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true);
        bool DeleteAvatar(string providerKey, bool softDelete = true);
        Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true);
        KarmaAkashicRecord AddKarmaToAvatar(IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        KarmaAkashicRecord RemoveKarmaFromAvatar(IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);

        IHolon SaveHolon(IHolon holon);
        Task<IHolon> SaveHolonAsync(IHolon holon);
        IEnumerable<IHolon> SaveHolons(IEnumerable<IHolon> holons);
        Task<IEnumerable<IHolon>> SaveHolonsAsync(IEnumerable<IHolon> holons);
        IHolon LoadHolon(Guid id);
        Task<IHolon> LoadHolonAsync(Guid id);
        IHolon LoadHolon(string providerKey);
        Task<IHolon> LoadHolonAsync(string providerKey);
        IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All);
        Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All);
        IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All);
        Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All);
        IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.All);
        Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.All);
        bool DeleteHolon(Guid id, bool softDelete = true);
        Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true);
        bool DeleteHolon(string providerKey, bool softDelete = true);
        Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true);

        Task<ISearchResults> SearchAsync(ISearchParams searchParams);

        event StorageProviderError StorageProviderError;

        //TODO: Lots more to come! ;-)
    }
}
