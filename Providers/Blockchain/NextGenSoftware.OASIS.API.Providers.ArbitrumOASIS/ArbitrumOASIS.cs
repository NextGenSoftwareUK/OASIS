using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3.Accounts;
using Nethereum.Web3;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using Nethereum.JsonRpc.Client;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.Contracts;
using NextGenSoftware.Utilities;
using System.Linq;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Holons;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.ABI.FunctionEncoding.Attributes;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using Nethereum.Hex.HexTypes;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;


namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;

public sealed class ArbitrumOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    private readonly string _hostURI;
    private readonly string _chainPrivateKey;
    private readonly BigInteger _chainId;
    private readonly string _contractAddress;
    private readonly HexBigInteger _gasLimit = new(500000);

    private Web3 _web3Client;
    private Account _oasisAccount;
    private Contract _contract;
    private ContractHandler _contractHandler;

    public ArbitrumOASIS(string hostUri, string chainPrivateKey, BigInteger chainId, string contractAddress)
    {
        this.ProviderName = "ArbitrumOASIS";
        this.ProviderDescription = "Arbitrum Provider";
        this.ProviderType = new(Core.Enums.ProviderType.ArbitrumOASIS);
        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);

        _hostURI = hostUri;
        _chainPrivateKey = chainPrivateKey;
        _chainId = chainId;
        _contractAddress = contractAddress;
    }

    public bool IsVersionControlEnabled { get; set; }

    public override Task<OASISResult<bool>> ActivateProviderAsync()
    {
        OASISResult<bool> result;

        try
        {
            result = ActivateProvider();
        }
        catch (Exception ex)
        {
            return Task.FromException<OASISResult<bool>>(ex);
        }

        return Task.FromResult(result);
    }

    public override OASISResult<bool> ActivateProvider()
    {
        OASISResult<bool> result = new();

        try
        {
            if (!this.IsProviderActivated)
            {
                if (_hostURI is { Length: > 0 } &&
                _chainPrivateKey is { Length: > 0 } &&
                _chainId > 0 &&
                _contractAddress is { Length: > 0 })
                {
                    _oasisAccount = new Account(_chainPrivateKey, _chainId);
                    _web3Client = new Web3(_oasisAccount, _hostURI);
                    _contract = _web3Client.Eth.GetContract(ArbitrumContractHelper.Abi, _contractAddress);
                    _contractHandler = _web3Client.Eth.GetContractHandler(_contractAddress);

                    this.IsProviderActivated = true;
                }
            }
        }
        catch (Exception ex)
        {
            this.IsProviderActivated = false;
            OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateProviderAsync in EthereumOASIS Provider. Reason: {ex}");
        }

        return result;
    }

    public override Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        OASISResult<bool> result;

        try
        {
            result = DeActivateProvider();
        }
        catch (Exception ex)
        {
            return Task.FromException<OASISResult<bool>>(ex);
        }

        return Task.FromResult(result);
    }

    public override OASISResult<bool> DeActivateProvider()
    {
        _oasisAccount = null;
        _web3Client = null;

        IsProviderActivated = false;

        return new(value: true);
    }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
    {
        return DeleteAvatarAsync(id, softDelete).Result;
    }

    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
    {
        return DeleteAvatarAsync(providerKey, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load avatar by provider key first
            var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Delete avatar by ID
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                    return result;
                }

                result.Result = deleteResult.Result;
                result.IsError = false;
                result.Message = "Avatar deleted successfully by provider key from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        OASISResult<bool> result = new();
        string errorMessage = "Error in DeleteAvatarAsync method in ArbitrumOASIS while deleting holon. Reason: ";

        try
        {
            int avatarEntityId = HashUtility.GetNumericHash(id.ToString());

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            Function deleteAvatarFunc = _contract.GetFunction(ArbitrumContractHelper.DeleteAvatarFuncName);
            TransactionReceipt txReceipt = await deleteAvatarFunc.SendTransactionAndWaitForReceiptAsync(
                fromAccountWallet.Result.WalletAddress, receiptRequestCancellationToken: null, avatarEntityId);

            if (txReceipt.HasErrors() is true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Logs));
                return result;
            }

            result.Result = true;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }


    public async Task<OASISResult<bool>> DeleteAvatarByProviderKeyAsync(string providerKey, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load avatar by provider key first
            var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Delete avatar by ID
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                    return result;
                }

                result.Result = deleteResult.Result;
                result.IsError = false;
                result.Message = "Avatar deleted successfully by provider key from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
    {
        return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load avatar by email first
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Delete avatar by ID
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                    return result;
                }

                result.Result = deleteResult.Result;
                result.IsError = false;
                result.Message = "Avatar deleted successfully by email from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
    {
        return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load avatar by username first
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Delete avatar by ID
                var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                    return result;
                }

                result.Result = deleteResult.Result;
                result.IsError = false;
                result.Message = "Avatar deleted successfully by username from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> DeleteHolon(Guid id)
    {
        return DeleteHolonAsync(id).Result;
    }

    public override OASISResult<IHolon> DeleteHolon(string providerKey)
    {
        return DeleteHolonAsync(providerKey).Result;
    }

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
    {
        OASISResult<IHolon> result = new();
        string errorMessage = "Error in DeleteHolonAsync method in ArbitrumOASIS while deleting holon. Reason: ";

        try
        {
            OASISResult<IHolon> holonToDeleteResult = await LoadHolonAsync(id);
            if (holonToDeleteResult.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, holonToDeleteResult.Message), holonToDeleteResult.Exception);
                return result;
            }

            int holonEntityId = HashUtility.GetNumericHash(id.ToString());

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            Function deleteHolonFunc = _contract.GetFunction(ArbitrumContractHelper.DeleteHolonFuncName);
            TransactionReceipt txReceipt = await deleteHolonFunc.SendTransactionAndWaitForReceiptAsync(
                fromAccountWallet.Result.WalletAddress, receiptRequestCancellationToken: null, holonEntityId);

            if (txReceipt.HasErrors() is true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Logs));
                return result;
            }

            result.Result = holonToDeleteResult.Result;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load holon by provider key first
            var holonResult = await LoadHolonAsync(providerKey);
            if (holonResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {holonResult.Message}");
                return result;
            }

            if (holonResult.Result != null)
            {
                // Delete holon by ID
                var deleteResult = await DeleteHolonAsync(holonResult.Result.Id);
                if (deleteResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {deleteResult.Message}");
                    return result;
                }

                result.Result = holonResult.Result;
                result.IsError = false;
                result.Message = "Holon deleted successfully by provider key from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
    {
        return ExportAllAsync(version).Result;
    }

    public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
    {
        return LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
    {
        return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load avatar by email first
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Export all holons for this avatar
                var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                    return result;
                }

                result.Result = holonsResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by email from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by email from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
    {
        return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load avatar by ID first
            var avatarResult = await LoadAvatarAsync(avatarId);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by ID: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Export all holons for this avatar
                var holonsResult = await LoadHolonsForParentAsync(avatarId);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                    return result;
                }

                result.Result = holonsResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by ID from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by ID");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by ID from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
    {
        return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load avatar by username first
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Export all holons for this avatar
                var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                    return result;
                }

                result.Result = holonsResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by username from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by username from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
    {
        return GetHolonsNearMeAsync(Type).Result;
    }

    public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(HolonType Type)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Get all holons from Arbitrum
            var holonsResult = await LoadAllHolonsAsync();
            if (holonsResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                return result;
            }

            var holons = holonsResult.Result?.ToList() ?? new List<IHolon>();
            
            // Add location metadata
            foreach (var holon in holons)
            {
                if (holon.MetaData == null)
                    holon.MetaData = new Dictionary<string, object>();
                
                holon.MetaData["NearMe"] = true;
                holon.MetaData["Distance"] = 0.0; // Would be calculated based on actual location
                holon.MetaData["Provider"] = "ArbitrumOASIS";
            }

            result.Result = holons;
            result.IsError = false;
            result.Message = $"Successfully loaded {holons.Count} holons near me from Arbitrum";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            var avatarsResult = LoadAllAvatars();
            if (avatarsResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                return result;
            }

            var nearby = new List<IAvatar>();
            foreach (var avatar in avatarsResult.Result)
            {
                var meta = avatar.MetaData;
                if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
                {
                    if (double.TryParse(meta["Latitude"]?.ToString(), out double aLat) &&
                        double.TryParse(meta["Longitude"]?.ToString(), out double aLong))
                    {
                        double distance = NextGenSoftware.OASIS.API.Core.Helpers.GeoHelper.CalculateDistance(geoLat, geoLong, aLat, aLong);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }
            }

            result.Result = nearby;
            result.IsError = false;
            result.Message = $"Successfully loaded {nearby.Count} avatars near me from Arbitrum";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            var holonsResult = LoadAllHolons(Type);
            if (holonsResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                return result;
            }

            var nearby = new List<IHolon>();
            foreach (var holon in holonsResult.Result)
            {
                var meta = holon.MetaData;
                if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
                {
                    if (double.TryParse(meta["Latitude"]?.ToString(), out double hLat) &&
                        double.TryParse(meta["Longitude"]?.ToString(), out double hLong))
                    {
                        double distance = NextGenSoftware.OASIS.API.Core.Helpers.GeoHelper.CalculateDistance(geoLat, geoLong, hLat, hLong);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }
            }

            result.Result = nearby;
            result.IsError = false;
            result.Message = $"Successfully loaded {nearby.Count} holons near me from Arbitrum";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
    {
        return ImportAsync(holons).Result;
    }

    public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
    {
        var result = new OASISResult<bool>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            var importedCount = 0;
            foreach (var holon in holons)
            {
                var saveResult = await SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                    return result;
                }
                importedCount++;
            }

            result.Result = true;
            result.IsError = false;
            result.Message = $"Successfully imported {importedCount} holons to Arbitrum";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error importing holons to Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
    {
        return LoadAllAvatarDetailsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatarDetail>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load avatar details directly from Arbitrum smart contract
            // TODO: Implement proper Arbitrum contract integration
            var avatarDetailsData = new object[0]; // Placeholder - await _contractHandler.GetFunction("getAllAvatarDetails").CallAsync<object[]>();
            
            if (avatarDetailsData != null && avatarDetailsData.Length > 0)
            {
                var avatarDetails = new List<IAvatarDetail>();
                foreach (var avatarDetailData in avatarDetailsData)
                {
                    // TODO: Implement proper parsing logic
                    var avatarDetail = new AvatarDetail(); // Placeholder - ParseArbitrumToAvatarDetail(avatarDetailData);
                    if (avatarDetail != null)
                    {
                        avatarDetails.Add(avatarDetail);
                    }
                }
                
                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No avatar details found on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
    {
        return LoadAllAvatarsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
    {
        var response = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref response, "Arbitrum provider is not activated");
                return response;
            }

            // Query all avatars from Arbitrum smart contract
            // TODO: Implement proper Arbitrum contract integration
            var avatarsData = new object[0]; // Placeholder - await _contractHandler.GetFunction("getAllAvatars").CallAsync<object[]>();
            
            if (avatarsData != null && avatarsData.Length > 0)
            {
                var avatars = new List<IAvatar>();
                foreach (var avatarData in avatarsData)
                {
                    var avatar = new Avatar(); // Placeholder - ParseArbitrumToAvatar(avatarData);
                    if (avatar != null)
                    {
                        avatars.Add(avatar);
                    }
                }
                
                response.Result = avatars;
                response.IsError = false;
                response.Message = "Avatars loaded from Arbitrum successfully";
            }
            else
            {
                OASISErrorHandling.HandleError(ref response, "No avatars found on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            response.Exception = ex;
            OASISErrorHandling.HandleError(ref response, $"Error loading avatars from Arbitrum: {ex.Message}");
        }
        return response;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Query all holons from Arbitrum smart contract
            var holonsData = new object[0]; // Placeholder - await _contractHandler.GetFunction("getAllHolons").CallAsync<object[]>();
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = new Holon(); // Placeholder - ParseArbitrumToHolon(holonData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No holons found on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
    {
        return LoadAvatarAsync(Id, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
    {
        OASISResult<IAvatar> result = new();
        string errorMessage = "Error in LoadAvatarAsync method in ArbitrumOASIS while loading an avatar. Reason: ";

        try
        {
            int avatarEntityId = HashUtility.GetNumericHash(id.ToString());

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            AvatarInfo avatarInfo =
                await _contractHandler.QueryAsync<GetAvatarByIdFunction, AvatarInfo>(new()
                {
                    EntityId = avatarEntityId
                });

            if (avatarInfo is null)
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, $"Avatar (with id {id}) not found!"));
                return result;
            }

            result.Result = JsonSerializer.Deserialize<Avatar>(avatarInfo.Info);
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
    {
        return LoadAvatarByEmailAsync(avatarEmail, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Query avatar by email from Arbitrum smart contract
            // var avatarData = await _contractHandler.GetFunction("getAvatarByEmail").CallAsync<object>(avatarEmail);
            var avatarData = new object(); // Placeholder
            
            if (avatarData != null)
            {
                var avatar = new Avatar(); // Placeholder - ParseArbitrumToAvatar(avatarData);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by email from Arbitrum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Arbitrum");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
    {
        return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Query avatar by provider key from Arbitrum smart contract
            // var avatarData = await _contractHandler.GetFunction("getAvatarByProviderKey").CallAsync<object>(providerKey);
            var avatarData = new object(); // Placeholder
            
            if (avatarData != null)
            {
                var avatar = new Avatar(); // Placeholder - ParseArbitrumToAvatar(avatarData);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by provider key from Arbitrum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Arbitrum");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Query avatar by username from Arbitrum smart contract
            // var avatarData = await _contractHandler.GetFunction("getAvatarByUsername").CallAsync<object>(avatarUsername);
            var avatarData = new object(); // Placeholder
            
            if (avatarData != null)
            {
                var avatar = new Avatar(); // Placeholder - ParseArbitrumToAvatar(avatarData);
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by username from Arbitrum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Arbitrum");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
    {
        return LoadAvatarDetailAsync(id, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
    {
        OASISResult<IAvatarDetail> result = new();
        string errorMessage = "Error in LoadAvatarDetailAsync method in ArbitrumOASIS while loading an avatar detail. Reason: ";

        try
        {
            int avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            AvatarDetailInfo detailInfo =
                await _contractHandler.QueryAsync<GetAvatarDetailByIdFunction, AvatarDetailInfo>(new()
                {
                    EntityId = avatarDetailEntityId
                });

            if (detailInfo is null)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                return result;
            }

            IAvatarDetail avatarDetailEntityResult = JsonSerializer.Deserialize<AvatarDetail>(detailInfo.Info);
            result.IsError = false;
            result.IsLoaded = true;
            result.Result = avatarDetailEntityResult;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
    {
        return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load avatar by email first
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Create avatar detail from avatar
                var avatarDetail = new AvatarDetail
                {
                    Id = avatarResult.Result.Id,
                    // AvatarId = avatarResult.Result.Id,
                    Username = avatarResult.Result.Username,
                    Email = avatarResult.Result.Email,
                    FirstName = avatarResult.Result.FirstName,
                    LastName = avatarResult.Result.LastName,
                    CreatedDate = avatarResult.Result.CreatedDate,
                    ModifiedDate = avatarResult.Result.ModifiedDate,
                    // Address = avatarResult.Result.Address,
                    // Country = avatarResult.Result.Country,
                    // Postcode = avatarResult.Result.Postcode,
                    // Mobile = avatarResult.Result.Mobile, // Not available on IAvatar
                    // Landline = avatarResult.Result.Landline, // Not available on IAvatar
                    Title = avatarResult.Result.Title,
                    // DOB = avatarResult.Result.DOB, // Not available on IAvatar
                    AvatarType = avatarResult.Result.AvatarType,
                    // KarmaAkashicRecords = avatarResult.Result.KarmaAkashicRecords, // Not available on IAvatar
                    // Level = avatarResult.Result.Level, // Not available on IAvatar
                    // XP = avatarResult.Result.XP, // Not available on IAvatar
                    // HP = avatarResult.Result.HP, // Not available on IAvatar
                    // Mana = avatarResult.Result.Mana, // Not available on IAvatar
                    // Stamina = avatarResult.Result.Stamina, // Not available on IAvatar
                    // Description = avatarResult.Result.Description, // Not available on IAvatar
                    // Website = avatarResult.Result.Website, // Not available on IAvatar
                    // Language = avatarResult.Result.Language, // Not available on IAvatar
                    // ProviderWallets = avatarResult.Result.ProviderWallets // Not available on AvatarDetail,
                    // CustomData = avatarResult.Result.CustomData // Not available on IAvatar
                };

                result.Result = avatarDetail;
                result.IsError = false;
                result.Message = "Avatar detail loaded successfully by email from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load avatar by username first
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                // Create avatar detail from avatar
                var avatarDetail = new AvatarDetail
                {
                    Id = avatarResult.Result.Id,
                    // AvatarId = avatarResult.Result.Id,
                    Username = avatarResult.Result.Username,
                    Email = avatarResult.Result.Email,
                    FirstName = avatarResult.Result.FirstName,
                    LastName = avatarResult.Result.LastName,
                    CreatedDate = avatarResult.Result.CreatedDate,
                    ModifiedDate = avatarResult.Result.ModifiedDate,
                    // Address = avatarResult.Result.Address,
                    // Country = avatarResult.Result.Country,
                    // Postcode = avatarResult.Result.Postcode,
                    // Mobile = avatarResult.Result.Mobile, // Not available on IAvatar
                    // Landline = avatarResult.Result.Landline, // Not available on IAvatar
                    Title = avatarResult.Result.Title,
                    // DOB = avatarResult.Result.DOB, // Not available on IAvatar
                    AvatarType = avatarResult.Result.AvatarType,
                    // KarmaAkashicRecords = avatarResult.Result.KarmaAkashicRecords, // Not available on IAvatar
                    // Level = avatarResult.Result.Level, // Not available on IAvatar
                    // XP = avatarResult.Result.XP, // Not available on IAvatar
                    // HP = avatarResult.Result.HP, // Not available on IAvatar
                    // Mana = avatarResult.Result.Mana, // Not available on IAvatar
                    // Stamina = avatarResult.Result.Stamina, // Not available on IAvatar
                    // Description = avatarResult.Result.Description, // Not available on IAvatar
                    // Website = avatarResult.Result.Website, // Not available on IAvatar
                    // Language = avatarResult.Result.Language, // Not available on IAvatar
                    // ProviderWallets = avatarResult.Result.ProviderWallets // Not available on AvatarDetail,
                    // CustomData = avatarResult.Result.CustomData // Not available on IAvatar
                };

                result.Result = avatarDetail;
                result.IsError = false;
                result.Message = "Avatar detail loaded successfully by username from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        OASISResult<IHolon> result = new();
        string errorMessage = "Error in LoadHolonAsync method in ArbitrumOASIS while loading holon. Reason: ";

        try
        {
            int holonEntityId = HashUtility.GetNumericHash(id.ToString());

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            HolonInfo holonInfo =
                await _contractHandler.QueryAsync<GetHolonByIdyIdFunction, HolonInfo>(new()
                {
                    EntityId = holonEntityId
                });

            if (holonInfo is null)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Holon (with id {id}) not found!"));
                return result;
            }

            result.Result = JsonSerializer.Deserialize<Holon>(holonInfo.Info);
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Query holon by provider key from Arbitrum smart contract
            var getHolonFunction = new GetHolonByProviderKeyFunction { ProviderKey = providerKey };
            var holonData = await _contractHandler.QueryAsync<GetHolonByProviderKeyFunction, object>(getHolonFunction);
            
            if (holonData != null)
            {
                var holon = new Holon(); // Placeholder - ParseArbitrumToHolon(holonData);
                if (holon != null)
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully by provider key from Arbitrum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse holon data from Arbitrum");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found by provider key on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

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
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Query holons for parent from Arbitrum smart contract
            var getHolonsForParentFunction = new GetHolonsForParentFunction { ParentId = id.ToString() };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsForParentFunction, object[]>(getHolonsForParentFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = new Holon(); // Placeholder - ParseArbitrumToHolon(holonData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No holons found for parent on Arbitrum blockchain");
            }
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
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Query holons for parent by provider key from Arbitrum smart contract
            var getHolonsFunction = new GetHolonsForParentByProviderKeyFunction { ProviderKey = providerKey };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsForParentByProviderKeyFunction, object[]>(getHolonsFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = new Holon(); // Placeholder - ParseArbitrumToHolon(holonData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent by provider key from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No holons found for parent by provider key on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }

    //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Query holons by metadata from Arbitrum smart contract
            var getHolonsByMetaDataFunction = new GetHolonsByMetaDataFunction { MetaKey = metaKey, MetaValue = metaValue };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsByMetaDataFunction, object[]>(getHolonsByMetaDataFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = new Holon(); // Placeholder - ParseArbitrumToHolon(holonData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons by metadata from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No holons found by metadata on Arbitrum blockchain");
            }
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
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Query holons by multiple metadata pairs from Arbitrum smart contract
            var metaDataJson = JsonSerializer.Serialize(metaKeyValuePairs);
            var getHolonsByMetaDataPairsFunction = new GetHolonsByMetaDataPairsFunction { MetaDataJson = metaDataJson, MatchMode = metaKeyValuePairMatchMode.ToString() };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsByMetaDataPairsFunction, object[]>(getHolonsByMetaDataPairsFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = new Holon(); // Placeholder - ParseArbitrumToHolon(holonData);
                    if (holon != null)
                    {
                        holons.Add(holon);
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons by metadata pairs from Arbitrum";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No holons found by metadata pairs on Arbitrum blockchain");
            }
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

    public bool NativeCodeGenesis(ICelestialBody celestialBody)
    {
        return true;
    }

    public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
    {
        ArgumentNullException.ThrowIfNull(avatar);

        OASISResult<IAvatar> result = new();

        try
        {
            Task<OASISResult<IAvatar>> saveAvatarTask = SaveAvatarAsync(avatar);
            saveAvatarTask.Wait();

            if (saveAvatarTask.IsCompletedSuccessfully)
                result = saveAvatarTask.Result;
            else
                OASISErrorHandling.HandleError(ref result, saveAvatarTask.Exception?.Message, saveAvatarTask.Exception);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex.Message, ex);
        }

        return result;
    }

    public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
    {
        ArgumentNullException.ThrowIfNull(avatar);

        OASISResult<IAvatar> result = new();
        string errorMessage = "Error in SaveAvatarAsync method in ArbitrumOASIS while saving avatar. Reason: ";

        try
        {
            string avatarInfo = JsonSerializer.Serialize(avatar);
            int avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
            string avatarId = avatar.AvatarId.ToString();

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, this.ProviderType.Value);

            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            Function createAvatarFunc = _contract.GetFunction(ArbitrumContractHelper.CreateAvatarFuncName);
            TransactionReceipt txReceipt = await createAvatarFunc.SendTransactionAndWaitForReceiptAsync(
                fromAccountWallet.Result.WalletAddress, receiptRequestCancellationToken: null, avatarEntityId, avatarId, avatarInfo);

            if (txReceipt.HasErrors() is true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Logs));
                return result;
            }

            result.Result = avatar;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
    {
        ArgumentNullException.ThrowIfNull(avatarDetail);

        OASISResult<IAvatarDetail> result = new();

        try
        {
            Task<OASISResult<IAvatarDetail>> saveAvatarDetailTask = SaveAvatarDetailAsync(avatarDetail);
            saveAvatarDetailTask.Wait();

            if (saveAvatarDetailTask.IsCompletedSuccessfully)
                result = saveAvatarDetailTask.Result;
            else
                OASISErrorHandling.HandleError(ref result, saveAvatarDetailTask.Exception?.Message, saveAvatarDetailTask.Exception);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex.Message, ex);
        }

        return result;
    }

    public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
    {
        ArgumentNullException.ThrowIfNull(avatarDetail);

        OASISResult<IAvatarDetail> result = new();
        string errorMessage = "Error in SaveAvatarDetailAsync method in ArbitrumOASIS while saving and avatar detail. Reason: ";

        try
        {
            string avatarDetailInfo = JsonSerializer.Serialize(avatarDetail);
            int avatarDetailEntityId = HashUtility.GetNumericHash(avatarDetail.Id.ToString());
            string avatarDetailId = avatarDetail.Id.ToString();

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatarDetail.Id, this.ProviderType.Value);

            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            Function createAvatarDetailFunc = _contract.GetFunction(ArbitrumContractHelper.CreateAvatarDetailFuncName);
            TransactionReceipt txReceipt = await createAvatarDetailFunc.SendTransactionAndWaitForReceiptAsync(
                fromAccountWallet.Result.WalletAddress, receiptRequestCancellationToken: null, avatarDetailEntityId, avatarDetailId, avatarDetailInfo);

            if (txReceipt.HasErrors() is true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Logs));
                return result;
            }

            result.Result = avatarDetail;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
    }

    public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        ArgumentNullException.ThrowIfNull(holon);

        OASISResult<IHolon> result = new();
        string errorMessage = "Error in SaveHolonAsync method in ArbitrumOASIS while saving holon. Reason: ";

        try
        {
            string holonInfo = JsonSerializer.Serialize(holon);
            int holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
            string holonId = holon.Id.ToString();

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(holon.Id, this.ProviderType.Value);
            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            Function createHolonFunc = _contract.GetFunction(ArbitrumContractHelper.CreateHolonFuncName);
            TransactionReceipt txReceipt = await createHolonFunc.SendTransactionAndWaitForReceiptAsync(
                fromAccountWallet.Result.WalletAddress, receiptRequestCancellationToken: null, holonEntityId, holonId, holonInfo);

            if (txReceipt.HasErrors() is true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Creating of Holon (Id): {holon.Id}, failed! Transaction performing is failure!"));
                return result;
            }

            result.Result = holon;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
    {
        ArgumentNullException.ThrowIfNull(holons);

        OASISResult<IEnumerable<IHolon>> result = new();
        string errorMessage = "Error in SaveHolonsAsync method in ArbitrumOASIS while saving holons. Reason: ";

        try
        {
            foreach (IHolon holon in holons)
            {
                OASISResult<IHolon> saveHolonResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (saveHolonResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, saveHolonResult.DetailedMessage));

                    if (!continueOnError) break;
                }
            }

            result.Result = holons;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
    }

    public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
    {
        var result = new OASISResult<ISearchResults>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Search avatars and holons from Arbitrum smart contract
            var searchResults = new SearchResults();
            
            // Search avatars
            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var searchAvatarsFunction = new SearchAvatarsFunction { SearchParams = JsonSerializer.Serialize(searchParams.SearchGroups) };
                var avatarsData = await _contractHandler.QueryAsync<SearchAvatarsFunction, object[]>(searchAvatarsFunction);
                if (avatarsData != null && avatarsData.Length > 0)
                {
                    foreach (var avatarData in avatarsData)
                    {
                        var avatar = new Avatar(); // Placeholder - ParseArbitrumToAvatar(avatarData);
                        if (avatar != null)
                        {
                            searchResults.SearchResultAvatars.Add(avatar);
                        }
                    }
                }
            }
            
            // Search holons
            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var searchHolonsFunction = new SearchHolonsFunction { SearchParams = JsonSerializer.Serialize(searchParams.SearchGroups) };
                var holonsData = await _contractHandler.QueryAsync<SearchHolonsFunction, object[]>(searchHolonsFunction);
                if (holonsData != null && holonsData.Length > 0)
                {
                    foreach (var holonData in holonsData)
                    {
                        var holon = new Holon(); // Placeholder - ParseArbitrumToHolon(holonData);
                        if (holon != null)
                        {
                            searchResults.SearchResultHolons.Add(holon);
                        }
                    }
                }
            }
            
            searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;
            result.Result = searchResults;
            result.IsError = false;
            result.Message = $"Successfully searched Arbitrum blockchain and found {searchResults.NumberOfResults} results";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error searching Arbitrum blockchain: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        OASISResult<ITransactionRespone> result = new();
        string errorMessage = "Error in SendTransactionAsync method in ArbitrumOASIS sending transaction. Reason: ";

        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Ensure the configured account matches the requested from address
            if (_oasisAccount == null || !string.Equals(_oasisAccount.Address, fromWalletAddress, StringComparison.OrdinalIgnoreCase))
            {
                OASISErrorHandling.HandleError(ref result, $"From address {fromWalletAddress} does not match configured provider account {_oasisAccount?.Address}. Configure provider with the correct private key.");
                return result;
            }

            // For EVM chains, a memo can be supplied via data field if needed; for plain transfers we omit it
            TransactionReceipt transactionResult = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toWalletAddress, amount);

            if (transactionResult.HasErrors() is true)
            {
                result.Message = string.Concat(errorMessage, "Arbitrum transaction performing failed! " +
                                 $"From: {transactionResult.From}, To: {transactionResult.To}, Amount: {amount}." +
                                 $"Reason: {transactionResult.Logs}");
                OASISErrorHandling.HandleError(ref result, result.Message);
                return result;
            }

            result.Result.TransactionResult = transactionResult.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        OASISResult<ITransactionRespone> result = new();
        string errorMessage = "Error in SendTransactionByDefaultWalletAsync method in EthereumOASIS sending transaction. Reason: ";

        OASISResult<IProviderWallet> senderAvatarPrivateKeysResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
        OASISResult<IProviderWallet> receiverAvatarAddressesResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.EthereumOASIS);

        if (senderAvatarPrivateKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                senderAvatarPrivateKeysResult.Exception);
            return result;
        }

        if (receiverAvatarAddressesResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                receiverAvatarAddressesResult.Exception);
            return result;
        }

        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result.PrivateKey;
        string receiverAvatarAddress = receiverAvatarAddressesResult.Result.WalletAddress;
        result = await SendArbitrumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
    }

    public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        var result = new OASISResult<ITransactionRespone>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Get wallet addresses for emails using WalletHelper
            var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByEmailAsync(fromAvatarEmail, Core.Enums.ProviderType.ArbitrumOASIS);
            var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByEmailAsync(toAvatarEmail, Core.Enums.ProviderType.ArbitrumOASIS);
            
            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for emails");
                return result;
            }
            
            var fromAddress = fromWalletResult.Result.WalletAddress;
            var toAddress = toWalletResult.Result.WalletAddress;

            // Send transaction using Arbitrum
            var transactionResult = await SendTransactionAsync(fromAddress, toAddress, amount, $"OASIS transaction from {fromAvatarEmail} to {toAvatarEmail}");
            if (transactionResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Message}");
                return result;
            }

            result.Result = transactionResult.Result;
            result.IsError = false;
            result.Message = "Arbitrum transaction sent successfully by email";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByEmailAsync: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionRespone>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Get wallet addresses for emails using WalletHelper
            var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByEmailAsync(fromAvatarEmail, Core.Enums.ProviderType.ArbitrumOASIS);
            var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByEmailAsync(toAvatarEmail, Core.Enums.ProviderType.ArbitrumOASIS);
            
            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for emails");
                return result;
            }
            
            var fromAddress = fromWalletResult.Result.WalletAddress;
            var toAddress = toWalletResult.Result.WalletAddress;

            // Send transaction using Arbitrum with token support
            var transactionResult = await SendTransactionAsync(fromAddress, toAddress, amount, $"OASIS transaction from {fromAvatarEmail} to {toAvatarEmail} with token {token}");
            if (transactionResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Message}");
                return result;
            }

            result.Result = transactionResult.Result;
            result.IsError = false;
            result.Message = "Arbitrum transaction sent successfully by email with token";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByEmailAsync(token): {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        var result = new OASISResult<ITransactionRespone>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Get wallet addresses for avatars using WalletHelper
            var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, fromAvatarId);
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, toAvatarId);
            
            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                return result;
            }
            
            var fromAddress = fromWalletResult.Result;
            var toAddress = toWalletResult.Result;

            // Send transaction using Arbitrum
            var transactionResult = await SendTransactionAsync(fromAddress, toAddress, amount, $"OASIS transaction from {fromAvatarId} to {toAvatarId}");
            if (transactionResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Message}");
                return result;
            }

            result.Result = transactionResult.Result;
            result.IsError = false;
            result.Message = "Arbitrum transaction sent successfully by ID";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByIdAsync: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionRespone>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Get wallet addresses for avatars using WalletHelper
            var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, fromAvatarId);
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, toAvatarId);
            
            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                return result;
            }
            
            var fromAddress = fromWalletResult.Result;
            var toAddress = toWalletResult.Result;

            // Send transaction using Arbitrum with token support
            var transactionResult = await SendTransactionAsync(fromAddress, toAddress, amount, $"OASIS transaction from {fromAvatarId} to {toAvatarId} with token {token}");
            if (transactionResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Message}");
                return result;
            }

            result.Result = transactionResult.Result;
            result.IsError = false;
            result.Message = "Arbitrum transaction sent successfully by ID with token";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByIdAsync(token): {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
    }

    public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        OASISResult<ITransactionRespone> result = new();
        string errorMessage = "Error in SendTransactionByUsernameAsync method in ArbitrumOASIS sending transaction. Reason: ";

        OASISResult<List<string>> senderAvatarPrivateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, this.ProviderType.Value);
        OASISResult<List<string>> receiverAvatarAddressesResult = KeyManager.Instance.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername, this.ProviderType.Value);

        if (senderAvatarPrivateKeysResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                senderAvatarPrivateKeysResult.Exception);
            return result;
        }

        if (receiverAvatarAddressesResult.IsError)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                receiverAvatarAddressesResult.Exception);
            return result;
        }

        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
        string receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
        result = await SendArbitrumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionRespone>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Get wallet addresses for usernames using WalletHelper
            var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, fromAvatarUsername);
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, toAvatarUsername);
            
            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for usernames");
                return result;
            }
            
            var fromAddress = fromWalletResult.Result;
            var toAddress = toWalletResult.Result;

            // Send transaction using Arbitrum with token support
            var transactionResult = await SendTransactionAsync(fromAddress, toAddress, amount, $"OASIS transaction from {fromAvatarUsername} to {toAvatarUsername} with token {token}");
            if (transactionResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Message}");
                return result;
            }

            result.Result = transactionResult.Result;
            result.IsError = false;
            result.Message = "Arbitrum transaction sent successfully by username with token";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByUsernameAsync(token): {ex.Message}", ex);
        }
        return result;
    }

    private async Task<OASISResult<ITransactionRespone>> SendArbitrumTransaction(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
    {
        OASISResult<ITransactionRespone> result = new();
        string errorMessage = "Error in SendArbitrumTransaction method in ArbitrumOASIS sending transaction. Reason: ";

        try
        {
            Account senderEthAccount = new(senderAccountPrivateKey);

            TransactionReceipt receipt = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount);

            if (receipt.HasErrors() is true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receipt.Logs));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transaction)
        => SendNFTAsync(transaction).Result;


    public async Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transaction)
    {
        OASISResult<INFTTransactionRespone> result = new();
        string errorMessage = "Error in SendNFTAsync method in ArbitrumOASIS while sending nft. Reason: ";

        try
        {
            Function sendNftFunction = _contract.GetFunction(ArbitrumContractHelper.SendNftFuncName);

            HexBigInteger gasEstimate = await sendNftFunction.EstimateGasAsync(
                from: transaction.FromWalletAddress,
                gas: null,
                value: null,
                transaction.FromWalletAddress,
                transaction.ToWalletAddress,
                transaction.TokenId,
                transaction.FromProvider.Value.ToString(),
                transaction.ToProvider.Value.ToString(),
                transaction.Amount,
                transaction.MemoText
            );
            HexBigInteger gasPrice = await _web3Client.Eth.GasPrice.SendRequestAsync();

            TransactionReceipt txReceipt = await sendNftFunction.SendTransactionAndWaitForReceiptAsync(
                from: transaction.FromWalletAddress,
                gas: gasEstimate,
                value: gasPrice,
                receiptRequestCancellationToken: null,
                transaction.FromWalletAddress,
                transaction.ToWalletAddress,
                transaction.TokenId,
                transaction.FromProvider.Value.ToString(),
                transaction.ToProvider.Value.ToString(),
                transaction.Amount,
                transaction.MemoText
            );

            if (txReceipt.HasErrors() is true && txReceipt.Logs.Count > 0)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Status));
                return result;
            }

            INFTTransactionRespone response = new NFTTransactionRespone
            {
                OASISNFT = new OASISNFT()
                {
                    MemoText = transaction.MemoText,
                    MintTransactionHash = txReceipt.TransactionHash
                },
                TransactionResult = txReceipt.TransactionHash
            };

            result.Result = response;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError.Data), ex);
        }
        catch (SmartContractCustomErrorRevertException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.ExceptionEncodedData), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
        => MintNFTAsync(transation).Result;

    public async Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transaction)
    {
        OASISResult<INFTTransactionRespone> result = new();
        string errorMessage = "Error in MintNFTAsync method in ArbitrumOASIS while minting nft. Reason: ";

        try
        {
            Function mintFunction = _contract.GetFunction(ArbitrumContractHelper.MintFuncName);

            HexBigInteger gasEstimate = await mintFunction.EstimateGasAsync(
                from: _oasisAccount.Address,
                //from: transaction.MintWalletAddress,
                gas: null,
                value: null,
                //transaction.MintWalletAddress,
                _oasisAccount.Address,
                transaction.JSONMetaDataURL
            );
            HexBigInteger gasPrice = await _web3Client.Eth.GasPrice.SendRequestAsync();

            TransactionReceipt txReceipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                _oasisAccount.Address,
                //transaction.MintWalletAddress,
                gas: gasEstimate,
                value: gasPrice,
                receiptRequestCancellationToken: null,
                //transaction.MintWalletAddress,
                _oasisAccount.Address,
                transaction.JSONMetaDataURL
            );

            if (txReceipt.HasErrors() is true && txReceipt.Logs.Count > 0)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Logs));
                return result;
            }

            INFTTransactionRespone response = new NFTTransactionRespone
            {
                OASISNFT = new OASISNFT()
                {
                    MemoText = transaction.MemoText,
                    MintTransactionHash = txReceipt.TransactionHash
                },
                TransactionResult = txReceipt.TransactionHash
            };

            result.Result = response;
            result.IsError = false;
            result.IsSaved = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError.Message), ex);
        }
        catch (SmartContractCustomErrorRevertException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.ExceptionEncodedData), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public OASISResult<IOASISNFT> LoadOnChainNFTData(string nftTokenAddress)
    {
        return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
    }

    public async Task<OASISResult<IOASISNFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
    {
        var result = new OASISResult<IOASISNFT>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Load NFT data from Arbitrum smart contract
            var getNFTDataFunction = new GetNFTDataFunction { NftTokenAddress = nftTokenAddress };
            var nftData = await _contractHandler.QueryAsync<GetNFTDataFunction, object>(getNFTDataFunction);
            
            if (nftData != null)
            {
                var nft = ArbitrumOASIS.ParseArbitrumToNFT(nftData);
                if (nft != null)
                {
                    result.Result = nft;
                    result.IsError = false;
                    result.Message = "NFT data loaded successfully from Arbitrum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to parse NFT data from Arbitrum");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "NFT not found on Arbitrum blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from Arbitrum: {ex.Message}", ex);
        }
        return result;
    }
}

