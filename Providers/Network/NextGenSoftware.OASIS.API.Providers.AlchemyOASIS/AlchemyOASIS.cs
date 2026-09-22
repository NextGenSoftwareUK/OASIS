using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AlchemyOASIS
{
    /// <summary>
    /// OASIS provider for Alchemy — Ethereum RPC gateway + Enhanced NFT API.
    /// Wallet addresses → Avatars. NFTs and transactions → Holons.
    /// </summary>
    public class AlchemyOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _rpcClient;
        private readonly HttpClient _nftClient;
        private bool _isActivated;

        public AlchemyOASIS(string apiKey = "")
        {
            var rpcBase = string.IsNullOrEmpty(apiKey) ? "https://eth-mainnet.g.alchemy.com/v2/demo" : $"https://eth-mainnet.g.alchemy.com/v2/{apiKey}";
            var nftBase = string.IsNullOrEmpty(apiKey) ? "https://eth-mainnet.g.alchemy.com/nft/v3/demo" : $"https://eth-mainnet.g.alchemy.com/nft/v3/{apiKey}";
            _rpcClient = new HttpClient { BaseAddress = new Uri(rpcBase) };
            _rpcClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _nftClient = new HttpClient { BaseAddress = new Uri(nftBase) };
            _nftClient.DefaultRequestHeaders.Add("Accept", "application/json");
            ProviderName = "AlchemyOASIS";
            ProviderDescription = "Alchemy Ethereum RPC Gateway and NFT API Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AlchemyOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private async Task<JsonElement?> JsonRpcAsync(string method, object[] @params)
        {
            var payload = new { jsonrpc = "2.0", id = 1, method, @params };
            var response = await _rpcClient.PostAsJsonAsync("", payload);
            if (!response.IsSuccessStatusCode) return null;
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            return body.TryGetProperty("result", out var r) ? r : (JsonElement?)null;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var blockNumber = await JsonRpcAsync("eth_blockNumber", Array.Empty<object>());
                if (blockNumber.HasValue) { _isActivated = true; result.Result = true; result.Message = $"AlchemyOASIS activated. Block: {blockNumber.Value}."; }
                else OASISErrorHandling.HandleError(ref result, "AlchemyOASIS: RPC health check failed.");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _isActivated = false; return new OASISResult<bool>(true); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var balance = await JsonRpcAsync("eth_getBalance", new object[] { providerKey, "latest" });
                var txCount = await JsonRpcAsync("eth_getTransactionCount", new object[] { providerKey, "latest" });
                var avatar = new Avatar();
                avatar.Username = providerKey;
                avatar.MetaData["alchemy_address"] = providerKey;
                avatar.MetaData["alchemy_eth_balance_hex"] = balance.HasValue ? balance.Value.GetString() : "0x0";
                avatar.MetaData["alchemy_tx_count_hex"] = txCount.HasValue ? txCount.Value.GetString() : "0x0";
                result.Result = avatar;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var tx = await JsonRpcAsync("eth_getTransactionByHash", new object[] { providerKey });
                if (!tx.HasValue || tx.Value.ValueKind == JsonValueKind.Null) { OASISErrorHandling.HandleError(ref result, $"AlchemyOASIS: Transaction '{providerKey}' not found."); return result; }
                var holon = new Holon();
                holon.Name = $"Eth Tx {providerKey[..Math.Min(10, providerKey.Length)]}...";
                holon.MetaData["alchemy_tx_hash"] = providerKey;
                holon.MetaData["alchemy_from"] = tx.Value.TryGetProperty("from", out var f) ? f.GetString() : "";
                holon.MetaData["alchemy_to"] = tx.Value.TryGetProperty("to", out var t) && t.ValueKind != JsonValueKind.Null ? t.GetString() : "";
                holon.MetaData["alchemy_value_hex"] = tx.Value.TryGetProperty("value", out var v) ? v.GetString() : "0x0";
                holon.MetaData["alchemy_block_number_hex"] = tx.Value.TryGetProperty("blockNumber", out var bn) ? bn.GetString() : "";
                result.Result = holon;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>(); var holons = new List<IHolon>();
            try
            {
                var response = await _nftClient.GetAsync($"/getNFTsForOwner?owner={providerKey}&withMetadata=true&pageSize=100");
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var nfts = body.TryGetProperty("ownedNfts", out var n) ? n : body;
                    foreach (var nft in nfts.EnumerateArray())
                    {
                        var holon = new Holon();
                        holon.Name = nft.TryGetProperty("name", out var nm) ? nm.GetString() : "NFT";
                        holon.Description = nft.TryGetProperty("description", out var d) ? d.GetString() : "";
                        holon.MetaData["alchemy_contract"] = nft.TryGetProperty("contract", out var c) && c.TryGetProperty("address", out var ca) ? ca.GetString() : "";
                        holon.MetaData["alchemy_token_id"] = nft.TryGetProperty("tokenId", out var tid) ? tid.GetString() : "";
                        holon.MetaData["alchemy_token_type"] = nft.TryGetProperty("tokenType", out var tt) ? tt.GetString() : "";
                        holons.Add(holon);
                    }
                    result.Result = holons;
                }
                else OASISErrorHandling.HandleError(ref result, $"AlchemyOASIS: getNFTsForOwner failed ({response.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Send signed transactions via eth_sendRawTransaction."); return await Task.FromResult(r); }
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Transactions require signing."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Use LoadHolonAsync(txHash)."); return await Task.FromResult(r); }
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Use LoadHolonsForParentAsync(walletAddress)."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Use LoadHolonsForParentAsync(string walletAddress)."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: MetaData search not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: MetaData search not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: On-chain transactions cannot be deleted."); return await Task.FromResult(r); }
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: On-chain transactions cannot be deleted."); return await Task.FromResult(r); }
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        { var r = new OASISResult<ISearchResults>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Use LoadHolonsForParentAsync(walletAddress)."); return await Task.FromResult(r); }
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Use LoadAvatarByProviderKey(walletAddress)."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Use LoadAvatarByProviderKey(walletAddress)."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Email lookup not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Enumeration not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: ETH accounts are keypairs; use a wallet library."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar) => SaveAvatarDetailAsync(avatar).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: ETH accounts cannot be deleted."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: ETH accounts cannot be deleted."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Import not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Geolocation not supported."); return r; }
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AlchemyOASIS: Geolocation not supported."); return r; }
    }
}
