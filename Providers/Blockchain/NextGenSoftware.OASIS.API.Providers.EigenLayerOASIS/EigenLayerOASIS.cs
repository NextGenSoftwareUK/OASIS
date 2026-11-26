using System;
using System.Collections.Generic;
using System.Net.Http;
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

namespace NextGenSoftware.OASIS.API.Providers.EigenLayerOASIS
{
    /// <summary>
    /// Read-only EigenLayer adapter over the DelegationManager contract on Ethereum mainnet: for an address it reads
    /// <c>isOperator(address)</c> and <c>delegatedTo(address)</c> with eth_call. Avatar = staker or operator address;
    /// holon = registered operator. EigenLayer runs no keyless public REST API (the former default host no longer
    /// resolves), and enumerating stakers, operators or AVS registrations needs an event indexer, so list reads and
    /// every write fail explicitly.
    /// </summary>
    public class EigenLayerOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        /// <summary>EigenLayer DelegationManager proxy on Ethereum mainnet.</summary>
        public const string MainnetDelegationManager = "0x39053d51b77dc0d36036fc1fcc8cb819df8ef37a";
        private const string IsOperatorSelector = "0x6d70f7ae";
        private const string DelegatedToSelector = "0x65da1264";
        private const string ZeroAddress = "0x0000000000000000000000000000000000000000";
        private const string NeedsIndexer = "Listing EigenLayer stakers, operators or AVS registrations needs an event indexer; load an address instead.";
        private const string ReadOnly = "EigenLayerOASIS is read-only: delegation and operator registration change only through on-chain transactions.";

        private readonly HttpClient _http;
        private readonly string _delegationManager;

        public EigenLayerOASIS(string rpcUrl = "https://ethereum-rpc.publicnode.com", string delegationManager = MainnetDelegationManager)
            : this(rpcUrl, delegationManager, null) { }

        internal EigenLayerOASIS(string rpcUrl, string delegationManager, HttpMessageHandler handler)
        {
            if (string.IsNullOrWhiteSpace(rpcUrl)) throw new ArgumentException("A JSON-RPC endpoint is required.", nameof(rpcUrl));
            if (!IsAddress(delegationManager)) throw new ArgumentException("A 20-byte DelegationManager address is required.", nameof(delegationManager));
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(rpcUrl);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("NextGenSoftware-OASIS-EigenLayerOASIS/2.0");
            _delegationManager = delegationManager.ToLowerInvariant();
            ProviderName = "EigenLayerOASIS";
            ProviderDescription = "EigenLayer restaking delegation and operator registration read from the DelegationManager contract.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EigenLayerOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        private static bool IsAddress(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 42 || !value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) return false;
            foreach (var c in value.AsSpan(2)) if (!Uri.IsHexDigit(c)) return false;
            return true;
        }

        private static string Canonical(string address)
        {
            if (!IsAddress(address)) throw new ArgumentException("An EigenLayer account is a 20-byte Ethereum address (0x + 40 hex digits).", nameof(address));
            return address.ToLowerInvariant();
        }

