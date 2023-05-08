using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core
{
    public abstract class OASISStorageProviderBase : OASISProvider, IOASISStorageProvider
    {
        public OASISStorageProviderBase() : base() { }

        public OASISStorageProviderBase(string OASISDNAPath) : base(OASISDNAPath) { }

        public OASISStorageProviderBase(OASISDNA OASISDNA) : base (OASISDNA) { }

        public OASISStorageProviderBase(OASISDNA OASISDNA, string OASISDNAPath) : base (OASISDNA, OASISDNAPath) { }

        public event OASISManager.StorageProviderError StorageProviderError;

        public Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatarAsync(IAvatarDetail avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink)
        {
            return avatar.KarmaEarntAsync(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        public Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatarAsync(IAvatarDetail avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink)
        {
            return avatar.KarmaLostAsync(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatarDetail avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink)
        {
            return avatar.KarmaEarnt(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        public OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(IAvatarDetail avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink)
        {
            return avatar.KarmaLost(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
        }

        protected void OnStorageProviderError(string endPoint, string reason, Exception errorDetails)
        {
            StorageProviderError?.Invoke(this, new OASISErrorEventArgs { EndPoint = endPoint, Reason = reason, Exception = errorDetails });
        }

        public abstract Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0);
        public abstract OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0);
        public abstract Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0);
        public abstract OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0);
        public abstract Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0);
        public abstract OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0);
        public abstract Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0);
        public abstract OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0);
        public abstract Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0);
        public abstract OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0);
        //public abstract Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0);
        //public abstract OASISResult<IAvatar> LoadAvatar(string username, int version = 0);
        public abstract Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0);
        public abstract OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0);
        public abstract Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0);
        public abstract OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0);
        public abstract Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0);
        public abstract OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0);
        public abstract Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0);
        public abstract OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0);
        public abstract Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar);
        public abstract OASISResult<IAvatar> SaveAvatar(IAvatar Avatar);
        public abstract Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar);
        public abstract OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar);
        public abstract Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true);
        public abstract OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true);
        public abstract OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true);
        public abstract OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true);
        public abstract OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true);
        public abstract Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);

        public abstract Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        public abstract OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true);
        public abstract OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true);
        public abstract Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true);
        public abstract OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true);
        public abstract OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true);

        public abstract Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons);
        public abstract OASISResult<bool> Import(IEnumerable<IHolon> holons);

        public abstract Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0);
        public abstract OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0);
        public abstract OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0);
        public abstract OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0);
        public abstract OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0);
    }
}
