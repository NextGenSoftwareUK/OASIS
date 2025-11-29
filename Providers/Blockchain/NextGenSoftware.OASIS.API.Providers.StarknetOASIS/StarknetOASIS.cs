using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using ProviderEnums = NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Starknet;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

public sealed class StarknetOASIS : OASISStorageProviderBase,
    IOASISStorageProvider,
    IOASISBlockchainStorageProvider,
    IOASISNETProvider,
    IOASISSmartContractProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _network;
    private readonly IStarknetRpcClient _rpcClient;
    private bool _isActivated;

    public StarknetOASIS(string network = "alpha-goerli", string apiBaseUrl = "https://alpha4.starknet.io")
    {
        ProviderName = nameof(StarknetOASIS);
        ProviderDescription = "Starknet privacy provider for cross-chain swaps";
        ProviderType = new EnumValue<ProviderEnums.ProviderType>(ProviderEnums.ProviderType.StarknetOASIS);
        ProviderCategory = new EnumValue<ProviderEnums.ProviderCategory>(ProviderEnums.ProviderCategory.StorageAndNetwork);

        _network = network;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl)
        };
        _rpcClient = new StarknetRpcClient(_httpClient, apiBaseUrl);
    }

    public override Task<OASISResult<bool>> ActivateProviderAsync()
    {
        var result = new OASISResult<bool>();
        try
        {
            _isActivated = true;
            IsProviderActivated = true;
            result.Result = true;
            result.IsError = false;
            result.Message = $"{ProviderName} activated against {_network}";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to activate {ProviderName}: {ex.Message}", ex);
        }

        return Task.FromResult(result);
    }

    public override OASISResult<bool> ActivateProvider()
    {
        return ActivateProviderAsync().Result;
    }

    public override Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        _isActivated = false;
        IsProviderActivated = false;
        return Task.FromResult(new OASISResult<bool>
        {
            Result = true,
            Message = $"{ProviderName} deactivated",
            IsError = false
        });
    }

    public override OASISResult<bool> DeActivateProvider()
    {
        return DeActivateProviderAsync().Result;
    }

    public async Task<OASISResult<string>> CreateAtomicSwapIntentAsync(string starknetAddress, decimal amount, string zcashAddress)
    {
        var result = new OASISResult<string>();

        if (!EnsureActivated(result))
        {
            return result;
        }

        try
        {
            var swapId = Guid.NewGuid().ToString();
            
            // Create a Holon to track this atomic swap
            var swapHolon = new Holon
            {
                Id = Guid.NewGuid(),
                Name = $"Atomic Swap {swapId}",
                Description = $"Zcash to Starknet atomic swap: {amount} ZEC -> {starknetAddress}",
                HolonType = HolonType.BridgeTransaction,
                ProviderKey = ProviderType.Value.ToString(),
                MetaData = new Dictionary<string, string>
                {
                    { "swapId", swapId },
                    { "starknetAddress", starknetAddress },
                    { "zcashAddress", zcashAddress },
                    { "amount", amount.ToString() },
                    { "status", BridgeTransactionStatus.Pending.ToString() },
                    { "fromChain", "Zcash" },
                    { "toChain", "Starknet" },
                    { "createdAt", DateTime.UtcNow.ToString("O") },
                    { "providerType", ProviderType.Value.ToString() }
                },
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            // Save the Holon using ProviderManager to persist to MongoDB or other storage providers
            var saveResult = await ProviderManager.Instance.SaveHolonAsync(swapHolon);
            if (saveResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to persist swap Holon: {saveResult.Message}");
                return result;
            }

            result.Result = swapId;
            result.IsError = false;
            result.Message = $"Atomic swap intent created for {amount} ZEC -> {starknetAddress}";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to create atomic swap intent: {ex.Message}", ex);
        }

        return result;
    }

    public async Task<OASISResult<decimal>> GetBalanceAsync(string accountAddress)
    {
        var result = new OASISResult<decimal>();

        if (!EnsureActivated(result))
        {
            return result;
        }

        try
        {
            var balanceResult = await _rpcClient.GetBalanceAsync(accountAddress);
            if (balanceResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, balanceResult.Message);
                return result;
            }

            result.Result = balanceResult.Result;
            result.IsError = false;
            result.Message = $"Balance retrieved for {accountAddress}";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to get balance: {ex.Message}", ex);
        }

        return result;
    }

    public async Task<OASISResult<BridgeTransactionStatus>> GetSwapStatusAsync(string swapId)
    {
        var result = new OASISResult<BridgeTransactionStatus>();

        if (!EnsureActivated(result))
        {
            return result;
        }

        try
        {
            // Load the swap Holon by metadata
            var searchParams = new SearchParams
            {
                SearchString = swapId,
                SearchType = SearchType.Contains
            };

            var holonsResult = await ProviderManager.Instance.SearchAsync(searchParams);
            if (holonsResult.IsError || holonsResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Swap {swapId} not found");
                return result;
            }

            var swapHolon = holonsResult.Result.Results?.FirstOrDefault(h => 
                h.MetaData?.ContainsKey("swapId") == true && 
                h.MetaData["swapId"] == swapId);

            if (swapHolon == null || !swapHolon.MetaData.ContainsKey("status"))
            {
                OASISErrorHandling.HandleError(ref result, $"Swap status not found for {swapId}");
                return result;
            }

            if (Enum.TryParse<BridgeTransactionStatus>(swapHolon.MetaData["status"], out var status))
            {
                result.Result = status;
                result.IsError = false;
                result.Message = $"Swap status retrieved: {status}";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Invalid status value: {swapHolon.MetaData["status"]}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to get swap status: {ex.Message}", ex);
        }

        return result;
    }

    public async Task<OASISResult<bool>> UpdateSwapStatusAsync(string swapId, BridgeTransactionStatus status, string transactionHash = null)
    {
        var result = new OASISResult<bool>();

        if (!EnsureActivated(result))
        {
            return result;
        }

        try
        {
            // Find the swap Holon
            var searchParams = new SearchParams
            {
                SearchString = swapId,
                SearchType = SearchType.Contains
            };

            var holonsResult = await ProviderManager.Instance.SearchAsync(searchParams);
            if (holonsResult.IsError || holonsResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Swap {swapId} not found");
                return result;
            }

            var swapHolon = holonsResult.Result.Results?.FirstOrDefault(h => 
                h.MetaData?.ContainsKey("swapId") == true && 
                h.MetaData["swapId"] == swapId);

            if (swapHolon == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Swap Holon not found for {swapId}");
                return result;
            }

            // Update status and transaction hash
            swapHolon.MetaData["status"] = status.ToString();
            if (!string.IsNullOrWhiteSpace(transactionHash))
            {
                swapHolon.MetaData["transactionHash"] = transactionHash;
            }
            swapHolon.MetaData["updatedAt"] = DateTime.UtcNow.ToString("O");
            swapHolon.ModifiedDate = DateTime.UtcNow;

            // Save updated Holon
            var saveResult = await ProviderManager.Instance.SaveHolonAsync(swapHolon);
            if (saveResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to update swap status: {saveResult.Message}");
                return result;
            }

            result.Result = true;
            result.IsError = false;
            result.Message = $"Swap status updated to {status}";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Failed to update swap status: {ex.Message}", ex);
        }

        return result;
    }

    private bool EnsureActivated<T>(OASISResult<T> result)
    {
        if (!_isActivated)
        {
            OASISErrorHandling.HandleError(ref result, $"{ProviderName} is not activated");
            return false;
        }

        return true;
    }

    private Task<OASISResult<T>> NotImplementedAsync<T>(string methodName)
    {
        return Task.FromResult(NotImplemented<T>(methodName));
    }

    private OASISResult<T> NotImplemented<T>(string methodName)
    {
        var result = new OASISResult<T>();
        OASISErrorHandling.HandleError(ref result, $"{methodName} is not implemented for {ProviderName}");
        return result;
    }

    public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0) => NotImplementedAsync<IEnumerable<IAvatar>>(nameof(LoadAllAvatarsAsync));
    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => NotImplemented<IEnumerable<IAvatar>>(nameof(LoadAllAvatars));
    public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0) => NotImplementedAsync<IAvatar>(nameof(LoadAvatarAsync));
    public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0) => NotImplemented<IAvatar>(nameof(LoadAvatar));
    public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0) => NotImplementedAsync<IAvatar>(nameof(LoadAvatarByProviderKeyAsync));
    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => NotImplemented<IAvatar>(nameof(LoadAvatarByProviderKey));
    public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0) => NotImplementedAsync<IAvatar>(nameof(LoadAvatarByUsernameAsync));
    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => NotImplemented<IAvatar>(nameof(LoadAvatarByUsername));
    public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0) => NotImplementedAsync<IAvatar>(nameof(LoadAvatarByEmailAsync));
    public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => NotImplemented<IAvatar>(nameof(LoadAvatarByEmail));
    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) => NotImplementedAsync<IAvatarDetail>(nameof(LoadAvatarDetailAsync));
    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => NotImplemented<IAvatarDetail>(nameof(LoadAvatarDetail));
    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0) => NotImplementedAsync<IAvatarDetail>(nameof(LoadAvatarDetailByEmailAsync));
    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => NotImplemented<IAvatarDetail>(nameof(LoadAvatarDetailByEmail));
    public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0) => NotImplementedAsync<IAvatarDetail>(nameof(LoadAvatarDetailByUsernameAsync));
    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => NotImplemented<IAvatarDetail>(nameof(LoadAvatarDetailByUsername));
    public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0) => NotImplementedAsync<IEnumerable<IAvatarDetail>>(nameof(LoadAllAvatarDetailsAsync));
    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => NotImplemented<IEnumerable<IAvatarDetail>>(nameof(LoadAllAvatarDetails));
    public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar) => NotImplementedAsync<IAvatar>(nameof(SaveAvatarAsync));
    public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar) => NotImplemented<IAvatar>(nameof(SaveAvatar));
    public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar) => NotImplementedAsync<IAvatarDetail>(nameof(SaveAvatarDetailAsync));
    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar) => NotImplemented<IAvatarDetail>(nameof(SaveAvatarDetail));
    public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true) => NotImplementedAsync<bool>(nameof(DeleteAvatarAsync));
    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => NotImplemented<bool>(nameof(DeleteAvatar));
    public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true) => NotImplementedAsync<bool>(nameof(DeleteAvatarAsync));
    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => NotImplemented<bool>(nameof(DeleteAvatar));
    public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true) => NotImplementedAsync<bool>(nameof(DeleteAvatarByEmailAsync));
    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => NotImplemented<bool>(nameof(DeleteAvatarByEmail));
    public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true) => NotImplementedAsync<bool>(nameof(DeleteAvatarByUsernameAsync));
    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => NotImplemented<bool>(nameof(DeleteAvatarByUsername));
    public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => NotImplementedAsync<ISearchResults>(nameof(SearchAsync));
    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => NotImplemented<ISearchResults>(nameof(Search));
    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();

        if (!EnsureActivated(result))
        {
            return result;
        }

        try
        {
            // Use ProviderManager to load from persistent storage
            var loadResult = await ProviderManager.Instance.LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (loadResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to load Holon: {loadResult.Message}");
                return result;
            }

            result.Result = loadResult.Result;
            result.IsError = false;
            result.Message = "Holon loaded successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading Holon: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }
    public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplementedAsync<IHolon>(nameof(LoadHolonAsync));
    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplemented<IHolon>(nameof(LoadHolon));
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(LoadHolonsForParentAsync));
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(LoadHolonsForParent));
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(LoadHolonsForParentAsync));
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(LoadHolonsForParent));
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(LoadHolonsByMetaDataAsync));
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(LoadHolonsByMetaData));
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(LoadHolonsByMetaDataAsync));
    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(LoadHolonsByMetaData));
    public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(LoadAllHolonsAsync));
    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(LoadAllHolons));
    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        var result = new OASISResult<IHolon>();

        if (!EnsureActivated(result))
        {
            return result;
        }

        if (holon == null)
        {
            OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
            return result;
        }

        try
        {
            // Use ProviderManager to save to persistent storage (MongoDB, etc.)
            var saveResult = await ProviderManager.Instance.SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
            if (saveResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to save Holon: {saveResult.Message}");
                return result;
            }

            result.Result = saveResult.Result;
            result.IsError = false;
            result.Message = "Holon saved successfully";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error saving Holon: {ex.Message}", ex);
        }

        return result;
    }

    public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
    }
    public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(SaveHolonsAsync));
    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => NotImplemented<IEnumerable<IHolon>>(nameof(SaveHolons));
    public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => NotImplementedAsync<IHolon>(nameof(DeleteHolonAsync));
    public override OASISResult<IHolon> DeleteHolon(Guid id) => NotImplemented<IHolon>(nameof(DeleteHolon));
    public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey) => NotImplementedAsync<IHolon>(nameof(DeleteHolonAsync));
    public override OASISResult<IHolon> DeleteHolon(string providerKey) => NotImplemented<IHolon>(nameof(DeleteHolon));
    public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) => NotImplementedAsync<bool>(nameof(ImportAsync));
    public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => NotImplemented<bool>(nameof(Import));
    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByIdAsync));
    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarById));
    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByUsernameAsync));
    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByUsername));
    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByEmailAsync));
    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(ExportAllDataForAvatarByEmail));
    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => NotImplementedAsync<IEnumerable<IHolon>>(nameof(ExportAllAsync));
    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => NotImplemented<IEnumerable<IHolon>>(nameof(ExportAll));

    public Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromAddress, string toAddress, decimal amount, string memo) => NotImplementedAsync<ITransactionRespone>(nameof(SendTransactionAsync));
    public OASISResult<ITransactionRespone> SendTransaction(string fromAddress, string toAddress, decimal amount, string memo) => NotImplemented<ITransactionRespone>(nameof(SendTransaction));

    public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long latitude, long longitude, int radius) => NotImplemented<IEnumerable<IAvatar>>(nameof(GetAvatarsNearMe));
    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long latitude, long longitude, int radius, HolonType type) => NotImplemented<IEnumerable<IHolon>>(nameof(GetHolonsNearMe));
}

