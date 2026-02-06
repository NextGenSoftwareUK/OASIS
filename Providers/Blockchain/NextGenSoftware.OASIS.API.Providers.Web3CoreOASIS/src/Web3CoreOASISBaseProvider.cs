using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using Nethereum.JsonRpc.Client;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Nethereum.Contracts;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using System.Net.Http;
using Newtonsoft.Json;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Threading;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Nethereum.Util;
using Nethereum.Hex.HexTypes;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public class Web3CoreOASISBaseProvider(string hostUri, string chainPrivateKey, string contractAddress) :
    OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
{
    private readonly string _hostURI = hostUri;
    private readonly string _chainPrivateKey = chainPrivateKey;
    private readonly string _contractAddress = contractAddress;

    private Web3CoreOASIS? _web3CoreOASIS;
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly string _apiBaseUrl = string.Empty; // configure in concrete implementations
    private Nethereum.Web3.Web3? _web3Client;

    public bool IsVersionControlEnabled { get; set; }

    private static Guid CreateDeterministicGuid(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Guid.Empty;
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new Guid(bytes.Take(16).ToArray());
    }

    //public Web3CoreOASISBaseProvider(string hostUri, string chainPrivateKey, BigInteger chainId, string contractAddress)
    //{
    //    this.ProviderName = "Web3CoreOASISBaseProvider";
    //    this.ProviderDescription = "Web3CoreOASISBaseProvider";
    //    this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EthereumOASIS);
    //    this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
    //    this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
    //    this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
    //    this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
    //    this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
    //    this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    //}

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
            // Require hostURI for activation (needed for RPC calls)
            // chainPrivateKey is optional - only needed for transactions, not for key generation
            if (_hostURI is { Length: > 0 })
            {
                // Only initialize Web3CoreOASIS and Web3 client if we have a private key
                // Otherwise, just mark as activated for read-only operations (like key generation)
                if (_chainPrivateKey is { Length: > 0 })
                {
                    _web3CoreOASIS = new(_chainPrivateKey, _hostURI, _contractAddress, Web3CoreOASISBaseProviderHelper.Abi);
                    // Initialize Web3 client for bridge operations
                    var account = new Account(_chainPrivateKey);
                    _web3Client = new Nethereum.Web3.Web3(account, _hostURI);
                }
                // Even without private key, we can activate for key generation and read operations
                this.IsProviderActivated = true;
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateProvider in {this.ProviderName} -> Web3CoreOASIS Provider. Reason: HostURI is required for activation.");
            }
        }
        catch (Exception ex)
        {
            this.IsProviderActivated = false;
            OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateProviderAsync in {this.ProviderName} -> Web3CoreOASIS Provider. Reason: {ex}");
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
        _web3CoreOASIS = null;
        IsProviderActivated = false;

        return new(value: true);
    }

    public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        => DeleteAvatarAsync(id, softDelete).Result;

    public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
    {
        return DeleteAvatarAsync(providerKey, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
    {
        OASISResult<bool> result = new();
        string errorMessage = "Error in DeleteAvatarAsync method in Web3CoreOASIS while deleting avatar. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            int avatarEntityId = HashUtility.GetNumericHash(id.ToString());
            bool isDeleted = await _web3CoreOASIS.DeleteAvatarAsync((uint)avatarEntityId);

            result.Result = isDeleted;
            result.IsError = false;
            result.IsSaved = isDeleted;
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

    public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
    {
        OASISResult<bool> result = new();
        string errorMessage = "Error in DeleteAvatarAsync method in Web3CoreOASIS while deleting avatar by provider key. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            result = await DeleteAvatarAsync(providerKey, softDelete);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
    {
        return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
    {
        OASISResult<bool> result = new();
        string errorMessage = "Error in DeleteAvatarByEmailAsync method in Web3CoreOASIS while deleting avatar by email. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            result = await DeleteAvatarByEmailAsync(avatarEmail, softDelete);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
    {
        return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
    }

    public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
    {
        OASISResult<bool> result = new();
        string errorMessage = "Error in DeleteAvatarByUsernameAsync method in Web3CoreOASIS while deleting avatar by username. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            result = await DeleteAvatarByUsernameAsync(avatarUsername, softDelete);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"{errorMessage}{ex.Message}", ex);
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
        string errorMessage = "Error in DeleteHolonAsync method in Web3CoreOASIS while deleting holon. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

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
            bool isDeleted = await _web3CoreOASIS.DeleteHolonAsync((uint)holonEntityId);

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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
                result.Message = "Holon deleted successfully by provider key from Web3Core";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Web3Core: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by email from Web3Core";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by email from Web3Core: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Export all holons for avatar from Web3Core
            var holonsResult = await LoadHolonsForParentAsync(avatarId);
            if (holonsResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                return result;
            }

            result.Result = holonsResult.Result;
            result.IsError = false;
            result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar from Web3Core";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar from Web3Core: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by username from Web3Core";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by username from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = _httpClient.GetAsync($"{_apiBaseUrl}/network/holons/nearby?type={Type}").Result;

            if (response.IsSuccessStatusCode)
            {
                var content = response.Content.ReadAsStringAsync().Result;
                var holons = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Holon>>(content);
                
                if (holons != null)
                {
                    result.Result = holons.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons?.Count ?? 0} holons near you from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting holons near you from Web3Core: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var allAvatarsResult = LoadAllAvatars();
            if (allAvatarsResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {allAvatarsResult.Message}");
                return result;
            }
            
            var nearbyAvatars = new List<IAvatar>();
            foreach (var avatar in allAvatarsResult.Result)
            {
                var meta = avatar.MetaData;
                if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
                {
                    if (double.TryParse(meta["Latitude"]?.ToString(), out double avatarLat) &&
                        double.TryParse(meta["Longitude"]?.ToString(), out double avatarLong))
                    {
                        double distance = NextGenSoftware.OASIS.API.Core.Helpers.GeoHelper.CalculateDistance(geoLat, geoLong, avatarLat, avatarLong);
                        if (distance <= radiusInMeters)
                            nearbyAvatars.Add(avatar);
                    }
                }
            }
            
            result.Result = nearbyAvatars;
            result.IsError = false;
            result.Message = $"Successfully loaded {nearbyAvatars.Count} avatars within {radiusInMeters}m of ({geoLat}, {geoLong}) from Web3Core";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting avatars near you from Web3Core: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var allHolonsResult = LoadAllHolons(Type);
            if (allHolonsResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons: {allHolonsResult.Message}");
                return result;
            }
            
            var nearbyHolons = new List<IHolon>();
            foreach (var holon in allHolonsResult.Result)
            {
                var meta = holon.MetaData;
                if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
                {
                    if (double.TryParse(meta["Latitude"]?.ToString(), out double holonLat) &&
                        double.TryParse(meta["Longitude"]?.ToString(), out double holonLong))
                    {
                        double distance = NextGenSoftware.OASIS.API.Core.Helpers.GeoHelper.CalculateDistance(geoLat, geoLong, holonLat, holonLong);
                        if (distance <= radiusInMeters)
                            nearbyHolons.Add(holon);
                    }
                }
            }
            
            result.Result = nearbyHolons;
            result.IsError = false;
            result.Message = $"Successfully loaded {nearbyHolons.Count} holons of type {Type} within {radiusInMeters}m of ({geoLat}, {geoLong}) from Web3Core";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error getting holons near you from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    // distance helpers moved to GeoHelper for reuse

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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
            result.Message = $"Successfully imported {importedCount} holons to Web3Core";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error importing holons to Web3Core: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Load all avatar details from Web3Core blockchain
            var avatarDetailsData = new OASISResult<List<IAvatarDetail>> { Result = new List<IAvatarDetail>() };
            if (avatarDetailsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details: {avatarDetailsData.Message}");
                return result;
            }

            if (avatarDetailsData.Result != null)
            {
                var avatarDetails = new List<IAvatarDetail>();
                foreach (var avatarDetailData in avatarDetailsData.Result)
                {
                    var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailData.ToString());
                    if (avatarDetail != null)
                    {
                        avatarDetails.Add(avatarDetail);
                    }
                }
                
                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Web3Core";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No avatar details found on Web3Core blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Web3Core: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Load all avatars from Web3Core blockchain
            var avatarsData = new OASISResult<List<IAvatar>> { Result = new List<IAvatar>() };
            if (avatarsData.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsData.Message}");
                return result;
            }

            if (avatarsData.Result != null)
            {
                var avatars = new List<IAvatar>();
                foreach (var avatarData in avatarsData.Result)
                {
                    var avatar = JsonConvert.DeserializeObject<Avatar>(avatarData.ToString());
                    if (avatar != null)
                    {
                        avatars.Add(avatar);
                    }
                }
                
                result.Result = avatars;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatars.Count} avatars from Web3Core";
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, "No avatars found on Web3Core blockchain");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatars from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/all?type={type}&version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holons = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Holon>>(content);
                
                if (holons != null)
                {
                    result.Result = holons.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/{id}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatar = Newtonsoft.Json.JsonConvert.DeserializeObject<Avatar>(content);
                
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
    {
        return LoadAvatarAsync(Id, version).Result;
    }


    public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-email/{Uri.EscapeDataString(avatarEmail)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatar = Newtonsoft.Json.JsonConvert.DeserializeObject<Avatar>(content);
                
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by email from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
    {
        return LoadAvatarByEmailAsync(avatarEmail, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatar = Newtonsoft.Json.JsonConvert.DeserializeObject<Avatar>(content);
                
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by provider key from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
    {
        return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
    }

    public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IAvatar>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-username/{Uri.EscapeDataString(avatarUsername)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatar = Newtonsoft.Json.JsonConvert.DeserializeObject<Avatar>(content);
                
                if (avatar != null)
                {
                    result.Result = avatar;
                    result.IsError = false;
                    result.Message = "Avatar loaded successfully by username from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
    {
        return LoadAvatarDetailAsync(id, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
    {
        OASISResult<IAvatarDetail> result = new();
        string errorMessage = "Error in LoadAvatarDetailAsync method in Web3CoreOASIS while loading an avatar detail. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            int avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());
            EntityOASIS detailInfo = await _web3CoreOASIS.GetAvatarDetailByIdAsync((uint)avatarDetailEntityId);

            result.IsError = false;
            result.IsLoaded = true;
            result.Result = Newtonsoft.Json.JsonConvert.DeserializeObject<AvatarDetail>(System.Text.Encoding.UTF8.GetString(detailInfo.Info))
                ?? throw new InvalidOperationException();
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

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/by-email/{Uri.EscapeDataString(avatarEmail)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatarDetail = Newtonsoft.Json.JsonConvert.DeserializeObject<AvatarDetail>(content);
                
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by email from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar detail from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
    {
        return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/by-username/{Uri.EscapeDataString(avatarUsername)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatarDetail = Newtonsoft.Json.JsonConvert.DeserializeObject<AvatarDetail>(content);
                
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by username from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar detail from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new Exception();
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/by-provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holon = Newtonsoft.Json.JsonConvert.DeserializeObject<Holon>(content);
                
                if (holon != null)
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully by provider key from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holon from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        OASISResult<IHolon> result = new();
        string errorMessage = "Error in LoadHolonAsync method in Web3CoreOASIS while loading holon. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            int holonEntityId = HashUtility.GetNumericHash(id.ToString());
            EntityOASIS holonInfo = await _web3CoreOASIS.GetHolonByIdAsync((uint)holonEntityId);

            result.Result = Newtonsoft.Json.JsonConvert.DeserializeObject<Holon>(System.Text.Encoding.UTF8.GetString(holonInfo.Info))
                ?? throw new InvalidOperationException();
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

    public async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/by-provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holon = Newtonsoft.Json.JsonConvert.DeserializeObject<Holon>(content);
                
                if (holon != null)
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully by provider key from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holon from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Web3Core: {ex.Message}", ex);
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

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/for-parent/{id}?type={type}&version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holons = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Holon>>(content);
                
                if (holons != null)
                {
                    result.Result = holons.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons for parent from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByProviderKeyAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/for-parent-by-provider-key/{Uri.EscapeDataString(providerKey)}?type={type}&version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holons = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Holon>>(content);
                
                if (holons != null)
                {
                    result.Result = holons.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons for parent by provider key from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentByProviderKeyAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/for-parent-by-provider-key/{Uri.EscapeDataString(providerKey)}?type={type}&version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holons = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Holon>>(content);
                
                if (holons != null)
                {
                    result.Result = holons.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons for parent by provider key from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Web3Core: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Web3Core is a blockchain provider, not a storage provider
            // Return empty result as blockchain providers don't store holons by metadata
            result.Result = new List<IHolon>();
            result.IsError = false;
            result.Message = "Web3Core blockchain provider does not support metadata-based holon queries";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Web3Core is a blockchain provider, not a storage provider
            // Return empty result as blockchain providers don't store holons by metadata
            result.Result = new List<IHolon>();
            result.IsError = false;
            result.Message = "Web3Core blockchain provider does not support metadata-based holon queries";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {ex.Message}", ex);
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
            sb.AppendLine("// Auto-generated by Web3CoreOASISBaseProvider.NativeCodeGenesis");
            sb.AppendLine("pragma solidity ^0.8.0;");
            sb.AppendLine();
            sb.AppendLine($"contract {celestialBody.Name?.ToPascalCase() ?? "Web3Contract"} {{");
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
                OASISErrorHandling.HandleError(ref result, saveAvatarTask.Exception?.Message ?? string.Empty, saveAvatarTask.Exception ?? new());
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
        string errorMessage = "Error in SaveAvatarAsync method in Web3CoreOASIS while saving avatar. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            string avatarInfo = Newtonsoft.Json.JsonConvert.SerializeObject(avatar);
            int avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
            string avatarId = avatar.AvatarId.ToString();

            await _web3CoreOASIS.CreateAvatarAsync(
                (uint)avatarEntityId, Encoding.UTF8.GetBytes(avatarId), Encoding.UTF8.GetBytes(avatarInfo));

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
                OASISErrorHandling.HandleError(ref result, saveAvatarDetailTask.Exception?.Message ?? string.Empty, saveAvatarDetailTask.Exception ?? new());
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
        string errorMessage = "Error in SaveAvatarDetailAsync method in Web3CoreOASIS while saving and avatar detail. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            string avatarDetailInfo = Newtonsoft.Json.JsonConvert.SerializeObject(avatarDetail);
            int avatarDetailEntityId = HashUtility.GetNumericHash(avatarDetail.Id.ToString());
            string avatarDetailId = avatarDetail.Id.ToString();

            await _web3CoreOASIS.CreateAvatarAsync(
                (uint)avatarDetailEntityId, Encoding.UTF8.GetBytes(avatarDetailId), Encoding.UTF8.GetBytes(avatarDetailInfo));

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
        string errorMessage = "Error in SaveHolonAsync method in Web3CoreOASIS while saving holon. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            string holonInfo = Newtonsoft.Json.JsonConvert.SerializeObject(holon);
            int holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
            string holonId = holon.Id.ToString();

            await _web3CoreOASIS.CreateAvatarAsync(
                (uint)holonEntityId, Encoding.UTF8.GetBytes(holonId), Encoding.UTF8.GetBytes(holonInfo));

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
        string errorMessage = "Error in SaveHolonsAsync method in Web3CoreOASIS while saving holons. Reason: ";

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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (searchParams == null)
            {
                OASISErrorHandling.HandleError(ref result, "Search parameters cannot be null");
                return result;
            }

            // Web3Core is a blockchain provider, not a storage provider
            // Return empty search results as blockchain providers don't support full-text search
            result.Result = new SearchResults
            {
                SearchResultAvatars = new List<IAvatar>(),
                SearchResultHolons = new List<IHolon>()
            };
            result.IsError = false;
            result.Message = "Web3Core blockchain provider does not support search. Use blockchain explorers or indexers for on-chain data queries.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error performing search: {ex.Message}", ex);
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
        string errorMessage = "Error in SendTransactionAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            TransactionReceipt transactionResult = await _web3CoreOASIS.SendTransactionAsync(toWalletAddress, amount);

            if (transactionResult.HasErrors() is true)
            {
                result.Message = string.Concat(errorMessage, "Web3CoreOASIS transaction performing failed! " +
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
        => SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;

    public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByDefaultWalletAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

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
        result = await SendTransactionBaseAsync(senderAvatarPrivateKey, receiverAvatarAddress, amount);

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
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByEmailAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        // Load avatars by email to get their wallet addresses
        var fromAvatarResult = await LoadAvatarByEmailAsync(fromAvatarEmail);
        var toAvatarResult = await LoadAvatarByEmailAsync(toAvatarEmail);
        
        if (fromAvatarResult.IsError || fromAvatarResult.Result == null)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Failed to load sender avatar: {fromAvatarResult.Message}"));
            return result;
        }
        
        if (toAvatarResult.IsError || toAvatarResult.Result == null)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Failed to load receiver avatar: {toAvatarResult.Message}"));
            return result;
        }
        
        // Get wallet addresses from avatars
        var fromWallet = fromAvatarResult.Result.ProviderWallets?.ContainsKey(this.ProviderType.Value) == true 
            ? fromAvatarResult.Result.ProviderWallets[this.ProviderType.Value]?.FirstOrDefault() 
            : null;
        var toWallet = toAvatarResult.Result.ProviderWallets?.ContainsKey(this.ProviderType.Value) == true 
            ? toAvatarResult.Result.ProviderWallets[this.ProviderType.Value]?.FirstOrDefault() 
            : null;
        
        if (fromWallet == null || toWallet == null)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Wallet addresses not found for avatars"));
            return result;
        }
        
        var fromWalletAddress = fromWallet.WalletAddress ?? fromWallet.PublicKey;
        var toWalletAddress = toWallet.WalletAddress ?? toWallet.PublicKey;
        
        if (string.IsNullOrWhiteSpace(fromWalletAddress) || string.IsNullOrWhiteSpace(toWalletAddress))
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Wallet addresses not found for avatars"));
            return result;
        }
        
        OASISResult<List<string>> senderAvatarPrivateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarByUsername(fromAvatarResult.Result.Username, this.ProviderType.Value);
        if (senderAvatarPrivateKeysResult.IsError || senderAvatarPrivateKeysResult.Result == null || senderAvatarPrivateKeysResult.Result.Count == 0)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get sender private key"));
            return result;
        }

        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
        result = await SendTransactionBaseAsync(senderAvatarPrivateKey, toWalletAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
    {
        // For token transactions, we would need to interact with the token contract
        // This is a placeholder that returns an error indicating token transactions need contract interaction
        OASISResult<ITransactionResponse> result = new();
        OASISErrorHandling.HandleError(ref result, "Token transactions require contract interaction. Use SendTokenAsync methods instead.");
        return Task.FromResult(result);
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
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionByIdAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        // Load avatars by ID to get their wallet addresses
        var fromAvatarResult = await LoadAvatarAsync(fromAvatarId);
        var toAvatarResult = await LoadAvatarAsync(toAvatarId);
        
        if (fromAvatarResult.IsError || fromAvatarResult.Result == null)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Failed to load sender avatar: {fromAvatarResult.Message}"));
            return result;
        }
        
        if (toAvatarResult.IsError || toAvatarResult.Result == null)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Failed to load receiver avatar: {toAvatarResult.Message}"));
            return result;
        }
        
        // Get wallet addresses from avatars
        var fromWalletAddress = fromAvatarResult.Result.ProviderWallets?.ContainsKey(this.ProviderType.Value) == true 
            ? fromAvatarResult.Result.ProviderWallets[this.ProviderType.Value]?.FirstOrDefault()?.PublicKey 
            : null;
        var toWalletAddress = toAvatarResult.Result.ProviderWallets?.ContainsKey(this.ProviderType.Value) == true 
            ? toAvatarResult.Result.ProviderWallets[this.ProviderType.Value]?.FirstOrDefault()?.PublicKey 
            : null;
        
        if (string.IsNullOrWhiteSpace(fromWalletAddress) || string.IsNullOrWhiteSpace(toWalletAddress))
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Wallet addresses not found for avatars"));
            return result;
        }
        
        OASISResult<List<string>> senderAvatarPrivateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarByUsername(fromAvatarResult.Result.Username, this.ProviderType.Value);
        if (senderAvatarPrivateKeysResult.IsError || senderAvatarPrivateKeysResult.Result == null || senderAvatarPrivateKeysResult.Result.Count == 0)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Failed to get sender private key"));
            return result;
        }
        
        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
        result = await SendTransactionBaseAsync(senderAvatarPrivateKey, toWalletAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
    {
        // For token transactions, we would need to interact with the token contract
        // This is a placeholder that returns an error indicating token transactions need contract interaction
        OASISResult<ITransactionResponse> result = new();
        OASISErrorHandling.HandleError(ref result, "Token transactions require contract interaction. Use SendTokenAsync methods instead.");
        return Task.FromResult(result);
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
        string errorMessage = "Error in SendTransactionByUsernameAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        OASISResult<List<string>> senderAvatarPrivateKeysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, this.ProviderType.Value);
        OASISResult<List<string>> receiverAvatarAddressesResult = KeyManager.Instance.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername, this.ProviderType.Value);

        string senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
        string receiverWalletAddress = receiverAvatarAddressesResult.Result[0];
        result = await SendTransactionBaseAsync(senderAvatarPrivateKey, receiverWalletAddress, amount);

        if (result.IsError)
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);

        return result;
    }

    public Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
    {
        // For token transactions, we would need to interact with the token contract
        // This is a placeholder that returns an error indicating token transactions need contract interaction
        OASISResult<ITransactionResponse> result = new();
        OASISErrorHandling.HandleError(ref result, "Token transactions require contract interaction. Use SendTokenAsync methods instead.");
        return Task.FromResult(result);
    }

    private async Task<OASISResult<ITransactionResponse>> SendTransactionBaseAsync(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
    {
        OASISResult<ITransactionResponse> result = new();
        string errorMessage = "Error in SendTransactionBaseAsync method in Web3CoreOASIS sending transaction. Reason: ";

        if (amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            Web3CoreOASIS web3CoreInstance = new(senderAccountPrivateKey, _hostURI, _contractAddress, Web3CoreOASISBaseProviderHelper.Abi);
            TransactionReceipt receipt = await web3CoreInstance.SendTransactionAsync(receiverAccountAddress, amount);

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
        string errorMessage = "Error in SendNFTAsync method in Web3CoreOASIS while sending nft. Reason: ";

        if (transaction.Amount <= 0)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.InvalidAmountError);
            return result;
        }

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            //TODO: Double check this still works! lol
            string transactionHash = await _web3CoreOASIS.SendNFTAsync(
                transaction.FromWalletAddress,
                transaction.ToWalletAddress,
                Convert.ToInt64(transaction.TokenId),
                //transaction.FromProvider.Value.ToString(),
                //transaction.ToProvider.Value.ToString(),
                "None", //Obsolete.
                "None",
                new BigInteger(transaction.Amount),
                transaction.MemoText
            );

            IWeb3NFTTransactionResponse response = new Web3NFTTransactionResponse
            {
                Web3NFT = new Web3NFT()
                {
                    MemoText = transaction.MemoText,
                    MintTransactionHash = transactionHash
                },
                TransactionResult = transactionHash
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
        string errorMessage = "Error in MintNFTAsync method in Web3CoreOASIS while minting nft. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            string metadataJson = Newtonsoft.Json.JsonConvert.SerializeObject(transaction);
            //string transactionHash = await _web3CoreOASIS.MintAsync(transaction.MintWalletAddress, metadataJson);
            string transactionHash = await _web3CoreOASIS.MintAsync(transaction.SendToAddressAfterMinting, metadataJson);

            IWeb3NFTTransactionResponse response = new Web3NFTTransactionResponse
            {
                Web3NFT = new Web3NFT()
                {
                    MemoText = transaction.MemoText,
                    MintTransactionHash = transactionHash
                },
                TransactionResult = transactionHash
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
        string errorMessage = "Error in LoadOnChainNFTDataAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (string.IsNullOrWhiteSpace(nftTokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                return result;
            }

            // ERC721 ABI for name, symbol, tokenURI
            var erc721Abi = "[{\"constant\":true,\"inputs\":[],\"name\":\"name\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[],\"name\":\"symbol\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"tokenURI\",\"outputs\":[{\"name\":\"\",\"type\":\"string\"}],\"type\":\"function\"},{\"constant\":true,\"inputs\":[{\"name\":\"_tokenId\",\"type\":\"uint256\"}],\"name\":\"ownerOf\",\"outputs\":[{\"name\":\"\",\"type\":\"address\"}],\"type\":\"function\"}]";
            var nftContract = _web3Client.Eth.GetContract(erc721Abi, nftTokenAddress);
            
            var nameFunction = nftContract.GetFunction("name");
            var symbolFunction = nftContract.GetFunction("symbol");
            
            var name = await nameFunction.CallAsync<string>();
            var symbol = await symbolFunction.CallAsync<string>();

            var nft = new Web3NFT
            {
                NFTTokenAddress = nftTokenAddress,
                Symbol = symbol
            };

            result.Result = nft;
            result.IsError = false;
            result.Message = "NFT data loaded successfully.";
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
        var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
        string errorMessage = "Error in BurnNFTAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
            var nftContract = web3Client.Eth.GetContract(erc721Abi, request.NFTTokenAddress);
            var burnFunction = nftContract.GetFunction("burn");
            
            var tokenId = BigInteger.Parse(request.Web3NFTId.ToString());
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
            result.Message = "NFT burned successfully.";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
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
        string errorMessage = "Error in SendTokenAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
                new HexBigInteger(600000), 
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
        string errorMessage = "Error in MintTokenAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
            var keysResult = KeyManager.Instance.GetProviderPrivateKeysForAvatarById(request.MintedByAvatarId, ProviderType.Value);
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
                new HexBigInteger(600000), 
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
        string errorMessage = "Error in BurnTokenAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
            // Get burn amount from token balance or use default
            var balanceOfFunction = erc20Contract.GetFunction("balanceOf");
            var balance = await balanceOfFunction.CallAsync<BigInteger>(senderAccount.Address);
            var burnAmount = balance > 0 ? (decimal)balance / (decimal)multiplier : 1m;
            var amountBigInt = new BigInteger(burnAmount * (decimal)multiplier);
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
        string errorMessage = "Error in LockTokenAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
            {
                OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                return result;
            }

            // Lock token by transferring to bridge pool
            var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
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
        string errorMessage = "Error in UnlockTokenAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Token address is required");
                return result;
            }

            // Get recipient address from KeyManager using UnlockedByAvatarId
            var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager.Instance, ProviderType.Value, request.UnlockedByAvatarId);
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
        string errorMessage = "Error in GetBalanceAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
            {
                OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                return result;
            }

            // Get native token balance (ETH, BNB, etc.)
            // Note: ERC20 token balance would require a separate TokenAddress parameter
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
        string errorMessage = "Error in GetTransactionsAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
        string errorMessage = "Error in GenerateKeyPairAsync method in Web3CoreOASIS. Reason: ";

        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var publicKey = ecKey.GetPublicAddress();

            result.Result = new NextGenSoftware.OASIS.API.Core.Objects.KeyPairAndWallet
            {
                PrivateKey = privateKey,
                PublicKey = publicKey,
                WalletAddress = publicKey, // Ethereum/Base address (0x format)
                WalletAddressLegacy = publicKey // Also set for compatibility
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
            var publicKey = ecKey.GetPublicAddress();

            result.Result = (publicKey, privateKey, string.Empty);
            result.IsError = false;
            result.Message = "Web3Core account created successfully. Seed phrase not applicable for direct key generation.";
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

        //TODO: Fix asap!
        //try
        //{
        //    if (!IsProviderActivated)
        //    {
        //        OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
        //        return result;           }

        //    var wallet = new Nethereum.HdWallet.Wallet(seedPhrase, null);
        //    var account = wallet.GetAccount(0);

        //    result.Result = (account.Address, account.PrivateKey);
        //    result.IsError = false;
        //    result.Message = "Web3Core account restored successfully.";
        //}
        //catch (Exception ex)
        //{
        //    OASISErrorHandling.HandleError(ref result, $"Error restoring account: {ex.Message}", ex);
        //}
        return result;
    }

    public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
    {
        var result = new OASISResult<BridgeTransactionResponse>();
        try
        {
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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

            var account = new Account(senderPrivateKey);
            var web3 = new Nethereum.Web3.Web3(account, _hostURI);

            // For bridge withdrawals, send to OASIS bridge pool address
            var bridgePoolAddress = _contractAddress;
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
            if (!IsProviderActivated || _web3Client == null)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
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
            OASISErrorHandling.HandleError(ref result, $"Error getting transaction status: {ex.Message}", ex);
            result.Result = BridgeTransactionStatus.NotFound;
        }
        return result;
    }

    #endregion
}

file static class Web3CoreOASISBaseProviderHelper
{
    public const string ProviderNotActivatedError = "Provider is not activated. Please activate provider and try again.";
    public const string InvalidAmountError = "Amount value must be greater than zero.";
    public const string Abi = @"
[
    {
      ""inputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""constructor""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""sender"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721IncorrectOwner"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""ERC721InsufficientApproval"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""approver"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidApprover"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidOperator"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidOwner"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""receiver"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidReceiver"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""sender"",
          ""type"": ""address""
        }
      ],
      ""name"": ""ERC721InvalidSender"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""ERC721NonexistentToken"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""OwnableInvalidOwner"",
      ""type"": ""error""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""account"",
          ""type"": ""address""
        }
      ],
      ""name"": ""OwnableUnauthorizedAccount"",
      ""type"": ""error""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""approved"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""Approval"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        },
        {
          ""indexed"": false,
          ""internalType"": ""bool"",
          ""name"": ""approved"",
          ""type"": ""bool""
        }
      ],
      ""name"": ""ApprovalForAll"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""previousOwner"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""newOwner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""OwnershipTransferred"",
      ""type"": ""event""
    },
    {
      ""anonymous"": false,
      ""inputs"": [
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""indexed"": true,
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""Transfer"",
      ""type"": ""event""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""approve"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""balanceOf"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes32"",
          ""name"": ""avatarId"",
          ""type"": ""bytes32""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""createAvatar"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes32"",
          ""name"": ""avatarId"",
          ""type"": ""bytes32""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""createAvatarDetail"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes32"",
          ""name"": ""holonId"",
          ""type"": ""bytes32""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""createHolon"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""deleteAvatar"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""deleteAvatarDetail"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""deleteHolon"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getApproved"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": """",
          ""type"": ""address""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getAvatarById"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""uint256"",
              ""name"": ""EntityId"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""bytes32"",
              ""name"": ""ExternalId"",
              ""type"": ""bytes32""
            },
            {
              ""internalType"": ""bytes"",
              ""name"": ""Info"",
              ""type"": ""bytes""
            }
          ],
          ""internalType"": ""struct EntityOASIS"",
          ""name"": """",
          ""type"": ""tuple""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getAvatarDetailById"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""uint256"",
              ""name"": ""EntityId"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""bytes32"",
              ""name"": ""ExternalId"",
              ""type"": ""bytes32""
            },
            {
              ""internalType"": ""bytes"",
              ""name"": ""Info"",
              ""type"": ""bytes""
            }
          ],
          ""internalType"": ""struct EntityOASIS"",
          ""name"": """",
          ""type"": ""tuple""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""getAvatarDetailsCount"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""count"",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""getAvatarsCount"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""count"",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getHolonById"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""uint256"",
              ""name"": ""EntityId"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""bytes32"",
              ""name"": ""ExternalId"",
              ""type"": ""bytes32""
            },
            {
              ""internalType"": ""bytes"",
              ""name"": ""Info"",
              ""type"": ""bytes""
            }
          ],
          ""internalType"": ""struct EntityOASIS"",
          ""name"": """",
          ""type"": ""tuple""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""getHolonsCount"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""count"",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""getTransferHistory"",
      ""outputs"": [
        {
          ""components"": [
            {
              ""internalType"": ""address"",
              ""name"": ""fromWalletAddress"",
              ""type"": ""address""
            },
            {
              ""internalType"": ""address"",
              ""name"": ""toWalletAddress"",
              ""type"": ""address""
            },
            {
              ""internalType"": ""string"",
              ""name"": ""fromProviderType"",
              ""type"": ""string""
            },
            {
              ""internalType"": ""string"",
              ""name"": ""toProviderType"",
              ""type"": ""string""
            },
            {
              ""internalType"": ""uint256"",
              ""name"": ""amount"",
              ""type"": ""uint256""
            },
            {
              ""internalType"": ""string"",
              ""name"": ""memoText"",
              ""type"": ""string""
            }
          ],
          ""internalType"": ""struct NFTTransfer[]"",
          ""name"": """",
          ""type"": ""tuple[]""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""owner"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        }
      ],
      ""name"": ""isApprovedForAll"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""metadataJson"",
          ""type"": ""string""
        }
      ],
      ""name"": ""mint"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""name"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""nextTokenId"",
      ""outputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""nftMetadata"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": ""metadataJson"",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": """",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""nftTransfers"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""fromWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""toWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""fromProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""toProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""amount"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""memoText"",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""owner"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": """",
          ""type"": ""address""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""ownerOf"",
      ""outputs"": [
        {
          ""internalType"": ""address"",
          ""name"": """",
          ""type"": ""address""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""renounceOwnership"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""safeTransferFrom"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""data"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""safeTransferFrom"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""fromWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""toWalletAddress"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""fromProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""toProviderType"",
          ""type"": ""string""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""amount"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""string"",
          ""name"": ""memoText"",
          ""type"": ""string""
        }
      ],
      ""name"": ""sendNFT"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""operator"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""bool"",
          ""name"": ""approved"",
          ""type"": ""bool""
        }
      ],
      ""name"": ""setApprovalForAll"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""bytes4"",
          ""name"": ""interfaceId"",
          ""type"": ""bytes4""
        }
      ],
      ""name"": ""supportsInterface"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [],
      ""name"": ""symbol"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""tokenURI"",
      ""outputs"": [
        {
          ""internalType"": ""string"",
          ""name"": """",
          ""type"": ""string""
        }
      ],
      ""stateMutability"": ""view"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""from"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""address"",
          ""name"": ""to"",
          ""type"": ""address""
        },
        {
          ""internalType"": ""uint256"",
          ""name"": ""tokenId"",
          ""type"": ""uint256""
        }
      ],
      ""name"": ""transferFrom"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""address"",
          ""name"": ""newOwner"",
          ""type"": ""address""
        }
      ],
      ""name"": ""transferOwnership"",
      ""outputs"": [],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""updateAvatar"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""updateAvatarDetail"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    },
    {
      ""inputs"": [
        {
          ""internalType"": ""uint256"",
          ""name"": ""entityId"",
          ""type"": ""uint256""
        },
        {
          ""internalType"": ""bytes"",
          ""name"": ""info"",
          ""type"": ""bytes""
        }
      ],
      ""name"": ""updateHolon"",
      ""outputs"": [
        {
          ""internalType"": ""bool"",
          ""name"": """",
          ""type"": ""bool""
        }
      ],
      ""stateMutability"": ""nonpayable"",
      ""type"": ""function""
    }
  ]
";

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