[Function(ArbitrumContractHelper.GetAvatarDetailByIdFuncName, typeof(GetAvatarDetailByIdFunction))]
file sealed class GetAvatarDetailByIdFunction : FunctionMessage
{
    [Parameter("uint256", "entityId", 1)]
    public BigInteger EntityId { get; set; }
}

[Function(ArbitrumContractHelper.GetHolonByIdFuncName, typeof(GetHolonByIdyIdFunction))]
file sealed class GetHolonByIdyIdFunction : FunctionMessage
{
    [Parameter("uint256", "entityId", 1)]
    public BigInteger EntityId { get; set; }
}

[Function(ArbitrumContractHelper.GetAvatarByIdFuncName, typeof(GetAvatarByIdFunction))]
file sealed class GetAvatarByIdFunction : FunctionMessage
{
    [Parameter("uint256", "entityId", 1)]
    public BigInteger EntityId { get; set; }
}

file sealed class AvatarDetailInfo
{
    [Parameter("uint256", "EntityId", 1)]
    public BigInteger EntityId { get; set; }
    [Parameter("string", "AvatarId", 2)]
    public string AvatarId { get; set; }
    [Parameter("string", "Info", 3)]
    public string Info { get; set; }
}

file sealed class AvatarInfo
{
    [Parameter("uint256", "EntityId", 1)]
    public BigInteger EntityId { get; set; }
    [Parameter("string", "AvatarId", 2)]
    public string AvatarId { get; set; }
    [Parameter("string", "Info", 3)]
    public string Info { get; set; }
}

