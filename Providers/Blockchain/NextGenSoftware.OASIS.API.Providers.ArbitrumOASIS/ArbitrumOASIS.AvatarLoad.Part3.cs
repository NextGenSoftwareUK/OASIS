using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using static NextGenSoftware.Utilities.KeyHelper;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;


namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;
public sealed partial class ArbitrumOASIS
{

    //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // The deployed contract has no getHolonsForParent function — scan all holons and filter by ParentId.
            uint holonCount = await _contractHandler.QueryAsync<GetHolonsCountFunction, uint>(new GetHolonsCountFunction());
            var holons = new List<IHolon>();
            for (uint i = 1; i <= holonCount; i++)
            {
                HolonInfo info = await _contractHandler.QueryAsync<GetHolonByIdyIdFunction, HolonInfo>(new() { Id = i });
                if (info == null || string.IsNullOrEmpty(info.Info)) continue;
                var holon = JsonConvert.DeserializeObject<Holon>(info.Info);
                if (holon != null && holon.ParentHolonId == id && (type == HolonType.All || holon.HolonType == type))
                    holons.Add(holon);
            }
            result.Result = holons;
            result.IsError = false;
            result.Message = $"Successfully loaded {holons.Count} holons for parent from Arbitrum";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // The deployed contract has no getHolonsForParentByProviderKey function.
            // Resolve the parent holon via hash-based entityId, then filter children by ParentHolonId.
            uint parentEntityId = (uint)Math.Abs(HashUtility.GetNumericHash(providerKey));
            HolonInfo parentInfo = await _contractHandler.QueryAsync<GetHolonByIdyIdFunction, HolonInfo>(new() { Id = parentEntityId });
            if (parentInfo == null || string.IsNullOrEmpty(parentInfo.Info))
            {
                OASISErrorHandling.HandleError(ref result, $"Parent holon with provider key '{providerKey}' not found on Arbitrum blockchain.");
                return result;
            }
            var parentHolon = JsonConvert.DeserializeObject<Holon>(parentInfo.Info);
            Guid parentId = parentHolon?.Id ?? Guid.Empty;

            uint holonCountByKey = await _contractHandler.QueryAsync<GetHolonsCountFunction, uint>(new GetHolonsCountFunction());
            var holonsByKey = new List<IHolon>();
            for (uint i = 1; i <= holonCountByKey; i++)
            {
                HolonInfo info = await _contractHandler.QueryAsync<GetHolonByIdyIdFunction, HolonInfo>(new() { Id = i });
                if (info == null || string.IsNullOrEmpty(info.Info)) continue;
                var holon = JsonConvert.DeserializeObject<Holon>(info.Info);
                if (holon != null && holon.ParentHolonId == parentId && (type == HolonType.All || holon.HolonType == type))
                    holonsByKey.Add(holon);
            }
            result.Result = holonsByKey;
            result.IsError = false;
            result.Message = $"Successfully loaded {holonsByKey.Count} holons for parent by provider key from Arbitrum";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentByCustomKeyAsync(customKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            if (string.IsNullOrWhiteSpace(customKey))
            {
                OASISErrorHandling.HandleError(ref result, "Custom key cannot be null or empty");
                return result;
            }

            // First load the parent holon by custom key
            var parentResult = await LoadHolonByCustomKeyAsync(customKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
            
            if (parentResult.IsError || parentResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Parent holon not found: {parentResult.Message}");
                return result;
            }

            // Then load children for the parent
            var childrenResult = await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            
            result.Result = childrenResult.Result;
            result.IsError = childrenResult.IsError;
            result.Message = childrenResult.Message;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by custom key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // The deployed contract has no getHolonsByMetaData function — scan all holons and filter in memory.
            uint holonCountMeta = await _contractHandler.QueryAsync<GetHolonsCountFunction, uint>(new GetHolonsCountFunction());
            var holonsByMeta = new List<IHolon>();
            for (uint i = 1; i <= holonCountMeta; i++)
            {
                HolonInfo info = await _contractHandler.QueryAsync<GetHolonByIdyIdFunction, HolonInfo>(new() { Id = i });
                if (info == null || string.IsNullOrEmpty(info.Info)) continue;
                var holon = JsonConvert.DeserializeObject<Holon>(info.Info);
                if (holon != null && holon.MetaData != null &&
                    holon.MetaData.ContainsKey(metaKey) &&
                    string.Equals(holon.MetaData[metaKey]?.ToString(), metaValue, StringComparison.OrdinalIgnoreCase) &&
                    (type == HolonType.All || holon.HolonType == type))
                    holonsByMeta.Add(holon);
            }
            result.Result = holonsByMeta;
            result.IsError = false;
            result.Message = $"Successfully loaded {holonsByMeta.Count} holons by metadata from Arbitrum";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // The deployed contract has no getHolonsByMetaDataPairs function — scan all holons and filter in memory.
            uint holonCountPairs = await _contractHandler.QueryAsync<GetHolonsCountFunction, uint>(new GetHolonsCountFunction());
            var holonsByPairs = new List<IHolon>();
            for (uint i = 1; i <= holonCountPairs; i++)
            {
                HolonInfo info = await _contractHandler.QueryAsync<GetHolonByIdyIdFunction, HolonInfo>(new() { Id = i });
                if (info == null || string.IsNullOrEmpty(info.Info)) continue;
                var holon = JsonConvert.DeserializeObject<Holon>(info.Info);
                if (holon == null || holon.MetaData == null) continue;
                if (type != HolonType.All && holon.HolonType != type) continue;

                bool matches = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All
                    ? metaKeyValuePairs.All(kv => holon.MetaData.ContainsKey(kv.Key) &&
                          string.Equals(holon.MetaData[kv.Key]?.ToString(), kv.Value, StringComparison.OrdinalIgnoreCase))
                    : metaKeyValuePairs.Any(kv => holon.MetaData.ContainsKey(kv.Key) &&
                          string.Equals(holon.MetaData[kv.Key]?.ToString(), kv.Value, StringComparison.OrdinalIgnoreCase));

                if (matches) holonsByPairs.Add(holon);
            }
            result.Result = holonsByPairs;
            result.IsError = false;
            result.Message = $"Successfully loaded {holonsByPairs.Count} holons by metadata pairs from Arbitrum";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

}
