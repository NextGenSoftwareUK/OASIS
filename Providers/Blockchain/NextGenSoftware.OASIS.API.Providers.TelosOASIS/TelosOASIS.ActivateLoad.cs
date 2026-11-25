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
            if (_eosioOASIS == null)
                return new OASISResult<bool> { IsError = true, Message = "The EOSIO client is not configured." };

            var result = _eosioOASIS.IsProviderActivated
                ? new OASISResult<bool>(true)
                : await _eosioOASIS.ActivateProviderAsync();
            IsProviderActivated = !result.IsError && result.Result;
            return result;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            if (_eosioOASIS == null)
                return new OASISResult<bool> { IsError = true, Message = "The EOSIO client is not configured." };

            var result = _eosioOASIS.IsProviderActivated
                ? new OASISResult<bool>(true)
                : _eosioOASIS.ActivateProvider();
            IsProviderActivated = !result.IsError && result.Result;
            return result;
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
            if (EOSIOOASIS == null)
                throw new InvalidOperationException("The EOSIO client is not configured.");

            return await EOSIOOASIS.GetBalanceAsync(telosAccountName, code, symbol);
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

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
            => _eosioOASIS.LoadAllAvatarsAsync(version);

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => _eosioOASIS.LoadAllAvatars(version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
            => _eosioOASIS.LoadAvatarByUsername(avatarUsername, version);

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
            => _eosioOASIS.LoadAvatarAsync(Id, version);

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => _eosioOASIS.LoadAvatarByEmailAsync(avatarEmail, version);

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => _eosioOASIS.LoadAvatarByUsernameAsync(avatarUsername, version);

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
            => _eosioOASIS.LoadAvatar(Id, version);

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
            => _eosioOASIS.LoadAvatarByEmail(avatarEmail, version);

    }
}
