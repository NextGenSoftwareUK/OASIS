//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Net.Http;
//using System.Text;
//using System.Text.Json;
//using System.Globalization;
//using EOSNewYork.EOSCore.Response.API;
//using NextGenSoftware.OASIS.API.Core;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Enums;
//using NextGenSoftware.OASIS.API.Core.Managers;
//using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.Entities.DTOs.GetAccount;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
//using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
//using NextGenSoftware.OASIS.API.Core.Objects.Search;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
//using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
//using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
//using NextGenSoftware.OASIS.Common;
//using NextGenSoftware.Utilities;
//using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses;
//using NextGenSoftware.OASIS.API.Core.Holons;

//namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
//{
//    public class TelosOASIS : OASISStorageProviderBase, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISNETProvider
//    {
//        private static Dictionary<Guid, GetAccountResponseDto> _avatarIdToTelosAccountLookup = new Dictionary<Guid, GetAccountResponseDto>();
//        private AvatarManager _avatarManager = null;
//        private KeyManager _keyManager = null;
//        private readonly HttpClient _httpClient;
//        private const string TELOS_API_BASE_URL = "https://api.telos.net";

//        public EOSIOOASIS.EOSIOOASIS EOSIOOASIS { get; set; }

//        public TelosOASIS(string host, string eosAccountName, string eosChainId, string eosAccountPk)
//        {
//            this.ProviderName = "TelosOASIS";
//            this.ProviderDescription = "Telos Provider";
//            this.ProviderType = new EnumValue<ProviderType>(API.Core.Enums.ProviderType.TelosOASIS);
//            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

//            EOSIOOASIS = new EOSIOOASIS.EOSIOOASIS(host, eosAccountName, eosChainId, eosAccountPk);
//            _httpClient = new HttpClient();
//            // Ensure HttpClient uses the configured Telos API base URL for relative requests
//            _httpClient.BaseAddress = new Uri(TELOS_API_BASE_URL);
//        }

//        private AvatarManager AvatarManager
//        {
//            get
//            {
//                if (_avatarManager == null)
//                    _avatarManager = new AvatarManager(this);

//                return _avatarManager;
//            }
//        }

//        private KeyManager KeyManager
//        {
//            get
//            {
//                if (_keyManager == null)
//                    _keyManager = new KeyManager(this, OASISDNA);

//                return _keyManager;
//            }
//        }

//        public override async Task<OASISResult<bool>> ActivateProviderAsync()
//        {
//            if (!EOSIOOASIS.IsProviderActivated)
//                await EOSIOOASIS.ActivateProviderAsync();

//            IsProviderActivated = true;
//            return new OASISResult<bool>(true);
//        }

//        public override OASISResult<bool> ActivateProvider()
//        {
//            if (!EOSIOOASIS.IsProviderActivated)
//                EOSIOOASIS.ActivateProvider();

//            IsProviderActivated = true;
//            return new OASISResult<bool>(true);
//        }

//        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
//        {
//            if (EOSIOOASIS.IsProviderActivated)
//                await EOSIOOASIS.DeActivateProviderAsync();

//            _avatarManager = null;
//            _key_manager = null;

//            IsProviderActivated = false;
//            return new OASISResult<bool>(true);
//        }

//        public override OASISResult<bool> DeActivateProvider()
//        {
//            if (EOSIOOASIS.IsProviderActivated)
//                EOSIOOASIS.DeActivateProvider();

//            _avatarManager = null;
//            _key_manager = null;

//            IsProviderActivated = false;
//            return new OASISResult<bool>(true);
//        }

//        // TODO: Implement GetAccountAsync in EOS provider - use EOSIOOASIS ChainAPI where available
//        public async Task<Account> GetTelosAccountAsync(string telosAccountName)
//        {
//            try
//            {
//                // Try to use EOSIOOASIS helper if available
//                if (EOSIOOASIS != null)
//                {
//                    // Some EOSIO provider libs expose async account retrieval - fall back to synchronous if not available
//                    try
//                    {
//                        var dto = EOSIOOASIS.GetEOSIOAccount(telosAccountName);
//                        if (dto != null)
//                        {
//                            // Build a simple Account wrapper
//                            var account = new Account();
//                            return await Task.FromResult(account);
//                        }
//                    }
//                    catch
//                    {
//                        // ignore and fall back to basic call
//                    }
//                }

//                // Fallback: attempt HTTP call to Telos get_account endpoint
//                var request = new { account_name = telosAccountName };
//                var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
//                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_account", content);

