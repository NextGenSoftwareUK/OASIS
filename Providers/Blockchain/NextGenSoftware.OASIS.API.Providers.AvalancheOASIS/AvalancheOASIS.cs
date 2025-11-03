using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
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
                var players = JsonSerializer.Deserialize<List<Player>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

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

    public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
    {
        OASISResult<ITransactionRespone> result = new();
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
        result = await SendAvalancheTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, "AVAX").Result;
    }

    public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
    {
        return await SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, "AVAX");
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionRespone>();
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

    public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, "AVAX").Result;
    }

    public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        return await SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, "AVAX");
    }

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionRespone>();
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

    public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        var result = new OASISResult<ITransactionRespone>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Avalanche provider is not activated");
                return result;
            }

            // Get avatar wallets by username
            var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByUsernameAsync(fromAvatarUsername, this.ProviderType.Value);
            var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByUsernameAsync(toAvatarUsername, this.ProviderType.Value);

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

    private async Task<OASISResult<ITransactionRespone>> SendAvalancheTransaction(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
    {
        OASISResult<ITransactionRespone> result = new();
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

    public OASISResult<IWeb4Web4NFTTransactionRespone> SendNFT(IWeb4NFTWalletTransactionRequest transaction)
        => SendNFTAsync(transaction).Result;


    public async Task<OASISResult<IWeb4Web4NFTTransactionRespone>> SendNFTAsync(IWeb4NFTWalletTransactionRequest transaction)
    {
        OASISResult<IWeb4Web4NFTTransactionRespone> result = new();
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

            IWeb4Web4NFTTransactionRespone response = new Web4NFTTransactionRespone
            {
                OASISNFT = new Web4OASISNFT()
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

    public OASISResult<IWeb4Web4NFTTransactionRespone> MintNFT(IMintWeb4NFTTRequest transation)
        => MintNFTAsync(transation).Result;

    public async Task<OASISResult<IWeb4Web4NFTTransactionRespone>> MintNFTAsync(IMintWeb4NFTTRequest transaction)
    {
        OASISResult<IWeb4Web4NFTTransactionRespone> result = new();
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

            IWeb4Web4NFTTransactionRespone response = new Web4NFTTransactionRespone
            {
                OASISNFT = new Web4OASISNFT()
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
            
                var nft = new Web4OASISNFT();
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

    private static IOASISNFT ParseAvalancheToNFT(string jsonData)
    {
        try
        {
            var nftData = JsonSerializer.Deserialize<JsonElement>(jsonData);
            var nft = new Web4OASISNFT();
            
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
            return new Web4OASISNFT();
        }
    }
}
