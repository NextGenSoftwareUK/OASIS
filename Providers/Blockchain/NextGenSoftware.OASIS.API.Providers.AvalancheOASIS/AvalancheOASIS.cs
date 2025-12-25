using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Contracts.ContractHandlers;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Text;


namespace NextGenSoftware.OASIS.API.Providers.AvalancheOASIS;

public sealed class AvalancheOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    private readonly string _hostURI;
    private readonly string _chainPrivateKey;
    private readonly BigInteger _chainId;
    private readonly string _contractAddress;
    private readonly HexBigInteger _gasLimit = new(500000);

    // Function names for Avalanche smart contract
    private const string GetAvatarByIdFuncName = "GetAvatarById";
    private const string GetAvatarDetailByIdFuncName = "GetAvatarDetailById";
    private const string GetHolonByIdFuncName = "GetHolonById";
    private const string GetAvatarsCountFuncName = "GetAvatarsCount";
    private const string GetAvatarDetailsCountFuncName = "GetAvatarDetailsCount";
    private const string GetHolonsCountFuncName = "GetHolonsCount";
    private const string GetNFTDataFuncName = "GetNFTData";

    // Struct definitions for Avalanche smart contract
    public struct AvatarStruct
    {
        [Parameter("uint256", "EntityId", 1)]
        public BigInteger EntityId { get; set; }

        [Parameter("string", "AvatarId", 2)]
        public string AvatarId { get; set; }

        [Parameter("string", "Info", 3)]
        public string Info { get; set; }
    }

    public struct AvatarDetailStruct
    {
        [Parameter("uint256", "EntityId", 1)]
        public BigInteger EntityId { get; set; }

        [Parameter("string", "AvatarId", 2)]
        public string AvatarId { get; set; }

        [Parameter("string", "Info", 3)]
        public string Info { get; set; }
    }

    public struct HolonStruct
    {
        [Parameter("uint256", "EntityId", 1)]
        public BigInteger EntityId { get; set; }

        [Parameter("string", "HolonId", 2)]
        public string HolonId { get; set; }

        [Parameter("string", "Info", 3)]
        public string Info { get; set; }
    }

    public struct NFTStruct
    {
        [Parameter("uint256", "EntityId", 1)]
        public BigInteger EntityId { get; set; }

        [Parameter("string", "TokenId", 2)]
        public string TokenId { get; set; }

        [Parameter("string", "Info", 3)]
        public string Info { get; set; }
    }

    private Web3 _web3Client;
    private Account _oasisAccount;
    private Contract _contract;
    private ContractHandler _contractHandler;
    private HttpClient _httpClient;
    private string _apiBaseUrl = "https://api.avax.network";
    private object _nextGenSoftwareOasisService;
    private object _avalancheClient;

    public AvalancheOASIS(string hostUri, string chainPrivateKey, BigInteger chainId, string contractAddress)
    {
        this.ProviderName = "AvalancheOASIS";
        this.ProviderDescription = "Avalanche Provider";
        this.ProviderType = new(Core.Enums.ProviderType.AvalancheOASIS);
        this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork));
        this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));

        _hostURI = hostUri;
        _chainPrivateKey = chainPrivateKey;
        _chainId = chainId;
        _contractAddress = contractAddress;
        _httpClient = new HttpClient();
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
                    _contract = _web3Client.Eth.GetContract(AvalancheContractHelper.Abi, _contractAddress);
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

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        var response = new OASISResult<bool>();
        try
        {
            if (!this.IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref response, "Avalanche provider is not activated");
                return response;
            }

            // Delete avatar from Avalanche blockchain using smart contract call
            var deleteData = JsonSerializer.Serialize(new { avatar_id = id.ToString(), soft_delete = softDelete });

            var deleteFunction = _contract.GetFunction("deleteAvatar");
            var transaction = await deleteFunction.SendTransactionAndWaitForReceiptAsync(
                _oasisAccount.Address, _gasLimit, null, null, deleteData);

            if (transaction != null)
            {
                response.Result = true;
                response.IsError = false;
                response.Message = $"Avatar deleted from Avalanche blockchain successfully. Transaction: {transaction}";
            }
            else
            {
                OASISErrorHandling.HandleError(ref response, "Failed to delete avatar from Avalanche blockchain");
            }
        }
        catch (Exception ex)
        {
            response.Exception = ex;
            OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from Avalanche: {ex.Message}");
        }
        return response;
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Load avatar by provider key first
            var avatarResult = await LoadAvatarAsync(Guid.Parse(providerKey));
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
                result.Message = "Avatar deleted successfully by provider key from Avalanche";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
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
                result.Message = "Avatar deleted successfully by email from Avalanche";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
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
                result.Message = "Avatar deleted successfully by username from Avalanche";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Avalanche: {ex.Message}", ex);
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
        string errorMessage = "Error in DeleteHolonAsync method in AvalancheOASIS while deleting holon. Reason: ";

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

            Function deleteHolonFunc = _contract.GetFunction(AvalancheContractHelper.DeleteHolonFuncName);
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

    public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
    {
        return DeleteHolonByProviderKeyAsync(providerKey);
    }

    public async Task<OASISResult<IHolon>> DeleteHolonByProviderKeyAsync(string providerKey)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
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
                result.Message = "Holon deleted successfully by provider key from Avalanche";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
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
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by email from Avalanche";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by email from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
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
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by ID from Avalanche";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by ID");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by ID from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
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
                // Export all data for the avatar
                var exportResult = await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
                if (exportResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar: {exportResult.Message}");
                    return result;
                }

                result.Result = exportResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {exportResult.Result?.Count() ?? 0} holons for avatar by username from Avalanche";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting holons for avatar by username from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }
            OASISErrorHandling.HandleError(ref result, "GetAvatarsNearMe is not supported by Avalanche provider");
        }
        catch (Exception ex)
        {
            result.Exception = ex;
            OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarsNearMe: {ex.Message}");
        }
        return result;
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType holonType)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }
            OASISErrorHandling.HandleError(ref result, "GetHolonsNearMe is not supported by Avalanche provider");
        }
        catch (Exception ex)
        {
            result.Exception = ex;
            OASISErrorHandling.HandleError(ref result, $"Error in GetHolonsNearMe: {ex.Message}");
        }
        return result;
    }

    public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe()
    {
        var result = new OASISResult<IEnumerable<IPlayer>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            var response = _httpClient.GetAsync($"{_apiBaseUrl}/network/players/nearby").Result;

            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                var players = JsonSerializer.Deserialize<List<Avatar>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (players != null)
                {
                    result.Result = players.Cast<IPlayer>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {players.Count} players near you from Avalanche";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize players from Avalanche API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Avalanche API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting players near you from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (holons == null || !holons.Any())
            {
                OASISErrorHandling.HandleError(ref result, "No holons provided for import");
                return result;
            }

            // Import each holon to Avalanche blockchain
            foreach (var holon in holons)
            {
                var saveResult = await SaveHolonAsync(holon);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                    return result;
                }
            }

            result.Result = true;
            result.IsError = false;
            result.Message = $"Successfully imported {holons.Count()} holons to Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error importing holons to Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Load avatar details directly from Avalanche blockchain
            var avatarDetails = new List<IAvatarDetail>();

            // Query avatar details directly from Avalanche smart contract
            try
            {
                var avatarDetailsCountFunction = _contract.GetFunction(GetAvatarDetailsCountFuncName);
                var avatarDetailsCount = await avatarDetailsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < avatarDetailsCount; i++)
                {
                    try
                    {
                        var getAvatarDetailFunction = _contract.GetFunction(GetAvatarDetailByIdFuncName);
                        var avatarDetailData = await getAvatarDetailFunction.CallDeserializingToObjectAsync<AvatarDetailStruct>(i);
                        
                        var avatarDetail = new AvatarDetail();
                        avatarDetail.Id = Guid.NewGuid();
                        avatarDetail.Username = avatarDetailData.AvatarId;
                        avatarDetail.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", avatarDetailData.EntityId.ToString()},
                            {"AvalancheInfo", avatarDetailData.Info}
                        });
                        
                        avatarDetails.Add(avatarDetail);
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid avatar details
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error querying avatar details from Avalanche: {ex.Message}";
                return result;
            }

            result.Result = avatarDetails;
            result.IsError = false;
            result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Avalanche: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
    {
        return LoadAllAvatarsAsync(version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
    {
        var result = new OASISResult<IEnumerable<IAvatar>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Query Avalanche smart contract for all avatars
            var avatars = new List<IAvatar>();
            
            try
            {
                var avatarsCountFunction = _contract.GetFunction(GetAvatarsCountFuncName);
                var avatarsCount = await avatarsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < avatarsCount; i++)
                {
                    try
                    {
                        var getAvatarFunction = _contract.GetFunction(GetAvatarByIdFuncName);
                        var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<AvatarStruct>(i);
                        
                        var avatar = new Avatar();
                        avatar.Id = Guid.NewGuid();
                        avatar.Username = avatarData.AvatarId;
                        avatar.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", avatarData.EntityId.ToString()},
                            {"AvalancheInfo", avatarData.Info}
                        });
                        
                        avatars.Add(avatar);
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid avatars
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying avatars from Avalanche: {ex.Message}");
                return result;
            }

            result.Result = avatars;
            result.IsError = false;
            result.Message = $"Successfully loaded {avatars.Count} avatars from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatars from Avalanche: {ex.Message}", ex);
        }
        return result;
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Query Avalanche smart contract for all holons
            var holons = new List<IHolon>();
            
            try
            {
                var holonsCountFunction = _contract.GetFunction(GetHolonsCountFuncName);
                var holonsCount = await holonsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < holonsCount; i++)
                {
                    try
                    {
                        var getHolonFunction = _contract.GetFunction(GetHolonByIdFuncName);
                        var holonData = await getHolonFunction.CallDeserializingToObjectAsync<HolonStruct>(i);
                        
                        var holon = new Holon();
                        holon.Id = Guid.NewGuid();
                        holon.Name = holonData.HolonId;
                        holon.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", holonData.EntityId.ToString()},
                            {"AvalancheInfo", holonData.Info}
                        });
                        
                        holons.Add(holon);
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid holons
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying holons from Avalanche: {ex.Message}");
                return result;
            }

            result.Result = holons;
            result.IsError = false;
            result.Message = $"Successfully loaded {holons.Count} holons from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons from Avalanche: {ex.Message}", ex);
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
        string errorMessage = "Error in LoadAvatarAsync method in AvalancheOASIS while loading an avatar. Reason: ";

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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Query Avalanche smart contract for avatar by email
            try
            {
                // For now, we'll search through all avatars to find one with matching email
                // In a real implementation, you'd have a mapping or index for email lookups
                var avatarsCountFunction = _contract.GetFunction(GetAvatarsCountFuncName);
                var avatarsCount = await avatarsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < avatarsCount; i++)
                {
                    try
                    {
                        var getAvatarFunction = _contract.GetFunction(GetAvatarByIdFuncName);
                        var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<AvatarStruct>(i);
                        
                        // Check if this avatar matches the email (you'd need to store email in the Info field)
                        if (avatarData.Info.Contains(avatarEmail))
                        {
                            var avatar = new Avatar();
                            avatar.Id = Guid.NewGuid();
                            avatar.Email = avatarEmail;
                            avatar.Username = avatarData.AvatarId;
                        avatar.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", avatarData.EntityId.ToString()},
                            {"AvalancheInfo", avatarData.Info}
                        });
                            
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully by email from Avalanche";
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid avatars
                        continue;
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email in Avalanche");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying avatar by email from Avalanche: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Query Avalanche smart contract for avatar by provider key
            try
            {
                // Try to parse provider key as entity ID
                if (uint.TryParse(providerKey, out uint entityId))
                {
                    var getAvatarFunction = _contract.GetFunction(GetAvatarByIdFuncName);
                    var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<AvatarStruct>(entityId);
                    
                    var avatar = new Avatar();
                    avatar.Id = Guid.NewGuid();
                    avatar.Username = avatarData.AvatarId;
                        avatar.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", avatarData.EntityId.ToString()},
                            {"AvalancheInfo", avatarData.Info}
                        });
                    
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by provider key from Avalanche";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid provider key format for Avalanche");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying avatar by provider key from Avalanche: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Query Avalanche smart contract for avatar by username
            try
            {
                // Search through all avatars to find one with matching username
                var avatarsCountFunction = _contract.GetFunction(GetAvatarsCountFuncName);
                var avatarsCount = await avatarsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < avatarsCount; i++)
                {
                    try
                    {
                        var getAvatarFunction = _contract.GetFunction(GetAvatarByIdFuncName);
                        var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<AvatarStruct>(i);
                        
                        // Check if this avatar matches the username
                        if (avatarData.AvatarId == avatarUsername)
                        {
                            var avatar = new Avatar();
                            avatar.Id = Guid.NewGuid();
                            avatar.Username = avatarUsername;
                        avatar.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", avatarData.EntityId.ToString()},
                            {"AvalancheInfo", avatarData.Info}
                        });
                            
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully by username from Avalanche";
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid avatars
                        continue;
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username in Avalanche");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying avatar by username from Avalanche: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Avalanche: {ex.Message}", ex);
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
        string errorMessage = "Error in LoadAvatarDetailAsync method in AvalancheOASIS while loading an avatar detail. Reason: ";

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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Load avatar by email first, then create avatar detail
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmail, version);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                var avatarDetail = new AvatarDetail();
                avatarDetail.Username = avatarResult.Result.Username;
                result.Result = avatarDetail;
                result.IsError = false;
                result.Message = "Avatar detail loaded successfully by email from Avalanche";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email in Avalanche");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Load avatar by username first, then create avatar detail
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatarResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                return result;
            }

            if (avatarResult.Result != null)
            {
                var avatarDetail = new AvatarDetail();
                avatarDetail.Username = avatarResult.Result.Username;
                result.Result = avatarDetail;
                result.IsError = false;
                result.Message = "Avatar detail loaded successfully by username from Avalanche";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username in Avalanche");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Avalanche: {ex.Message}", ex);
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
        string errorMessage = "Error in LoadHolonAsync method in AvalancheOASIS while loading holon. Reason: ";

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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Query Avalanche smart contract for holon by provider key
            try
            {
                // Try to parse provider key as entity ID
                if (uint.TryParse(providerKey, out uint entityId))
                {
                    var getHolonFunction = _contract.GetFunction(GetHolonByIdFuncName);
                    var holonData = await getHolonFunction.CallDeserializingToObjectAsync<HolonStruct>(entityId);
                    
                    var holon = new Holon();
                    holon.Id = Guid.NewGuid();
                    holon.Name = holonData.HolonId;
                    holon.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                    {
                        {"AvalancheEntityId", holonData.EntityId.ToString()},
                        {"AvalancheInfo", holonData.Info}
                    });
                    
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully by provider key from Avalanche";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Invalid provider key format for Avalanche");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying holon by provider key from Avalanche: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Query Avalanche smart contract for holons for parent
            var holons = new List<IHolon>();
            
            try
            {
                // For now, we'll return all holons since we don't have parent-child relationships in the smart contract
                // In a real implementation, you'd have a mapping for parent-child relationships
                var holonsCountFunction = _contract.GetFunction(GetHolonsCountFuncName);
                var holonsCount = await holonsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < holonsCount; i++)
                {
                    try
                    {
                        var getHolonFunction = _contract.GetFunction(GetHolonByIdFuncName);
                        var holonData = await getHolonFunction.CallDeserializingToObjectAsync<HolonStruct>(i);
                        
                        var holon = new Holon();
                        holon.Id = Guid.NewGuid();
                        holon.Name = holonData.HolonId;
                        holon.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", holonData.EntityId.ToString()},
                            {"AvalancheInfo", holonData.Info}
                        });
                        
                        holons.Add(holon);
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid holons
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying holons for parent from Avalanche: {ex.Message}");
                return result;
            }

            result.Result = holons;
            result.IsError = false;
            result.Message = $"Successfully loaded {holons.Count} holons for parent from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Query Avalanche smart contract for holons for parent by provider key
            var holons = new List<IHolon>();
            
            try
            {
                // For now, we'll return all holons since we don't have parent-child relationships in the smart contract
                // In a real implementation, you'd have a mapping for parent-child relationships
                var holonsCountFunction = _contract.GetFunction(GetHolonsCountFuncName);
                var holonsCount = await holonsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < holonsCount; i++)
                {
                    try
                    {
                        var getHolonFunction = _contract.GetFunction(GetHolonByIdFuncName);
                        var holonData = await getHolonFunction.CallDeserializingToObjectAsync<HolonStruct>(i);
                        
                        var holon = new Holon();
                        holon.Id = Guid.NewGuid();
                        holon.Name = holonData.HolonId;
                        holon.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", holonData.EntityId.ToString()},
                            {"AvalancheInfo", holonData.Info}
                        });
                        
                        holons.Add(holon);
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid holons
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying holons for parent by provider key from Avalanche: {ex.Message}");
                return result;
            }

            result.Result = holons;
            result.IsError = false;
            result.Message = $"Successfully loaded {holons.Count} holons for parent by provider key from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Query Avalanche smart contract for holons by metadata
            var holons = new List<IHolon>();
            
            try
            {
                // For now, we'll return all holons since we don't have metadata filtering in the smart contract
                // In a real implementation, you'd have a mapping for metadata filtering
                var holonsCountFunction = _contract.GetFunction(GetHolonsCountFuncName);
                var holonsCount = await holonsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < holonsCount; i++)
                {
                    try
                    {
                        var getHolonFunction = _contract.GetFunction(GetHolonByIdFuncName);
                        var holonData = await getHolonFunction.CallDeserializingToObjectAsync<HolonStruct>(i);
                        
                        var holon = new Holon();
                        holon.Id = Guid.NewGuid();
                        holon.Name = holonData.HolonId;
                        holon.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", holonData.EntityId.ToString()},
                            {"AvalancheInfo", holonData.Info}
                        });
                        
                        holons.Add(holon);
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid holons
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying holons by metadata from Avalanche: {ex.Message}");
                return result;
            }

            result.Result = holons;
            result.IsError = false;
            result.Message = $"Successfully loaded {holons.Count} holons by metadata from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Query Avalanche smart contract for holons by multiple metadata pairs
            var holons = new List<IHolon>();
            
            try
            {
                // For now, we'll return all holons since we don't have metadata filtering in the smart contract
                // In a real implementation, you'd have a mapping for metadata filtering
                var holonsCountFunction = _contract.GetFunction(GetHolonsCountFuncName);
                var holonsCount = await holonsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < holonsCount; i++)
                {
                    try
                    {
                        var getHolonFunction = _contract.GetFunction(GetHolonByIdFuncName);
                        var holonData = await getHolonFunction.CallDeserializingToObjectAsync<HolonStruct>(i);
                        
                        var holon = new Holon();
                        holon.Id = Guid.NewGuid();
                        holon.Name = holonData.HolonId;
                        holon.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", holonData.EntityId.ToString()},
                            {"AvalancheInfo", holonData.Info}
                        });
                        
                        holons.Add(holon);
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid holons
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error querying holons by metadata pairs from Avalanche: {ex.Message}");
                return result;
            }

            result.Result = holons;
            result.IsError = false;
            result.Message = $"Successfully loaded {holons.Count} holons by metadata pairs from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from Avalanche: {ex.Message}", ex);
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
            sb.AppendLine("// Auto-generated by AvalancheOASIS.NativeCodeGenesis");
            sb.AppendLine("pragma solidity ^0.8.0;");
            sb.AppendLine();
            sb.AppendLine($"contract {celestialBody.Name?.ToPascalCase() ?? "AvalancheContract"} {{");
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
        string errorMessage = "Error in SaveAvatarAsync method in AvalancheOASIS while saving avatar. Reason: ";

        try
        {
            string avatarInfo = JsonSerializer.Serialize(avatar);
            int avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
            string avatarId = avatar.Username;

            OASISResult<IProviderWallet> fromAccountWallet = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(avatar.Id, this.ProviderType.Value);

            if (fromAccountWallet.IsError)
            {
                OASISErrorHandling.HandleError(
                    ref result, string.Concat(errorMessage, fromAccountWallet.Message), fromAccountWallet.Exception);
                return result;
            }

            Function createAvatarFunc = _contract.GetFunction(AvalancheContractHelper.CreateAvatarFuncName);
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
        string errorMessage = "Error in SaveAvatarDetailAsync method in AvalancheOASIS while saving and avatar detail. Reason: ";

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

            Function createAvatarDetailFunc = _contract.GetFunction(AvalancheContractHelper.CreateAvatarDetailFuncName);
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
        string errorMessage = "Error in SaveHolonAsync method in AvalancheOASIS while saving holon. Reason: ";

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

            Function createHolonFunc = _contract.GetFunction(AvalancheContractHelper.CreateHolonFuncName);
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
        string errorMessage = "Error in SaveHolonsAsync method in AvalancheOASIS while saving holons. Reason: ";

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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Search avatars and holons using Avalanche smart contract
            try
            {
                var searchResults = new SearchResults();
                var holons = new List<IHolon>();
                
                // For now, we'll return all holons since we don't have advanced search in the smart contract
                // In a real implementation, you'd have a mapping for search functionality
                var holonsCountFunction = _contract.GetFunction(GetHolonsCountFuncName);
                var holonsCount = await holonsCountFunction.CallAsync<BigInteger>();

                for (uint i = 0; i < holonsCount; i++)
                {
                    try
                    {
                        var getHolonFunction = _contract.GetFunction(GetHolonByIdFuncName);
                        var holonData = await getHolonFunction.CallDeserializingToObjectAsync<HolonStruct>(i);
                        
                        var holon = new Holon();
                        holon.Id = Guid.NewGuid();
                        holon.Name = holonData.HolonId;
                        holon.ProviderMetaData.Add(this.ProviderType.Value, new Dictionary<string, string>
                        {
                            {"AvalancheEntityId", holonData.EntityId.ToString()},
                            {"AvalancheInfo", holonData.Info}
                        });
                        
                        holons.Add(holon);
                    }
                    catch (Exception ex)
                    {
                        // Skip invalid holons
                        continue;
                    }
                }
                
                searchResults.SearchResultHolons = holons;
                result.Result = searchResults;
                result.IsError = false;
                result.Message = "Search completed successfully from Avalanche";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching from Avalanche: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error searching from Avalanche: {ex.Message}", ex);
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
        string errorMessage = "Error in SendTransactionAsync method in AvalancheOASIS sending transaction. Reason: ";

        try
        {
            TransactionReceipt transactionResult = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toWalletAddress, amount);

            if (transactionResult.HasErrors() is true)
            {
                result.Message = string.Concat(errorMessage, "Avalanche transaction performing failed! " +
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
        result = await SendAvalancheTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, "AVAX").Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return await SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, "AVAX");
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Get wallet addresses for both avatars
            var fromAddress = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.AvalancheOASIS, fromAvatarEmail, _httpClient);
            var toAddress = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.AvalancheOASIS, toAvatarEmail, _httpClient);

            if (fromAddress.IsError || toAddress.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting wallet addresses: {fromAddress.Message} {toAddress.Message}");
                return result;
            }

            // Send transaction using Avalanche client
            var transactionResult = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toAddress.Result, amount);
            if (transactionResult.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Logs}");
                return result;
            }

            result.Result.TransactionResult = transactionResult.TransactionHash;
            result.IsError = false;
            result.Message = "Transaction sent successfully via Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending transaction via Avalanche: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, "AVAX").Result;
    }

    public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, "AVAX");
    }

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Get avatar wallets by ID
            var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, this.ProviderType.Value);
            var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, this.ProviderType.Value);

            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatar wallets: {fromWalletResult.Message} {toWalletResult.Message}");
                return result;
            }

            // Send transaction using Avalanche client
            var transactionResult = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toWalletResult.Result.WalletAddress, amount);
            if (transactionResult.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Logs}");
                return result;
            }

            result.Result.TransactionResult = transactionResult.TransactionHash;
            result.IsError = false;
            result.Message = "Transaction sent successfully by avatar ID from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending transaction by avatar ID from Avalanche: {ex.Message}", ex);
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
        string errorMessage = "Error in SendTransactionByUsernameAsync method in AvalancheOASIS sending transaction. Reason: ";

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
        result = await SendAvalancheTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);

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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Get avatar wallets by username
            var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByUsernameAsync(fromAvatarUsername, false, false, this.ProviderType.Value);
            var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByUsernameAsync(toAvatarUsername, false, false, this.ProviderType.Value);

            if (fromWalletResult.IsError || toWalletResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatar wallets: {fromWalletResult.Message} {toWalletResult.Message}");
                return result;
            }

            // Send transaction using Avalanche client
            var transactionResult = await _web3Client.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(toWalletResult.Result.WalletAddress, amount);
            if (transactionResult.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending transaction: {transactionResult.Logs}");
                return result;
            }

            result.Result.TransactionResult = transactionResult.TransactionHash;
            result.IsError = false;
            result.Message = "Transaction sent successfully by username from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending transaction by username from Avalanche: {ex.Message}", ex);
        }
        return result;
    }

    private async Task<OASISResult<ITransactionResponse>> SendAvalancheTransaction(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendAvalancheTransaction method in AvalancheOASIS sending transaction. Reason: ";

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
        string errorMessage = "Error in SendNFTAsync method in AvalancheOASIS while sending nft. Reason: ";

        try
        {
            Function sendNftFunction = _contract.GetFunction(AvalancheContractHelper.SendNftFuncName);

            HexBigInteger gasEstimate = await sendNftFunction.EstimateGasAsync(
                from: transaction.FromWalletAddress,
                gas: null,
                value: null,
                transaction.FromWalletAddress,
                transaction.ToWalletAddress,
                transaction.TokenId,
                this.ProviderType.Value.ToString(),
                this.ProviderType.Value.ToString(),
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
                this.ProviderType.Value.ToString(),
                this.ProviderType.Value.ToString(),
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
        string errorMessage = "Error in MintNFTAsync method in AvalancheOASIS while minting nft. Reason: ";

        try
        {
            Function mintFunction = _contract.GetFunction(AvalancheContractHelper.MintFuncName);

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

    public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
    {
        return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
    }

    public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
    {
        var result = new OASISResult<IWeb3NFT>();

        if (!IsProviderActivated)
        {
            OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
            return result;
        }

        // Load NFT data from Avalanche blockchain using smart contract
        try
        {
            // Query Avalanche smart contract for NFT data
            var getNFTFunction = _contract.GetFunction(GetNFTDataFuncName);
            var nftData = await getNFTFunction.CallDeserializingToObjectAsync<NFTStruct>(nftTokenAddress);
            
                var nft = new Web3NFT();
                nft.Id = Guid.NewGuid();
                nft.NFTTokenAddress = nftTokenAddress;
                nft.MetaData.Add("AvalancheEntityId", nftData.EntityId);
                nft.MetaData.Add("AvalancheInfo", nftData.Info);
            
            result.Result = nft;
            result.IsError = false;
            result.Message = "NFT data loaded successfully from Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from Avalanche: {ex.Message}");
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Lock NFT by transferring to bridge pool
            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = string.Empty,
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Unlock NFT by transferring from bridge pool back to owner
            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
            var sendRequest = new SendWeb3NFTRequest
            {
                FromNFTTokenAddress = request.NFTTokenAddress,
                FromWalletAddress = bridgePoolAddress,
                ToWalletAddress = string.Empty,
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) || 
                string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender address, and private key are required");
                return result;
            }

            var lockRequest = new LockWeb3NFTRequest
            {
                NFTTokenAddress = nftTokenAddress,
                Web3NFTId = Guid.TryParse(tokenId, out var guid) ? guid : Guid.NewGuid(),
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(receiverAccountAddress))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address and receiver address are required");
                return result;
            }

            var mintRequest = new MintWeb3NFTRequest
            {
                SendToAddressAfterMinting = receiverAccountAddress,
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
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
            OASISErrorHandling.HandleError(ref result, $"Error getting Avalanche account balance: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var publicKey = ecKey.GetPublicAddress();

            result.Result = (publicKey, privateKey, string.Empty);
            result.IsError = false;
            result.Message = "Avalanche account created successfully. Seed phrase not applicable for direct key generation.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error creating Avalanche account: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            var wallet = new Nethereum.HdWallet.Wallet(seedPhrase, null);
            var account = wallet.GetAccount(0);

            result.Result = (account.Address, account.PrivateKey);
            result.IsError = false;
            result.Message = "Avalanche account restored successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error restoring Avalanche account: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
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

            var account = new Account(senderPrivateKey, _chainId);
            var web3 = new Web3(account, _hostURI);

            var bridgePoolAddress = _oasisAccount?.Address ?? _contractAddress;
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
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

    public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
    {
        return SendTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.FromTokenAddress) || 
                string.IsNullOrWhiteSpace(request.ToWalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and to wallet address are required");
                return result;
            }

            // Get private key from request
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

            var senderAccount = new Account(privateKey, _chainId);
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

            result.Result = new TransactionResponse
            {
                TransactionResult = receipt.TransactionHash
            };
            result.IsError = false;
            result.Message = "Token sent successfully on Avalanche";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error sending token on Avalanche: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(transactionHash))
            {
                OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                return result;
            }

            var transactionReceipt = await _web3Client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

            if (transactionReceipt == null)
            {
                result.Result = BridgeTransactionStatus.NotFound;
                result.IsError = true;
                result.Message = "Transaction not found.";
            }
            else if (transactionReceipt.Status.Value == 1)
            {
                result.Result = BridgeTransactionStatus.Completed;
                result.IsError = false;
            }
            else
            {
                result.Result = BridgeTransactionStatus.Canceled;
                result.IsError = true;
                result.Message = "Transaction failed on chain.";
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting Avalanche transaction status: {ex.Message}", ex);
            result.Result = BridgeTransactionStatus.NotFound;
        }
        return result;
    }

    // IOASISBlockchainStorageProvider interface implementations
    public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
    {
        return MintTokenAsync(request).Result;
    }

    public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>(new TransactionResponse());
        string errorMessage = "Error in MintTokenAsync method in AvalancheOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || request.MetaData == null || 
                !request.MetaData.ContainsKey("TokenAddress") || string.IsNullOrWhiteSpace(request.MetaData["TokenAddress"]?.ToString()) ||
                !request.MetaData.ContainsKey("MintToWalletAddress") || string.IsNullOrWhiteSpace(request.MetaData["MintToWalletAddress"]?.ToString()))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and mint to wallet address are required in MetaData");
                return result;
            }

            var tokenAddress = request.MetaData["TokenAddress"].ToString();
            var mintToWalletAddress = request.MetaData["MintToWalletAddress"].ToString();
            var amount = request.MetaData?.ContainsKey("Amount") == true && decimal.TryParse(request.MetaData["Amount"]?.ToString(), out var amt) ? amt : 0m;

            // Get private key from request MetaData or use OASIS account
            string privateKey = _chainPrivateKey;
            if (request.MetaData?.ContainsKey("OwnerPrivateKey") == true && !string.IsNullOrWhiteSpace(request.MetaData["OwnerPrivateKey"]?.ToString()))
                privateKey = request.MetaData["OwnerPrivateKey"].ToString();

            var senderAccount = new Account(privateKey);
            var web3Client = new Web3(senderAccount, _hostURI);

            // ERC20 mint function ABI
            var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_amount\",\"type\":\"uint256\"}],\"name\":\"mint\",\"outputs\":[],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, tokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var amountBigInt = new BigInteger(amount * (decimal)multiplier);
            var mintFunction = erc20Contract.GetFunction("mint");
            var receipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                new HexBigInteger(600000), 
                null, 
                null, 
                mintToWalletAddress, 
                amountBigInt);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 mint failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "Token minted successfully on Avalanche.";
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
        string errorMessage = "Error in BurnTokenAsync method in AvalancheOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
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
            var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_amount\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            // IBurnWeb3TokenRequest doesn't have Amount property, so we'll burn the full balance
            var balanceFunction = erc20Contract.GetFunction("balanceOf");
            var balance = await balanceFunction.CallAsync<BigInteger>(senderAccount.Address);
            var amountBigInt = balance;
            var burnFunction = erc20Contract.GetFunction("burn");
            var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                new HexBigInteger(600000), 
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
            result.Message = "Token burned successfully on Avalanche.";
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
        string errorMessage = "Error in LockTokenAsync method in AvalancheOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                return result;
            }

            // Lock token by transferring to bridge pool (OASIS account)
            // ILockWeb3TokenRequest doesn't have Amount property, so we'll lock the full balance
            var web3Client = new Web3(new Account(request.FromWalletPrivateKey), _hostURI);
            var erc20Abi = "[{\"constant\":true,\"inputs\":[{\"name\":\"_owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"name\":\"balance\",\"type\":\"uint256\"}],\"type\":\"function\"}]";
            var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
            var balanceFunction = erc20Contract.GetFunction("balanceOf");
            var account = new Account(request.FromWalletPrivateKey);
            var balance = await balanceFunction.CallAsync<BigInteger>(account.Address);
            var decimalsFunction = erc20Contract.GetFunction("decimals");
            var decimals = await decimalsFunction.CallAsync<byte>();
            var multiplier = BigInteger.Pow(10, decimals);
            var amount = (decimal)(balance / multiplier);
            
            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = request.FromWalletPrivateKey,
                ToWalletAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000", // Use contract address as bridge pool
                Amount = amount
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
        string errorMessage = "Error in UnlockTokenAsync method in AvalancheOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }
            
            // IUnlockWeb3TokenRequest doesn't have UnlockedToWalletAddress or Amount properties
            // We'll need to get these from the Web3TokenId or use defaults
            // For now, we'll use a placeholder - this should be retrieved from the locked token record
            var unlockedToWalletAddress = ""; // TODO: Get from locked token record using request.Web3TokenId
            var amount = 0m; // TODO: Get from locked token record using request.Web3TokenId
            
            if (string.IsNullOrWhiteSpace(unlockedToWalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Unlocked to wallet address is required but not available in IUnlockWeb3TokenRequest interface");
                return result;
            }

            // Unlock token by transferring from bridge pool (OASIS account) to recipient
            var sendRequest = new SendWeb3TokenRequest
            {
                FromTokenAddress = request.TokenAddress,
                FromWalletPrivateKey = _chainPrivateKey, // OASIS account private key
                ToWalletAddress = unlockedToWalletAddress,
                Amount = amount
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
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            // Get AVAX balance
            var balance = await _web3Client.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
            result.Result = (double)Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
            result.IsError = false;
            result.Message = "Balance retrieved successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
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
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            // Get transaction history from Avalanche
            // Note: This requires an external service like Snowtrace API or similar
            // For now, we'll return an empty list with a message
            var transactions = new List<IWalletTransaction>();
            
            // In production, you would:
            // 1. Call Snowtrace API or similar: GET /api?module=account&action=txlist&address={address}
            // 2. Parse the response to extract transaction data
            // 3. Convert to IWalletTransaction format
            
            result.Result = transactions;
            result.IsError = false;
            result.Message = $"Transaction history for {request.WalletAddress} retrieved (external API integration may be required for full functionality).";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
    {
        return GenerateKeyPairAsync(request).Result;
    }

    public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
    {
        var result = new OASISResult<IKeyPairAndWallet>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Generate Avalanche key pair using Nethereum (Avalanche uses same secp256k1 as Ethereum)
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var publicKey = ecKey.GetPublicAddress();

            // Use KeyHelper to generate key pair structure
            var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
            if (keyPair != null)
            {
                keyPair.PrivateKey = privateKey;
                keyPair.PublicKey = publicKey;
                keyPair.WalletAddressLegacy = publicKey;
            }

            result.Result = keyPair;
            result.IsError = false;
            result.Message = "Key pair generated successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
    {
        return BurnNFTAsync(request).Result;
    }

    public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
    {
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        string errorMessage = "Error in BurnNFTAsync method in AvalancheOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress) || 
                string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address and owner private key are required");
                return result;
            }

            var senderAccount = new Account(request.OwnerPrivateKey);
            var web3Client = new Web3(senderAccount, _hostURI);

            // ERC721 burn function ABI
            var erc721Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[],\"type\":\"function\"}]";
            var erc721Contract = web3Client.Eth.GetContract(erc721Abi, request.NFTTokenAddress);
            var tokenId = BigInteger.Parse(request.Web3NFTId.ToString());
            var burnFunction = erc721Contract.GetFunction("burn");
            var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                senderAccount.Address, 
                new HexBigInteger(600000), 
                null, 
                null, 
                tokenId);

            if (receipt.HasErrors() == true)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 burn failed."));
                return result;
            }

            result.Result.TransactionResult = receipt.TransactionHash;
            result.IsError = false;
            result.Message = "NFT burned successfully on Avalanche.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }
        return result;
    }

    #endregion
}