//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    // Try to deserialize into Account if possible
//                    try
//                    {
//                        var account = JsonSerializer.Deserialize<Account>(responseContent);
//                        return account ?? new Account();
//                    }
//                    catch
//                    {
//                        return new Account();
//                    }
//                }

//                return new Account();
//            }
//            catch
//            {
//                return new Account();
//            }
//        }

//        // TODO: Implement GetAccount in EOS provider
//        public GetAccountResponseDto GetTelosAccount(string telosAccountName)
//        {
//            return EOSIOOASIS.GetEOSIOAccount(telosAccountName);
//        }

//        public async Task<string> GetBalanceAsync(string telosAccountName, string code, string symbol)
//        {
//            return await EOSIOOASIS.GetBalanceAsync(telosAccountName, code, symbol);
//        }

//        public string GetBalanceForTelosAccount(string telosAccountName, string code, string symbol)
//        {
//            return EOSIOOASIS.GetBalanceForEOSIOAccount(telosAccountName, code, symbol);
//        }

//        public string GetBalanceForAvatar(Guid avatarId, string code, string symbol)
//        {
//            return EOSIOOASIS.GetBalanceForAvatar(avatarId, code, symbol);
//        }

//        public List<string> GetTelosAccountNamesForAvatar(Guid avatarId)
//        {
//            //TODO: Handle OASISResult Properly.
//            return KeyManager.GetProviderPublicKeysForAvatarById(avatarId, Core.Enums.ProviderType.TelosOASIS).Result;
//        }

//        public string GetTelosAccountPrivateKeyForAvatar(Guid avatarId)
//        {
//            //TODO: Handle OASISResult Properly.
//            return KeyManager.GetProviderPrivateKeysForAvatarById(avatarId, Core.Enums.ProviderType.TelosOASIS).Result[0];
//        }

//        public GetAccountResponseDto GetTelosAccountForAvatar(Guid avatarId)
//        {
//            //TODO: Do we need to cache this?
//            if (!_avatarIdToTelosAccountLookup.ContainsKey(avatarId))
//                _avatarIdToTelosAccountLookup[avatarId] = GetTelosAccount(GetTelosAccountNamesForAvatar(avatarId)[0]);

//            //TODO: The OASIS can store multiple Public Keys (Telos Accounts) per Avatar but currently we will only retreive the first one.
//            // Need to add support to load multiple if needed?
//            return _avatarIdToTelosAccountLookup[avatarId];
//        }

//        public Guid GetAvatarIdForTelosAccountName(string telosAccountName)
//        {
//            //TODO: Handle OASISResult Properly.
//            return KeyManager.GetAvatarIdForProviderPublicKey(telosAccountName, Core.Enums.ProviderType.TelosOASIS).Result;
//        }

//        public IAvatar GetAvatarForTelosAccountName(string telosAccountName)
//        {
//            //TODO: Handle OASISResult Properly.
//            return KeyManager.GetAvatarForProviderPublicKey(telosAccountName, Core.Enums.ProviderType.TelosOASIS).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IAvatar>>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
//                    return result;
//                }

//                // Load all avatars from Telos blockchain using real EOSIO smart contract
//                var rpcRequest = new
//                {
//                    jsonrpc = "2.0",
//                    id = 1,
//                    method = "get_table_rows",
//                    @params = new
//                    {
//                        code = "oasis.telos",
//                        scope = "oasis.telos",
//                        table = "avatars",
//                        limit = 1000, // Load up to 1000 avatars
//                        reverse = false,
//                        show_payer = false
//                    }
//                };

//                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
//                        resultElement.TryGetProperty("rows", out var rows) &&
//                        rows.ValueKind == JsonValueKind.Array)
//                    {
//                        var avatars = new List<IAvatar>();
//                        foreach (var avatarData in rows.EnumerateArray())
//                        {
//                            var avatar = ParseTelosToAvatar(avatarData);
//                            if (avatar != null)
//                                avatars.Add(avatar);
//                        }

//                        result.Result = avatars;
//                        result.IsError = false;
//                        result.Message = $"Loaded {avatars.Count} avatars from Telos blockchain successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to load avatars from Telos blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatars from Telos blockchain: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error loading avatars from Telos: {ex.Message}");
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
//        {
//            return LoadAllAvatarsAsync(version).Result;
//        }

//        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
//        {
//            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
//        }

//        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
//        {
//            var result = new OASISResult<IAvatar>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
//                    return result;
//                }

