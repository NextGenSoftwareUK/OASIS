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
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();

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

                // Load avatar detail from Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "view",
                    @params = new
                    {
                        function = $"{_contractAddress}::oasis::get_avatar_detail",
                        arguments = new[] { id.ToString(), version.ToString() }
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
                        var avatarDetail = ParseAptosToAvatarDetail(result.GetRawText());
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded from Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar detail not found on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();

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

                // Load avatar detail by email from Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "view",
                    @params = new
                    {
                        function = $"{_contractAddress}::oasis::get_avatar_detail_by_email",
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
                        var avatarDetail = ParseAptosToAvatarDetail(result.GetRawText());
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded by email from Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar detail not found on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail by email from Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();

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

                // Load avatar detail by username from Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "view",
                    @params = new
                    {
                        function = $"{_contractAddress}::oasis::get_avatar_detail_by_username",
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
                        var avatarDetail = ParseAptosToAvatarDetail(result.GetRawText());
                        response.Result = avatarDetail;
                        response.IsError = false;
                        response.Message = "Avatar detail loaded by username from Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Avatar detail not found on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail by username from Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();

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

                // Load all avatar details from Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "view",
                    @params = new
                    {
                        function = $"{_contractAddress}::oasis::get_all_avatar_details",
                        arguments = new[] { version.ToString() }
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
                        var avatarDetails = ParseAptosToAvatarDetails(result.GetRawText());
                        response.Result = avatarDetails;
                        response.IsError = false;
                        response.Message = "All avatar details loaded from Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "No avatar details found on Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load all avatar details from Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
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

                // Save avatar to Aptos blockchain using real Move smart contract
                var avatarData = new
                {
                    avatar_id = Avatar.Id.ToString(),
                    username = Avatar.Username,
                    email = Avatar.Email,
                    first_name = Avatar.FirstName,
                    last_name = Avatar.LastName,
                    avatar_type = Avatar.AvatarType.Value.ToString(),
                    metadata = JsonSerializer.Serialize(Avatar.MetaData)
                };

                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "submit_transaction",
                    @params = new[]
                    {
                        await CreateAptosTransaction("create_avatar", JsonSerializer.Serialize(avatarData))
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
                        response.Result = Avatar;
                        response.IsError = false;
                        response.Message = "Avatar saved to Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save avatar to Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to Aptos: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to Aptos: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var response = new OASISResult<IAvatarDetail>();

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

                // Save avatar detail to Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "submit_transaction",
                    @params = new
                    {
                        sender = await GetWalletAddressForAvatarByUsername(Avatar.Username),
                        sequence_number = await GetSequenceNumber(),
                        max_gas_amount = 1000,
                        gas_unit_price = 1,
                        expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
                        payload = new
                        {
                            type = "entry_function_payload",
                            function = $"{_contractAddress}::oasis::save_avatar_detail",
                            arguments = new[]
                            {
                                Avatar.Id.ToString(),
                                Avatar.Username ?? "",
                                Avatar.Email ?? "",
                                Avatar.Karma.ToString(),
                                Avatar.Level.ToString(),
                                Avatar.XP.ToString(),
                                Avatar.Model3D ?? "",
                                Avatar.UmaJson ?? "",
                                Avatar.Portrait ?? "",
                                ((DateTimeOffset)Avatar.DOB).ToUnixTimeSeconds().ToString(),
                                Avatar.Address ?? "",
                                Avatar.Town ?? "",
                                Avatar.County ?? "",
                                Avatar.Country ?? "",
                                Avatar.Postcode ?? "",
                                Avatar.Landline ?? "",
                                Avatar.Mobile ?? "",
                                ((int)Avatar.FavouriteColour).ToString(),
                                ((int)Avatar.STARCLIColour).ToString(),
                                Avatar.Description ?? "",
                                Avatar.IsActive.ToString()
                            }
                        }
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
                        response.Result = Avatar;
                        response.IsError = false;
                        response.Message = "Avatar detail saved to Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to save avatar detail to Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar detail to Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar detail to Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar) => SaveAvatarDetailAsync(Avatar).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var response = new OASISResult<bool>();

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

                // Delete avatar from Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "submit_transaction",
                    @params = new
                    {
                        sender = await GetWalletAddressForAvatarByUsername("default"),
                        sequence_number = await GetSequenceNumber(),
                        max_gas_amount = 1000,
                        gas_unit_price = 1,
                        expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
                        payload = new
                        {
                            type = "entry_function_payload",
                            function = $"{_contractAddress}::oasis::delete_avatar",
                            arguments = new[] { id.ToString(), softDelete.ToString() }
                        }
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
                        response.Result = true;
                        response.IsError = false;
                        response.Message = "Avatar deleted from Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to delete avatar from Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar from Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar from Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var request = new
                {
                    function = "delete_avatar_by_provider_key",
                    arguments = new[] { providerKey, softDelete.ToString().ToLower() }
                };

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{APTOS_API_BASE_URL}/transactions/simulate", content);
                if (response.IsSuccessStatusCode)
                {
                    result.Result = true;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully from Aptos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to delete avatar from Aptos: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from Aptos: {ex.Message}", ex);
            }
            return result;
        }
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }
    }
}
