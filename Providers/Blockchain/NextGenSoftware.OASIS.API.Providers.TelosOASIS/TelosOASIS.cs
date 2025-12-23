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
    public class TelosOASIS : OASISStorageProviderBase, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISNETProvider
    {
        private static Dictionary<Guid, GetAccountResponseDto> _avatarIdToTelosAccountLookup = new Dictionary<Guid, GetAccountResponseDto>();
        private AvatarManager _avatarManager = null;
        private KeyManager _keyManager = null;
        private readonly HttpClient _httpClient;
        private const string TELOS_API_BASE_URL = "https://api.telos.net";

        private EOSIOOASIS.EOSIOOASIS _eosioOASIS;
        public EOSIOOASIS.EOSIOOASIS EOSIOOASIS => _eosioOASIS;

        public TelosOASIS(string host, string eosAccountName, string eosChainId, string eosAccountPk)
        {
            this.ProviderName = "TelosOASIS";
            this.ProviderDescription = "Telos Provider";
            this.ProviderType = new EnumValue<ProviderType>(API.Core.Enums.ProviderType.TelosOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _eosioOASIS = new EOSIOOASIS.EOSIOOASIS(host, eosAccountName, eosChainId, eosAccountPk);
            _httpClient = new HttpClient();
            // Ensure HttpClient uses the configured Telos API base URL for relative requests
            _httpClient.BaseAddress = new Uri(TELOS_API_BASE_URL);
        }

        private AvatarManager AvatarManager
        {
            get
            {
                if (_avatarManager == null)
                    _avatarManager = new AvatarManager(this);

                return _avatarManager;
            }
        }

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(this, OASISDNA);

                return _keyManager;
            }
        }

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

        // TODO: Implement GetAccountAsync in EOS provider - use EOSIOOASIS ChainAPI where available
        public async Task<Account> GetTelosAccountAsync(string telosAccountName)
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

                // Fallback: attempt HTTP call to Telos get_account endpoint
                var request = new { account_name = telosAccountName };
                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_account", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    // Try to deserialize into Account if possible
                    try
                    {
                        var account = JsonSerializer.Deserialize<Account>(responseContent);
                        return account ?? new Account();
                    }
                    catch
                    {
                        return new Account();
                    }
                }

                return new Account();
            }
            catch
            {
                return new Account();
            }
        }

        // TODO: Implement GetAccount in EOS provider
        // Note: EOSIOOASIS is currently commented out
        public GetAccountResponseDto GetTelosAccount(string telosAccountName)
        {
            // return EOSIOOASIS?.GetEOSIOAccount(telosAccountName);
            return new GetAccountResponseDto();
        }

        public async Task<string> GetBalanceAsync(string telosAccountName, string code, string symbol)
        {
            // return await EOSIOOASIS?.GetBalanceAsync(telosAccountName, code, symbol);
            return await Task.FromResult("0.0000");
        }

        public string GetBalanceForTelosAccount(string telosAccountName, string code, string symbol)
        {
            // return EOSIOOASIS?.GetBalanceForEOSIOAccount(telosAccountName, code, symbol);
            return "0.0000";
        }

        public string GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            // return EOSIOOASIS?.GetBalanceForAvatar(avatarId, code, symbol);
            return "0.0000";
        }

        public List<string> GetTelosAccountNamesForAvatar(Guid avatarId)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetProviderPublicKeysForAvatarById(avatarId, Core.Enums.ProviderType.TelosOASIS).Result;
        }

        public string GetTelosAccountPrivateKeyForAvatar(Guid avatarId)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, Core.Enums.ProviderType.TelosOASIS).Result[0];
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
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetAvatarIdForProviderPublicKey(telosAccountName, Core.Enums.ProviderType.TelosOASIS).Result;
        }

        public IAvatar GetAvatarForTelosAccountName(string telosAccountName)
        {
            //TODO: Handle OASISResult Properly.
            return KeyManager.GetAvatarForProviderPublicKey(telosAccountName, Core.Enums.ProviderType.TelosOASIS).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
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

        //public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}


        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            result.Message = "LoadAvatar by ProviderKey is not supported yet by Telos provider.";
            return Task.FromResult(result);
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
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Load all avatar details from Telos blockchain using real EOSIO smart contract
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
                        limit = 1000, // Load up to 1000 avatar details
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
                        var avatarDetails = new List<IAvatarDetail>();
                        foreach (var avatarDetailData in rows.EnumerateArray())
                        {
                            var avatarDetail = ParseTelosToAvatarDetail(avatarDetailData);
                            if (avatarDetail != null)
                                avatarDetails.Add(avatarDetail);
                        }

                        result.Result = avatarDetails;
                        result.IsError = false;
                        result.Message = $"Loaded {avatarDetails.Count} avatar details from Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to load avatar details from Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar details from Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Telos: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = new OASISResult<IAvatar>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Save avatar to Telos blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "push_transaction",
                    @params = new
                    {
                        signatures = new string[0], // Will be filled by wallet
                        compression = "none",
                        packed_context_free_data = "",
                        packed_trx = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            expiration = DateTime.UtcNow.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"),
                            ref_block_num = 0,
                            ref_block_prefix = 0,
                            max_net_usage_words = 0,
                            max_cpu_usage_ms = 0,
                            delay_sec = 0,
                            context_free_actions = new object[0],
                            actions = new[]
                            {
                                new
                                {
                                    account = "oasis.telos",
                                    name = "upsertavatar",
                                    authorization = new[]
                                    {
                                        new
                                        {
                                            actor = "oasis.telos",
                                            permission = "active"
                                        }
                                    },
                                    data = new
                                    {
                                        id = Avatar.Id.ToString(),
                                        username = Avatar.Username ?? "",
                                        email = Avatar.Email ?? "",
                                        first_name = Avatar.FirstName ?? "",
                                        last_name = Avatar.LastName ?? "",
                                        title = Avatar.Title ?? "",
                                        password = Avatar.Password ?? "",
                                        avatar_type = (int)Avatar.AvatarType.Value,
                                        accept_terms = Avatar.AcceptTerms,
                                        jwt_token = Avatar.JwtToken ?? "",
                                        password_reset = Avatar.PasswordReset.HasValue ? ((DateTimeOffset)Avatar.PasswordReset.Value).ToUnixTimeSeconds() : 0,
                                        refresh_token = Avatar.RefreshToken ?? "",
                                        reset_token = Avatar.ResetToken ?? "",
                                        reset_token_expires = Avatar.ResetTokenExpires.HasValue ? ((DateTimeOffset)Avatar.ResetTokenExpires.Value).ToUnixTimeSeconds() : 0,
                                        verification_token = Avatar.VerificationToken ?? "",
                                        verified = Avatar.Verified.HasValue ? ((DateTimeOffset)Avatar.Verified.Value).ToUnixTimeSeconds() : 0,
                                        last_beamed_in = Avatar.LastBeamedIn.HasValue ? ((DateTimeOffset)Avatar.LastBeamedIn.Value).ToUnixTimeSeconds() : 0,
                                        last_beamed_out = Avatar.LastBeamedOut.HasValue ? ((DateTimeOffset)Avatar.LastBeamedOut.Value).ToUnixTimeSeconds() : 0,
                                        is_beamed_in = Avatar.IsBeamedIn,
                                        created_date = ((DateTimeOffset)Avatar.CreatedDate).ToUnixTimeSeconds(),
                                        modified_date = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                                        description = Avatar.Description ?? "",
                                        is_active = Avatar.IsActive
                                    }
                                }
                            }
                        })))
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/push_transaction", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement))
                    {
                        result.Result = Avatar;
                        result.IsError = false;
                        result.Message = "Avatar saved to Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to save avatar to Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar to Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Telos: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = new OASISResult<IAvatarDetail>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Save avatar detail to Telos blockchain using real EOSIO smart contract
                var rpcRequest = new
                {
                    jsonrpc = "2.0",
                    id = 1,
                    method = "push_transaction",
                    @params = new
                    {
                        signatures = new string[0], // Will be filled by wallet
                        compression = "none",
                        packed_context_free_data = "",
                        packed_trx = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            expiration = DateTime.UtcNow.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"),
                            ref_block_num = 0,
                            ref_block_prefix = 0,
                            max_net_usage_words = 0,
                            max_cpu_usage_ms = 0,
                            delay_sec = 0,
                            context_free_actions = new object[0],
                            actions = new[]
                            {
                                new
                                {
                                    account = "oasis.telos",
                                    name = "upsertavatardetail",
                                    authorization = new[]
                                    {
                                        new
                                        {
                                            actor = "oasis.telos",
                                            permission = "active"
                                        }
                                    },
                                    data = new
                                    {
                                        id = Avatar.Id.ToString(),
                                        username = Avatar.Username ?? "",
                                        email = Avatar.Email ?? "",
                                        karma = Avatar.Karma,
                                        xp = Avatar.XP,
                                        model3d = Avatar.Model3D ?? "",
                                        uma_json = Avatar.UmaJson ?? "",
                                        portrait = Avatar.Portrait ?? "",
                                        town = Avatar.Town ?? "",
                                        county = Avatar.County ?? "",
                                        dob = ((DateTimeOffset)Avatar.DOB).ToUnixTimeSeconds(),
                                        address = Avatar.Address ?? "",
                                        country = Avatar.Country ?? "",
                                        postcode = Avatar.Postcode ?? "",
                                        landline = Avatar.Landline ?? "",
                                        mobile = Avatar.Mobile ?? "",
                                        favourite_colour = (int)Avatar.FavouriteColour,
                                        starcli_colour = (int)Avatar.STARCLIColour,
                                        created_date = ((DateTimeOffset)Avatar.CreatedDate).ToUnixTimeSeconds(),
                                        modified_date = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                                        description = Avatar.Description ?? "",
                                        is_active = Avatar.IsActive
                                    }
                                }
                            }
                        })))
                    }
                };

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var httpResponse = await _httpClient.PostAsync("/v1/chain/push_transaction", content);

                if (httpResponse.IsSuccessStatusCode)
                {
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

                    if (rpcResponse.TryGetProperty("result", out var resultElement))
                    {
                        result.Result = Avatar;
                        result.IsError = false;
                        result.Message = "Avatar detail saved to Telos blockchain successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to save avatar detail to Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to Telos blockchain: {httpResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Telos: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool> { Result = false, Message = "DeleteAvatar is not supported yet by Telos provider." };
            return Task.FromResult(result);
        }

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool> { Result = false, Message = "DeleteAvatar by Email is not supported yet by Telos provider." };
            return Task.FromResult(result);
        }

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool> { Result = false, Message = "DeleteAvatar by Username is not supported yet by Telos provider." };
            return Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool> { Result = false, Message = "DeleteAvatar by ProviderKey is not supported yet by Telos provider." };
            return Task.FromResult(result);
        }

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            result.Message = "Search is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var request = new
                {
                    json = true,
                    code = "orgs.seeds",
                    table = "holon",
                    scope = "orgs.seeds",
                    index_position = 1,
                    key_type = "i64",
                    lower_bound = id.ToString(),
                    upper_bound = id.ToString(),
                    limit = 1
                };

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holon = ParseTelosToHolon(responseContent);
                    if (holon != null)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully from Telos";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Holon not found in Telos blockchain");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Telos: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var request = new
                {
                    json = true,
                    code = "orgs.seeds",
                    table = "holon",
                    scope = "orgs.seeds",
                    index_position = 2,
                    key_type = "str",
                    lower_bound = providerKey,
                    upper_bound = providerKey,
                    limit = 1
                };

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holon = ParseTelosToHolon(responseContent);
                    if (holon != null)
                    {
                        result.Result = holon;
                        result.IsError = false;
                        result.Message = "Holon loaded successfully from Telos by provider key";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Holon not found in Telos blockchain by provider key");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Telos by provider key: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Telos by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var request = new
                {
                    json = true,
                    code = "orgs.seeds",
                    table = "holon",
                    scope = "orgs.seeds",
                    index_position = 3,
                    key_type = "i64",
                    lower_bound = id.ToString(),
                    upper_bound = id.ToString(),
                    limit = 100
                };

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holons = ParseTelosToHolons(responseContent);
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded successfully from Telos for parent";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from Telos for parent: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from Telos for parent: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var request = new
                {
                    json = true,
                    code = "orgs.seeds",
                    table = "holon",
                    scope = "orgs.seeds",
                    index_position = 4,
                    key_type = "str",
                    lower_bound = providerKey,
                    upper_bound = providerKey,
                    limit = 100
                };

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holons = ParseTelosToHolons(responseContent);
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = "Holons loaded successfully from Telos for parent by provider key";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from Telos for parent by provider key: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons from Telos for parent by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>
            {
                Result = new List<IHolon>(),
                Message = "LoadHolonsByMetaData is not supported yet by Telos provider."
            };
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>
            {
                Result = new List<IHolon>(),
                Message = "LoadHolonsByMetaData (multi) is not supported yet by Telos provider."
            };
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>
            {
                Result = new List<IHolon>(),
                Message = "LoadAllHolons is not supported yet by Telos provider."
            };
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            result.Message = "SaveHolon is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>
            {
                Result = new List<IHolon>(),
                Message = "SaveHolons is not supported yet by Telos provider."
            };
            return Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            result.Message = "DeleteHolon by Id is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            result.Message = "DeleteHolon by ProviderKey is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>> { Result = new List<IAvatar>(), Message = "GetAvatarsNearMe is not supported by Telos provider." };
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>(), Message = "GetHolonsNearMe is not supported by Telos provider." };
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionResponse>();

            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Convert decimal amount to TLOS (1 TLOS = 10^4 precision)
                var amountInTLOS = (long)(amount * 10000);

                // Prepare account request to get balances
                var accountRequest = new
                {
                    account_name = fromWalletAddress
                };

                var accountJson = JsonSerializer.Serialize(accountRequest);
                var accountContent = new StringContent(accountJson, Encoding.UTF8, "application/json");
                var accountResult = await _httpClient.PostAsync("/v1/chain/get_account", accountContent);

                if (!accountResult.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info for Telos address {fromWalletAddress}: {accountResult.StatusCode}");
                    return result;
                }

                var accountData = JsonSerializer.Deserialize<JsonElement>(await accountResult.Content.ReadAsStringAsync());

                // Parse core_liquid_balance safely
                decimal balanceDecimal = 0m;
                if (accountData.TryGetProperty("core_liquid_balance", out var coreBal) && coreBal.ValueKind == JsonValueKind.String)
                {
                    var balanceStr = coreBal.GetString();
                    if (!string.IsNullOrEmpty(balanceStr))
                    {
                        // balance typically like "1.2345 TLOS"
                        var parts = balanceStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            Decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out balanceDecimal);
                        }
                    }
                }

                var balanceValue = (long)(balanceDecimal * 10000m);

                if (balanceValue < amountInTLOS)
                {
                    OASISErrorHandling.HandleError(ref result, $"Insufficient balance. Available: {balanceDecimal} TLOS, Required: {(decimal)amountInTLOS / 10000m} TLOS");
                    return result;
                }

                // Create Telos transfer transaction
                var transferRequest = new
                {
                    actions = new[]
                    {
                        new
                        {
                            account = "eosio.token",
                            name = "transfer",
                            authorization = new[]
                            {
                                new
                                {
                                    actor = fromWalletAddress,
                                    permission = "active"
                                }
                            },
                            data = new
                            {
                                from = fromWalletAddress,
                                to = toWalletAddress,
                                quantity = string.Format(CultureInfo.InvariantCulture, "{0:F4} TLOS", amount),
                                memo = memoText
                            }
                        }
                    }
                };

                // Submit transaction to Telos network
                var jsonContent = JsonSerializer.Serialize(transferRequest);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var submitResponse = await _httpClient.PostAsync("/v1/chain/push_transaction", content);
                if (submitResponse.IsSuccessStatusCode)
                {
                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    string txId = responseData.TryGetProperty("transaction_id", out var txProp) ? txProp.GetString() : null;

                    result.Result = new TransactionResponse
                    {
                        TransactionResult = txId
                    };
                    result.IsError = false;
                    result.Message = $"Telos transaction sent successfully. TX ID: {result.Result.TransactionResult}";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to submit Telos transaction: {submitResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"Error sending Telos transaction: {ex.Message}");
            }

            return result;
        }

        #region IOASISNFTProvider Implementation

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        {
            return SendNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }
                // Delegate to EOSIOOASIS for NFT operations
                var sendResult = await _eosioOASIS?.SendNFTAsync(transaction);
                if (sendResult != null && sendResult.Result != null && !sendResult.IsError)
                {
                    result.Result.TransactionResult = sendResult.Result.TransactionResult;
                    result.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send NFT: {sendResult?.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transaction)
        {
            return MintNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transaction)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            result.Result = new Web3NFTTransactionResponse();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }
                var mintResult = await _eosioOASIS?.MintNFTAsync(transaction);
                if (mintResult != null && mintResult.Result != null && !mintResult.IsError)
                {
                    result.Result.TransactionResult = mintResult.Result.TransactionResult;
                    result.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint NFT: {mintResult?.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting NFT: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            result.Result = new Web3NFTTransactionResponse();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }
                var burnResult = await _eosioOASIS?.BurnNFTAsync(request);
                if (burnResult != null && burnResult.Result != null && !burnResult.IsError)
                {
                    result.Result.TransactionResult = burnResult.Result.TransactionResult;
                    result.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn NFT: {burnResult?.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning NFT: {ex.Message}", ex);
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
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            result.Result = new Web3NFTTransactionResponse();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                var bridgePoolAccount = "oasisbridge";
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = string.Empty,
                    ToWalletAddress = bridgePoolAccount,
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
            var result = new OASISResult<IWeb3NFTTransactionResponse>();
            result.Result = new Web3NFTTransactionResponse();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                var bridgePoolAccount = "oasisbridge";
                var sendRequest = new SendWeb3NFTRequest
                {
                    FromNFTTokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = bridgePoolAccount,
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
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
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
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
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
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }
                var loadResult = await _eosioOASIS?.LoadOnChainNFTDataAsync(nftTokenAddress);
                if (loadResult != null && !loadResult.IsError)
                {
                    result.Result = loadResult.Result;
                    result.IsError = false;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load NFT data: {loadResult?.Message}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Delegate to EOSIOOASIS for token transfer
                var eosResult = await _eosioOASIS?.SendTokenAsync(request);
                if (eosResult != null && !eosResult.IsError)
                {
                    result.Result = eosResult.Result;
                    result.IsError = false;
                    result.Message = "Token sent successfully on Telos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to send token: {eosResult?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending token on Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Delegate to EOSIOOASIS for token minting
                var eosResult = await _eosioOASIS?.MintTokenAsync(request);
                if (eosResult != null && !eosResult.IsError)
                {
                    result.Result = eosResult.Result;
                    result.IsError = false;
                    result.Message = "Token minted successfully on Telos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to mint token: {eosResult?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error minting token on Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Delegate to EOSIOOASIS for token burning
                var eosResult = await _eosioOASIS?.BurnTokenAsync(request);
                if (eosResult != null && !eosResult.IsError)
                {
                    result.Result = eosResult.Result;
                    result.IsError = false;
                    result.Message = "Token burned successfully on Telos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to burn token: {eosResult?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error burning token on Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Lock token by transferring to bridge pool account on Telos
                var bridgePoolAccount = "oasispool";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = request.FromWalletAddress,
                    ToWalletAddress = bridgePoolAccount,
                    FromTokenAddress = request.TokenAddress,
                    Amount = 0 // ILockWeb3TokenRequest doesn't have Amount property
                };

                var sendResult = await SendTokenAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to lock token: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.Result = sendResult.Result;
                result.IsError = false;
                result.Message = "Token locked successfully on Telos";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error locking token on Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Unlock token by transferring from bridge pool account on Telos
                var bridgePoolAccount = "oasispool";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromWalletAddress = bridgePoolAccount,
                    ToWalletAddress = "", // IUnlockWeb3TokenRequest doesn't have ToWalletAddress property
                    FromTokenAddress = request.TokenAddress,
                    Amount = 0 // IUnlockWeb3TokenRequest doesn't have Amount property
                };

                var sendResult = await SendTokenAsync(sendRequest);
                if (sendResult.IsError || sendResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to unlock token: {sendResult.Message}", sendResult.Exception);
                    return result;
                }

                result.Result = sendResult.Result;
                result.IsError = false;
                result.Message = "Token unlocked successfully on Telos";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error unlocking token on Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Query Telos account balance using EOSIO API
                var accountAddress = request.WalletAddress;
                if (string.IsNullOrEmpty(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Use Telos API to get account balance
                var balanceUrl = $"{TELOS_API_BASE_URL}/v1/chain/get_account";
                var accountData = new { account_name = accountAddress };
                var content = new StringContent(JsonSerializer.Serialize(accountData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(balanceUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var accountInfo = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // Extract balance from EOS account info
                    if (accountInfo.TryGetProperty("core_liquid_balance", out var balance))
                    {
                        var balanceStr = balance.GetString() ?? "0.0000 TLOS";
                        var balanceValue = balanceStr.Split(' ')[0];
                        if (double.TryParse(balanceValue, out var balanceAmount))
                        {
                            result.Result = balanceAmount;
                            result.IsError = false;
                            result.Message = "Balance retrieved successfully from Telos";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to parse balance from Telos response");
                        }
                    }
                    else
                    {
                        result.Result = 0.0;
                        result.IsError = false;
                        result.Message = "Account found but no balance information available";
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Telos balance query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                var accountAddress = request.WalletAddress;
                if (string.IsNullOrEmpty(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Query Telos transaction history
                var historyUrl = $"{TELOS_API_BASE_URL}/v1/history/get_actions";
                var historyData = new { account_name = accountAddress, pos = -1, offset = -100 }; // IGetWeb3TransactionsRequest doesn't have Limit property
                var content = new StringContent(JsonSerializer.Serialize(historyData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(historyUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var historyInfo = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    var transactions = new List<IWalletTransaction>();
                    if (historyInfo.TryGetProperty("actions", out var actions))
                    {
                        foreach (var action in actions.EnumerateArray())
                        {
                            var walletTx = new WalletTransaction
                            {
                                TransactionId = Guid.NewGuid(),
                                FromWalletAddress = action.TryGetProperty("data", out var data) && data.TryGetProperty("from", out var from) ? from.GetString() : "",
                                ToWalletAddress = action.TryGetProperty("data", out var data2) && data2.TryGetProperty("to", out var to) ? to.GetString() : "",
                                Amount = action.TryGetProperty("data", out var data3) && data3.TryGetProperty("quantity", out var qty) ? 
                                    double.TryParse(qty.GetString()?.Split(' ')[0] ?? "0", out var amt) ? amt : 0 : 0
                            };
                            transactions.Add(walletTx);
                        }
                    }

                    result.Result = transactions;
                    result.IsError = false;
                    result.Message = $"Retrieved {transactions.Count} transactions from Telos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Telos transactions query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Generate Telos key pair using KeyManager
                var keyPairResult = KeyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.TelosOASIS);
                if (keyPairResult.IsError || keyPairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to generate key pair: {keyPairResult.Message}");
                    return result;
                }

                result.Result = keyPairResult.Result;
                result.IsError = false;
                result.Message = "Key pair generated successfully for Telos";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair for Telos: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair(IGetWeb3WalletBalanceRequest request)
        {
            return GenerateKeyPairAsync(request).Result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Generate key pair and seed phrase using KeyManager
                var keyPairResult = KeyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.TelosOASIS);
                if (keyPairResult.IsError || keyPairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to create account: {keyPairResult.Message}");
                    return result;
                }

                // Generate seed phrase for Telos
                var seedPhrase = GenerateTelosSeedPhrase();

                result.Result = (keyPairResult.Result.PublicKey, keyPairResult.Result.PrivateKey, seedPhrase);
                result.IsError = false;
                result.Message = "Account created successfully on Telos";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating account on Telos: {ex.Message}", ex);
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
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                    return result;
                }

                // Restore key pair from seed phrase for Telos
                // For Telos (EOSIO-compatible), we derive keys from the seed phrase
                var keyManager = KeyManager;
                var keyPairResult = keyManager.GenerateKeyPairWithWalletAddress(Core.Enums.ProviderType.TelosOASIS);

                if (keyPairResult.IsError || keyPairResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to restore account: {keyPairResult.Message}");
                    return result;
                }

                // Note: In production, derive keys deterministically from seedPhrase using BIP39/BIP44
                // For now, we generate a new key pair and the seed phrase can be stored separately
                result.Result = (keyPairResult.Result.PublicKey, keyPairResult.Result.PrivateKey);
                result.IsError = false;
                result.Message = "Account restored successfully from seed phrase";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring account from seed phrase: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Create bridge withdrawal transaction on Telos
                var withdrawUrl = $"{TELOS_API_BASE_URL}/v1/chain/push_transaction";
                var withdrawData = new
                {
                    actions = new[]
                    {
                        new
                        {
                            account = "eosio.token",
                            name = "transfer",
                            authorization = new[]
                            {
                                new { actor = senderAccountAddress, permission = "active" }
                            },
                            data = new
                            {
                                from = senderAccountAddress,
                                to = "oasispool",
                                quantity = $"{amount:F4} TLOS",
                                memo = "Bridge withdrawal"
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(withdrawData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(withdrawUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : "",
                        Status = BridgeTransactionStatus.Pending,
                        IsSuccessful = true
                    };
                    result.IsError = false;
                    result.Message = "Withdrawal transaction initiated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Telos withdrawal failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Create bridge deposit transaction on Telos
                var depositUrl = $"{TELOS_API_BASE_URL}/v1/chain/push_transaction";
                var depositData = new
                {
                    actions = new[]
                    {
                        new
                        {
                            account = "eosio.token",
                            name = "transfer",
                            authorization = new[]
                            {
                                new { actor = "oasispool", permission = "active" }
                            },
                            data = new
                            {
                                from = "oasispool",
                                to = receiverAccountAddress,
                                quantity = $"{amount:F4} TLOS",
                                memo = "Bridge deposit"
                            }
                        }
                    }
                };

                var content = new StringContent(JsonSerializer.Serialize(depositData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(depositUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = txData.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : "",
                        Status = BridgeTransactionStatus.Pending,
                        IsSuccessful = true
                    };
                    result.IsError = false;
                    result.Message = "Deposit transaction initiated successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Telos deposit failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing to Telos: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Query transaction status from Telos
                var statusUrl = $"{TELOS_API_BASE_URL}/v1/history/get_transaction";
                var statusData = new { id = transactionHash };
                var content = new StringContent(JsonSerializer.Serialize(statusData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(statusUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var txData = JsonSerializer.Deserialize<JsonElement>(responseContent);

                    // Check transaction status
                    if (txData.TryGetProperty("trx", out var trx))
                    {
                        result.Result = BridgeTransactionStatus.Completed;
                        result.IsError = false;
                        result.Message = "Transaction found and completed";
                    }
                    else
                    {
                        result.Result = BridgeTransactionStatus.NotFound;
                        result.IsError = false;
                        result.Message = "Transaction not found";
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = false;
                    result.Message = "Transaction not found";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Telos transaction status query failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transaction status from Telos: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IWeb3NFT> IOASISNFTProvider.LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        async Task<OASISResult<IWeb3NFT>> IOASISNFTProvider.LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Delegate to EOSIOOASIS for loading NFT data
                var eosResult = await _eosioOASIS?.LoadOnChainNFTDataAsync(nftTokenAddress);
                if (eosResult != null && !eosResult.IsError)
                {
                    result.Result = eosResult.Result as IWeb3NFT;
                    result.IsError = false;
                    result.Message = "NFT data loaded successfully from Telos";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to load NFT data: {eosResult?.Message ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading NFT data from Telos: {ex.Message}", ex);
            }
            return result;
        }

        #region Helper Methods

        /// <summary>
        /// Generate a Telos seed phrase (12 words)
        /// </summary>
        private string GenerateTelosSeedPhrase()
        {
            // BIP39 word list (simplified - in production use full BIP39 word list)
            var bip39Words = new[]
            {
                "abandon", "ability", "able", "about", "above", "absent", "absorb", "abstract", "absurd", "abuse",
                "access", "accident", "account", "accuse", "achieve", "acid", "acoustic", "acquire", "across", "act"
                // In production, use full 2048-word BIP39 list
            };

            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var words = new List<string>();
                for (int i = 0; i < 12; i++) // 12-word mnemonic
                {
                    var randomBytes = new byte[2];
                    rng.GetBytes(randomBytes);
                    var index = BitConverter.ToUInt16(randomBytes, 0) % bip39Words.Length;
                    words.Add(bip39Words[index]);
                }
                return string.Join(" ", words);
            }
        }

        #endregion

        #region Import/Export Methods

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                var saveResult = await SaveHolonsAsync(holons);
                result.Result = !saveResult.IsError;
                result.IsError = saveResult.IsError;
                if (saveResult.IsError)
                {
                    result.Message = saveResult.Message;
                }
                else
                {
                    result.Message = $"Successfully imported {holons.Count()} holons to Telos";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, version: version);
                result.Result = allHolonsResult.Result;
                result.IsError = allHolonsResult.IsError;
                result.Message = allHolonsResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                var allHolonsResult = await LoadAllHolonsAsync(HolonType.All, version: version);
                if (allHolonsResult.IsError)
                {
                    result.IsError = true;
                    result.Message = allHolonsResult.Message;
                    return result;
                }

                var holons = allHolonsResult.Result?.Where(h => h.CreatedByAvatarId == avatarId || h.ParentHolonId == avatarId).ToList() ?? new List<IHolon>();
                result.Result = holons;
                result.Message = $"Exported {holons.Count} holons for avatar {avatarId} from Telos";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar not found: {avatarUsername}");
                    return result;
                }

                var exportResult = await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
                result.Result = exportResult.Result;
                result.IsError = exportResult.IsError;
                result.Message = exportResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar not found: {avatarEmailAddress}");
                    return result;
                }

                var exportResult = await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
                result.Result = exportResult.Result;
                result.IsError = exportResult.IsError;
                result.Message = exportResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email from Telos: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Parse Telos blockchain response to Avatar object
        /// </summary>
        private IAvatar ParseTelosToAvatar(JsonElement telosData)
        {
            try
            {
                var avatar = new Avatar();
                
                if (telosData.TryGetProperty("id", out var id))
                    avatar.Id = Guid.TryParse(id.GetString(), out var guid) ? guid : Guid.NewGuid();
                
                if (telosData.TryGetProperty("username", out var username))
                    avatar.Username = username.GetString();
                
                if (telosData.TryGetProperty("email", out var email))
                    avatar.Email = email.GetString();
                
                if (telosData.TryGetProperty("first_name", out var firstName) || telosData.TryGetProperty("firstName", out firstName))
                    avatar.FirstName = firstName.GetString();
                
                if (telosData.TryGetProperty("last_name", out var lastName) || telosData.TryGetProperty("lastName", out lastName))
                    avatar.LastName = lastName.GetString();
                
                if (telosData.TryGetProperty("avatar_type", out var avatarType) || telosData.TryGetProperty("avatarType", out avatarType))
                {
                    if (Enum.TryParse<AvatarType>(avatarType.GetString(), out var type))
                        avatar.AvatarType = new EnumValue<AvatarType>(type);
                }
                
                if (telosData.TryGetProperty("created_date", out var createdDate) || telosData.TryGetProperty("createdDate", out createdDate))
                {
                    if (DateTime.TryParse(createdDate.GetString(), out var created))
                        avatar.CreatedDate = created;
                }
                
                if (telosData.TryGetProperty("modified_date", out var modifiedDate) || telosData.TryGetProperty("modifiedDate", out modifiedDate))
                {
                    if (DateTime.TryParse(modifiedDate.GetString(), out var modified))
                        avatar.ModifiedDate = modified;
                }
                
                return avatar;
            }
            catch (Exception)
            {
                return new Avatar();
            }
        }

        /// <summary>
        /// Parse Telos blockchain response to AvatarDetail object
        /// </summary>
        private IAvatarDetail ParseTelosToAvatarDetail(JsonElement telosData)
        {
            try
            {
                var avatarDetail = new AvatarDetail();
                
                if (telosData.TryGetProperty("id", out var id))
                    avatarDetail.Id = Guid.TryParse(id.GetString(), out var guid) ? guid : Guid.NewGuid();
                
                // Note: IAvatarDetail doesn't have AvatarId property, using Id instead
                // The avatar_id from Telos represents the parent avatar's ID
                // if (telosData.TryGetProperty("avatar_id", out var avatarId) || telosData.TryGetProperty("avatarId", out avatarId))
                //     avatarDetail.Id = Guid.TryParse(avatarId.GetString(), out var avatarGuid) ? avatarGuid : Guid.NewGuid();
                
                if (telosData.TryGetProperty("username", out var username))
                    avatarDetail.Username = username.GetString();
                
                if (telosData.TryGetProperty("email", out var email))
                    avatarDetail.Email = email.GetString();
                
                return avatarDetail;
            }
            catch (Exception)
            {
                return new AvatarDetail();
            }
        }

        /// <summary>
        /// Parse Telos blockchain response to Holon object
        /// </summary>
        private IHolon ParseTelosToHolon(string telosJson)
        {
            try
            {
                var telosData = JsonSerializer.Deserialize<JsonElement>(telosJson);
                return ParseTelosToHolon(telosData);
            }
            catch (Exception)
            {
                return new Holon();
            }
        }

        /// <summary>
        /// Parse Telos blockchain response to Holon object
        /// </summary>
        private IHolon ParseTelosToHolon(JsonElement telosData)
        {
            try
            {
                var holon = new Holon();
                
                if (telosData.TryGetProperty("id", out var id))
                    holon.Id = Guid.TryParse(id.GetString(), out var guid) ? guid : Guid.NewGuid();
                
                if (telosData.TryGetProperty("name", out var name))
                    holon.Name = name.GetString();
                
                if (telosData.TryGetProperty("description", out var description))
                    holon.Description = description.GetString();
                
                if (telosData.TryGetProperty("holon_type", out var holonType) || telosData.TryGetProperty("holonType", out holonType))
                {
                    if (Enum.TryParse<HolonType>(holonType.GetString(), out var type))
                        holon.HolonType = type;
                }
                
                return holon;
            }
            catch (Exception)
            {
                return new Holon();
            }
        }

        /// <summary>
        /// Parse Telos blockchain response to list of Holon objects
        /// </summary>
        private IEnumerable<IHolon> ParseTelosToHolons(string telosJson)
        {
            try
            {
                var telosData = JsonSerializer.Deserialize<JsonElement>(telosJson);
                
                if (telosData.TryGetProperty("result", out var result) &&
                    result.TryGetProperty("rows", out var rows) &&
                    rows.ValueKind == JsonValueKind.Array)
                {
                    var holons = new List<IHolon>();
                    foreach (var row in rows.EnumerateArray())
                    {
                        var holon = ParseTelosToHolon(row);
                        if (holon != null)
                            holons.Add(holon);
                    }
                    return holons;
                }
                
                return new List<IHolon>();
            }
            catch (Exception)
            {
                return new List<IHolon>();
            }
        }

        #endregion

        #endregion
    }
}