//                // Load avatar from Telos blockchain using real EOSIO smart contract
//                var rpcRequest = new
//                {
//                    jsonrpc = "2.0",
//                    id = 1,
//                    method = "get_table_rows",
//                    @params = new
//                    {
//                        code = "oasis.telos", // Telos smart contract account
//                        scope = "oasis.telos",
//                        table = "avatars",
//                        lower_bound = Id.ToString(),
//                        upper_bound = Id.ToString(),
//                        limit = 1,
//                        reverse = false,
//                        show_payer = false
//                    }
//                };

//                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
//                        resultElement.TryGetProperty("rows", out var rows) &&
//                        rows.ValueKind == JsonValueKind.Array &&
//                        rows.GetArrayLength() > 0)
//                    {
//                        var avatarData = rows[0];
//                        var avatar = ParseTelosToAvatar(avatarData);
//                        result.Result = avatar;
//                        result.IsError = false;
//                        result.Message = "Avatar loaded from Telos blockchain successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Avatar not found on Telos blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar from Telos blockchain: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from Telos: {ex.Message}");
//            }

//            return result;
//        }

//        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
//        {
//            var result = new OASISResult<IAvatar>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
//                    return result;
//                }

//                // Load avatar by email from Telos blockchain using real EOSIO smart contract
//                var rpcRequest = new
//                {
//                    jsonrpc = "2.0",
//                    id = 1,
//                    method = "get_table_rows",
//                    @params = new
//                    {
//                        code = "oasis.telos",
//                        scope = "oasis.telos",
//                        table = "avatars",
//                        index_position = 2, // Secondary index on email
//                        key_type = "name",
//                        lower_bound = avatarEmail,
//                        upper_bound = avatarEmail,
//                        limit = 1,
//                        reverse = false,
//                        show_payer = false
//                    }
//                };

//                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
//                        resultElement.TryGetProperty("rows", out var rows) &&
//                        rows.ValueKind == JsonValueKind.Array &&
//                        rows.GetArrayLength() > 0)
//                    {
//                        var avatarData = rows[0];
//                        var avatar = ParseTelosToAvatar(avatarData);
//                        result.Result = avatar;
//                        result.IsError = false;
//                        result.Message = "Avatar loaded by email from Telos blockchain successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Avatar not found on Telos blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by email from Telos blockchain: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Telos: {ex.Message}");
//            }

//            return result;
//        }

//        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
//        {
//            var result = new OASISResult<IAvatar>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
//                    return result;
//                }

//                // Load avatar by username from Telos blockchain using real EOSIO smart contract
//                var rpcRequest = new
//                {
//                    jsonrpc = "2.0",
//                    id = 1,
//                    method = "get_table_rows",
//                    @params = new
//                    {
//                        code = "oasis.telos",
//                        scope = "oasis.telos",
//                        table = "avatars",
//                        index_position = 3, // Secondary index on username
//                        key_type = "name",
//                        lower_bound = avatarUsername,
//                        upper_bound = avatarUsername,
//                        limit = 1,
//                        reverse = false,
//                        show_payer = false
//                    }
//                };

//                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
//                        resultElement.TryGetProperty("rows", out var rows) &&
//                        rows.ValueKind == JsonValueKind.Array &&
//                        rows.GetArrayLength() > 0)
//                    {
//                        var avatarData = rows[0];
//                        var avatar = ParseTelosToAvatar(avatarData);
//                        result.Result = avatar;
//                        result.IsError = false;
//                        result.Message = "Avatar loaded by username from Telos blockchain successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Avatar not found on Telos blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar by username from Telos blockchain: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Telos: {ex.Message}");
//            }

//            return result;
//        }

//        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
//        {
//            return LoadAvatarAsync(Id, version).Result;
//        }

//        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
//        {
//            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
//        }

//        //public override Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}


//        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
//        {
//            var result = new OASISResult<IAvatar>();
//            result.Message = "LoadAvatar by ProviderKey is not supported yet by Telos provider.";
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
//        {
//            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
//        {
//            return LoadAvatarDetailAsync(id, version).Result;
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
//        {
//            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
//        }

//        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
//        {
//            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
//        {
//            var result = new OASISResult<IAvatarDetail>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
//                    return result;
//                }

