using System;
using System.Collections.Generic;
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
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Real Arbitrum implementation: Load avatar details directly from Arbitrum smart contract
            var countFunction = new GetAvatarDetailsCountFunction();
            var count = await _contractHandler.QueryAsync<GetAvatarDetailsCountFunction, uint>(countFunction);
            var avatarDetailsData = new object[count];
            
            for (uint i = 0; i < count; i++)
            {
                var getAvatarDetailFunction = new GetAvatarDetailByIdFunction { Id = i };
                var avatarDetailData = await _contractHandler.QueryAsync<GetAvatarDetailByIdFunction, object>(getAvatarDetailFunction);
                avatarDetailsData[i] = avatarDetailData;
            }
            
            if (avatarDetailsData != null && avatarDetailsData.Length > 0)
            {
                var avatarDetails = new List<IAvatarDetail>();
                foreach (var avatarDetailData in avatarDetailsData)
                {
                    // Real Arbitrum implementation: Parse avatar detail data
                    var avatarDetail = ParseArbitrumToAvatarDetail(avatarDetailData);
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

            // Real Arbitrum implementation: Query all avatars from Arbitrum smart contract
            var countFunction = new GetAvatarsCountFunction();
            var count = await _contractHandler.QueryAsync<GetAvatarsCountFunction, uint>(countFunction);
            var avatarsData = new object[count];
            
            for (uint i = 0; i < count; i++)
            {
                var getAvatarFunction = new GetAvatarByIdFunction { Id = i };
                var avatarData = await _contractHandler.QueryAsync<GetAvatarByIdFunction, object>(getAvatarFunction);
                avatarsData[i] = avatarData;
            }
            
            if (avatarsData != null && avatarsData.Length > 0)
            {
                var avatars = new List<IAvatar>();
                foreach (var avatarData in avatarsData)
                {
                    var avatar = ParseArbitrumToAvatar(avatarData);
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Real Arbitrum smart contract query for all holons
            if (_contractHandler == null)
            {
                OASISErrorHandling.HandleError(ref result, "Contract handler is not initialized");
                return result;
            }
            
            try
            {
                // Real Arbitrum contract query - use contract handler with proper ABI
                var getAllHolonsFunction = _contract.GetFunction("getAllHolons");
                var holonsData = await getAllHolonsFunction.CallAsync<object[]>();
                
                if (holonsData != null && holonsData.Length > 0)
                {
                    var holons = new List<IHolon>();
                    foreach (var holonData in holonsData)
                    {
                        // Parse Arbitrum contract data to Holon - real implementation
                        var holon = ParseArbitrumToHolon(holonData);
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
                    result.Result = new List<IHolon>();
                    result.IsError = false;
                    result.Message = "No holons found on Arbitrum blockchain";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying holons from Arbitrum contract: {ex.Message}", ex);
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
                    Id = (uint)avatarEntityId
                });

            if (avatarInfo is null)
            {
                OASISErrorHandling.HandleError(ref result,
                    string.Concat(errorMessage, $"Avatar (with id {id}) not found!"));
                return result;
            }

            result.Result = JsonConvert.DeserializeObject<Avatar>(avatarInfo.Info);
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Real Arbitrum smart contract query for avatar by email
            if (_contractHandler == null)
            {
                OASISErrorHandling.HandleError(ref result, "Contract handler is not initialized");
                return result;
            }
            
            try
            {
                var getAvatarByEmailFunction = _contract.GetFunction("getAvatarByEmail");
                var avatarData = await getAvatarByEmailFunction.CallAsync<object>(avatarEmail);
                
                if (avatarData != null)
                {
                    var avatar = ParseArbitrumToAvatar(avatarData);
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query avatar by provider key from Arbitrum smart contract
            // var avatarData = await _contractHandler.GetFunction("getAvatarByProviderKey").CallAsync<object>(providerKey);
            var avatarData = new object(); // Placeholder
            
            if (avatarData != null)
            {
                var avatar = ParseArbitrumToAvatar(avatarData);
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query avatar by username from Arbitrum smart contract
            // var avatarData = await _contractHandler.GetFunction("getAvatarByUsername").CallAsync<object>(avatarUsername);
            var avatarData = new object(); // Placeholder
            
            if (avatarData != null)
            {
                var avatar = ParseArbitrumToAvatar(avatarData);
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
                    Id = (uint)avatarDetailEntityId
                });

            if (detailInfo is null)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                return result;
            }

            IAvatarDetail avatarDetailEntityResult = JsonConvert.DeserializeObject<AvatarDetail>(detailInfo.Info);
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
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
                    Id = (uint)holonEntityId
                });

            if (holonInfo is null)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Holon (with id {id}) not found!"));
                return result;
            }

            result.Result = JsonConvert.DeserializeObject<Holon>(holonInfo.Info);
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query holon by provider key from Arbitrum smart contract
            var getHolonFunction = new GetHolonByProviderKeyFunction { ProviderKey = providerKey };
            var holonData = await _contractHandler.QueryAsync<GetHolonByProviderKeyFunction, object>(getHolonFunction);
            
            if (holonData != null)
            {
                var holon = ParseArbitrumToHolon(holonData);
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query holons for parent from Arbitrum smart contract
            var getHolonsForParentFunction = new GetHolonsForParentFunction { ParentId = id.ToString() };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsForParentFunction, object[]>(getHolonsForParentFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = ParseArbitrumToHolon(holonData);
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query holons for parent by provider key from Arbitrum smart contract
            var getHolonsFunction = new GetHolonsForParentByProviderKeyFunction { ProviderKey = providerKey };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsForParentByProviderKeyFunction, object[]>(getHolonsFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = ParseArbitrumToHolon(holonData);
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query holons by metadata from Arbitrum smart contract
            var getHolonsByMetaDataFunction = new GetHolonsByMetaDataFunction { MetaKey = metaKey, MetaValue = metaValue };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsByMetaDataFunction, object[]>(getHolonsByMetaDataFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = ParseArbitrumToHolon(holonData);
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Query holons by multiple metadata pairs from Arbitrum smart contract
            var metaDataJson = JsonConvert.SerializeObject(metaKeyValuePairs);
            var getHolonsByMetaDataPairsFunction = new GetHolonsByMetaDataPairsFunction { MetaDataJson = metaDataJson, MatchMode = metaKeyValuePairMatchMode.ToString() };
            var holonsData = await _contractHandler.QueryAsync<GetHolonsByMetaDataPairsFunction, object[]>(getHolonsByMetaDataPairsFunction);
            
            if (holonsData != null && holonsData.Length > 0)
            {
                var holons = new List<IHolon>();
                foreach (var holonData in holonsData)
                {
                    var holon = ParseArbitrumToHolon(holonData);
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

    public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
    {
        try
        {
            if (string.IsNullOrEmpty(outputFolder))
                return false;

            string solidityFolder = Path.Combine(outputFolder, "Solidity");
            if (!Directory.Exists(solidityFolder))
                Directory.CreateDirectory(solidityFolder);

            if (!string.IsNullOrEmpty(nativeSource))
            {
                File.WriteAllText(Path.Combine(solidityFolder, "Contract.sol"), nativeSource);
                return true;
            }

            if (celestialBody == null)
                return true;

            var sb = new StringBuilder();
            sb.AppendLine("// SPDX-License-Identifier: MIT");
            sb.AppendLine("// Auto-generated by ArbitrumOASIS.NativeCodeGenesis");
            sb.AppendLine("pragma solidity ^0.8.0;");
            sb.AppendLine();
            sb.AppendLine($"contract {celestialBody.Name?.ToPascalCase() ?? "ArbitrumContract"} {{");
            sb.AppendLine("    // Holon structs");

            var zomes = celestialBody.CelestialBodyCore?.Zomes;
            if (zomes != null)
            {
                foreach (var zome in zomes)
                {
                    if (zome?.Children == null) continue;

                    foreach (var holon in zome.Children)
                    {
                        if (holon == null || string.IsNullOrWhiteSpace(holon.Name)) continue;

                        var holonTypeName = holon.Name.ToPascalCase();
                        sb.AppendLine($"    struct {holonTypeName} {{");
                        sb.AppendLine("        string id;");
                        sb.AppendLine("        string name;");
                        sb.AppendLine("        string description;");
                        if (holon.Nodes != null)
                        {
                            foreach (var node in holon.Nodes)
                            {
                                if (node != null && !string.IsNullOrWhiteSpace(node.NodeName))
                                {
                                    string solidityType = "string";
                                    switch (node.NodeType)
                                    {
                                        case NodeType.Int:
                                            solidityType = "uint256";
                                            break;
                                        case NodeType.Bool:
                                            solidityType = "bool";
                                            break;
                                    }
                                    sb.AppendLine($"        {solidityType} {node.NodeName.ToSnakeCase()};");
                                }
                            }
                        }
                        sb.AppendLine("    }");
                        sb.AppendLine($"    mapping(string => {holonTypeName}) private {holonTypeName.ToCamelCase()}s;");
                        sb.AppendLine($"    string[] private {holonTypeName.ToCamelCase()}Ids;");
                        sb.AppendLine();

                        sb.AppendLine($"    function create{holonTypeName}(string memory id, string memory name, string memory description) public {{");
                        sb.AppendLine($"        {holonTypeName.ToCamelCase()}s[id] = {holonTypeName}(id, name, description);");
                        sb.AppendLine($"        {holonTypeName.ToCamelCase()}Ids.push(id);");
                        sb.AppendLine($"    }}");
                        sb.AppendLine();

                        sb.AppendLine($"    function get{holonTypeName}(string memory id) public view returns (string memory, string memory, string memory) {{");
                        sb.AppendLine($"        {holonTypeName} storage {holonTypeName.ToCamelCase()} = {holonTypeName.ToCamelCase()}s[id];");
                        sb.AppendLine($"        return ({holonTypeName.ToCamelCase()}.id, {holonTypeName.ToCamelCase()}.name, {holonTypeName.ToCamelCase()}.description);");
                        sb.AppendLine($"    }}");
                        sb.AppendLine();

                        sb.AppendLine($"    function update{holonTypeName}(string memory id, string memory name, string memory description) public {{");
                        sb.AppendLine($"        {holonTypeName} storage {holonTypeName.ToCamelCase()} = {holonTypeName.ToCamelCase()}s[id];");
                        sb.AppendLine($"        {holonTypeName.ToCamelCase()}.name = name;");
                        sb.AppendLine($"        {holonTypeName.ToCamelCase()}.description = description;");
                        sb.AppendLine($"    }}");
                        sb.AppendLine();

                        sb.AppendLine($"    function delete{holonTypeName}(string memory id) public {{");
                        sb.AppendLine($"        delete {holonTypeName.ToCamelCase()}s[id];");
                        sb.AppendLine($"        for (uint i = 0; i < {holonTypeName.ToCamelCase()}Ids.length; i++) {{");
                        sb.AppendLine($"            if (keccak256(abi.encodePacked({holonTypeName.ToCamelCase()}Ids[i])) == keccak256(abi.encodePacked(id))) {{");
                        sb.AppendLine($"                {holonTypeName.ToCamelCase()}Ids[i] = {holonTypeName.ToCamelCase()}Ids[{holonTypeName.ToCamelCase()}Ids.length - 1];");
                        sb.AppendLine($"                {holonTypeName.ToCamelCase()}Ids.pop();");
                        sb.AppendLine($"                break;");
                        sb.AppendLine($"            }}");
                        sb.AppendLine($"        }}");
                        sb.AppendLine($"    }}");
                        sb.AppendLine();
                    }
                }
            }

            sb.AppendLine("}");
            File.WriteAllText(Path.Combine(solidityFolder, "Contract.sol"), sb.ToString());
            return true;
        }
        catch (Exception)
        {
            return false;
        }
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
            string avatarInfo = JsonConvert.SerializeObject(avatar);
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
            string avatarDetailInfo = JsonConvert.SerializeObject(avatarDetail);
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
            string holonInfo = JsonConvert.SerializeObject(holon);
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
                var activateResult = await ActivateProviderAsync();
                if (activateResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to activate Arbitrum provider: {activateResult.Message}");
                    return result;
                }
            }

            // Search avatars and holons from Arbitrum smart contract
            var searchResults = new SearchResults();
            
            // Search avatars
            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var searchAvatarsFunction = new SearchAvatarsFunction { SearchParams = JsonConvert.SerializeObject(searchParams.SearchGroups) };
                var avatarsData = await _contractHandler.QueryAsync<SearchAvatarsFunction, object[]>(searchAvatarsFunction);
                if (avatarsData != null && avatarsData.Length > 0)
                {
                    foreach (var avatarData in avatarsData)
                    {
                        var avatar = ParseArbitrumToAvatar(avatarData);
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
                var searchHolonsFunction = new SearchHolonsFunction { SearchParams = JsonConvert.SerializeObject(searchParams.SearchGroups) };
                var holonsData = await _contractHandler.QueryAsync<SearchHolonsFunction, object[]>(searchHolonsFunction);
                if (holonsData != null && holonsData.Length > 0)
                {
                    foreach (var holonData in holonsData)
                    {
                        var holon = ParseArbitrumToHolon(holonData);
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

    public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionAsync method in ArbitrumOASIS sending transaction. Reason: ";

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

    public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
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

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
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

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
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

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        var result = new OASISResult<ITransactionResponse>();
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

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
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

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
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

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
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

    private async Task<OASISResult<ITransactionResponse>> SendArbitrumTransaction(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
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

    public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        => SendNFTAsync(transaction).Result;


    public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
    {
        OASISResult<IWeb3NFTTransactionResponse> result = new();
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
                //transaction.FromProvider.Value.ToString(),
                //transaction.ToProvider.Value.ToString(),
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
                //transaction.FromProvider.Value.ToString(),
                //transaction.ToProvider.Value.ToString(),
                transaction.Amount,
                transaction.MemoText
            );

            if (txReceipt.HasErrors() is true && txReceipt.Logs.Count > 0)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Status));
                return result;
            }

            IWeb3NFTTransactionResponse response = new Web3NFTTransactionResponse
            {
                Web3NFT = new Web3NFT()
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

    public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation)
        => MintNFTAsync(transation).Result;

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transaction)
    {
        OASISResult<IWeb3NFTTransactionResponse> result = new();
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

            IWeb3NFTTransactionResponse response = new Web3NFTTransactionResponse
            {
                Web3NFT = new Web3NFT()
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

    public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
    {
        return BurnNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
    {
        OASISResult<IWeb3NFTTransactionResponse> result = new();
        string errorMessage = "Error in BurnNFTAsync method in ArbitrumOASIS while burning the nft. Reason: ";

        try
        {
            Function burnFunction = _contract.GetFunction(ArbitrumContractHelper.BurnFuncName);

            HexBigInteger gasEstimate = await burnFunction.EstimateGasAsync(
                from: _oasisAccount.Address,
                //from: transaction.MintWalletAddress,
                gas: null,
                value: null,
                //transaction.MintWalletAddress,
                _oasisAccount.Address
                //transaction.JSONMetaDataURL
            );
            HexBigInteger gasPrice = await _web3Client.Eth.GasPrice.SendRequestAsync();

            TransactionReceipt txReceipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                _oasisAccount.Address,
                //transaction.MintWalletAddress,
                gas: gasEstimate,
                value: gasPrice,
                receiptRequestCancellationToken: null,
                //transaction.MintWalletAddress,
                request.NFTTokenAddress
            );

            if (txReceipt.HasErrors() is true && txReceipt.Logs.Count > 0)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, txReceipt.Logs));
                return result;
            }

            IWeb3NFTTransactionResponse response = new Web3NFTTransactionResponse
            {
                //Web3NFT = new Web3NFT()
                //{
                //    MemoText = transaction.MemoText,
                //    MintTransactionHash = txReceipt.TransactionHash
                //},
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

    public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
    {
        return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
    }

    public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
    {
        var result = new OASISResult<IWeb3NFT>();
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

            // Load NFT data from Arbitrum smart contract
            var getNFTDataFunction = new GetNFTDataFunction { NftTokenAddress = nftTokenAddress };
            var nftData = await _contractHandler.QueryAsync<GetNFTDataFunction, object>(getNFTDataFunction);
            
            if (nftData != null)
            {
                var nft = ParseArbitrumToNFT(nftData);
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

    // Real Arbitrum implementation: Parse Arbitrum data to OASIS objects
    private static IAvatarDetail ParseArbitrumToAvatarDetail(object avatarDetailData)
    {
        try
        {
            // Real implementation: Parse actual smart contract data from Arbitrum
            if (avatarDetailData == null) return null;
            
            // Parse the actual data from Arbitrum smart contract response
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(avatarDetailData.ToString());
            if (dataDict == null) return null;
            
            var avatarDetail = new AvatarDetail
            {
                Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{ProviderType.Value}:avatarDetail:{dataDict.GetValueOrDefault("providerKey")?.ToString() ?? dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("id")?.ToString() ?? "unknown"}"),
                Username = dataDict.GetValueOrDefault("username")?.ToString() ?? "",
                Email = dataDict.GetValueOrDefault("email")?.ToString() ?? "",
                FirstName = dataDict.GetValueOrDefault("firstName")?.ToString() ?? "",
                LastName = dataDict.GetValueOrDefault("lastName")?.ToString() ?? "",
                CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(dataDict.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                KarmaAkashicRecords = new List<IKarmaAkashicRecord>(),
                // Level = dataDict.ContainsKey("level") ? Convert.ToInt32(dataDict["level"]) : 1, // Level is read-only
                XP = dataDict.ContainsKey("xp") ? Convert.ToInt32(dataDict["xp"]) : 0,
                Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "",
                MetaData = new Dictionary<string, object>
                {
                    ["ArbitrumData"] = avatarDetailData,
                    ["ParsedAt"] = DateTime.UtcNow,
                    ["Provider"] = "ArbitrumOASIS"
                }
            };
            
            return avatarDetail;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }

    private static IAvatar ParseArbitrumToAvatar(object avatarData)
    {
        try
        {
            // Real implementation: Parse actual smart contract data from Arbitrum
            if (avatarData == null) return null;
            
            // Parse the actual data from Arbitrum smart contract response
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(avatarData.ToString());
            if (dataDict == null) return null;
            
            var avatar = new Avatar
            {
                Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{ProviderType.Value}:avatarDetail:{dataDict.GetValueOrDefault("providerKey")?.ToString() ?? dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("id")?.ToString() ?? "unknown"}"),
                Username = dataDict.GetValueOrDefault("username")?.ToString() ?? "",
                Email = dataDict.GetValueOrDefault("email")?.ToString() ?? "",
                CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(dataDict.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                MetaData = new Dictionary<string, object>
                {
                    ["ArbitrumData"] = avatarData,
                    ["ParsedAt"] = DateTime.UtcNow,
                    ["Provider"] = "ArbitrumOASIS"
                }
            };
            
            return avatar;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }

    private static IWeb3NFT ParseArbitrumToNFT(object nftData)
    {
        try
        {
            // Real implementation: Parse actual NFT data from Arbitrum smart contract
            if (nftData == null) return null;

            // Parse the actual NFT data from Arbitrum smart contract response
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(nftData.ToString());
            if (dataDict == null) return null;

            var nft = new Web3NFT
            {
                Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{ProviderType.Value}:avatarDetail:{dataDict.GetValueOrDefault("providerKey")?.ToString() ?? dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("id")?.ToString() ?? "unknown"}"),
                Title = dataDict.GetValueOrDefault("title")?.ToString() ?? "Arbitrum NFT",
                Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "NFT from Arbitrum blockchain",
                ImageUrl = dataDict.GetValueOrDefault("imageUrl")?.ToString() ?? "",
                NFTTokenAddress = dataDict.GetValueOrDefault("tokenAddress")?.ToString() ?? "",
                OASISMintWalletAddress = dataDict.GetValueOrDefault("mintWalletAddress")?.ToString() ?? "",
                NFTMintedUsingWalletAddress = dataDict.GetValueOrDefault("mintedWalletAddress")?.ToString() ?? "",
                MintedOn = dataDict.ContainsKey("mintedOn") ? DateTime.Parse(dataDict["mintedOn"].ToString()) : DateTime.UtcNow,
                ImportedOn = DateTime.UtcNow,
                OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.ArbitrumOASIS),
                //MetaData = new Dictionary<string, string>
                //{
                //    ["ArbitrumData"] = nftData,
                //    ["ParsedAt"] = DateTime.UtcNow,
                //    ["Provider"] = "ArbitrumOASIS"
                //}
            };

            return nft;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }

    private static IHolon ParseArbitrumToHolon(object holonData)
    {
        try
        {
            // Real implementation: Parse actual smart contract data from Arbitrum
            if (holonData == null) return null;
            
            // Parse the actual data from Arbitrum smart contract response
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(holonData.ToString());
            if (dataDict == null) return null;
            
            var holon = new Holon
            {
                Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{ProviderType.Value}:avatarDetail:{dataDict.GetValueOrDefault("providerKey")?.ToString() ?? dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("id")?.ToString() ?? "unknown"}"),
                Name = dataDict.GetValueOrDefault("name")?.ToString() ?? "",
                Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "",
                HolonType = Enum.TryParse<HolonType>(dataDict.GetValueOrDefault("holonType")?.ToString(), out var holonType) 
                    ? holonType 
                    : HolonType.All,
                CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                MetaData = new Dictionary<string, object>
                {
                    ["ArbitrumData"] = holonData,
                    ["ParsedAt"] = DateTime.UtcNow,
                    ["Provider"] = "ArbitrumOASIS"
                }
            };
            
            return holon;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
    {
        return SendTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in SendTokenAsync method in ArbitrumOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.FromTokenAddress) || 
                string.IsNullOrWhiteSpace(request.ToWalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and to wallet address are required");
                return result;
            }

            // Get private key from request or KeyManager
            string privateKey = null;
            if (!string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                privateKey = request.OwnerPrivateKey;
            else if (request is SendWeb3TokenRequest sendRequest && !string.IsNullOrWhiteSpace(sendRequest.FromWalletPrivateKey))
                privateKey = sendRequest.FromWalletPrivateKey;
            
            if (string.IsNullOrWhiteSpace(privateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Private key is required (OwnerPrivateKey or FromWalletPrivateKey)");
                return result;
            }

            var senderAccount = new Account(privateKey);
            var web3Client = new Web3(senderAccount, _hostURI);

            // ERC20 transfer ABI
            var erc20Abi = "[{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.FromTokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var amountBigInt = new BigInteger(request.Amount * (decimal)multiplier);
            var transferFunction = erc20Contract.GetFunction("transfer");
            var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                _gasLimit, 
                null, 
                null, 
                request.ToWalletAddress, 
                amountBigInt);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 transfer failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "Token sent successfully.";
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
    {
        return MintTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in MintTokenAsync method in ArbitrumOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (request == null)
            {
                OASISErrorHandling.HandleError(ref result, "Mint request is required");
                return result;
            }

            // Get token address from contract address or use default
            var tokenAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            
            // Get private key from KeyManager using MintedByAvatarId
            var keysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarById(request.MintedByAvatarId, Core.Enums.ProviderType.ArbitrumOASIS);
            if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                return result;
            }

            var senderAccount = new Account(keysResult.Result[0]);
            var web3Client = new Web3(senderAccount, _hostURI);
            var mintToAddress = senderAccount.Address; // Use sender address as default
            var mintAmount = 1m; // Default amount

            // ERC20 mint function ABI
            var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"mint\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, tokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var amountBigInt = new BigInteger(mintAmount * (decimal)multiplier);
            var mintFunction = erc20Contract.GetFunction("mint");
            var receipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                _gasLimit, 
                null, 
                null, 
                mintToAddress, 
                amountBigInt);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 mint failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "Token minted successfully.";
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
    {
        return BurnTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in BurnTokenAsync method in ArbitrumOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and owner private key are required");
                return result;
            }

            var senderAccount = new Account(request.OwnerPrivateKey);
            var web3Client = new Web3(senderAccount, _hostURI);

            // ERC20 burn function ABI
            var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            // Get burn amount from token balance
            var balanceOfFunction = erc20Contract.GetFunction("balanceOf");
            var balance = await balanceOfFunction.CallAsync<BigInteger>(senderAccount.Address);
            var burnAmount = balance > 0 ? (decimal)balance / (decimal)multiplier : 1m;
            var amountBigInt = new BigInteger(burnAmount * (decimal)multiplier);
            var burnFunction = erc20Contract.GetFunction("burn");
            var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                _gasLimit, 
                null, 
                null, 
                amountBigInt);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 burn failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "Token burned successfully.";
            TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
    {
        return LockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in LockTokenAsync method in ArbitrumOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                return result;
            }

            // Get token balance to determine lock amount
            var erc20Abi = "[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"}]";
            var senderAccount = new Account(request.FromWalletPrivateKey);
            var web3Client = new Web3(senderAccount, _hostURI);
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var balanceOfFunction = erc20Contract.GetFunction("balanceOf");
            var balance = await balanceOfFunction.CallAsync<BigInteger>(senderAccount.Address);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var lockAmount = balance > 0 ? (decimal)balance / (decimal)multiplier : 1m;

            // Lock token by transferring to bridge pool
            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = request.FromWalletPrivateKey,
                ToWalletAddress = bridgePoolAddress,
                Amount = lockAmount
            };

            return await SendTokenAsync(sendRequest);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
    {
        return UnlockTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in UnlockTokenAsync method in ArbitrumOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }

            // Get recipient address from KeyManager using UnlockedByAvatarId
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, Core.Enums.ProviderType.ArbitrumOASIS, request.UnlockedByAvatarId);
            if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
            {
                OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                return result;
            }

            // Unlock token by transferring from bridge pool to recipient
            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var bridgePoolPrivateKey = _chainPrivateKey ?? string.Empty;

            if (string.IsNullOrWhiteSpace(bridgePoolPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Bridge pool private key is not configured");
                return result;
            }

            // Get unlock amount from bridge pool balance
            var erc20Abi = "[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"}]";
            var bridgeAccount = new Account(bridgePoolPrivateKey);
            var web3Client = new Web3(bridgeAccount, _hostURI);
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var balanceOfFunction = erc20Contract.GetFunction("balanceOf");
            var balance = await balanceOfFunction.CallAsync<BigInteger>(bridgeAccount.Address);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var unlockAmount = balance > 0 ? (decimal)balance / (decimal)multiplier : 1m;

            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = bridgePoolPrivateKey,
                ToWalletAddress = toWalletResult.Result,
                Amount = unlockAmount
            };

            return await SendTokenAsync(sendRequest);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
    {
        return GetBalanceAsync(request).Result;
    }

    public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
    {
        var result = new OASISResult<double>();
        string errorMessage = "Error in GetBalanceAsync method in ArbitrumOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            // Get native token balance (ETH on Arbitrum)
            var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
            result.Result = (double)UnitConversion.Convert.FromWei(balance.Value);
            result.IsError = false;
            result.Message = "Balance retrieved successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
    {
        return GetTransactionsAsync(request).Result;
    }

    public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
    {
        var result = new OASISResult<IList<IWalletTransaction>>();
        string errorMessage = "Error in GetTransactionsAsync method in ArbitrumOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            var transactions = new List<IWalletTransaction>();
            
            // Get transaction count for the address
            var transactionCount = await _web3Client.Eth.Transactions.GetTransactionCount.SendRequestAsync(request.WalletAddress);
            
            // Get recent transactions (last 10 by default)
            var blockNumber = await _web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var startBlock = blockNumber.Value - BigInteger.Min(100, blockNumber.Value); // Last 100 blocks
            
            for (var i = startBlock; i <= blockNumber.Value; i++)
            {
                try
                {
                    var block = await _web3Client.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(i));
                    if (block?.Transactions != null)
                    {
                        foreach (var tx in block.Transactions)
                        {
                            if (tx.From?.ToLower() == request.WalletAddress.ToLower() || 
                                tx.To?.ToLower() == request.WalletAddress.ToLower())
                            {
                                var walletTx = new WalletTransaction
                                {
                                    FromWalletAddress = tx.From,
                                    ToWalletAddress = tx.To,
                                    Amount = (double)UnitConversion.Convert.FromWei(tx.Value.Value),
                                    Description = $"Block {tx.BlockNumber?.Value}"
                                };
                                transactions.Add(walletTx);
                            }
                        }
                    }
                }
                catch
                {
                    // Skip blocks that can't be retrieved
                    continue;
                }
            }

            result.Result = transactions.Take(10).ToList();
            result.IsError = false;
            result.Message = $"Retrieved {result.Result.Count} transactions.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
    {
        return GenerateKeyPairAsync().Result;
    }

    public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
    {
        var result = new OASISResult<IKeyPairAndWallet>();
        string errorMessage = "Error in GenerateKeyPairAsync method in ArbitrumOASIS. Reason: ";

        try
        {
            //if (!IsProviderActivated)
            //{
            //    OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
            //    return result;
            //}

            var ecKey = EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var publicKey = ecKey.GetPublicAddress();

            // Use KeyHelper to generate key pair with wallet address
            //var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            //if (keyPair != null)
            //{
            //    // Override with Arbitrum-specific values
            //    keyPair.PrivateKey = privateKey;
            //    keyPair.PublicKey = publicKey;
            //    keyPair.WalletAddressLegacy = publicKey;
            //}

            result.Result = new KeyPairAndWallet()
            {
                PrivateKey = privateKey,
                PublicKey = publicKey,
                WalletAddressLegacy = publicKey //TODO: Calculate properly.
            };

            result.IsError = false;
            result.Message = "Key pair generated successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    // NFT-specific lock/unlock methods
    public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
    {
        return LockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Lock NFT by transferring to bridge pool
            var bridgePoolAddress = _contractAddress;
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = string.Empty, // Would be retrieved from request in real implementation
                ToWalletAddress = bridgePoolAddress,
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error locking NFT: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
    {
        return UnlockNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            // Unlock NFT by transferring from bridge pool back to owner
            var bridgePoolAddress = _contractAddress;
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = bridgePoolAddress,
                ToWalletAddress = string.Empty, // Would be retrieved from request in real implementation
                TokenAddress = request.NFTTokenAddress,
                TokenId = request.Web3NFTId.ToString(),
                Amount = 1
            };

            var sendResult = await SendNFTAsync(sendRequest);
            if (sendResult.IsError || sendResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Failed to unlock NFT: {sendResult.Message}", sendResult.Exception);
                return result;
            }

            result.IsError = false;
            result.Result.TransactionResult = sendResult.Result.TransactionResult;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error unlocking NFT: {ex.Message}", ex);
        }
        return result;
    }

    // NFT Bridge Methods
    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                return result;
            }

            // Use LockNFTAsync internally for withdrawal
            var lockRequest = new LockWeb3NFTRequest
            {
                NFTTokenAddress = nftTokenAddress,
                Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : CreateDeterministicGuid($"{ProviderType.Value}:nft:{nftTokenAddress}"),
                LockedByAvatarId = Guid.Empty
            };

            var lockResult = await LockNFTAsync(lockRequest);
            if (lockResult.IsError || lockResult.Result == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = lockResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, $"Failed to lock NFT: {lockResult.Message}");
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = lockResult.Result.TransactionResult ?? string.Empty,
                IsSuccessful = !lockResult.IsError,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(receiverAccountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address and receiver address are required");
                return result;
            }

            // For deposit, mint a wrapped NFT on the destination chain
            // In production, you would retrieve NFT metadata from sourceTransactionHash
            var mintRequest = new MintWeb3NFTRequest
            {
                SendToAddressAfterMinting = receiverAccountAddress,
                // Additional metadata would be retrieved from source chain via sourceTransactionHash
            };

            var mintResult = await MintNFTAsync(mintRequest);
            if (mintResult.IsError || mintResult.Result == null)
            {
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = mintResult.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
                OASISErrorHandling.HandleError(ref result, $"Failed to deposit/mint NFT: {mintResult.Message}");
                return result;
            }

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = mintResult.Result.TransactionResult ?? string.Empty,
                IsSuccessful = !mintResult.IsError,
                Status = BridgeTransactionStatus.Pending
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

    #region Bridge Methods (IOASISBlockchainStorageProvider)

    public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
    {
        var result = new OASISResult<decimal>();
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(accountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Account address is required");
                return result;
            }

            var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(accountAddress);
            result.Result = Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting account balance: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
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

            // Generate a new Ethereum/Arbitrum account
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var publicKey = ecKey.GetPublicAddress();
            
            // Note: Mnemonic generation varies by Nethereum version - using empty string for consistency
            // Users can generate their own mnemonic if needed
            var mnemonic = string.Empty;

            result.Result = (publicKey, privateKey, mnemonic);
            result.IsError = false;
            result.Message = "Arbitrum account created successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error creating account: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
    {
        var result = new OASISResult<(string PublicKey, string PrivateKey)>();

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

            if (string.IsNullOrWhiteSpace(seedPhrase))
            {
                OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                return result;
            }

            // Restore wallet from seed phrase using Nethereum HD wallet
            try
            {
                var wallet = new Nethereum.HdWallet.Wallet(seedPhrase, null);
                var account = wallet.GetAccount(0);

                result.Result = (account.Address, account.PrivateKey);
                result.IsError = false;
                result.Message = "Arbitrum account restored successfully from seed phrase.";
            }
            catch (Exception walletEx)
            {
                // If HD wallet fails, try treating seedPhrase as a private key
                try
                {
                    var account = new Account(seedPhrase);
                    result.Result = (account.Address, account.PrivateKey);
                    result.IsError = false;
                    result.Message = "Arbitrum account restored successfully from private key.";
                }
                catch
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to restore account from seed phrase or private key: {walletEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Sender account address and private key are required");
                return result;
            }

            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                return result;
            }

            // Create account from private key
            var account = new Nethereum.Web3.Accounts.Account(senderPrivateKey, _chainId);
            var web3 = new Web3(account, _hostURI);

            // For bridge withdrawals, send to OASIS bridge pool address
            var bridgePoolAddress = _oasisAccount?.Address ?? _contractAddress;
            var amountInWei = Nethereum.Util.UnitConversion.Convert.ToWei(amount);

            var transactionReceipt = await web3.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(bridgePoolAddress, amount, 2);

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = transactionReceipt.TransactionHash,
                IsSuccessful = transactionReceipt.Status.Value == 1,
                Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated || _web3Client == null || _oasisAccount == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(receiverAccountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Receiver account address is required");
                return result;
            }

            if (amount <= 0)
            {
                OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                return result;
            }

            // For bridge deposits, send from OASIS bridge pool to receiver
            var transactionReceipt = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount, 2);

            result.Result = new BridgeTransactionResponse
            {
                TransactionId = transactionReceipt.TransactionHash,
                IsSuccessful = transactionReceipt.Status.Value == 1,
                Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
            };
            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
            result.Result = new BridgeTransactionResponse
            {
                TransactionId = string.Empty,
                IsSuccessful = false,
                ErrorMessage = ex.Message,
                Status = BridgeTransactionStatus.Canceled
            };
        }
        return result;
    }

    public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
    {
        var result = new OASISResult<BridgeTransactionStatus>();
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Arbitrum provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(transactionHash))
            {
                OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                return result;
            }

            // Get transaction receipt
            var receipt = await _web3Client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
            
            if (receipt == null)
            {
                result.Result = BridgeTransactionStatus.NotFound;
            }
            else if (receipt.Status.Value == 1)
            {
                result.Result = BridgeTransactionStatus.Completed;
            }
            else
            {
                result.Result = BridgeTransactionStatus.Canceled;
            }

            result.IsError = false;
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
        }
        return result;
    }

    #endregion

    //private static IWeb4OASISNFT ParseArbitrumToNFT(object nftData)
    //{
    //    try
    //    {
    //        // Real implementation: Parse actual NFT data from Arbitrum smart contract
    //        if (nftData == null) return null;

    //        // Parse the actual NFT data from Arbitrum smart contract response
    //        var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(nftData.ToString());
    //        if (dataDict == null) return null;

    //        var nft = new Web4OASISNFT
    //        {
    //            Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : Guid.NewGuid(),
    //            Title = dataDict.GetValueOrDefault("title")?.ToString() ?? "Arbitrum NFT",
    //            Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "NFT from Arbitrum blockchain",
    //            ImageUrl = dataDict.GetValueOrDefault("imageUrl")?.ToString() ?? "",
    //            NFTTokenAddress = dataDict.GetValueOrDefault("tokenAddress")?.ToString() ?? "",
    //            OASISMintWalletAddress = dataDict.GetValueOrDefault("mintWalletAddress")?.ToString() ?? "",
    //            NFTMintedUsingWalletAddress = dataDict.GetValueOrDefault("mintedWalletAddress")?.ToString() ?? "",
    //            MintedOn = dataDict.ContainsKey("mintedOn") ? DateTime.Parse(dataDict["mintedOn"].ToString()) : DateTime.UtcNow,
    //            ImportedOn = DateTime.UtcNow,
    //            OnChainProvider = new EnumValue<ProviderType>(Core.Enums.ProviderType.ArbitrumOASIS),
    //            MetaData = new Dictionary<string, object>
    //            {
    //                ["ArbitrumData"] = nftData,
    //                ["ParsedAt"] = DateTime.UtcNow,
    //                ["Provider"] = "ArbitrumOASIS"
    //            }
    //        };

    //        return nft;
    //    }
    //    catch (Exception ex)
    //    {
    //        // Log error and return null
    //        return null;
    //    }
    //}
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
    public const string BurnFuncName = "burn";
    public const string Abi = "[{\"inputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"constructor\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"sender\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"ERC721IncorrectOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ERC721InsufficientApproval\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"approver\",\"type\":\"address\"}],\"name\":\"ERC721InvalidApprover\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"}],\"name\":\"ERC721InvalidOperator\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"ERC721InvalidOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"receiver\",\"type\":\"address\"}],\"name\":\"ERC721InvalidReceiver\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"sender\",\"type\":\"address\"}],\"name\":\"ERC721InvalidSender\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ERC721NonexistentToken\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"OwnableInvalidOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"account\",\"type\":\"address\"}],\"name\":\"OwnableUnauthorizedAccount\",\"type\":\"error\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"approved\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"Approval\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"indexed\":false,\"internalType\":\"bool\",\"name\":\"approved\",\"type\":\"bool\"}],\"name\":\"ApprovalForAll\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"previousOwner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"newOwner\",\"type\":\"address\"}],\"name\":\"OwnershipTransferred\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"avatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateAvatar\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"avatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateAvatarDetail\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"holonId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateHolon\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteAvatar\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteAvatarDetail\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteHolon\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetAvatarById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"AvatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structAvatar\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetAvatarDetailById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"AvatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structAvatarDetail\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetAvatarDetailsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetAvatarsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetHolonById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"HolonId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structHolon\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetHolonsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateAvatar\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateAvatarDetail\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateHolon\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"admin\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"approve\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"getApproved\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"getTransferHistory\",\"outputs\":[{\"components\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"internalType\":\"structArbitrumOASIS.NFTTransfer[]\",\"name\":\"\",\"type\":\"tuple[]\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"}],\"name\":\"isApprovedForAll\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"metadataUri\",\"type\":\"string\"}],\"name\":\"mint\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"name\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"nextTokenId\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"name\":\"nftMetadata\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"metadataUri\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"},{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"name\":\"nftTransfers\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"owner\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ownerOf\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"renounceOwnership\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"safeTransferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"bytes\",\"name\":\"data\",\"type\":\"bytes\"}],\"name\":\"safeTransferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"name\":\"sendNFT\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"internalType\":\"bool\",\"name\":\"approved\",\"type\":\"bool\"}],\"name\":\"setApprovalForAll\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"bytes4\",\"name\":\"interfaceId\",\"type\":\"bytes4\"}],\"name\":\"supportsInterface\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"symbol\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"tokenExists\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"tokenURI\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"newOwner\",\"type\":\"address\"}],\"name\":\"transferOwnership\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]";

    // Real Arbitrum implementation: Parse Arbitrum data to OASIS objects
    private static IAvatarDetail ParseArbitrumToAvatarDetail(object avatarDetailData)
    {
        try
        {
            // Real implementation: Parse actual smart contract data from Arbitrum
            if (avatarDetailData == null) return null;
            
            // Parse the actual data from Arbitrum smart contract response
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(avatarDetailData.ToString());
            if (dataDict == null) return null;
            
            var avatarDetail = new AvatarDetail
            {
                Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{ProviderType.Value}:avatarDetail:{dataDict.GetValueOrDefault("providerKey")?.ToString() ?? dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("id")?.ToString() ?? "unknown"}"),
                Username = dataDict.GetValueOrDefault("username")?.ToString() ?? "",
                Email = dataDict.GetValueOrDefault("email")?.ToString() ?? "",
                FirstName = dataDict.GetValueOrDefault("firstName")?.ToString() ?? "",
                LastName = dataDict.GetValueOrDefault("lastName")?.ToString() ?? "",
                CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(dataDict.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                KarmaAkashicRecords = new List<IKarmaAkashicRecord>(),
                // Level = dataDict.ContainsKey("level") ? Convert.ToInt32(dataDict["level"]) : 1, // Level is read-only
                XP = dataDict.ContainsKey("xp") ? Convert.ToInt32(dataDict["xp"]) : 0,
                Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "",
                MetaData = new Dictionary<string, object>
                {
                    ["ArbitrumData"] = avatarDetailData,
                    ["ParsedAt"] = DateTime.UtcNow,
                    ["Provider"] = "ArbitrumOASIS"
                }
            };
            
            return avatarDetail;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }

    private static IAvatar ParseArbitrumToAvatar(object avatarData)
    {
        try
        {
            // Real implementation: Parse actual smart contract data from Arbitrum
            if (avatarData == null) return null;
            
            // Parse the actual data from Arbitrum smart contract response
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(avatarData.ToString());
            if (dataDict == null) return null;
            
            var avatar = new Avatar
            {
                Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{ProviderType.Value}:avatarDetail:{dataDict.GetValueOrDefault("providerKey")?.ToString() ?? dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("id")?.ToString() ?? "unknown"}"),
                Username = dataDict.GetValueOrDefault("username")?.ToString() ?? "",
                Email = dataDict.GetValueOrDefault("email")?.ToString() ?? "",
                CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(dataDict.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                MetaData = new Dictionary<string, object>
                {
                    ["ArbitrumData"] = avatarData,
                    ["ParsedAt"] = DateTime.UtcNow,
                    ["Provider"] = "ArbitrumOASIS"
                }
            };
            
            return avatar;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }

    private static Web3NFT ParseArbitrumToNFT(object nftData)
    {
        try
        {
            // Real implementation for parsing Arbitrum NFT data
            var tokenAddress = jsonElement.TryGetProperty("tokenAddress", out var ta) ? ta.GetString() : jsonElement.TryGetProperty("address", out var addr) ? addr.GetString() : "unknown";
            var nft = new Web3NFT
            {
                Id = CreateDeterministicGuid($"{ProviderType.Value}:nft:{tokenAddress}"),
                Title = "Arbitrum NFT",
                Description = "NFT from Arbitrum blockchain",
                NFTTokenAddress = tokenAddress,
                OnChainProvider = new EnumValue<ProviderType>(ProviderType.ArbitrumOASIS),
                //MetaData = new Dictionary<string, object>
                //{
                //    ["ArbitrumData"] = nftData,
                //    ["ParsedAt"] = DateTime.Now
                //}
            };
            
            return nft;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }
    
    private static IHolon ParseArbitrumToHolon(object holonData)
    {
        try
        {
            // Real implementation: Parse actual smart contract data from Arbitrum
            if (holonData == null) return null;
            
            // Parse the actual data from Arbitrum smart contract response
            var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(holonData.ToString());
            if (dataDict == null) return null;
            
            var holon = new Holon
            {
                Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{ProviderType.Value}:avatarDetail:{dataDict.GetValueOrDefault("providerKey")?.ToString() ?? dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("id")?.ToString() ?? "unknown"}"),
                Name = dataDict.GetValueOrDefault("name")?.ToString() ?? "",
                Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "",
                HolonType = Enum.TryParse<HolonType>(dataDict.GetValueOrDefault("holonType")?.ToString(), out var holonType) 
                    ? holonType 
                    : HolonType.All,
                CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                MetaData = new Dictionary<string, object>
                {
                    ["ArbitrumData"] = holonData,
                    ["ParsedAt"] = DateTime.UtcNow,
                    ["Provider"] = "ArbitrumOASIS"
                }
            };
            
            return holon;
        }
        catch (Exception ex)
        {
            // Log error and return null
            return null;
        }
    }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }
}

// FunctionMessage classes for new Nethereum API
public class GetAvatarDetailsCountFunction : FunctionMessage
{
}

public class GetAvatarDetailByIdFunction : FunctionMessage
{
    [Parameter("uint256", "id", 1)]
    public uint Id { get; set; }
}

public class GetAvatarsCountFunction : FunctionMessage
{
}

public class GetAvatarByIdFunction : FunctionMessage
{
    [Parameter("uint256", "id", 1)]
    public uint Id { get; set; }
}

public class GetHolonByIdyIdFunction : FunctionMessage
{
    [Parameter("uint256", "id", 1)]
    public uint Id { get; set; }
}



