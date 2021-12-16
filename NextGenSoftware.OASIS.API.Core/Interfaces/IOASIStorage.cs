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
        OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey);
        Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey);
        OASISResult<IAvatar> LoadAvatar(Guid id);
        OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail);
        OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername);
        Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id);
        Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail);
        Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername);
        OASISResult<IAvatar> LoadAvatar(string username);
        Task<OASISResult<IAvatar>> LoadAvatarAsync(string username);
        OASISResult<IAvatar> LoadAvatar(string username, string password);
        Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password);
        OASISResult<IEnumerable<IAvatar>> LoadAllAvatars();
        Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync();
        OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id);
        OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail);
        OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername);
        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id);
        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername);
        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail);
        OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails();
        Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync();
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
        OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true);
        OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatarAsync(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatarAsync(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);

        //TODO: We need to migrate ALL OASIS methods to use the OASISResult Pattern ASAP! Thankyou! :)
        OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildrenRecursive = true);
        Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildrenRecursive = true);
        OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true);
        Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true);
        OASISResult<IHolon> LoadHolon(Guid id);

      //  T LoadHolon<T>(Guid id) where T : IHolon;
        Task<OASISResult<IHolon>> LoadHolonAsync(Guid id);
        OASISResult<IHolon> LoadHolon(string providerKey);
        Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All);
        OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All);
        Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All);
        OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true);
        Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true);
        OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true);
        Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true);

        Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams);

        event StorageProviderError StorageProviderError;

        //TODO: Lots more to come! ;-)
    }
}