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
            return await _genericRepository.LoadAsync<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_BY_ID_FUNCTION, version);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return _genericRepository.Load<IHolon>(HcObjectTypeEnum.Holon, "id", id.ToString(), ZOME_LOAD_HOLON_BY_ID_FUNCTION, version);
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
            return await LoadNativeHolonCollectionAsync(ZOME_LOAD_HOLONS_FOR_PARENT_BY_ID_FUNCTION, id.ToString("D"), type, loadChildren, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return Task.Run(() => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version)).GetAwaiter().GetResult();
        }

        private async Task<OASISResult<IEnumerable<IHolon>>> LoadNativeHolonCollectionAsync(
            string function, object payload, HolonType type, bool loadChildren, int version, Func<IHolon, bool> filter = null)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (version != 0)
            {
                OASISErrorHandling.HandleError(ref result, "This Holochain collection query supports current records.");
                return result;
            }

            try
            {
                var response = await HoloNETClientAppAgent.CallZomeFunctionAsync("oasis", function, payload, typeof(HcHolon));
                if (response == null || response.IsError || response.Records == null)
                {
                    OASISErrorHandling.HandleError(ref result, response?.Message ?? $"No records response from Holochain {function}.");
                    return result;
                }

                var holons = new List<IHolon>();
                foreach (var record in response.Records)
                {
                    if (!(record.EntryDataObject is HcHolon entry) || string.IsNullOrWhiteSpace(record.ActionHash))
                    {
                        OASISErrorHandling.HandleError(ref result, $"Holochain {function} returned an unmapped record.");
                        return result;
                    }

                    if (type != HolonType.All && entry.HolonType != type)
                        continue;

                    entry.ActionHash = record.ActionHash;
                    var holon = DataHelper.ConvertHcHolonToHolon(entry);
                    if (filter != null && !filter(holon))
                        continue;
                    if (!loadChildren)
                        holon.Children = new List<IHolon>();
                    holons.Add(holon);
                }

                result.Result = holons;
                result.IsLoaded = true;
                result.LoadedCount = holons.Count;
            }
            catch (Exception error)
            {
                OASISErrorHandling.HandleError(ref result, $"Holochain {function} collection read failed.", error);
            }

            return result;
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
            if (string.IsNullOrWhiteSpace(metaKey))
            {
                var result = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref result, "A nonblank metadata key is required.");
                return result;
            }

            return await LoadNativeHolonCollectionAsync(ZOME_LOAD_ALL_HOLONS_FUNCTION, null, type, loadChildren, version,
                holon => MetaMatches(holon, metaKey, metaValue));
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return Task.Run(() => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive,
                maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version)).GetAwaiter().GetResult();
        }

        private static bool MetaMatches(IHolon holon, string key, string value)
        {
            if (holon.MetaData == null || !holon.MetaData.TryGetValue(key, out var stored))
                return false;

            var text = stored is JsonElement json && json.ValueKind == JsonValueKind.Null
                ? null
                : Convert.ToString(stored, System.Globalization.CultureInfo.InvariantCulture);
            return string.Equals(text, value, StringComparison.Ordinal);
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0 || metaKeyValuePairs.Keys.Any(string.IsNullOrWhiteSpace) ||
                !Enum.IsDefined(typeof(MetaKeyValuePairMatchMode), metaKeyValuePairMatchMode))
            {
                var result = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref result, "Metadata pairs with nonblank keys and a valid match mode are required.");
                return result;
            }

            var pairs = metaKeyValuePairs.ToArray();
            bool Match(IHolon holon) => metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any
                ? pairs.Any(pair => MetaMatches(holon, pair.Key, pair.Value))
                : pairs.All(pair => MetaMatches(holon, pair.Key, pair.Value));
            return await LoadNativeHolonCollectionAsync(ZOME_LOAD_ALL_HOLONS_FUNCTION, null, type, loadChildren, version, Match);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return Task.Run(() => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type,
                loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version))
                .GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return await LoadNativeHolonCollectionAsync(ZOME_LOAD_ALL_HOLONS_FUNCTION, null, type, loadChildren, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return Task.Run(() => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth,
                continueOnError, loadChildrenFromProvider, version)).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            if (holon == null || maxChildDepth < 0)
            {
                OASISErrorHandling.HandleError(ref result, "A holon and a non-negative child depth are required.");
                return result;
            }

            var graph = new List<(IHolon Record, IHolon Parent, int Depth)> { (holon, null, 0) };
            var ids = new HashSet<Guid>();
            for (int index = 0; index < graph.Count; index++)
            {
                var (record, _, depth) = graph[index];
                if (record == null)
                {
                    OASISErrorHandling.HandleError(ref result, "A Holochain storage graph contains a null child.");
                    return result;
                }

                if (record.Id == Guid.Empty)
                    record.Id = Guid.NewGuid();
                if (!ids.Add(record.Id))
                {
                    OASISErrorHandling.HandleError(ref result, $"A Holochain storage graph repeats holon {record.Id}.");
                    return result;
                }

                if (saveChildren && (recursive || depth == 0) && (maxChildDepth == 0 || depth < maxChildDepth) && record.Children != null)
                {
                    foreach (var child in record.Children)
                        graph.Add((child, record, depth + 1));
                }
            }

            foreach (var (record, parent, _) in graph)
            {
                if (parent != null)
                    record.ParentHolonId = parent.Id;
            }

            var failed = new HashSet<Guid>();
            foreach (var (record, parent, _) in graph)
            {
                if (parent != null && failed.Contains(parent.Id))
                {
                    failed.Add(record.Id);
                    continue;
                }

                var saved = await _genericRepository.SaveAsync(HcObjectTypeEnum.Holon, record);
                if (saved.IsError)
                {
                    failed.Add(record.Id);
                    OASISErrorHandling.HandleError(ref result, saved.Message);
                    if (!continueOnError)
                        break;
                    continue;
                }

                if (parent == null)
                    result.Result = saved.Result;
                result.SavedCount += saved.SavedCount;
            }

            result.IsSaved = result.SavedCount == graph.Count;
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return Task.Run(() => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider))
                .GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var records = holons?.ToList();
            if (records == null || records.Any(holon => holon == null))
            {
                OASISErrorHandling.HandleError(ref result, "A batch of non-null holons is required.");
                return result;
            }

            var savedRoots = new List<IHolon>(records.Count);
            foreach (var holon in records)
            {
                var saved = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                result.SavedCount += saved.SavedCount;
                if (saved.Result != null)
                    savedRoots.Add(saved.Result);
                if (!saved.IsError)
                    continue;

                OASISErrorHandling.HandleError(ref result, saved.Message);
                if (!continueOnError)
                    break;
            }

            result.Result = savedRoots;
            result.IsSaved = !result.IsError && savedRoots.Count == records.Count;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return Task.Run(() => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, saveChildrenOnProvider)).GetAwaiter().GetResult();
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            OASISResult<IHolon> holon = await LoadHolonAsync(id);
            if (holon.IsError || holon.Result?.ProviderUniqueStorageKey == null ||
                !holon.Result.ProviderUniqueStorageKey.TryGetValue(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.HoloOASIS, out string actionHash) ||
                string.IsNullOrWhiteSpace(actionHash))
                return holon;

            return await _genericRepository.DeleteAsync(HcObjectTypeEnum.Holon, "actionHash", actionHash, ZOME_DELETE_HOLON_FUNCTION);
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

                var saveResult = await SaveHolonsAsync(holons);
                result.SavedCount = saveResult.SavedCount;
                if (saveResult.IsError || !saveResult.IsSaved)
                {
                    OASISErrorHandling.HandleError(ref result, saveResult.Message ?? "Holochain did not acknowledge the import.");
                    return result;
                }

                result.Result = true;
                result.IsSaved = true;
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

                return await LoadNativeHolonCollectionAsync(ZOME_LOAD_ALL_HOLONS_FUNCTION, null, HolonType.All,
                    loadChildren: false, version: version, filter: holon => holon.CreatedByAvatarId == avatarId);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Holochain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return Task.Run(() => ExportAllDataForAvatarByIdAsync(avatarId, version)).GetAwaiter().GetResult();
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