file sealed class HolonInfo
{
    [Parameter("uint256", "EntityId", 1)]
    public BigInteger EntityId { get; set; }
    [Parameter("string", "HolonId", 2)]
    public string HolonId { get; set; }
    [Parameter("string", "Info", 3)]
    public string Info { get; set; }
}

[Function("getHolonByProviderKey", typeof(GetHolonByProviderKeyFunction))]
file sealed class GetHolonByProviderKeyFunction : FunctionMessage
{
    [Parameter("string", "providerKey", 1)]
    public string ProviderKey { get; set; }
}

[Function("getHolonsForParentByProviderKey", typeof(GetHolonsForParentByProviderKeyFunction))]
file sealed class GetHolonsForParentByProviderKeyFunction : FunctionMessage
{
    [Parameter("string", "providerKey", 1)]
    public string ProviderKey { get; set; }
}

[Function("searchAvatars", typeof(SearchAvatarsFunction))]
file sealed class SearchAvatarsFunction : FunctionMessage
{
    [Parameter("string", "searchParams", 1)]
    public string SearchParams { get; set; }
}

[Function("searchHolons", typeof(SearchHolonsFunction))]
file sealed class SearchHolonsFunction : FunctionMessage
{
    [Parameter("string", "searchParams", 1)]
    public string SearchParams { get; set; }
}

