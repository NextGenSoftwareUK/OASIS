using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Globalization;
using EOSNewYork.EOSCore.Response.API;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using System.Threading;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
{
    public partial class TelosOASIS
    {
        public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string verificationToken, int version = 0)
        {
            return LoadAvatarByVerificationTokenAsync(verificationToken, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string verificationToken, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                var rpcRequest = new
                {
                    jsonrpc = "2.0", id = 1, method = "get_table_rows",
                    @params = new
                    {
                        code = "oasis.telos", scope = "oasis.telos", table = "avatars",
                        index_position = 5, key_type = "name",
                        lower_bound = verificationToken, upper_bound = verificationToken,
                        limit = 1, reverse = false, show_payer = false
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
                        resultElement.TryGetProperty("rows", out var rows) &&
                        rows.ValueKind == JsonValueKind.Array &&
                        rows.GetArrayLength() > 0)
                    {
                        result.Result = ParseTelosToAvatar(rows[0]);
                        result.IsError = false;
                        result.Message = "Avatar loaded by verification token from Telos blockchain successfully";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Avatar not found on Telos blockchain");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by verification token from Telos blockchain: {httpResponse.StatusCode}");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by verification token from Telos: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByResetToken(string resetToken, int version = 0)
        {
            return LoadAvatarByResetTokenAsync(resetToken, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string resetToken, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                var rpcRequest = new
                {
                    jsonrpc = "2.0", id = 1, method = "get_table_rows",
                    @params = new
                    {
                        code = "oasis.telos", scope = "oasis.telos", table = "avatars",
                        index_position = 6, key_type = "name",
                        lower_bound = resetToken, upper_bound = resetToken,
                        limit = 1, reverse = false, show_payer = false
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
                        resultElement.TryGetProperty("rows", out var rows) &&
                        rows.ValueKind == JsonValueKind.Array &&
                        rows.GetArrayLength() > 0)
                    {
                        result.Result = ParseTelosToAvatar(rows[0]);
                        result.IsError = false;
                        result.Message = "Avatar loaded by reset token from Telos blockchain successfully";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Avatar not found on Telos blockchain");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by reset token from Telos blockchain: {httpResponse.StatusCode}");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by reset token from Telos: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string refreshToken, int version = 0)
        {
            return LoadAvatarByRefreshTokenAsync(refreshToken, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string refreshToken, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                var rpcRequest = new
                {
                    jsonrpc = "2.0", id = 1, method = "get_table_rows",
                    @params = new
                    {
                        code = "oasis.telos", scope = "oasis.telos", table = "avatars",
                        index_position = 7, key_type = "name",
                        lower_bound = refreshToken, upper_bound = refreshToken,
                        limit = 1, reverse = false, show_payer = false
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
                        resultElement.TryGetProperty("rows", out var rows) &&
                        rows.ValueKind == JsonValueKind.Array &&
                        rows.GetArrayLength() > 0)
                    {
                        result.Result = ParseTelosToAvatar(rows[0]);
                        result.IsError = false;
                        result.Message = "Avatar loaded by refresh token from Telos blockchain successfully";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, "Avatar not found on Telos blockchain");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by refresh token from Telos blockchain: {httpResponse.StatusCode}");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by refresh token from Telos: {ex.Message}");
            }
            return result;
        }

        //public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}


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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by provider key from Telos blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "get_table_rows",
                    @params = new
                    {
                        code = "oasis.telos",
                        scope = "oasis.telos",
                        table = "avatars",
                        index_position = 3, // Secondary index on provider key
                        key_type = "name",
                        lower_bound = providerKey,
                        upper_bound = providerKey,
                        limit = 1,
                        reverse = false,
                        show_payer = false
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
                        resultElement.TryGetProperty("rows", out var rows) &&
                        rows.ValueKind == JsonValueKind.Array &&
                        rows.GetArrayLength() > 0)
                    {
                        var avatarData = rows[0];
                        var avatar = ParseTelosToAvatar(avatarData);
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded by provider key from Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar not found on Telos blockchain by provider key");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by provider key from Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Telos: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar detail from Telos blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "get_table_rows",
                    @params = new
                    {
                        code = "oasis.telos", // Telos smart contract account
                        scope = "oasis.telos",
                        table = "avatardetails",
                        lower_bound = id.ToString(),
                        upper_bound = id.ToString(),
                        limit = 1,
                        reverse = false,
                        show_payer = false
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
                        resultElement.TryGetProperty("rows", out var rows) &&
                        rows.ValueKind == JsonValueKind.Array &&
                        rows.GetArrayLength() > 0)
                    {
                        var avatarDetailData = rows[0];
                        var avatarDetail = ParseTelosToAvatarDetail(avatarDetailData);

                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.Message = "Avatar detail loaded from Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar detail not found in Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail from Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Telos: {ex.Message}");
            }

            return result;
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
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar detail by username from Telos blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "get_table_rows",
                    @params = new
                    {
                        code = "oasis.telos", // Telos smart contract account
                        scope = "oasis.telos",
                        table = "avatardetails",
                        index_position = 2, // Username index
                        lower_bound = avatarUsername,
                        upper_bound = avatarUsername,
                        limit = 1,
                        reverse = false,
                        show_payer = false
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
                        resultElement.TryGetProperty("rows", out var rows) &&
                        rows.ValueKind == JsonValueKind.Array &&
                        rows.GetArrayLength() > 0)
                    {
                        var avatarDetailData = rows[0];
                        var avatarDetail = ParseTelosToAvatarDetail(avatarDetailData);

                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.Message = "Avatar detail loaded from Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar detail not found in Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail from Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Telos: {ex.Message}");
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar detail by email from Telos blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "get_table_rows",
                    @params = new
                    {
                        code = "oasis.telos", // Telos smart contract account
                        scope = "oasis.telos",
                        table = "avatardetails",
                        index_position = 3, // Email index
                        lower_bound = avatarEmail,
                        upper_bound = avatarEmail,
                        limit = 1,
                        reverse = false,
                        show_payer = false
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
                        resultElement.TryGetProperty("rows", out var rows) &&
                        rows.ValueKind == JsonValueKind.Array &&
                        rows.GetArrayLength() > 0)
                    {
                        var avatarDetailData = rows[0];
                        var avatarDetail = ParseTelosToAvatarDetail(avatarDetailData);

                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.Message = "Avatar detail loaded from Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar detail not found in Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail from Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Telos: {ex.Message}");
            }

            return result;
        }

    }
}