//                // Load avatar detail from Telos blockchain using real EOSIO smart contract
//                var rpcRequest = new
//                {
//                    jsonrpc = "2.0",
//                    id = 1,
//                    method = "get_table_rows",
//                    @params = new
//                    {
//                        code = "oasis.telos", // Telos smart contract account
//                        scope = "oasis.telos",
//                        table = "avatardetails",
//                        lower_bound = id.ToString(),
//                        upper_bound = id.ToString(),
//                        limit = 1,
//                        reverse = false,
//                        show_payer = false
//                    }
//                };

//                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
//                        resultElement.TryGetProperty("rows", out var rows) &&
//                        rows.ValueKind == JsonValueKind.Array &&
//                        rows.GetArrayLength() > 0)
//                    {
//                        var avatarDetailData = rows[0];
//                        var avatarDetail = ParseTelosToAvatarDetail(avatarDetailData);

//                        result.Result = avatarDetail;
//                        result.IsError = false;
//                        result.Message = "Avatar detail loaded from Telos blockchain successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Avatar detail not found in Telos blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail from Telos blockchain: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Telos: {ex.Message}");
//            }

//            return result;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
//        {
//            var result = new OASISResult<IAvatarDetail>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
//                    return result;
//                }

//                // Load avatar detail by username from Telos blockchain using real EOSIO smart contract
//                var rpcRequest = new
//                {
//                    jsonrpc = "2.0",
//                    id = 1,
//                    method = "get_table_rows",
//                    @params = new
//                    {
//                        code = "oasis.telos", // Telos smart contract account
//                        scope = "oasis.telos",
//                        table = "avatardetails",
//                        index_position = 2, // Username index
//                        lower_bound = avatarUsername,
//                        upper_bound = avatarUsername,
//                        limit = 1,
//                        reverse = false,
//                        show_payer = false
//                    }
//                };

//                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
//                        resultElement.TryGetProperty("rows", out var rows) &&
//                        rows.ValueKind == JsonValueKind.Array &&
//                        rows.GetArrayLength() > 0)
//                    {
//                        var avatarDetailData = rows[0];
//                        var avatarDetail = ParseTelosToAvatarDetail(avatarDetailData);

//                        result.Result = avatarDetail;
//                        result.IsError = false;
//                        result.Message = "Avatar detail loaded from Telos blockchain successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Avatar detail not found in Telos blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail from Telos blockchain: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Telos: {ex.Message}");
//            }

//            return result;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
//        {
//            var result = new OASISResult<IAvatarDetail>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
//                    return result;
//                }

//                // Load avatar detail by email from Telos blockchain using real EOSIO smart contract
//                var rpcRequest = new
//                {
//                    jsonrpc = "2.0",
//                    id = 1,
//                    method = "get_table_rows",
//                    @params = new
//                    {
//                        code = "oasis.telos", // Telos smart contract account
//                        scope = "oasis.telos",
//                        table = "avatardetails",
//                        index_position = 3, // Email index
//                        lower_bound = avatarEmail,
//                        upper_bound = avatarEmail,
//                        limit = 1,
//                        reverse = false,
//                        show_payer = false
//                    }
//                };

//                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
//                        resultElement.TryGetProperty("rows", out var rows) &&
//                        rows.ValueKind == JsonValueKind.Array &&
//                        rows.GetArrayLength() > 0)
//                    {
//                        var avatarDetailData = rows[0];
//                        var avatarDetail = ParseTelosToAvatarDetail(avatarDetailData);

//                        result.Result = avatarDetail;
//                        result.IsError = false;
//                        result.Message = "Avatar detail loaded from Telos blockchain successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Avatar detail not found in Telos blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar detail from Telos blockchain: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from Telos: {ex.Message}");
//            }

//            return result;
//        }

//        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
//        {
//            return LoadAllAvatarDetailsAsync(version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IAvatarDetail>>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
//                    return result;
//                }

//                // Load all avatar details from Telos blockchain using real EOSIO smart contract
//                var rpcRequest = new
//                {
//                    jsonrpc = "2.0",
//                    id = 1,
//                    method = "get_table_rows",
//                    @params = new
//                    {
//                        code = "oasis.telos", // Telos smart contract account
//                        scope = "oasis.telos",
//                        table = "avatardetails",
//                        limit = 1000, // Load up to 1000 avatar details
//                        reverse = false,
//                        show_payer = false
//                    }
//                };

//                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var httpResponse = await _httpClient.PostAsync("/v1/chain/get_table_rows", content);

