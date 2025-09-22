using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    // This interface is responsbile for persisting data/state to storage, this could be a local DB or other local 
    // storage or through a distributed/decentralised provider such as IPFS (IPFSOASIS Provider coming soon) or Holochain (HoloOASIS Provider implemented).
    public interface IOASISStorageProvider : IOASISProvider
    {
        Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0);
        OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0);
        Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0);
        OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0);
        Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0);
        OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0);
        Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0);
        OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0);
        //Task<OASISResult<IAvatar>> LoadAvatarByJwtTokenAsync(string avatarUsername, int version = 0);
        //OASISResult<IAvatar> LoadAvatarByJwtToken(string avatarUsername, int version = 0);
        Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0);
        OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0);
        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0);
        OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0);
        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0);
        OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0);
        Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0);
        OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0);
        Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0);
        OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0);

        Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar);
        OASISResult<IAvatar> SaveAvatar(IAvatar Avatar);
        Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar);
        OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar);
        Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true);
        OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true);
        OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true);
        OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true);
        Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true);
        OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true); //TODO: Currently not used - may remove later? Is it needed?
        Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatarAsync(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatarAsync(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null);
        
        Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false);
        OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false);
        Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false);
        OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false);
        Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //OASISResult<IHolon> LoadHolonByMetaData(Dictionary<string, string> metaKeyValuePairs, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);

        //TODO: I cant see why we need customkey when we can use metakeys?!? - I think we should remove customkey and just use metakeys...
        //Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //OASISResult<IEnumerable<IHolon>> LoadHolonsByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);


        //TODO: Works same as metaData above but allows querying for the built-in OASIS Holon fields such as Name, Description, HolonType, IsActive 
        //But this has a lot of overlap with Search so may be redundant?
        //Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByHolonDataAsync(string holonField, string holonValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //OASISResult<IEnumerable<IHolon>> LoadHolonsByHolonData(string holonField, string holonValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByHolonDataAsync(Dictionary<string, string> holonFieldKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //OASISResult<IEnumerable<IHolon>> LoadHolonsByHolonData(Dictionary<string, string> holonFieldKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);


        //Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0);
        //Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id, bool softDelete = true);
        //OASISResult<IHolon> DeleteHolon(Guid id, bool softDelete = true);
        //Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey, bool softDelete = true);
        //OASISResult<IHolon> DeleteHolon(string providerKey, bool softDelete = true);

        Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id);
        OASISResult<IHolon> DeleteHolon(Guid id);
        Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey);
        OASISResult<IHolon> DeleteHolon(string providerKey);


        //TODO: Implement these methods ASAP - this is how we can share data across silos, then merge, aggregate, sense-make, perform actions on the full internet data, etc...
        Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons); //Imports all data into the OASIS from a given provider (will then be auto-replicated to all providers). NOTE: The Provider will need to convert the providers raw data into a list of holons (holonize the data).
        OASISResult<bool> Import(IEnumerable<IHolon> holons); //Imports all data into the OASIS from a given provider (will then be auto-replicated to all providers). NOTE: The Provider will need to convert the providers raw data into a list of holons (holonize the data).
        Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0); //Exports all data for a given avatar and provider. Version = 0 - Latest version. Version = -1 All versions.
        OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0); //Exports all data for a given avatar and provider. Version = 0 - Latest version. Version = -1 All versions.
        Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0); //Exports all data for a given avatar and provider. Version = 0 - Latest version. Version = -1 All versions.
        OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0); //Exports all data for a given avatar and provider. Version = 0 - Latest version. Version = -1 All versions.
        Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0); //Exports all data for a given avatar and provider. Version = 0 - Latest version. Version = -1 All versions.
        OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0); //Exports all data for a given avatar and provider. Version = 0 - Latest version. Version = -1 All versions.
        Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0); //Exports all data for a given provider. Version = 0 - Latest version. Version = -1 All versions.
        OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0); //Exports all data for a given provider. Version = 0 - Latest version. Version = -1 All versions.

        Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);
        OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0);

        public event StorageProviderError OnStorageProviderError;
        //TODO: Lots more to come! ;-)
    }
}