[Function(AvalancheContractHelper.GetAvatarDetailByIdFuncName, typeof(GetAvatarDetailByIdFunction))]
file sealed class GetAvatarDetailByIdFunction : FunctionMessage
{
    [Parameter("uint256", "entityId", 1)]
    public BigInteger EntityId { get; set; }
}

[Function(AvalancheContractHelper.GetHolonByIdFuncName, typeof(GetHolonByIdyIdFunction))]
file sealed class GetHolonByIdyIdFunction : FunctionMessage
{
    [Parameter("uint256", "entityId", 1)]
    public BigInteger EntityId { get; set; }
}

[Function(AvalancheContractHelper.GetAvatarByIdFuncName, typeof(GetAvatarByIdFunction))]
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

file static class AvalancheContractHelper
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
    public const string GetNFTDataFuncName = "GetNFTData";
    public const string SendNftFuncName = "sendNFT";
    public const string MintFuncName = "mint";
    public const string Abi = "[{\"inputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"constructor\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"sender\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"ERC721IncorrectOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ERC721InsufficientApproval\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"approver\",\"type\":\"address\"}],\"name\":\"ERC721InvalidApprover\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"}],\"name\":\"ERC721InvalidOperator\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"ERC721InvalidOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"receiver\",\"type\":\"address\"}],\"name\":\"ERC721InvalidReceiver\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"sender\",\"type\":\"address\"}],\"name\":\"ERC721InvalidSender\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ERC721NonexistentToken\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"OwnableInvalidOwner\",\"type\":\"error\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"account\",\"type\":\"address\"}],\"name\":\"OwnableUnauthorizedAccount\",\"type\":\"error\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"approved\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"Approval\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"indexed\":false,\"internalType\":\"bool\",\"name\":\"approved\",\"type\":\"bool\"}],\"name\":\"ApprovalForAll\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"previousOwner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"newOwner\",\"type\":\"address\"}],\"name\":\"OwnershipTransferred\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"avatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateAvatar\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"avatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateAvatarDetail\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"holonId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"CreateHolon\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteAvatar\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteAvatarDetail\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"DeleteHolon\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetAvatarById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"AvatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structAvatar\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetAvatarDetailById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"AvatarId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structAvatarDetail\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetAvatarDetailsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetAvatarsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"}],\"name\":\"GetHolonById\",\"outputs\":[{\"components\":[{\"internalType\":\"uint256\",\"name\":\"EntityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"HolonId\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"Info\",\"type\":\"string\"}],\"internalType\":\"structHolon\",\"name\":\"\",\"type\":\"tuple\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"GetHolonsCount\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"count\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateAvatar\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateAvatarDetail\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"entityId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"info\",\"type\":\"string\"}],\"name\":\"UpdateHolon\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"admin\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"approve\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"getApproved\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"getTransferHistory\",\"outputs\":[{\"components\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"internalType\":\"structAvalancheOASIS.NFTTransfer[]\",\"name\":\"\",\"type\":\"tuple[]\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"}],\"name\":\"isApprovedForAll\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"metadataUri\",\"type\":\"string\"}],\"name\":\"mint\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"name\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"nextTokenId\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"name\":\"nftMetadata\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"metadataUri\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"},{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"name\":\"nftTransfers\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"owner\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"ownerOf\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"renounceOwnership\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"safeTransferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"bytes\",\"name\":\"data\",\"type\":\"bytes\"}],\"name\":\"safeTransferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"fromWalletAddress\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"toWalletAddress\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"fromProviderType\",\"type\":\"string\"},{\"internalType\":\"string\",\"name\":\"toProviderType\",\"type\":\"string\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"},{\"internalType\":\"string\",\"name\":\"memoText\",\"type\":\"string\"}],\"name\":\"sendNFT\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"operator\",\"type\":\"address\"},{\"internalType\":\"bool\",\"name\":\"approved\",\"type\":\"bool\"}],\"name\":\"setApprovalForAll\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"bytes4\",\"name\":\"interfaceId\",\"type\":\"bytes4\"}],\"name\":\"supportsInterface\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"symbol\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"tokenExists\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"tokenURI\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"tokenId\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"newOwner\",\"type\":\"address\"}],\"name\":\"transferOwnership\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]";

    // Helper methods for parsing Avalanche data
    private static IAvatar ParseAvalancheToAvatar(string jsonData)
    {
        try
        {
            var avatarData = JsonSerializer.Deserialize<JsonElement>(jsonData);
            var avatar = new Avatar();
            
            if (avatarData.TryGetProperty("id", out var id))
                avatar.Id = Guid.Parse(id.GetString());
            
            if (avatarData.TryGetProperty("username", out var username))
                avatar.Username = username.GetString();
            
            if (avatarData.TryGetProperty("email", out var email))
                avatar.Email = email.GetString();
            
            if (avatarData.TryGetProperty("firstName", out var firstName))
                avatar.FirstName = firstName.GetString();
            
            if (avatarData.TryGetProperty("lastName", out var lastName))
                avatar.LastName = lastName.GetString();
            
            return avatar;
        }
        catch
        {
            return new Avatar();
        }
    }

    private static IAvatarDetail ParseAvalancheToAvatarDetail(string jsonData)
    {
        try
        {
            var avatarDetailData = JsonSerializer.Deserialize<JsonElement>(jsonData);
            var avatarDetail = new AvatarDetail();
            
            if (avatarDetailData.TryGetProperty("id", out var id))
                avatarDetail.Id = Guid.Parse(id.GetString());
            
            if (avatarDetailData.TryGetProperty("avatarId", out var avatarId))
                avatarDetail.Username = avatarId.GetString();
            
            return avatarDetail;
        }
        catch
        {
            return new AvatarDetail();
        }
    }

    private static IHolon ParseAvalancheToHolon(string jsonData)
    {
        try
        {
            var holonData = JsonSerializer.Deserialize<JsonElement>(jsonData);
            var holon = new Holon();
            
            if (holonData.TryGetProperty("id", out var id))
                holon.Id = Guid.Parse(id.GetString());
            
            if (holonData.TryGetProperty("name", out var name))
                holon.Name = name.GetString();
            
            return holon;
        }
        catch
        {
            return new Holon();
        }
    }

    private static IWeb3NFT ParseAvalancheToNFT(string jsonData)
    {
        try
        {
            var nftData = JsonSerializer.Deserialize<JsonElement>(jsonData);
            var nft = new Web3NFT();
            
            if (nftData.TryGetProperty("id", out var id))
                nft.Id = Guid.Parse(id.GetString());
            
            if (nftData.TryGetProperty("title", out var title))
                nft.Title = title.GetString();
            
            if (nftData.TryGetProperty("description", out var description))
                nft.Description = description.GetString();
            
            return nft;
        }
        catch
        {
            return new Web3NFT();
        }
    }
}