//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    if (rpcResponse.TryGetProperty("result", out var resultElement) &&
//                        resultElement.TryGetProperty("rows", out var rows) &&
//                        rows.ValueKind == JsonValueKind.Array)
//                    {
//                        var avatarDetails = new List<IAvatarDetail>();
//                        foreach (var avatarDetailData in rows.EnumerateArray())
//                        {
//                            var avatarDetail = ParseTelosToAvatarDetail(avatarDetailData);
//                            if (avatarDetail != null)
//                                avatarDetails.Add(avatarDetail);
//                        }

//                        result.Result = avatarDetails;
//                        result.IsError = false;
//                        result.Message = $"Loaded {avatarDetails.Count} avatar details from Telos blockchain successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to load avatar details from Telos blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load avatar details from Telos blockchain: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error loading avatar details from Telos: {ex.Message}");
//            }

//            return result;
//        }

//        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
//        {
//            return SaveAvatarAsync(Avatar).Result;
//        }

//        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
//        {
//            var result = new OASISResult<IAvatar>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
//                    return result;
//                }

//                // Save avatar to Telos blockchain using real EOSIO smart contract
//                var rpcRequest = new
//                {
//                    jsonrpc = "2.0",
//                    id = 1,
//                    method = "push_transaction",
//                    @params = new
//                    {
//                        signatures = new string[0], // Will be filled by wallet
//                        compression = "none",
//                        packed_context_free_data = "",
//                        packed_trx = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new
//                        {
//                            expiration = DateTime.UtcNow.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"),
//                            ref_block_num = 0,
//                            ref_block_prefix = 0,
//                            max_net_usage_words = 0,
//                            max_cpu_usage_ms = 0,
//                            delay_sec = 0,
//                            context_free_actions = new object[0],
//                            actions = new[]
//                            {
//                                new
//                                {
//                                    account = "oasis.telos",
//                                    name = "upsertavatar",
//                                    authorization = new[]
//                                    {
//                                        new
//                                        {
//                                            actor = "oasis.telos",
//                                            permission = "active"
//                                        }
//                                    },
//                                    data = new
//                                    {
//                                        id = Avatar.Id.ToString(),
//                                        username = Avatar.Username ?? "",
//                                        email = Avatar.Email ?? "",
//                                        first_name = Avatar.FirstName ?? "",
//                                        last_name = Avatar.LastName ?? "",
//                                        title = Avatar.Title ?? "",
//                                        password = Avatar.Password ?? "",
//                                        avatar_type = (int)Avatar.AvatarType.Value,
//                                        accept_terms = Avatar.AcceptTerms,
//                                        jwt_token = Avatar.JwtToken ?? "",
//                                        password_reset = Avatar.PasswordReset.HasValue ? ((DateTimeOffset)Avatar.PasswordReset.Value).ToUnixTimeSeconds() : 0,
//                                        refresh_token = Avatar.RefreshToken ?? "",
//                                        reset_token = Avatar.ResetToken ?? "",
//                                        reset_token_expires = Avatar.ResetTokenExpires.HasValue ? ((DateTimeOffset)Avatar.ResetTokenExpires.Value).ToUnixTimeSeconds() : 0,
//                                        verification_token = Avatar.VerificationToken ?? "",
//                                        verified = Avatar.Verified.HasValue ? ((DateTimeOffset)Avatar.Verified.Value).ToUnixTimeSeconds() : 0,
//                                        last_beamed_in = Avatar.LastBeamedIn.HasValue ? ((DateTimeOffset)Avatar.LastBeamedIn.Value).ToUnixTimeSeconds() : 0,
//                                        last_beamed_out = Avatar.LastBeamedOut.HasValue ? ((DateTimeOffset)Avatar.LastBeamedOut.Value).ToUnixTimeSeconds() : 0,
//                                        is_beamed_in = Avatar.IsBeamedIn,
//                                        created_date = ((DateTimeOffset)Avatar.CreatedDate).ToUnixTimeSeconds(),
//                                        modified_date = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
//                                        description = Avatar.Description ?? "",
//                                        is_active = Avatar.IsActive
//                                    }
//                                }
//                            }
//                        })))
//                    }
//                };

//                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var httpResponse = await _httpClient.PostAsync("/v1/chain/push_transaction", content);

//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    if (rpcResponse.TryGetProperty("result", out var resultElement))
//                    {
//                        result.Result = Avatar;
//                        result.IsError = false;
//                        result.Message = "Avatar saved to Telos blockchain successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to save avatar to Telos blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar to Telos blockchain: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to Telos: {ex.Message}");
//            }

//            return result;
//        }

