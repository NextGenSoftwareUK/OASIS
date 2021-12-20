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
        public OASISDNA OASISDNA { get; set; }
        public string OASISDNAPath { get; set; }

        public event AvatarManager.StorageProviderError StorageProviderError;

        public OASISStorageProviderBase()
        {
            OASISDNAManager.LoadDNA();
            this.OASISDNA = OASISDNAManager.OASISDNA;
            this.OASISDNAPath = OASISDNAManager.OASISDNAPath;
        }

        public OASISStorageProviderBase(string OASISDNAPath)
        {
            this.OASISDNAPath = OASISDNAPath;
            OASISDNAManager.LoadDNA(OASISDNAPath);
            this.OASISDNA = OASISDNAManager.OASISDNA;
        }

        public OASISStorageProviderBase(OASISDNA OASISDNA)
        {
            this.OASISDNA = OASISDNA;
            this.OASISDNAPath = OASISDNAManager.OASISDNAPath;
        }

        public OASISStorageProviderBase(OASISDNA OASISDNA, string OASISDNAPath)
        {
            this.OASISDNA = OASISDNA;
            this.OASISDNAPath = OASISDNAPath;
        }

        //event StorageProviderError IOASISStorageProvider.StorageProviderError
        //{
        //    add
        //    {
        //        throw new NotImplementedException();
        //    }

        //    remove
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        //TODO: COme back to this...
        //public List<Avatar> LoadAvatarsWithoutPasswords(IEnumerable<Avatar> avatars)
        //{
        //    return avatars.Select(x => x.WithoutPassword());
        //}

        //public Avatar WithoutPassword(this Avatar user)
        //{
        //    user.Password = null;
        //    return user;
        //}

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
            StorageProviderError?.Invoke(this, new AvatarManagerErrorEventArgs { EndPoint = endPoint, Reason = reason, ErrorDetails = errorDetails });
        }

        /*
        public abstract Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync();
        public abstract IEnumerable<IAvatar> LoadAllAvatars();
        public abstract IAvatar LoadAvatarByUsername(string avatarUsername);
        public abstract Task<IAvatar> LoadAvatarAsync(Guid Id);
        public abstract Task<IAvatar> LoadAvatarByEmailAsync(string avatarEmail);
        public abstract Task<IAvatar> LoadAvatarByUsernameAsync(string avatarUsername);
        public abstract IAvatar LoadAvatar(Guid Id);
        public abstract IAvatar LoadAvatarByEmail(string avatarEmail);
        public abstract Task<IAvatar> LoadAvatarAsync(string username, string password);
        public abstract IAvatar LoadAvatar(string username, string password);
        public abstract IAvatar LoadAvatar(string username);
        public abstract Task<IAvatar> LoadAvatarAsync(string username);
        public abstract Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey);
        public abstract IAvatar LoadAvatarForProviderKey(string providerKey);
        // public abstract Task<IAvatarThumbnail> LoadAvatarThumbnailAsync(Guid id);
        //  public abstract IAvatarThumbnail LoadAvatarThumbnail(Guid id);
        public abstract IAvatarDetail LoadAvatarDetail(Guid id);
        public abstract IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail);
        public abstract IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername);
        public abstract Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id);
        public abstract Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername);
        public abstract Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail);
        public abstract IEnumerable<IAvatarDetail> LoadAllAvatarDetails();
        public abstract Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync();
        public abstract IAvatar SaveAvatar(IAvatar Avatar);
        public abstract Task<IAvatar> SaveAvatarAsync(IAvatar Avatar);
        public abstract IAvatarDetail SaveAvatarDetail(IAvatarDetail Avatar);
        public abstract Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail Avatar);
        public abstract OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true);
        public abstract OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true);
        public abstract OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true);
        public abstract OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true);
        public abstract Task<ISearchResults> SearchAsync(ISearchParams searchParams);
        public abstract IHolon LoadHolon(Guid id);
        public abstract Task<IHolon> LoadHolonAsync(Guid id);
        public abstract IHolon LoadHolon(string providerKey);
        public abstract Task<IHolon> LoadHolonAsync(string providerKey);
        public abstract IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All);
        public abstract Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All);
        public abstract IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All);
        public abstract IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.All);
        public abstract Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.All);

        //TODO: We need to migrate ALL OASIS methods to use the OASISResult Pattern ASAP! Thankyou! :)
        public abstract OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildrenRecursive = true);
        public abstract Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildrenRecursive = true);
        public abstract OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true);
        public abstract bool DeleteHolon(Guid id, bool softDelete = true);
        public abstract Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true);
        public abstract bool DeleteHolon(string providerKey, bool softDelete = true);
        public abstract Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true);
        */

        public abstract Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0);
        public abstract OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0);
        public abstract OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0);
        public abstract Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0);
        public abstract Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0);
        public abstract Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0);
        public abstract OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0);
        public abstract OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0);
        public abstract Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0);
        public abstract OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0);
        public abstract OASISResult<IAvatar> LoadAvatar(string username, int version = 0);
        public abstract Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0);
        public abstract Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0);
        public abstract OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0);
        // public abstract Task<IAvatarThumbnail> LoadAvatarThumbnailAsync(Guid id);
        //  public abstract IAvatarThumbnail LoadAvatarThumbnail(Guid id);
        public abstract OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0);
        public abstract OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0);
        public abstract OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0);
        public abstract Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0);
        public abstract Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0);
        public abstract Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0);
        public abstract OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0);
        public abstract Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0);
        public abstract OASISResult<IAvatar> SaveAvatar(IAvatar Avatar);
        public abstract Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar);
        public abstract OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar);
        public abstract Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar);
        public abstract OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true);
        public abstract OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true);
        public abstract OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true);
        public abstract OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true);
        public abstract Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, int version = 0);

        //TODO: We need to migrate ALL OASIS methods to use the OASISResult Pattern ASAP! Thankyou! :)
        public abstract OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        public abstract Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true);
        public abstract OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true);
        public abstract Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true);
        public abstract OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true);
        public abstract OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true);
        public abstract Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true);
    }
}
