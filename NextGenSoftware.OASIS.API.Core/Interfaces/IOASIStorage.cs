using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
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
        IAvatar LoadAvatarByEmail(string avatarEmail);
        IAvatar LoadAvatarByUsername(string avatarUsername);
        Task<IAvatar> LoadAvatarAsync(Guid Id);
        Task<IAvatar> LoadAvatarByEmailAsync(string avatarEmail);
        Task<IAvatar> LoadAvatarByUsernameAsync(string avatarUsername);
        IAvatar LoadAvatar(string username);
        Task<IAvatar> LoadAvatarAsync(string username);
        IAvatar LoadAvatar(string username, string password);
        Task<IAvatar> LoadAvatarAsync(string username, string password);
        IEnumerable<IAvatar> LoadAllAvatars();
        Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync();
        IAvatarDetail LoadAvatarDetail(Guid id);
        IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail);
        IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername);
        Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id);
        Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername);
        Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail);
        IEnumerable<IAvatarDetail> LoadAllAvatarDetails();
        Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync();
       // IAvatarThumbnail LoadAvatarThumbnail(Guid id);
       // Task<IAvatarThumbnail> LoadAvatarThumbnailAsync(Guid id);
        IAvatar SaveAvatar(IAvatar Avatar);
        Task<IAvatar> SaveAvatarAsync(IAvatar Avatar);
        IAvatarDetail SaveAvatarDetail(IAvatarDetail Avatar);
        Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail Avatar);
        bool DeleteAvatar(Guid id, bool softDelete = true);
        bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true);
        bool DeleteAvatarByUsername(string avatarUsername, bool softDelete = true);
        Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true);
        Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true);
        Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true);
        bool DeleteAvatar(string providerKey, bool softDelete = true);
        Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true);
        KarmaAkashicRecord AddKarmaToAvatar(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        KarmaAkashicRecord RemoveKarmaFromAvatar(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);

        //TODO: We need to migrate ALL OASIS methods to use the OASISResult Pattern ASAP! Thankyou! :)
        OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildrenRecursive = true);
        Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildrenRecursive = true);
        OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true);
        Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true);
        IHolon LoadHolon(Guid id);

      //  T LoadHolon<T>(Guid id) where T : IHolon;
        Task<IHolon> LoadHolonAsync(Guid id);
        IHolon LoadHolon(string providerKey);
        Task<IHolon> LoadHolonAsync(string providerKey);
        IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All);
        Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All);
        IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All);
        IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.All);
        Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.All);
        bool DeleteHolon(Guid id, bool softDelete = true);
        Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true);
        bool DeleteHolon(string providerKey, bool softDelete = true);
        Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true);

        Task<ISearchResults> SearchAsync(ISearchParams searchParams);
        
        Task<IEnumerable<IOland>> LoadAllOlandsAsync();
        Task<IOland> LoadOlandAsync(int id);
        Task<bool> DeleteOlandAsync(int id);
        Task<bool> DeleteOlandAsync(int id, bool safeDelete);
        Task<int> CreateOlandAsync(IOland oland);
        Task<bool> UpdateOlandAsync(IOland oland);
        
        IEnumerable<IOland> LoadAllOlands();
        IOland LoadOland(int id);
        bool DeleteOland(int id);
        bool DeleteOland(int id, bool safeDelete);
        int CreateOland(IOland oland);
        bool UpdateOland(IOland oland);

        event StorageProviderError StorageProviderError;

        //TODO: Lots more to come! ;-)
    }
}
