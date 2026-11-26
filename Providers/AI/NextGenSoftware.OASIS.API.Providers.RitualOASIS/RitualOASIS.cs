using System;
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

namespace NextGenSoftware.OASIS.API.Providers.RitualOASIS
{
    /// <summary>
    /// Read-only Ritual Chain adapter over its public JSON-RPC. Ritual Chain replaced the Infernet node protocol with native
    /// precompiles, so this provider no longer targets an Infernet node REST API (whose api/status and api/containers routes
    /// never existed). Activation checks the pinned Ritual Chain genesis. Avatar = account (balance, nonce, contract code);
    /// holon = transaction by hash (receipt status). Compute runs through funded precompile transactions, so listings and
    /// every write fail explicitly.
    /// </summary>
    public class RitualOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        /// <summary>Genesis block hash of Ritual Chain (chain id 0x310).</summary>
        public const string GenesisHash = "0x220f7b8da21377ee959d6bf3dc6397ffb5ec0737c37b08974c08247d0822ba93";
        private const string NoListing = "Ritual Chain has no listing API for accounts or transactions; load an address or a transaction hash.";
        private const string ReadOnly = "RitualOASIS is read-only: Ritual compute runs through funded precompile transactions.";

        private readonly HttpClient _http;

        public RitualOASIS(string rpcUrl = "https://rpc.ritualfoundation.org") : this(rpcUrl, null) { }

        internal RitualOASIS(string rpcUrl, HttpMessageHandler handler)
        {
            if (string.IsNullOrWhiteSpace(rpcUrl)) throw new ArgumentException("A Ritual Chain JSON-RPC endpoint is required.", nameof(rpcUrl));
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(rpcUrl);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("NextGenSoftware-OASIS-RitualOASIS/2.0");
            ProviderName = "RitualOASIS";
            ProviderDescription = "Ritual Chain accounts and transactions read over its public JSON-RPC.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.RitualOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.AI);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.AI));
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        private static bool IsHex(string value, int digits)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != digits + 2 || !value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return false;
            foreach (var c in value.AsSpan(2)) if (!Uri.IsHexDigit(c)) return false;
            return true;
        }

        private async Task<JsonElement> RpcAsync(string method, params object[] parameters)
        {
            var body = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method, @params = parameters });
            using var response = await _http.PostAsync("", new StringContent(body, Encoding.UTF8, "application/json"));
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"Ritual {method} failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.TryGetProperty("error", out var error))
                throw new InvalidOperationException($"Ritual {method} failed: " + (error.TryGetProperty("message", out var m) ? m.GetString() : error.GetRawText()));
            return document.RootElement.GetProperty("result").Clone();
        }

        private static string Quantity(JsonElement hex) => BigInteger.Parse("0" + hex.GetString()![2..], NumberStyles.HexNumber).ToString(CultureInfo.InvariantCulture);

        private static OASISResult<T> Unsupported<T>(string message)
        {
            var r = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref r, message);
            return r;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                var genesis = await RpcAsync("eth_getBlockByNumber", "0x0", false);
                var hash = genesis.GetProperty("hash").GetString();
                if (!string.Equals(hash, GenesisHash, StringComparison.OrdinalIgnoreCase))
                {
                    OASISErrorHandling.HandleError(ref r, $"The endpoint is not Ritual Chain (genesis {hash}).");
                    return r;
                }
                IsProviderActivated = true;
                r.Result = true;
                r.Message = "RitualOASIS activated against Ritual Chain.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual activation failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool> { Result = true, Message = "RitualOASIS deactivated." });
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        private async Task<Dictionary<string, object>> ReadAccountAsync(string address)
        {
            if (!IsHex(address, 40)) throw new ArgumentException("A Ritual account is a 20-byte address (0x + 40 hex digits).", nameof(address));
            var account = address.ToLowerInvariant();
            var balance = await RpcAsync("eth_getBalance", account, "latest");
            var nonce = await RpcAsync("eth_getTransactionCount", account, "latest");
            var code = await RpcAsync("eth_getCode", account, "latest");
            return new Dictionary<string, object>
            {
                ["address"] = account,
                ["balance_wei"] = Quantity(balance),
                ["nonce"] = Quantity(nonce),
                ["is_contract"] = code.GetString() is { Length: > 2 } ? "true" : "false",
                ["chain"] = "ritual",
            };
        }

        // Avatar = Ritual Chain account
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var data = await ReadAccountAsync(address);
                var avatar = new Avatar { Username = (string)data["address"] };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.RitualOASIS] = (string)data["address"];
                foreach (var kv in data) avatar.MetaData[kv.Key] = kv.Value;
                r.Result = avatar;
                r.IsLoaded = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual LoadAvatarByProviderKey failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => LoadAvatarByProviderKeyAsync(u, v);
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) => Task.FromResult(Unsupported<IAvatar>("Ritual accounts are addresses, not OASIS GUIDs."));
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) => Task.FromResult(Unsupported<IAvatar>("Ritual accounts have no email."));
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) => Task.FromResult(Unsupported<IEnumerable<IAvatar>>(NoListing));
        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) => Task.FromResult(Unsupported<IAvatar>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) => Task.FromResult(Unsupported<IAvatarDetail>("Ritual accounts are addresses, not OASIS GUIDs."));
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) => Task.FromResult(Unsupported<IAvatarDetail>("Ritual accounts have no email."));

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var data = await ReadAccountAsync(address);
                var detail = new AvatarDetail { Username = (string)data["address"] };
                foreach (var kv in data) detail.MetaData[kv.Key] = kv.Value;
                r.Result = detail;
                r.IsLoaded = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual LoadAvatarDetailByUsername failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) => Task.FromResult(Unsupported<IAvatarDetail>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) => Task.FromResult(Unsupported<IEnumerable<IAvatarDetail>>(NoListing));

        // Holon = Ritual Chain transaction
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string txHash, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                if (!IsHex(txHash, 64)) { OASISErrorHandling.HandleError(ref r, "A Ritual transaction is a 32-byte hash (0x + 64 hex digits)."); return r; }
                var tx = await RpcAsync("eth_getTransactionByHash", txHash);
                if (tx.ValueKind == JsonValueKind.Null) { OASISErrorHandling.HandleError(ref r, $"No Ritual transaction {txHash}."); return r; }
                var receipt = await RpcAsync("eth_getTransactionReceipt", txHash);
                var holon = new Holon { Name = txHash };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.RitualOASIS] = txHash;
                holon.MetaData["tx_hash"] = tx.GetProperty("hash").GetString();
                holon.MetaData["from"] = tx.GetProperty("from").GetString();
                holon.MetaData["to"] = tx.TryGetProperty("to", out var to) && to.ValueKind == JsonValueKind.String ? to.GetString() : "";
                holon.MetaData["block_number"] = tx.GetProperty("blockNumber").ValueKind == JsonValueKind.String ? Quantity(tx.GetProperty("blockNumber")) : "";
                holon.MetaData["status"] = receipt.ValueKind == JsonValueKind.Null ? "pending" : receipt.GetProperty("status").GetString() == "0x1" ? "success" : "reverted";
                r.Result = holon;
                r.IsLoaded = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IHolon>("Ritual transactions are hashes, not OASIS GUIDs."));
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
            if (string.IsNullOrEmpty(query)) { OASISErrorHandling.HandleError(ref r, "Search needs a Ritual address in FilterByMetaData[\"query\"]."); return r; }
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
