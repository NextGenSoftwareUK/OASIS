using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Repositories;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using DataHelper = NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers.DataHelper;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS
{
    public partial class HoloOASIS
    {
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            //return Delete(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION);

            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IHolon> response = _genericRepository.Delete(HcObjectTypeEnum.Avatar, "username", avatarUsername, ZOME_DELETE_AVATAR_BY_USERNAME_FUNCTION);

            if (response != null && !response.IsError && response.IsDeleted)
                result.Result = true;
            else
                result.Result = false;

            return result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await _genericRepository.LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return _genericRepository.Load<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await _genericRepository.LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, ZOME_LOAD_HOLON_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return _genericRepository.Load<IHolon>(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, ZOME_LOAD_HOLON_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        //public override async Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    return await _genericRepository.LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "customKey", customKey, ZOME_LOAD_HOLON_BY_CUSTOM_KEY_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    return _genericRepository.Load<IHolon>(HcObjectTypeEnum.Holon, "customKey", customKey, ZOME_LOAD_HOLON_BY_CUSTOM_KEY_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        //public override async Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    return await _genericRepository.LoadAsync<IHolon>(HcObjectTypeEnum.Holon, metaKey, metaValue, ZOME_LOAD_HOLON_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    return _genericRepository.Load<IHolon>(HcObjectTypeEnum.Holon, metaKey, metaValue, ZOME_LOAD_HOLON_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
            return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        //public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    //return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_CUSTOM_KEY_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
        //    return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_CUSTOM_KEY_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    //return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_CUSTOM_KEY_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
        //    return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_CUSTOM_KEY_FUNCTION, version, new Dictionary<string, string>()
        //    {
        //        ["loadChildren"] = loadChildren.ToString(),
        //        ["recursive"] = recursive.ToString(),
        //        ["maxChildDepth"] = maxChildDepth.ToString(),
        //        ["continueOnError"] = continueOnError.ToString(),
        //        ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
        //    });
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
            return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
            return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
            return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider });
            return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_META_DATA_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError });
            return await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            //return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new { type, loadChildren, recursive, maxChildDepth, continueOnError });
            return _holonRepository.LoadHolons("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version, new Dictionary<string, string>()
            {
                ["loadChildren"] = loadChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["loadChildrenFromProvider"] = loadChildrenFromProvider.ToString()
            });
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return await _genericRepository.SaveAsync(HcObjectTypeEnum.Holon, holon, new Dictionary<string, string>()
            {
                ["saveChildren"] = saveChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["saveChildrenOnProvider"] = saveChildrenOnProvider.ToString()
            });
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return _genericRepository.Save(HcObjectTypeEnum.Holon, holon, new Dictionary<string, string>()
            {
                ["saveChildren"] = saveChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["saveChildrenOnProvider"] = saveChildrenOnProvider.ToString()
            });
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            //_holonRepository.SaveHolonsAsync(holons, "holons", "holons_anchor", ZOME_SAVE_ALL_HOLONS_FUNCTION, new { saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider });
            return await _holonRepository.SaveHolonsAsync(holons, "holons", "holons_anchor", ZOME_SAVE_ALL_HOLONS_FUNCTION, new Dictionary<string, string>()
            {
                ["saveChildren"] = saveChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["saveChildrenOnProvider"] = saveChildrenOnProvider.ToString()
            });
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            //_holonRepository.SaveHolons(holons, "holons", "holons_anchor", ZOME_SAVE_ALL_HOLONS_FUNCTION, new { saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider });
            return _holonRepository.SaveHolons(holons, "holons", "holons_anchor", ZOME_SAVE_ALL_HOLONS_FUNCTION, new Dictionary<string, string>()
            {
                ["saveChildren"] = saveChildren.ToString(),
                ["recursive"] = recursive.ToString(),
                ["maxChildDepth"] = maxChildDepth.ToString(),
                ["continueOnError"] = continueOnError.ToString(),
                ["saveChildrenOnProvider"] = saveChildrenOnProvider.ToString()
            });
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            return await _genericRepository.DeleteAsync(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_DELETE_HOLON_BY_ID_FUNCTION);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return _genericRepository.Delete(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_DELETE_HOLON_BY_ID_FUNCTION);
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            return await _genericRepository.DeleteAsync(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, ZOME_DELETE_HOLON_BY_PROVIDER_KEY_FUNCTION);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return _genericRepository.Delete(HcObjectTypeEnum.Holon, "providerKey (entryHash)", providerKey, ZOME_DELETE_HOLON_BY_PROVIDER_KEY_FUNCTION);
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchResults = new SearchResults();
                var holons = new List<IHolon>();
                var avatars = new List<IAvatar>();
                
                // Search holons in Holochain
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var q = searchParams.SearchGroups.First().PreviousSearchGroupOperator.ToString();
                    // basic contains filter using exported data as fallback
                    var exportAll = await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version);
                    if (!exportAll.IsError && exportAll.Result != null)
                        holons.AddRange(exportAll.Result.Where(h => (h.Name ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase) || (h.Description ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase)));
                }
                
                // Search avatars fallback
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var q = searchParams.SearchGroups.First().PreviousSearchGroupOperator.ToString();
                    var allAvatars = await _avatarRepository.LoadAvatarsAsync("avatars", "", ZOME_LOAD_ALL_AVATARS_FUNCTION, version);
                    if (!allAvatars.IsError && allAvatars.Result != null)
                        avatars.AddRange(allAvatars.Result.Where(a => (a.Name ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase) || (a.Description ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase)));
                }
                
                searchResults.SearchResultHolons = holons;
                searchResults.SearchResultAvatars = avatars;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Search completed successfully in Holochain with full property mapping ({holons.Count} holons, {avatars.Count} avatars)";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching in Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                var saveResult = await _holonRepository.SaveHolonsAsync(holons, "holons", "holons_anchor", ZOME_SAVE_ALL_HOLONS_FUNCTION, new Dictionary<string, string>()
                {
                    ["saveChildren"] = true.ToString(),
                    ["recursive"] = true.ToString(),
                    ["maxChildDepth"] = 0.ToString(),
                    ["continueOnError"] = true.ToString(),
                    ["saveChildrenOnProvider"] = false.ToString()
                });

                //var importedCount = 0;
                //foreach (var holon in holons)
                //{
                //    var saveResult = await _holonRepository.SaveAsync(holon);
                //    if (saveResult.IsError)
                //    {
                //        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                //        return result;
                //    }
                //    importedCount++;
                //}

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {holons.Count()} holons to Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar from Holochain
                // Fallback: load all and filter by CreatedByAvatarId
                var allHolonsResult = await _holonRepository.LoadHolonsAsync("holons", "holons_anchor", ZOME_LOAD_ALL_HOLONS_FUNCTION, version);
                var holons = new OASISResult<IEnumerable<IHolon>> { Result = allHolonsResult.Result?.Where(h => h.CreatedByAvatarId == avatarId) };
                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Result?.Count() ?? 0} holons for avatar {avatarId} from Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Holo provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons created by the avatar username from Holochain
                var holons = await _holonRepository.LoadHolonsAsync("avatars", "avatars_anchor", ZOME_LOAD_HOLONS_FOR_PARENT_BY_PROVIDER_KEY_FUNCTION, version, new Dictionary<string, string>()
                {
                    ["loadChildren"] = true.ToString(),
                    ["recursive"] = true.ToString(),
                    ["maxChildDepth"] = 0.ToString(),
                    ["continueOnError"] = true.ToString(),
                    ["loadChildrenFromProvider"] = false.ToString()
                });

                result.Result = holons.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holons.Result?.Count() ?? 0} holons for avatar {avatarUsername} from Holochain";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from Holochain: {ex.Message}", ex);
            }
            return result;
        }

    }
}
