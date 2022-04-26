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
    public interface IOASISStorageProvider : IOASISProvider
    {
        OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0);
        Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0);
        OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0);
        OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0);
        OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0);
        Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0);
        Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0);
        Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0);
        OASISResult<IAvatar> LoadAvatar(string username, int version = 0);
        Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0);
        //OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0);
        //Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0);
        OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0);
        Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0);
        OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0);
        OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0);
        OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0);
        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0);
        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0);
        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0);
        OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0);
        Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0);
        // IAvatarThumbnail LoadAvatarThumbnail(Guid id);
        // Task<IAvatarThumbnail> LoadAvatarThumbnailAsync(Guid id);
        OASISResult<IAvatar> SaveAvatar(IAvatar Avatar);
        Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar);
        OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar);
        Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar);
        OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true);
        OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true);
        OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true);
        OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true); //TODO: Currently not used - may remove later? Is it needed?
        Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true);
        OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatarAsync(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatarAsync(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);

        //TODO: We need to migrate ALL OASIS methods to use the OASISResult Pattern ASAP! Thankyou! :)
        OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true);
        Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true);
        OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);

      //  T LoadHolon<T>(Guid id) where T : IHolon;
        Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true);
        Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true);
        OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true);
        Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true);

        Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);

        event StorageProviderError StorageProviderError;

        //TODO: Lots more to come! ;-)
    }
}