using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using NextGenSoftware.OASIS.API.Core.Objects;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS
{
    public partial class AptosOASIS
    {
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                if (!_isActivated)
                {
                    // Test connection to Aptos network
                    var testResponse = await _httpClient.GetAsync("/");
                    if (testResponse.IsSuccessStatusCode)
                    {
                        _isActivated = true;
                        response.Result = true;
                        response.Message = "Aptos provider activated successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to connect to Aptos network: {testResponse.StatusCode}");
                    }
                }
                else
                {
                    response.Result = true;
                    response.Message = "Aptos provider is already activated";
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating Aptos provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();

            try
            {
                _isActivated = false;
                response.Result = true;
                response.Message = "Aptos provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating Aptos provider: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar from Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "view",
                    @params = new
                    {
                        function = $"{_contractAddress}::oasis::get_avatar",
                        arguments = new[] { id.ToString() }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        var avatar = ParseAptosToAvatar(result.GetRawText());
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from Aptos: {ex.Message}");
            }

            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }




        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long latitude, long longitude, int version = 0)
        {
            return GetAvatarsNearMeAsync(latitude, longitude, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> GetAvatarsNearMeAsync(long latitude, long longitude, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Aptos provider is not activated");
                    return response;
                }

                // Load all avatars and filter by geospatial distance
                var allAvatarsResult = await LoadAllAvatarsAsync(version);
                if (allAvatarsResult.IsError || allAvatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars: {allAvatarsResult.Message}");
                    return response;
                }

                // Filter avatars by geospatial distance (using metadata for location)
                var nearbyAvatars = new List<IAvatar>();
                var centerLat = latitude / 1000000.0; // Convert from microdegrees to degrees
                var centerLon = longitude / 1000000.0;
                const int radiusInMeters = 10000; // Default 10km radius

                foreach (var avatar in allAvatarsResult.Result)
                {
                    if (avatar.MetaData != null && 
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lonObj))
                    {
                        if (double.TryParse(latObj?.ToString(), out var avatarLat) &&
                            double.TryParse(lonObj?.ToString(), out var avatarLon))
                        {
                            var distance = GeoHelper.CalculateDistance(centerLat, centerLon, avatarLat, avatarLon);
                            if (distance <= radiusInMeters)
                            {
                                nearbyAvatars.Add(avatar);
                            }
                        }
                    }
                }

                response.Result = nearbyAvatars;
                response.IsError = false;
                response.Message = $"Found {nearbyAvatars.Count} avatars near location";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting avatars near location: {ex.Message}");
            }
            return response;
        }

        public OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe(long latitude, long longitude, int version = 0)
        {
            // Players are avatars, so get avatars and convert to players
            var avatarsResult = GetAvatarsNearMe(latitude, longitude, version);
            if (avatarsResult.IsError || avatarsResult.Result == null)
            {
                return new OASISResult<IEnumerable<IPlayer>>
                {
                    IsError = avatarsResult.IsError,
                    Message = avatarsResult.Message
                };
            }
            
            // Convert avatars to players
            var players = avatarsResult.Result.Where(a => a is IPlayer).Cast<IPlayer>().ToList();
            return new OASISResult<IEnumerable<IPlayer>>
            {
                Result = players,
                IsError = false,
                Message = $"Found {players.Count} players near location"
            };
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long latitude, long longitude, int version = 0, HolonType holonType = HolonType.All)
        {   
            return GetHolonsNearMeAsync(latitude, longitude, holonType, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(long latitude, long longitude, HolonType type, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Aptos provider is not activated");
                    return response;
                }

                // Load all holons and filter by geospatial distance
                var allHolonsResult = await LoadAllHolonsAsync(type, version: version);
                if (allHolonsResult.IsError || allHolonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load holons: {allHolonsResult.Message}");
                    return response;
                }

                // Filter holons by geospatial distance (using metadata for location)
                var nearbyHolons = new List<IHolon>();
                var centerLat = latitude / 1000000.0; // Convert from microdegrees to degrees
                var centerLon = longitude / 1000000.0;
                const int radiusInMeters = 10000; // Default 10km radius

                foreach (var holon in allHolonsResult.Result)
                {
                    if (holon.MetaData != null && 
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lonObj))
                    {
                        if (double.TryParse(latObj?.ToString(), out var holonLat) &&
                            double.TryParse(lonObj?.ToString(), out var holonLon))
                        {
                            var distance = GeoHelper.CalculateDistance(centerLat, centerLon, holonLat, holonLon);
                            if (distance <= radiusInMeters)
                            {
                                nearbyHolons.Add(holon);
                            }
                        }
                    }
                }

                response.Result = nearbyHolons;
                response.IsError = false;
                response.Message = $"Found {nearbyHolons.Count} holons near location";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error getting holons near location: {ex.Message}");
            }
            return response;
        }



        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query all avatars from Aptos blockchain using smart contract call
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "get_account_resources",
                    @params = new object[]
                    {
                        "0x1", // Aptos account address
                        new { version = "latest" }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        var avatars = new List<IAvatar>();
                        var resources = result.EnumerateArray();

                        foreach (var resource in resources)
                        {
                            if (resource.TryGetProperty("type", out var type) &&
                                type.GetString().Contains("Avatar"))
                            {
                                var avatar = ParseAptosToAvatar(resource);
                                if (avatar != null)
                                {
                                    avatars.Add(avatar);
                                }
                            }
                        }

                        response.Result = avatars;
                        response.IsError = false;
                        response.Message = "Avatars loaded from Aptos successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No avatars found on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars from Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatars from Aptos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query avatar by provider key from Aptos blockchain
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "get_account_resource",
                    @params = new object[]
                    {
                        providerKey,
                        "0x1::Avatar::Avatar",
                        new { version = "latest" }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        var avatar = ParseAptosToAvatar(result);
                        if (avatar != null)
                        {
                            response.Result = avatar;
                            response.IsError = false;
                            response.Message = "Avatar loaded from Aptos by provider key successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from Aptos response");
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Aptos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar by username from Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "view",
                    @params = new
                    {
                        function = $"{_contractAddress}::oasis::get_avatar_by_username",
                        arguments = new[] { avatarUsername, version.ToString() }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        var avatar = ParseAptosToAvatar(result.GetRawText());
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded by username from Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar by username from Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();

            try
            {
                if (!_isActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Aptos provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Load avatar by email from Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "view",
                    @params = new
                    {
                        function = $"{_contractAddress}::oasis::get_avatar_by_email",
                        arguments = new[] { avatarEmail, version.ToString() }
                    }
                };

                var jsonContent = JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    if (rpcResponse.TryGetProperty("result", out var result))
                    {
                        var avatar = ParseAptosToAvatar(result.GetRawText());
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded by email from Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar not found on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar by email from Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }
    }
}