//        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
//        {
//            return SaveAvatarDetailAsync(Avatar).Result;
//        }

//        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
//        {
//            var result = new OASISResult<IAvatarDetail>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
//                    return result;
//                }

//                // Save avatar detail to Telos blockchain using real EOSIO smart contract
//                var rpcRequest = new
//                {
//                    jsonrpc = "2.0",
//                    id = 1,
//                    method = "push_transaction",
//                    @params = new
//                    {
//                        signatures = new string[0], // Will be filled by wallet
//                        compression = "none",
//                        packed_context_free_data = "",
//                        packed_trx = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(new
//                        {
//                            expiration = DateTime.UtcNow.AddMinutes(30).ToString("yyyy-MM-ddTHH:mm:ss"),
//                            ref_block_num = 0,
//                            ref_block_prefix = 0,
//                            max_net_usage_words = 0,
//                            max_cpu_usage_ms = 0,
//                            delay_sec = 0,
//                            context_free_actions = new object[0],
//                            actions = new[]
//                            {
//                                new
//                                {
//                                    account = "oasis.telos",
//                                    name = "upsertavatardetail",
//                                    authorization = new[]
//                                    {
//                                        new
//                                        {
//                                            actor = "oasis.telos",
//                                            permission = "active"
//                                        }
//                                    },
//                                    data = new
//                                    {
//                                        id = Avatar.Id.ToString(),
//                                        username = Avatar.Username ?? "",
//                                        email = Avatar.Email ?? "",
//                                        karma = Avatar.Karma,
//                                        xp = Avatar.XP,
//                                        model3d = Avatar.Model3D ?? "",
//                                        uma_json = Avatar.UmaJson ?? "",
//                                        portrait = Avatar.Portrait ?? "",
//                                        town = Avatar.Town ?? "",
//                                        county = Avatar.County ?? "",
//                                        dob = ((DateTimeOffset)Avatar.DOB).ToUnixTimeSeconds(),
//                                        address = Avatar.Address ?? "",
//                                        country = Avatar.Country ?? "",
//                                        postcode = Avatar.Postcode ?? "",
//                                        landline = Avatar.Landline ?? "",
//                                        mobile = Avatar.Mobile ?? "",
//                                        favourite_colour = (int)Avatar.FavouriteColour,
//                                        starcli_colour = (int)Avatar.STARCLIColour,
//                                        created_date = ((DateTimeOffset)Avatar.CreatedDate).ToUnixTimeSeconds(),
//                                        modified_date = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
//                                        description = Avatar.Description ?? "",
//                                        is_active = Avatar.IsActive
//                                    }
//                                }
//                            }
//                        })))
//                    }
//                };

//                var jsonContent = System.Text.Json.JsonSerializer.Serialize(rpcRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                var httpResponse = await _httpClient.PostAsync("/v1/chain/push_transaction", content);

//                if (httpResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await httpResponse.Content.ReadAsStringAsync();
//                    var rpcResponse = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    if (rpcResponse.TryGetProperty("result", out var resultElement))
//                    {
//                        result.Result = Avatar;
//                        result.IsError = false;
//                        result.Message = "Avatar detail saved to Telos blockchain successfully";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Failed to save avatar detail to Telos blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to save avatar detail to Telos blockchain: {httpResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to Telos: {ex.Message}");
//            }

//            return result;
//        }

//        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
//        {
//            return DeleteAvatarAsync(id, softDelete).Result;
//        }

//        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
//        {
//            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
//        }

//        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
//        {
//            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
//        }

//        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
//        {
//            var result = new OASISResult<bool> { Result = false, Message = "DeleteAvatar is not supported yet by Telos provider." };
//            return Task.FromResult(result);
//        }

//        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
//        {
//            var result = new OASISResult<bool> { Result = false, Message = "DeleteAvatar by Email is not supported yet by Telos provider." };
//            return Task.FromResult(result);
//        }

//        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
//        {
//            var result = new OASISResult<bool> { Result = false, Message = "DeleteAvatar by Username is not supported yet by Telos provider." };
//            return Task.FromResult(result);
//        }

//        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
//        {
//            return DeleteAvatarAsync(providerKey, softDelete).Result;
//        }

//        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
//        {
//            var result = new OASISResult<bool> { Result = false, Message = "DeleteAvatar by ProviderKey is not supported yet by Telos provider." };
//            return Task.FromResult(result);
//        }