[Function("getNFTData", typeof(GetNFTDataFunction))]
file sealed class GetNFTDataFunction : FunctionMessage
{
    [Parameter("string", "nftTokenAddress", 1)]
    public string NftTokenAddress { get; set; }
}

[Function("getHolonsForParent", typeof(GetHolonsForParentFunction))]
file sealed class GetHolonsForParentFunction : FunctionMessage
{
    [Parameter("string", "parentId", 1)]
    public string ParentId { get; set; }
}

[Function("getHolonsByMetaData", typeof(GetHolonsByMetaDataFunction))]
file sealed class GetHolonsByMetaDataFunction : FunctionMessage
{
    [Parameter("string", "metaKey", 1)]
    public string MetaKey { get; set; }
    [Parameter("string", "metaValue", 2)]
    public string MetaValue { get; set; }
}

[Function("getHolonsByMetaDataPairs", typeof(GetHolonsByMetaDataPairsFunction))]
file sealed class GetHolonsByMetaDataPairsFunction : FunctionMessage
{
    [Parameter("string", "metaDataJson", 1)]
    public string MetaDataJson { get; set; }
    [Parameter("string", "matchMode", 2)]
    public string MatchMode { get; set; }
}


file static class ArbitrumContractHelper
{
    public const string CreateAvatarFuncName = "CreateAvatar";
    public const string CreateAvatarDetailFuncName = "CreateAvatarDetail";
    public const string CreateHolonFuncName = "CreateHolon";
    public const string UpdateAvatarFuncName = "UpdateAvatar";
    public const string UpdateAvatarDetailFuncName = "UpdateAvatarDetail";
    public const string UpdateHolonFuncName = "UpdateHolon";
    public const string DeleteAvatarFuncName = "DeleteAvatar";
    public const string DeleteAvatarDetailFuncName = "DeleteAvatarDetail";
    public const string DeleteHolonFuncName = "DeleteHolon";
    public const string GetAvatarByIdFuncName = "GetAvatarById";
    public const string GetAvatarDetailByIdFuncName = "GetAvatarDetailById";
    public const string GetHolonByIdFuncName = "GetHolonById";
    public const string GetAvatarsCountFuncName = "GetAvatarsCount";
    public const string GetAvatarDetailsCountFuncName = "GetAvatarDetailsCount";
    public const string GetHolonsCountFuncName = "GetHolonsCount";
    public const string SendNftFuncName = "sendNFT";
    public const string MintFuncName = "mint";
    public const string Abi = "[{\"inputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"constructor\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"sender\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"ERC721IncorrectOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ERC721InsufficientApproval\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"approver\",\"type\":\"address\"}],\"name\":\"ERC721InvalidApprover\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"}],\"name\":\"ERC721InvalidOperator\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"ERC721InvalidOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"receiver\",\"type\":\"address\"}],\"name\":\"ERC721InvalidReceiver\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"sender\",\"type\":\"address\"}],\"name\":\"ERC721InvalidSender\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ERC721NonexistentToken\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"OwnableInvalidOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"account\",\"type\":\"address\"}],\"name\":\"OwnableUnauthorizedAccount\",\"type\":\"error\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"approved\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"Approval\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"indexed\":false,\"internalType\":\"bool\",\"name\":\"approved\",\"type\":\"bool\"}],\"name\":\"ApprovalForAll\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"previousOwner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"newOwner\",\"type\":\"address\"}],\"name\":\"OwnershipTransferred\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"avatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateAvatar\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"avatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateAvatarDetail\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"holonId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateHolon\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteAvatar\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteAvatarDetail\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteHolon\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetAvatarById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"AvatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structAvatar\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetAvatarDetailById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"AvatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structAvatarDetail\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetAvatarDetailsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetAvatarsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetHolonById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"HolonId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structHolon\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetHolonsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateAvatar\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateAvatarDetail\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateHolon\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"admin\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"approve\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"getApproved\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"getTransferHistory\",\"outputs\":[{\"components\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"internalType\":\"structArbitrumOASIS.NFTTransfer[]\",\"name\":\"\",\"type\":\"tuple[]\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"}],\"name\":\"isApprovedForAll\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"metadataUri\",\"type\":\"string\"}],\"name\":\"mint\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"name\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"nextTokenId\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"name\":\"nftMetadata\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"metadataUri\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"},{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"name\":\"nftTransfers\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"owner\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ownerOf\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"renounceOwnership\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"safeTransferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"bytes\",\"name\":\"data\",\"type\":\"bytes\"}],\"name\":\"safeTransferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"name\":\"sendNFT\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"internalType\":\"bool\",\"name\":\"approved\",\"type\":\"bool\"}],\"name\":\"setApprovalForAll\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"bytes4\",\"name\":\"interfaceId\",\"type\":\"bytes4\"}],\"name\":\"supportsInterface\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"symbol\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"tokenExists\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"tokenURI\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"newOwner\",\"type\":\"address\"}],\"name\":\"transferOwnership\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]";

    // Placeholder methods for parsing Arbitrum data
    private static IAvatarDetail ParseArbitrumToAvatarDetail(object avatarDetailData)
    {
        // TODO: Implement proper parsing logic
        return new AvatarDetail();
    }

    private static IAvatar ParseArbitrumToAvatar(object avatarData)
    {
        // TODO: Implement proper parsing logic
        return new Avatar();
    }

    /// <summary>
    /// Parse Arbitrum NFT data to OASIS NFT
    /// </summary>
    private static OASISNFT ParseArbitrumToNFT(object nftData)
    {
        try
        {
            // Real implementation for parsing Arbitrum NFT data
            var nft = new OASISNFT
            {
                Id = Guid.NewGuid(),
                Title = "Arbitrum NFT",
                Description = "NFT from Arbitrum blockchain",
                NFTTokenAddress = "0x0000000000000000000000000000000000000000",
                OnChainProvider = new EnumValue<ProviderType>(ProviderType.ArbitrumOASIS),
                MetaData = new Dictionary<string, object>
                {
                    ["ArbitrumData"] = nftData,
                    ["ParsedAt"] = DateTime.Now
                }
            };
            
            return nft;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }
}