        private async Task<string> CallAsync(string selector, string address)
        {
            var data = selector + address[2..].PadLeft(64, '0');
            var body = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, method = "eth_call", @params = new object[] { new { to = _delegationManager, data }, "latest" } });
            using var response = await _http.PostAsync("", new StringContent(body, Encoding.UTF8, "application/json"));
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"JSON-RPC eth_call failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.TryGetProperty("error", out var error))
                throw new InvalidOperationException("DelegationManager call reverted: " + (error.TryGetProperty("message", out var m) ? m.GetString() : error.GetRawText()));
            var result = document.RootElement.GetProperty("result").GetString();
            if (result == null || result.Length != 66) throw new InvalidOperationException("The DelegationManager returned an unexpected word.");
            return result[2..];
        }

        internal sealed record Delegation(string Address, bool IsOperator, string DelegatedTo);

        private async Task<Delegation> ReadAsync(string address)
        {
            var account = Canonical(address);
            var isOperator = await CallAsync(IsOperatorSelector, account);
            var delegatedTo = "0x" + (await CallAsync(DelegatedToSelector, account))[24..];
            return new Delegation(account, isOperator[^1] == '1' && isOperator.TrimStart('0') == "1", delegatedTo);
        }

        private void Describe(Dictionary<string, object> metaData, Delegation d)
        {
            metaData["address"] = d.Address;
            metaData["is_operator"] = d.IsOperator ? "true" : "false";
            metaData["delegated_to"] = d.DelegatedTo == ZeroAddress ? "" : d.DelegatedTo;
            metaData["delegation_manager"] = _delegationManager;
        }

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
                // A zero-address probe proves the endpoint and contract answer the DelegationManager interface.
                await CallAsync(IsOperatorSelector, ZeroAddress);
                IsProviderActivated = true;
                r.Result = true;
                r.Message = "EigenLayerOASIS activated against the DelegationManager.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer activation failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool> { Result = true, Message = "EigenLayerOASIS deactivated." });
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = staker or operator address
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var d = await ReadAsync(address);
                var avatar = new Avatar { Username = d.Address };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.EigenLayerOASIS] = d.Address;
                Describe(avatar.MetaData, d);
                r.Result = avatar;
                r.IsLoaded = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer LoadAvatarByProviderKey failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => LoadAvatarByProviderKeyAsync(u, v);
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) => Task.FromResult(Unsupported<IAvatar>("EigenLayer accounts are Ethereum addresses, not OASIS GUIDs."));
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) => Task.FromResult(Unsupported<IAvatar>("EigenLayer accounts have no email."));
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) => Task.FromResult(Unsupported<IEnumerable<IAvatar>>(NeedsIndexer));
        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) => Task.FromResult(Unsupported<IAvatar>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) => Task.FromResult(Unsupported<IAvatarDetail>("EigenLayer accounts are Ethereum addresses, not OASIS GUIDs."));
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) => Task.FromResult(Unsupported<IAvatarDetail>("EigenLayer accounts have no email."));

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var d = await ReadAsync(address);
                var detail = new AvatarDetail { Username = d.Address };
                Describe(detail.MetaData, d);
                r.Result = detail;
                r.IsLoaded = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer LoadAvatarDetailByUsername failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) => Task.FromResult(Unsupported<IAvatarDetail>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) => Task.FromResult(Unsupported<IEnumerable<IAvatarDetail>>(NeedsIndexer));

        // Holon = registered operator
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var d = await ReadAsync(key);
                if (!d.IsOperator) { OASISErrorHandling.HandleError(ref r, $"{d.Address} is not a registered EigenLayer operator."); return r; }
                var holon = new Holon { Name = d.Address };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.EigenLayerOASIS] = d.Address;
                Describe(holon.MetaData, d);
                r.Result = holon;
                r.IsLoaded = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IHolon>("EigenLayer operators are Ethereum addresses, not OASIS GUIDs."));
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) => Task.FromResult(Unsupported<IHolon>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NeedsIndexer));
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => Task.FromResult(Unsupported<IHolon>(ReadOnly));
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string k) => Task.FromResult(Unsupported<IHolon>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NeedsIndexer));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string operatorAddress, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NeedsIndexer));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NeedsIndexer));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NeedsIndexer));

        /// <summary>Search by address (FilterByMetaData["query"]).</summary>
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            var query = sp?.FilterByMetaData?.ContainsKey("query") == true ? sp.FilterByMetaData["query"] : "";
            if (string.IsNullOrEmpty(query)) { OASISErrorHandling.HandleError(ref r, "Search needs an Ethereum address in FilterByMetaData[\"query\"]."); return r; }
            var account = await LoadAvatarByProviderKeyAsync(query);
            if (account.IsError) { OASISErrorHandling.HandleError(ref r, account.Message); return r; }
            r.Result = new SearchResults { SearchResultAvatars = new List<IAvatar> { account.Result } };
            return r;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NeedsIndexer));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string address, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NeedsIndexer));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NeedsIndexer));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(NeedsIndexer));
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