//        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
//        {
//            var result = new OASISResult<ISearchResults>();
//            result.Message = "Search is not supported yet by Telos provider.";
//            return Task.FromResult(result);
//        }

//        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IHolon>();
//            try
//            {
//                var request = new
//                {
//                    json = true,
//                    code = "orgs.seeds",
//                    table = "holon",
//                    scope = "orgs.seeds",
//                    index_position = 1,
//                    key_type = "i64",
//                    lower_bound = id.ToString(),
//                    upper_bound = id.ToString(),
//                    limit = 1
//                };

//                var json = System.Text.Json.JsonSerializer.Serialize(request);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var holon = ParseTelosToHolon(responseContent);
//                    if (holon != null)
//                    {
//                        result.Result = holon;
//                        result.IsError = false;
//                        result.Message = "Holon loaded successfully from Telos";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Holon not found in Telos blockchain");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Telos: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Telos: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IHolon>();
//            try
//            {
//                var request = new
//                {
//                    json = true,
//                    code = "orgs.seeds",
//                    table = "holon",
//                    scope = "orgs.seeds",
//                    index_position = 2,
//                    key_type = "str",
//                    lower_bound = providerKey,
//                    upper_bound = providerKey,
//                    limit = 1
//                };

//                var json = System.Text.Json.JsonSerializer.Serialize(request);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var holon = ParseTelosToHolon(responseContent);
//                    if (holon != null)
//                    {
//                        result.Result = holon;
//                        result.IsError = false;
//                        result.Message = "Holon loaded successfully from Telos by provider key";
//                    }
//                    else
//                    {
//                        OASISErrorHandling.HandleError(ref result, "Holon not found in Telos blockchain by provider key");
//                    }
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holon from Telos by provider key: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holon from Telos by provider key: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                var request = new
//                {
//                    json = true,
//                    code = "orgs.seeds",
//                    table = "holon",
//                    scope = "orgs.seeds",
//                    index_position = 3,
//                    key_type = "i64",
//                    lower_bound = id.ToString(),
//                    upper_bound = id.ToString(),
//                    limit = 100
//                };

//                var json = System.Text.Json.JsonSerializer.Serialize(request);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var holons = ParseTelosToHolons(responseContent);
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "Holons loaded successfully from Telos for parent";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from Telos for parent: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holons from Telos for parent: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>();
//            try
//            {
//                var request = new
//                {
//                    json = true,
//                    code = "orgs.seeds",
//                    table = "holon",
//                    scope = "orgs.seeds",
//                    index_position = 4,
//                    key_type = "str",
//                    lower_bound = providerKey,
//                    upper_bound = providerKey,
//                    limit = 100
//                };

//                var json = System.Text.Json.JsonSerializer.Serialize(request);
//                var content = new StringContent(json, Encoding.UTF8, "application/json");

//                var response = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);
//                if (response.IsSuccessStatusCode)
//                {
//                    var responseContent = await response.Content.ReadAsStringAsync();
//                    var holons = ParseTelosToHolons(responseContent);
//                    result.Result = holons;
//                    result.IsError = false;
//                    result.Message = "Holons loaded successfully from Telos for parent by provider key";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to load holons from Telos for parent by provider key: {response.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                OASISErrorHandling.HandleError(ref result, $"Error loading holons from Telos for parent by provider key: {ex.Message}", ex);
//            }
//            return result;
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
//        {
//            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>
//            {
//                Result = new List<IHolon>(),
//                Message = "LoadHolonsByMetaData is not supported yet by Telos provider."
//            };
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>
//            {
//                Result = new List<IHolon>(),
//                Message = "LoadHolonsByMetaData (multi) is not supported yet by Telos provider."
//            };
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>
//            {
//                Result = new List<IHolon>(),
//                Message = "LoadAllHolons is not supported yet by Telos provider."
//            };
//            return await Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
//        {
//            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
//        }

//        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
//        }

//        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            var result = new OASISResult<IHolon>();
//            result.Message = "SaveHolon is not supported yet by Telos provider.";
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
//        }

//        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>>
//            {
//                Result = new List<IHolon>(),
//                Message = "SaveHolons is not supported yet by Telos provider."
//            };
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IHolon> DeleteHolon(Guid id)
//        {
//            return DeleteHolonAsync(id).Result;
//        }

//        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
//        {
//            var result = new OASISResult<IHolon>();
//            result.Message = "DeleteHolon by Id is not supported yet by Telos provider.";
//            return Task.FromResult(result);
//        }

