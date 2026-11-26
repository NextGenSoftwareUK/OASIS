using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BittensorOASIS
{
    /// <summary>
    /// Read-only Bittensor adapter over the subtensor node's public JSON-RPC (Finney). The former default, the taostats.io
    /// REST API, now refuses requests without a bot-gate token. Activation checks the pinned Finney genesis. Avatar = account
    /// (SS58 address or 0x account id): System.Account nonce and TAO balances (rao). Holon = block by number. Listings and every
    /// write fail explicitly: enumerating accounts or subnets needs an indexer, and staking or registration needs signed extrinsics.
    /// </summary>
    public class BittensorOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        /// <summary>Genesis block hash of Bittensor Finney (mainnet).</summary>
        public const string FinneyGenesisHash = "0x2f0555cc76fc2840a25a6ea3b9637146806f1f44b090c175ffde2a7e5ab36c03";
        private const string NoListing = "Listing Bittensor accounts or subnets needs an indexer; load an account address or a block number.";
        private const string ReadOnly = "BittensorOASIS is read-only: staking, registration and transfers need signed extrinsics.";
        private static readonly byte[] SystemAccountPrefix = [.. SubstrateHashing.Twox128("System"), .. SubstrateHashing.Twox128("Account")];

        private readonly HttpClient _http;

        public BittensorOASIS(string rpcUrl = "https://entrypoint-finney.opentensor.ai") : this(rpcUrl, null) { }

        internal BittensorOASIS(string rpcUrl, HttpMessageHandler handler)
        {
            if (string.IsNullOrWhiteSpace(rpcUrl)) throw new ArgumentException("A subtensor JSON-RPC endpoint is required.", nameof(rpcUrl));
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(rpcUrl);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("NextGenSoftware-OASIS-BittensorOASIS/2.0");
            ProviderName = "BittensorOASIS";
            ProviderDescription = "Bittensor accounts and blocks read from the subtensor node's public JSON-RPC.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BittensorOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.AI);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.AI));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        private async Task<JsonElement> RpcAsync(string method, params object[] parameters)
        {
            var body = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method, @params = parameters });
            using var response = await _http.PostAsync("", new StringContent(body, Encoding.UTF8, "application/json"));
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Subtensor {method} failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.TryGetProperty("error", out var error))
                throw new InvalidOperationException($"Subtensor {method} failed: " + (error.TryGetProperty("message", out var m) ? m.GetString() : error.GetRawText()));
            return document.RootElement.GetProperty("result").Clone();
        }

        private static OASISResult<T> Unsupported<T>(string message)
        {
            var r = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref r, message);
            return r;
        }

        /// <summary>The 32-byte account id from an SS58 address (network prefix 42) or a 0x-prefixed hex id.</summary>
        internal static byte[] AccountId(string account)
        {
            if (string.IsNullOrWhiteSpace(account)) throw new ArgumentException("A Bittensor account is required.", nameof(account));
            if (account.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                var bytes = Convert.FromHexString(account[2..]);
                if (bytes.Length != 32) throw new ArgumentException("A hex Bittensor account id is 32 bytes.", nameof(account));
                return bytes;
            }
            return SubstrateHashing.DecodeSs58(account);
        }

        /// <summary>System.Account storage key: twox128("System") ++ twox128("Account") ++ blake2_128_concat(account id).</summary>
        internal static string AccountStorageKey(byte[] accountId) =>
            "0x" + Convert.ToHexString([.. SystemAccountPrefix, .. SubstrateHashing.Blake2_128Concat(accountId)]).ToLowerInvariant();

        /// <summary>Decodes subtensor's AccountInfo: nonce, consumers, providers, sufficients (u32 each), then AccountData
        /// free, reserved, frozen (u64 each, rao) and flags (u128).</summary>
        internal static Dictionary<string, object> DecodeAccountInfo(string hex)
        {
            var v = Convert.FromHexString(hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? hex[2..] : hex);
            if (v.Length < 40) throw new FormatException($"Unexpected subtensor AccountInfo length {v.Length}.");
            return new Dictionary<string, object>
            {
                ["nonce"] = BinaryPrimitives.ReadUInt32LittleEndian(v).ToString(CultureInfo.InvariantCulture),
                ["providers"] = BinaryPrimitives.ReadUInt32LittleEndian(v.AsSpan(8)).ToString(CultureInfo.InvariantCulture),
                ["free_rao"] = BinaryPrimitives.ReadUInt64LittleEndian(v.AsSpan(16)).ToString(CultureInfo.InvariantCulture),
                ["reserved_rao"] = BinaryPrimitives.ReadUInt64LittleEndian(v.AsSpan(24)).ToString(CultureInfo.InvariantCulture),
                ["frozen_rao"] = BinaryPrimitives.ReadUInt64LittleEndian(v.AsSpan(32)).ToString(CultureInfo.InvariantCulture),
            };
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                var genesis = (await RpcAsync("chain_getBlockHash", 0)).GetString();
                if (!string.Equals(genesis, FinneyGenesisHash, StringComparison.OrdinalIgnoreCase))
                {
                    OASISErrorHandling.HandleError(ref r, $"The endpoint is not Bittensor Finney (genesis {genesis}).");
                    return r;
                }
                IsProviderActivated = true;
                r.Result = true;
                r.Message = "BittensorOASIS activated against Bittensor Finney.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor activation failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool> { Result = true, Message = "BittensorOASIS deactivated." });
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        private async Task<Dictionary<string, object>> ReadAccountAsync(string account)
        {
            var id = AccountId(account);
            var key = AccountStorageKey(id);
            var stored = await RpcAsync("state_getStorage", key);
            // An account that never received funds has no System.Account entry: it exists with zero balances.
            var data = stored.ValueKind == JsonValueKind.String
                ? DecodeAccountInfo(stored.GetString()!)
                : new Dictionary<string, object> { ["nonce"] = "0", ["providers"] = "0", ["free_rao"] = "0", ["reserved_rao"] = "0", ["frozen_rao"] = "0" };
            data["account_id"] = "0x" + Convert.ToHexString(id).ToLowerInvariant();
            data["storage_key"] = key;
            data["chain"] = "bittensor-finney";
            return data;
        }

        // Avatar = Bittensor account
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string account, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var data = await ReadAccountAsync(account);
                var avatar = new Avatar { Username = account };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.BittensorOASIS] = (string)data["account_id"];
                foreach (var kv in data) avatar.MetaData[kv.Key] = kv.Value;
                r.Result = avatar;
                r.IsLoaded = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor LoadAvatarByProviderKey failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => LoadAvatarByProviderKeyAsync(u, v);
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) => Task.FromResult(Unsupported<IAvatar>("Bittensor accounts are SS58 addresses, not OASIS GUIDs."));
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) => Task.FromResult(Unsupported<IAvatar>("Bittensor accounts have no email."));
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) => Task.FromResult(Unsupported<IEnumerable<IAvatar>>(NoListing));
        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) => Task.FromResult(Unsupported<IAvatar>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) => Task.FromResult(Unsupported<IAvatarDetail>("Bittensor accounts are SS58 addresses, not OASIS GUIDs."));
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) => Task.FromResult(Unsupported<IAvatarDetail>("Bittensor accounts have no email."));

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string account, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var data = await ReadAccountAsync(account);
                var detail = new AvatarDetail { Username = account };
                foreach (var kv in data) detail.MetaData[kv.Key] = kv.Value;
                r.Result = detail;
                r.IsLoaded = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor LoadAvatarDetailByUsername failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) => Task.FromResult(Unsupported<IAvatarDetail>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) => Task.FromResult(Unsupported<IEnumerable<IAvatarDetail>>(NoListing));

        // Holon = block by number
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string blockNumber, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                if (!ulong.TryParse(blockNumber, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                {
                    OASISErrorHandling.HandleError(ref r, "A Bittensor holon is a block number.");
                    return r;
                }
                var hash = await RpcAsync("chain_getBlockHash", number);
                if (hash.ValueKind != JsonValueKind.String) { OASISErrorHandling.HandleError(ref r, $"Bittensor block {number} does not exist yet."); return r; }
                var header = await RpcAsync("chain_getHeader", hash.GetString());
                var holon = new Holon { Name = $"Bittensor block {number}" };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.BittensorOASIS] = hash.GetString();
                holon.MetaData["block_number"] = number.ToString(CultureInfo.InvariantCulture);
                holon.MetaData["block_hash"] = hash.GetString();
                holon.MetaData["parent_hash"] = header.GetProperty("parentHash").GetString();
                holon.MetaData["state_root"] = header.GetProperty("stateRoot").GetString();
                holon.MetaData["extrinsics_root"] = header.GetProperty("extrinsicsRoot").GetString();
                r.Result = holon;
                r.IsLoaded = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IHolon>("Bittensor blocks are numbers, not OASIS GUIDs."));
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) => Task.FromResult(Unsupported<IHolon>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NoListing));
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => Task.FromResult(Unsupported<IHolon>(ReadOnly));
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string k) => Task.FromResult(Unsupported<IHolon>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NoListing));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string operatorAddress, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NoListing));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NoListing));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NoListing));

        /// <summary>Search by address (FilterByMetaData["query"]).</summary>
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            var query = sp?.FilterByMetaData?.ContainsKey("query") == true ? sp.FilterByMetaData["query"] : "";
            if (string.IsNullOrEmpty(query)) { OASISErrorHandling.HandleError(ref r, "Search needs a Bittensor account in FilterByMetaData[\"query\"]."); return r; }
            var account = await LoadAvatarByProviderKeyAsync(query);
            if (account.IsError) { OASISErrorHandling.HandleError(ref r, account.Message); return r; }
            r.Result = new SearchResults { SearchResultAvatars = new List<IAvatar> { account.Result } };
            return r;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NoListing));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string address, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NoListing));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NoListing));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NoListing));
        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h) => Task.FromResult(Unsupported<bool>(ReadOnly));

        // Synchronous members delegate to the async reads.
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int v = 0) => LoadAvatarAsync(id, v).Result;
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string k, int v = 0) => LoadAvatarByProviderKeyAsync(k, v).Result;
        public override OASISResult<IAvatar> LoadAvatarByUsername(string u, int v = 0) => LoadAvatarByUsernameAsync(u, v).Result;
        public override OASISResult<IAvatar> LoadAvatarByEmail(string e, int v = 0) => LoadAvatarByEmailAsync(e, v).Result;
        public override OASISResult<IAvatar> SaveAvatar(IAvatar a) => SaveAvatarAsync(a).Result;
        public override OASISResult<bool> DeleteAvatar(Guid id, bool s = true) => DeleteAvatarAsync(id, s).Result;
        public override OASISResult<bool> DeleteAvatar(string k, bool s = true) => DeleteAvatarAsync(k, s).Result;
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool s = true) => DeleteAvatarByEmailAsync(e, s).Result;
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool s = true) => DeleteAvatarByUsernameAsync(u, s).Result;
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int v = 0) => LoadAllAvatarsAsync(v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int v = 0) => LoadAvatarDetailAsync(id, v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int v = 0) => LoadAvatarDetailByEmailAsync(e, v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int v = 0) => LoadAvatarDetailByUsernameAsync(u, v).Result;
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail ad) => SaveAvatarDetailAsync(ad).Result;
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int v = 0) => LoadAllAvatarDetailsAsync(v).Result;
        public override OASISResult<IHolon> LoadHolon(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonAsync(id, lc, rec, md, coe, lcfp, v).Result;
        public override OASISResult<IHolon> LoadHolon(string k, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonAsync(k, lc, rec, md, coe, lcfp, v).Result;
        public override OASISResult<IHolon> SaveHolon(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) => SaveHolonAsync(h, sc, rec, md, coe, scop).Result;
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> h, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) => SaveHolonsAsync(h, sc, rec, md, cd, coe, scop).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadAllHolonsAsync(ht, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override OASISResult<IHolon> DeleteHolon(string k) => DeleteHolonAsync(k).Result;
        public override OASISResult<ISearchResults> Search(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0) => SearchAsync(sp, lc, rec, md, coe, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsForParentAsync(id, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsForParentAsync(k, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsByMetaDataAsync(mk, mv, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsByMetaDataAsync(m, mm, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<bool> Import(IEnumerable<IHolon> h) => ImportAsync(h).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int v = 0) => ExportAllDataForAvatarByIdAsync(id, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int v = 0) => ExportAllDataForAvatarByUsernameAsync(u, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int v = 0) => ExportAllDataForAvatarByEmailAsync(e, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int v = 0) => ExportAllAsync(v).Result;
    }
}
