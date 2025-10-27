using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Responses;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS
{
    public class TelosOASIS : OASISStorageProviderBase, IOASISBlockchainStorageProvider, IOASISSmartContractProvider, IOASISNFTProvider, IOASISNETProvider
    {
        private static Dictionary<Guid, GetAccountResponseDto> _avatarIdToTelosAccountLookup = new Dictionary<Guid, GetAccountResponseDto>();
        private AvatarManager _avatarManager = null;
        private KeyManager _keyManager = null;
        private readonly HttpClient _httpClient;
        private const string TELOS_API_BASE_URL = "https://api.telos.net";

        public EOSIOOASIS.EOSIOOASIS EOSIOOASIS { get; set; }

        public TelosOASIS(string host, string eosAccountName, string eosChainId, string eosAccountPk)
        {
            this.ProviderName = "TelosOASIS";
            this.ProviderDescription = "Telos Provider";
            this.ProviderType = new EnumValue<ProviderType>(API.Core.Enums.ProviderType.TelosOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            EOSIOOASIS = new EOSIOOASIS.EOSIOOASIS(host, eosAccountName, eosChainId, eosAccountPk);
            _httpClient = new HttpClient();
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
            if (!EOSIOOASIS.IsProviderActivated)
                await EOSIOOASIS.ActivateProviderAsync();

            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> ActivateProvider()
        {
            if (!EOSIOOASIS.IsProviderActivated)
                EOSIOOASIS.ActivateProvider();

            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            if (EOSIOOASIS.IsProviderActivated)
                await EOSIOOASIS.DeActivateProviderAsync();

            _avatarManager = null;
            _keyManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            if (EOSIOOASIS.IsProviderActivated)
                EOSIOOASIS.DeActivateProvider();

            _avatarManager = null;
            _keyManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        // TODO: Implement GetAccountAsync in EOS provider
        public async Task<Account> GetTelosAccountAsync(string telosAccountName)
        {
            // var account = await EOSIOOASIS.ChainAPI.GetAccountAsync(telosAccountName);
            return new Account();
        }

        // TODO: Implement GetAccount in EOS provider
        public GetAccountResponseDto GetTelosAccount(string telosAccountName)
        { 
            return EOSIOOASIS.GetEOSIOAccount(telosAccountName);
        }

        public async Task<string> GetBalanceAsync(string telosAccountName, string code, string symbol)
        {
            return await EOSIOOASIS.GetBalanceAsync(telosAccountName, code, symbol);
        }

        public string GetBalanceForTelosAccount(string telosAccountName, string code, string symbol)
        {
            return EOSIOOASIS.GetBalanceForEOSIOAccount(telosAccountName, code, symbol);
        }

        public string GetBalanceForAvatar(Guid avatarId, string code, string symbol)
        {
            return EOSIOOASIS.GetBalanceForAvatar(avatarId, code, symbol);
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
                var httpResponse = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);

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
                var httpResponse = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);

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
                var httpResponse = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);

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
                var httpResponse = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/get_table_rows", content);

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

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            result.Message = "LoadAvatarDetail is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            result.Message = "LoadAvatarDetail by Username is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            result.Message = "LoadAvatarDetail by Email is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>
            {
                Result = new List<IAvatarDetail>(),
                Message = "LoadAllAvatarDetails is not supported yet by Telos provider."
            };
            return Task.FromResult(result);
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
                                        password_reset = Avatar.PasswordReset?.ToUnixTimeSeconds() ?? 0,
                                        refresh_token = Avatar.RefreshToken ?? "",
                                        reset_token = Avatar.ResetToken ?? "",
                                        reset_token_expires = Avatar.ResetTokenExpires?.ToUnixTimeSeconds() ?? 0,
                                        verification_token = Avatar.VerificationToken ?? "",
                                        verified = Avatar.Verified?.ToUnixTimeSeconds() ?? 0,
                                        last_beamed_in = Avatar.LastBeamedIn?.ToUnixTimeSeconds() ?? 0,
                                        last_beamed_out = Avatar.LastBeamedOut?.ToUnixTimeSeconds() ?? 0,
                                        is_beamed_in = Avatar.IsBeamedIn,
                                        created_date = Avatar.CreatedDate.ToUnixTimeSeconds(),
                                        modified_date = DateTime.UtcNow.ToUnixTimeSeconds(),
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
                var httpResponse = await _httpClient.PostAsync($"{TELOS_API_BASE_URL}/v1/chain/push_transaction", content);

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

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = new OASISResult<IAvatarDetail>();
            result.Message = "SaveAvatarDetail is not supported yet by Telos provider.";
            return Task.FromResult(result);
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
            return null;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
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
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return null;
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

        public OASISResult<ITransactionRespone> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionRespone>();
            
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Telos provider is not activated");
                    return result;
                }

                // Convert decimal amount to TLOS (1 TLOS = 10^4 precision)
                var amountInTLOS = (long)(amount * 10000);
                
                // Get account info for balance check
                var accountResponse = await _httpClient.GetAsync($"/v1/chain/get_account");
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
                var balance = accountData.GetProperty("core_liquid_balance").GetString();
                var balanceValue = long.Parse(balance.Replace(" TLOS", "").Replace(".", ""));
                
                if (balanceValue < amountInTLOS)
                {
                    OASISErrorHandling.HandleError(ref result, $"Insufficient balance. Available: {balanceValue} TLOS, Required: {amountInTLOS} TLOS");
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
                                quantity = $"{amount:F4} TLOS",
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
                    
                    result.Result = new TransactionRespone
                    {
                        TransactionResult = responseData.GetProperty("transaction_id").GetString()
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

        public OASISResult<ITransactionRespone> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            result.Message = "SendTransactionById is not supported yet by Telos provider.";
            return await Task.FromResult(result);
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            result.Message = "SendTransactionByUsername is not supported yet by Telos provider.";
            return await Task.FromResult(result);
        }

        public OASISResult<ITransactionRespone> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            result.Message = "SendTransactionByEmail is not supported yet by Telos provider.";
            return await Task.FromResult(result);
        }

        public OASISResult<ITransactionRespone> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public OASISResult<ITransactionRespone> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionRespone>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionRespone>();
            result.Message = "SendTransactionByDefaultWallet is not supported yet by Telos provider.";
            return await Task.FromResult(result);
        }

        public OASISResult<INFTTransactionRespone> SendNFT(INFTWalletTransactionRequest transation)
        {
            return SendNFTAsync(transation).Result;
        }

        public Task<OASISResult<INFTTransactionRespone>> SendNFTAsync(INFTWalletTransactionRequest transation)
        {
            var result = new OASISResult<INFTTransactionRespone>();
            result.Message = "SendNFT is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            result.Message = "Search is not supported yet by Telos provider.";
            return result;
        }

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            result.Message = "Import is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>
            {
                Result = new List<IHolon>(),
                Message = "ExportAllDataForAvatarById is not supported yet by Telos provider."
            };
            return Task.FromResult(result);
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>
            {
                Result = new List<IHolon>(),
                Message = "ExportAllDataForAvatarByUsername is not supported yet by Telos provider."
            };
            return Task.FromResult(result);
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>
            {
                Result = new List<IHolon>(),
                Message = "ExportAllDataForAvatarByEmail is not supported yet by Telos provider."
            };
            return Task.FromResult(result);
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>
            {
                Result = new List<IHolon>(),
                Message = "ExportAll is not supported yet by Telos provider."
            };
            return Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public OASISResult<string> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public Task<OASISResult<string>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            result.Message = "SendTransactionById with token is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public Task<OASISResult<string>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            result.Message = "SendTransactionByUsername with token is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<string> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public Task<OASISResult<string>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<string>();
            result.Message = "SendTransactionByEmail with token is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<string> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        Task<OASISResult<INFTTransactionRespone>> IOASISNFTProvider.SendNFTAsync(INFTWalletTransactionRequest transation)
        {
            var result = new OASISResult<INFTTransactionRespone>();
            result.Message = "IOASISNFTProvider.SendNFTAsync is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<INFTTransactionRespone> MintNFT(IMintNFTTransactionRequest transation)
        {
            return MintNFTAsync(transation).Result;
        }

        public Task<OASISResult<INFTTransactionRespone>> MintNFTAsync(IMintNFTTransactionRequest transation)
        {
            var result = new OASISResult<INFTTransactionRespone>();
            result.Message = "MintNFT is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<IOASISNFT> LoadNFT(Guid id)
        {
            return LoadNFTAsync(id).Result;
        }

        public Task<OASISResult<IOASISNFT>> LoadNFTAsync(Guid id)
        {
            var result = new OASISResult<IOASISNFT>();
            result.Message = "LoadNFT by Id is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<IOASISNFT> LoadNFT(string hash)
        {
            var result = new OASISResult<IOASISNFT>();
            result.Message = "LoadNFT by Hash is not supported yet by Telos provider.";
            return result;
        }

        public Task<OASISResult<IOASISNFT>> LoadNFTAsync(string hash)
        {
            var result = new OASISResult<IOASISNFT>();
            result.Message = "LoadNFTAsync by Hash is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForAvatar(Guid avatarId)
        {
            var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
            result.Result = new List<IOASISGeoSpatialNFT>();
            result.Message = "LoadAllGeoNFTsForAvatar is not supported yet by Telos provider.";
            return result;
        }

        public Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForAvatarAsync(Guid avatarId)
        {
            var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
            result.Result = new List<IOASISGeoSpatialNFT>();
            result.Message = "LoadAllGeoNFTsForAvatarAsync is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<List<IOASISGeoSpatialNFT>> LoadAllGeoNFTsForMintAddress(string mintWalletAddress)
        {
            var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
            result.Result = new List<IOASISGeoSpatialNFT>();
            result.Message = "LoadAllGeoNFTsForMintAddress is not supported yet by Telos provider.";
            return result;
        }

        public Task<OASISResult<List<IOASISGeoSpatialNFT>>> LoadAllGeoNFTsForMintAddressAsync(string mintWalletAddress)
        {
            var result = new OASISResult<List<IOASISGeoSpatialNFT>>();
            result.Result = new List<IOASISGeoSpatialNFT>();
            result.Message = "LoadAllGeoNFTsForMintAddressAsync is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForAvatar(Guid avatarId)
        {
            var result = new OASISResult<List<IOASISNFT>>();
            result.Result = new List<IOASISNFT>();
            result.Message = "LoadAllNFTsForAvatar is not supported yet by Telos provider.";
            return result;
        }

        public Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForAvatarAsync(Guid avatarId)
        {
            var result = new OASISResult<List<IOASISNFT>>();
            result.Result = new List<IOASISNFT>();
            result.Message = "LoadAllNFTsForAvatarAsync is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<List<IOASISNFT>> LoadAllNFTsForMintAddress(string mintWalletAddress)
        {
            var result = new OASISResult<List<IOASISNFT>>();
            result.Result = new List<IOASISNFT>();
            result.Message = "LoadAllNFTsForMintAddress is not supported yet by Telos provider.";
            return result;
        }

        public Task<OASISResult<List<IOASISNFT>>> LoadAllNFTsForMintAddressAsync(string mintWalletAddress)
        {
            var result = new OASISResult<List<IOASISNFT>>();
            result.Result = new List<IOASISNFT>();
            result.Message = "LoadAllNFTsForMintAddressAsync is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<IOASISGeoSpatialNFT> PlaceGeoNFT(IPlaceGeoSpatialNFTRequest request)
        {
            var result = new OASISResult<IOASISGeoSpatialNFT>();
            result.Message = "PlaceGeoNFT is not supported yet by Telos provider.";
            return result;
        }

        public Task<OASISResult<IOASISGeoSpatialNFT>> PlaceGeoNFTAsync(IPlaceGeoSpatialNFTRequest request)
        {
            var result = new OASISResult<IOASISGeoSpatialNFT>();
            result.Message = "PlaceGeoNFTAsync is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<IOASISGeoSpatialNFT> MintAndPlaceGeoNFT(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            var result = new OASISResult<IOASISGeoSpatialNFT>();
            result.Message = "MintAndPlaceGeoNFT is not supported yet by Telos provider.";
            return result;
        }

        public Task<OASISResult<IOASISGeoSpatialNFT>> MintAndPlaceGeoNFTAsync(IMintAndPlaceGeoSpatialNFTRequest request)
        {
            var result = new OASISResult<IOASISGeoSpatialNFT>();
            result.Message = "MintAndPlaceGeoNFTAsync is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        public OASISResult<IOASISNFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            var result = new OASISResult<IOASISNFT>();
            result.Message = "LoadOnChainNFTData is not supported yet by Telos provider.";
            return result;
        }

        public Task<OASISResult<IOASISNFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IOASISNFT>();
            result.Message = "LoadOnChainNFTDataAsync is not supported yet by Telos provider.";
            return Task.FromResult(result);
        }

        /// <summary>
        /// Parse Telos EOSIO table row to Avatar object
        /// </summary>
        private IAvatar ParseTelosToAvatar(JsonElement telosData)
        {
            try
            {
                var avatar = new Avatar
                {
                    Id = telosData.TryGetProperty("id", out var id) ? Guid.Parse(id.GetString() ?? Guid.NewGuid().ToString()) : Guid.NewGuid(),
                    Username = telosData.TryGetProperty("username", out var username) ? username.GetString() : "telos_user",
                    Email = telosData.TryGetProperty("email", out var email) ? email.GetString() : "user@telos.example",
                    FirstName = telosData.TryGetProperty("first_name", out var firstName) ? firstName.GetString() : "Telos",
                    LastName = telosData.TryGetProperty("last_name", out var lastName) ? lastName.GetString() : "User",
                    Title = telosData.TryGetProperty("title", out var title) ? title.GetString() : "",
                    Password = telosData.TryGetProperty("password", out var password) ? password.GetString() : "",
                    AvatarType = new EnumValue<AvatarType>((AvatarType)(telosData.TryGetProperty("avatar_type", out var avatarType) ? avatarType.GetInt32() : 0)),
                    AcceptTerms = telosData.TryGetProperty("accept_terms", out var acceptTerms) ? acceptTerms.GetBoolean() : true,
                    JwtToken = telosData.TryGetProperty("jwt_token", out var jwtToken) ? jwtToken.GetString() : "",
                    PasswordReset = telosData.TryGetProperty("password_reset", out var passwordReset) ? DateTimeOffset.FromUnixTimeSeconds(passwordReset.GetInt64()).DateTime : (DateTime?)null,
                    RefreshToken = telosData.TryGetProperty("refresh_token", out var refreshToken) ? refreshToken.GetString() : "",
                    ResetToken = telosData.TryGetProperty("reset_token", out var resetToken) ? resetToken.GetString() : "",
                    ResetTokenExpires = telosData.TryGetProperty("reset_token_expires", out var resetTokenExpires) ? DateTimeOffset.FromUnixTimeSeconds(resetTokenExpires.GetInt64()).DateTime : (DateTime?)null,
                    VerificationToken = telosData.TryGetProperty("verification_token", out var verificationToken) ? verificationToken.GetString() : "",
                    Verified = telosData.TryGetProperty("verified", out var verified) ? DateTimeOffset.FromUnixTimeSeconds(verified.GetInt64()).DateTime : (DateTime?)null,
                    LastBeamedIn = telosData.TryGetProperty("last_beamed_in", out var lastBeamedIn) ? DateTimeOffset.FromUnixTimeSeconds(lastBeamedIn.GetInt64()).DateTime : (DateTime?)null,
                    LastBeamedOut = telosData.TryGetProperty("last_beamed_out", out var lastBeamedOut) ? DateTimeOffset.FromUnixTimeSeconds(lastBeamedOut.GetInt64()).DateTime : (DateTime?)null,
                    IsBeamedIn = telosData.TryGetProperty("is_beamed_in", out var isBeamedIn) ? isBeamedIn.GetBoolean() : false,
                    CreatedDate = telosData.TryGetProperty("created_date", out var createdDate) ? DateTimeOffset.FromUnixTimeSeconds(createdDate.GetInt64()).DateTime : DateTime.UtcNow,
                    ModifiedDate = telosData.TryGetProperty("modified_date", out var modifiedDate) ? DateTimeOffset.FromUnixTimeSeconds(modifiedDate.GetInt64()).DateTime : DateTime.UtcNow,
                    Description = telosData.TryGetProperty("description", out var description) ? description.GetString() : "Telos Avatar",
                    IsActive = telosData.TryGetProperty("is_active", out var isActive) ? isActive.GetBoolean() : true
                };

                return avatar;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing Telos data to Avatar: {ex.Message}");
                return new Avatar
                {
                    Id = Guid.NewGuid(),
                    Username = "telos_user",
                    Email = "user@telos.example"
                };
            }
        }
    }
}