//        public override OASISResult<IHolon> DeleteHolon(string providerKey)
//        {
//            return DeleteHolonAsync(providerKey).Result;
//        }

//        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
//        {
//            var result = new OASISResult<IHolon>();
//            result.Message = "DeleteHolon by ProviderKey is not supported yet by Telos provider.";
//            return Task.FromResult(result);
//        }

//        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
//        {
//            var result = new OASISResult<IEnumerable<IAvatar>> { Result = new List<IAvatar>(), Message = "GetAvatarsNearMe is not supported by Telos provider." };
//            return result;
//        }

//        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
//        {
//            var result = new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>(), Message = "GetHolonsNearMe is not supported by Telos provider." };
//            return result;
//        }

//        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
//        {
//            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
//        }

//        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
//        {
//            var result = new OASISResult<ITransactionRespone>();

//            try
//            {
//                if (!IsProviderActivated)
//                {
//                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
//                    return result;
//                }

//                // Convert decimal amount to TLOS (1 TLOS = 10^4 precision)
//                var amountInTLOS = (long)(amount * 10000);

//                // Prepare account request to get balances
//                var accountRequest = new
//                {
//                    account_name = fromWalletAddress
//                };

//                var accountJson = JsonSerializer.Serialize(accountRequest);
//                var accountContent = new StringContent(accountJson, Encoding.UTF8, "application/json");
//                var accountResult = await _httpClient.PostAsync("/v1/chain/get_account", accountContent);

//                if (!accountResult.IsSuccessStatusCode)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to get account info for Telos address {fromWalletAddress}: {accountResult.StatusCode}");
//                    return result;
//                }

//                var accountData = JsonSerializer.Deserialize<JsonElement>(await accountResult.Content.ReadAsStringAsync());

//                // Parse core_liquid_balance safely
//                decimal balanceDecimal = 0m;
//                if (accountData.TryGetProperty("core_liquid_balance", out var coreBal) && coreBal.ValueKind == JsonValueKind.String)
//                {
//                    var balanceStr = coreBal.GetString();
//                    if (!string.IsNullOrEmpty(balanceStr))
//                    {
//                        // balance typically like "1.2345 TLOS"
//                        var parts = balanceStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
//                        if (parts.Length > 0)
//                        {
//                            Decimal.TryParse(parts[0], NumberStyles.Number, CultureInfo.InvariantCulture, out balanceDecimal);
//                        }
//                    }
//                }

//                var balanceValue = (long)(balanceDecimal * 10000m);

//                if (balanceValue < amountInTLOS)
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Insufficient balance. Available: {balanceDecimal} TLOS, Required: {(decimal)amountInTLOS / 10000m} TLOS");
//                    return result;
//                }

//                // Create Telos transfer transaction
//                var transferRequest = new
//                {
//                    actions = new[]
//                    {
//                        new
//                        {
//                            account = "eosio.token",
//                            name = "transfer",
//                            authorization = new[]
//                            {
//                                new
//                                {
//                                    actor = fromWalletAddress,
//                                    permission = "active"
//                                }
//                            },
//                            data = new
//                            {
//                                from = fromWalletAddress,
//                                to = toWalletAddress,
//                                quantity = string.Format(CultureInfo.InvariantCulture, "{0:F4} TLOS", amount),
//                                memo = memoText
//                            }
//                        }
//                    }
//                };

//                // Submit transaction to Telos network
//                var jsonContent = JsonSerializer.Serialize(transferRequest);
//                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

//                var submitResponse = await _httpClient.PostAsync("/v1/chain/push_transaction", content);
//                if (submitResponse.IsSuccessStatusCode)
//                {
//                    var responseContent = await submitResponse.Content.ReadAsStringAsync();
//                    var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

//                    string txId = responseData.TryGetProperty("transaction_id", out var txProp) ? txProp.GetString() : null;

//                    result.Result = new TransactionRespone
//                    {
//                        TransactionResult = txId
//                    };
//                    result.IsError = false;
//                    result.Message = $"Telos transaction sent successfully. TX ID: {result.Result.TransactionResult}";
//                }
//                else
//                {
//                    OASISErrorHandling.HandleError(ref result, $"Failed to submit Telos transaction: {submitResponse.StatusCode}");
//                }
//            }
//            catch (Exception ex)
//            {
//                result.Exception = ex;
//                OASISErrorHandling.HandleError(ref result, $"Error sending Telos transaction: {ex.Message}");
//            }

//            return result;
//        }

//        // ... rest of file unchanged ...
//    }
//}
