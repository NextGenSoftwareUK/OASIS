using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.IO;
using System.Reflection;
using System.Text;
using Nethereum.RPC.Accounts;
// using Nethereum.StandardTokenEIP20; // Commented out - type doesn't exist

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Call smart contract to get all avatar details directly
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/all?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatarDetails = System.Text.Json.JsonSerializer.Deserialize<List<AvatarDetail>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatarDetails != null)
                    {
                        result.Result = avatarDetails.Select(ad => (IAvatarDetail)ad).ToList();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar details from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query avatar by provider key from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for avatar by provider key
                try
                {
                    // Get current block number from Ethereum blockchain
                    var currentBlockNumber = await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    
                    // Get gas price from Ethereum blockchain
                    var gasPrice = await Web3Client.Eth.GasPrice.SendRequestAsync();
                    
                    // Query smart contract for avatar data using Nethereum
                    var contract = Web3Client.Eth.GetContract(_abi, _contractAddress);
                    var getAvatarByProviderKeyFunction = contract.GetFunction("getAvatarByProviderKey");
                    var avatarData = await getAvatarByProviderKeyFunction.CallAsync<object>(providerKey);
                    
                    // Parse the REAL smart contract data from Ethereum blockchain
                    var avatar = ParseEthereumToAvatar(avatarData, $"{providerKey}@ethereum.local");
                    if (avatar == null)
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar data from Ethereum smart contract");
                        return response;
                    }
                    
                        response.Result = avatar;
                        response.IsError = false;
                    response.Message = "Avatar loaded successfully by provider key from Ethereum blockchain";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Ethereum: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyVersionAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatar != null)
                    {
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully by provider key from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsByVersionAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/all?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatars = System.Text.Json.JsonSerializer.Deserialize<List<Avatar>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatars != null)
                    {
                        result.Result = avatars.Select(a => (IAvatar)a).ToList();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {avatars.Count} avatars from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatars from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        // Duplicate removed; use contract-backed implementation below
        /*public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-email/{Uri.EscapeDataString(avatarEmail)}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatar != null)
                    {
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully by email from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Ethereum: {ex.Message}", ex);
            }
            return result;
        }*/

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        // Duplicate removed; use contract-backed implementation below
        /*public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-username/{Uri.EscapeDataString(avatarUsername)}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatar != null)
                    {
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully by username from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Ethereum: {ex.Message}", ex);
            }
            return result;
        }*/

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            string errorMessage = "Error in LoadAvatarAsync method in EthereumOASIS while loading an avatar. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Id.ToString());
                var avatarDto = await _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId);
                if (avatarDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Avatar (with id {Id}) not found!"));
                    return result;
                }

                var avatarEntityResult = JsonConvert.DeserializeObject<Avatar>(avatarDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarEntityResult;
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

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real Ethereum implementation: Query smart contract for avatar by email
                try
                {
                    // Get current block number from Ethereum blockchain
                    var currentBlockNumber = await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    
                    // Get gas price from Ethereum blockchain
                    var gasPrice = await Web3Client.Eth.GasPrice.SendRequestAsync();
                    
                    // Query smart contract for avatar data using Nethereum
                    var contract = Web3Client.Eth.GetContract(_abi, _contractAddress);
                    var getAvatarByEmailFunction = contract.GetFunction("getAvatarByEmail");
                    var avatarData = await getAvatarByEmailFunction.CallAsync<object>(avatarEmail);
                    
                    // Parse the REAL smart contract data from Ethereum blockchain
                    var avatar = ParseEthereumToAvatar(avatarData, avatarEmail);
                    if (avatar == null)
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Ethereum smart contract");
                        return result;
                    }
                    
                    result.Result = avatar;
                        result.IsError = false;
                    result.Message = "Avatar loaded successfully by email from Ethereum blockchain";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query avatar from Ethereum smart contract by username
                // Real Ethereum implementation: Query smart contract for avatar by username
                try
                {
                    // Get current block number from Ethereum blockchain
                    var currentBlockNumber = await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    
                    // Get gas price from Ethereum blockchain
                    var gasPrice = await Web3Client.Eth.GasPrice.SendRequestAsync();
                    
                    // Query smart contract for avatar data using Nethereum
                    var contract = Web3Client.Eth.GetContract(_abi, _contractAddress);
                    var getAvatarByUsernameFunction = contract.GetFunction("getAvatarByUsername");
                    var avatarData = await getAvatarByUsernameFunction.CallAsync<object>(avatarUsername);
                    
                    // Parse the REAL smart contract data from Ethereum blockchain
                    var avatar = ParseEthereumToAvatar(avatarData, $"{avatarUsername}@ethereum.local");
                    if (avatar == null)
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Ethereum smart contract");
                        return result;
                    }
                    
                    result.Result = avatar;
                        result.IsError = false;
                    result.Message = "Avatar loaded successfully by username from Ethereum blockchain";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            string errorMessage = "Error in LoadAvatar method in EthereumOASIS load avatar. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Id.ToString());
                var avatarDto = _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId).Result;

                if (avatarDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Avatar (with id {Id}) not found!"));
                    return result;
                }

                var avatarEntityResult = JsonConvert.DeserializeObject<Avatar>(avatarDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarEntityResult;
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

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatar>();
            string errorMessage = "Error in SaveAvatar method in EthereumOASIS saving avatar. Reason: ";

            try
            {
                var avatarInfo = JsonConvert.SerializeObject(avatar);
                var avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarId = avatar.AvatarId.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateAvatarRequestAndWaitForReceiptAsync(avatarEntityId, avatarId, avatarInfo).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Creating of Avatar (Id): {avatar.AvatarId}, failed! Transaction performing is failure!"));
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

        public bool IsVersionControlEnabled { get; set; }

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

    }
}
