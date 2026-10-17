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

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            if (_eosioOASIS != null && !_eosioOASIS.IsProviderActivated)
                await _eosioOASIS.ActivateProviderAsync();

            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> ActivateProvider()
        {
            if (_eosioOASIS != null && !_eosioOASIS.IsProviderActivated)
                _eosioOASIS.ActivateProvider();

            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            if (_eosioOASIS != null && _eosioOASIS.IsProviderActivated)
                await _eosioOASIS.DeActivateProviderAsync();

            _avatarManager = null;
            _keyManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            if (_eosioOASIS != null && _eosioOASIS.IsProviderActivated)
                _eosioOASIS.DeActivateProvider();

            _avatarManager = null;
            _keyManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public async Task<GetAccountResponseDto> GetTelosAccountAsync(string telosAccountName)
        {
            try
            {
                // Try to use EOSIOOASIS helper if available
                // Note: EOSIOOASIS is currently commented out
                // if (EOSIOOASIS != null)
                // {
                //     // Some EOSIO provider libs expose async account retrieval - fall back to synchronous if not available
                //     try
                //     {
                //         var dto = EOSIOOASIS.GetEOSIOAccount(telosAccountName);
                //         if (dto != null)
                //         {
                //             // Build a simple Account wrapper
                //             var account = new Account();
                //             return await Task.FromResult(account);
                //         }
                //     }
                //     catch
                //     {
                //         // ignore and fall back to basic call
                //     }
                // }

                // Use EOSIO RPC to get account information
                var request = new { account_name = telosAccountName };
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_account", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var accountData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    // Parse account data from EOSIO RPC response
                    return new GetAccountResponseDto
                    {
                        AccountName = telosAccountName,
                        Created = accountData.TryGetProperty("created", out var created) ? created.GetString() : "",
                        CoreLiquidBalance = accountData.TryGetProperty("core_liquid_balance", out var balance) ? balance.GetString() : "0.0000 TLOS",
                        RamQuota = accountData.TryGetProperty("ram_quota", out var ramQuota) ? ramQuota.GetInt64().ToString() : "0",
                        NetWeight = accountData.TryGetProperty("net_weight", out var netWeight) ? netWeight.GetString() : "0 TLOS",
                        CpuWeight = accountData.TryGetProperty("cpu_weight", out var cpuWeight) ? cpuWeight.GetString() : "0 TLOS"
                    };
                }
                return new GetAccountResponseDto { AccountName = telosAccountName };
            }
            catch
            {
                return new GetAccountResponseDto();
            }
        }

        public GetAccountResponseDto GetTelosAccount(string telosAccountName)
        {
            return GetTelosAccountAsync(telosAccountName).Result;
        }

        public async Task<string> GetBalanceAsync(string telosAccountName, string code, string symbol)
        {
            // return await EOSIOOASIS?.GetBalanceAsync(telosAccountName, code, symbol);
            return await Task.FromResult("0.0000");
        }

        public string GetBalanceForTelosAccount(string telosAccountName, string code, string symbol)
        {
            return EOSIOOASIS?.GetBalanceForEOSIOAccount(telosAccountName, code, symbol) ?? "0.0000";
        }

        public string GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            return EOSIOOASIS?.GetBalanceForAvatar(avatarId, code, symbol) ?? "0.0000";
        }

        public List<string> GetTelosAccountNamesForAvatar(Guid avatarId)
        {
            var result = KeyManager.GetProviderPublicKeysForAvatarById(avatarId, Core.Enums.ProviderType.TelosOASIS);
            return result.IsError ? new List<string>() : result.Result ?? new List<string>();
        }

        public string GetTelosAccountPrivateKeyForAvatar(Guid avatarId)
        {
            var result = KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, Core.Enums.ProviderType.TelosOASIS);
            return result.IsError || result.Result == null || result.Result.Count == 0 ? "" : result.Result[0];
        }

        public GetAccountResponseDto GetTelosAccountForAvatar(Guid avatarId)
        {
            //TODO: Do we need to cache this?
            if (!_avatarIdToTelosAccountLookup.ContainsKey(avatarId))
                _avatarIdToTelosAccountLookup[avatarId] = GetTelosAccount(GetTelosAccountNamesForAvatar(avatarId)[0]);

            //TODO: The OASIS can store multiple Public Keys (Telos Accounts) per Avatar but currently we will only retreive the first one.
            // Need to add support to load multiple if needed?
            return _avatarIdToTelosAccountLookup[avatarId];
        }

        public Guid GetAvatarIdForTelosAccountName(string telosAccountName)
        {
            var result = KeyManager.GetAvatarIdForProviderPublicKey(telosAccountName, Core.Enums.ProviderType.TelosOASIS);
            return result.IsError ? Guid.Empty : result.Result;
        }

        public IAvatar GetAvatarForTelosAccountName(string telosAccountName)
        {
            var result = KeyManager.GetAvatarForProviderPublicKey(telosAccountName, Core.Enums.ProviderType.TelosOASIS);
            return result.IsError ? null : result.Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();

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

                // Load all avatars from Telos blockchain using real EOSIO smart contract
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
                        limit = 1000, // Load up to 1000 avatars
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
                        rows.ValueKind == JsonValueKind.Array)
                    {
                        var avatars = new List<IAvatar>();
                        foreach (var avatarData in rows.EnumerateArray())
                        {
                            var avatar = ParseTelosToAvatar(avatarData);
                            if (avatar != null)
                                avatars.Add(avatar);
                        }

                        result.Result = avatars;
                        result.IsError = false;
                        result.Message = $"Loaded {avatars.Count} avatars from Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to load avatars from Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatars from Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatars from Telos: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
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

                // Load avatar from Telos blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "get_table_rows",
                    @params = new
                    {
                        code = "oasis.telos", // Telos smart contract account
                        scope = "oasis.telos",
                        table = "avatars",
                        lower_bound = Id.ToString(),
                        upper_bound = Id.ToString(),
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
                        result.Message = "Avatar loaded from Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar not found on Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar from Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Telos: {ex.Message}");
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by email from Telos blockchain using real EOSIO smart contract
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
                        index_position = 2, // Secondary index on email
                        key_type = "name",
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
                        var avatarData = rows[0];
                        var avatar = ParseTelosToAvatar(avatarData);
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded by email from Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar not found on Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by email from Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Telos: {ex.Message}");
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
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Telos provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by username from Telos blockchain using real EOSIO smart contract
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
                        index_position = 3, // Secondary index on username
                        key_type = "name",
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
                        var avatarData = rows[0];
                        var avatar = ParseTelosToAvatar(avatarData);
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded by username from Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Avatar not found on Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by username from Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Telos: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            return LoadAvatarAsync(Id, version).Result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

    }
}
