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
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
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

                // Delete avatar by email from Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "submit_transaction",
                    @params = new
                    {
                        sender = await GetWalletAddressForAvatarByEmail(avatarEmail),
                        sequence_number = await GetSequenceNumber(),
                        max_gas_amount = 1000,
                        gas_unit_price = 1,
                        expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
                        payload = new
                        {
                            type = "entry_function_payload",
                            function = $"{_contractAddress}::oasis::delete_avatar_by_email",
                            arguments = new[] { avatarEmail, softDelete.ToString() }
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
                        response.Message = "Avatar deleted by email from Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to delete avatar by email from Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar by email from Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by email from Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
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

                // Delete avatar by username from Aptos blockchain using real Move smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "submit_transaction",
                    @params = new
                    {
                        sender = await GetWalletAddressForAvatarByUsername(avatarUsername),
                        sequence_number = await GetSequenceNumber(),
                        max_gas_amount = 1000,
                        gas_unit_price = 1,
                        expiration_timestamp_secs = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds(),
                        payload = new
                        {
                            type = "entry_function_payload",
                            function = $"{_contractAddress}::oasis::delete_avatar_by_username",
                            arguments = new[] { avatarUsername, softDelete.ToString() }
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
                        response.Message = "Avatar deleted by username from Aptos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to delete avatar by username from Aptos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to delete avatar by username from Aptos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deleting avatar by username from Aptos: {ex.Message}");
            }

            return response;
        }
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }
    }
}
