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

namespace NextGenSoftware.OASIS.API.Providers.ChainlinkFunctionsOASIS
{
    /// <summary>
    /// Read-only Chainlink Functions adapter. Subscriptions live in the Functions Router contract, so they are read with
    /// eth_call (<c>getSubscription(uint64)</c>) through a JSON-RPC endpoint; avatar = subscription. Chainlink runs no
    /// public REST gateway for subscriptions or requests (the former default host never existed). Requests exist only as
    /// router events and are created by LINK-funded consumer contracts, so request reads and every write fail explicitly.
    /// </summary>
    public class ChainlinkFunctionsOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        /// <summary>Ethereum mainnet Functions Router (typeAndVersion: "Functions Router v1.0.0").</summary>
        public const string MainnetRouter = "0x65Dcc24F8ff9e51F10DCc7Ed1e4e2A61e6E14bd6";
        private const string TypeAndVersionSelector = "0x181f5a77";
        private const string GetSubscriptionSelector = "0xa47c7696";
        private const string GetSubscriptionCountSelector = "0x66419970";

        private readonly HttpClient _http;
        private readonly string _router;

        public ChainlinkFunctionsOASIS(string rpcUrl = "https://ethereum-rpc.publicnode.com", string routerAddress = MainnetRouter)
            : this(rpcUrl, routerAddress, null) { }

        internal ChainlinkFunctionsOASIS(string rpcUrl, string routerAddress, HttpMessageHandler handler)
        {
            if (string.IsNullOrWhiteSpace(rpcUrl)) throw new ArgumentException("A JSON-RPC endpoint is required.", nameof(rpcUrl));
            if (string.IsNullOrWhiteSpace(routerAddress) || routerAddress.Length != 42 || !routerAddress.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A 20-byte Functions Router address is required.", nameof(routerAddress));
            _http = handler == null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _http.BaseAddress = new Uri(rpcUrl);
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("NextGenSoftware-OASIS-ChainlinkFunctionsOASIS/2.0");
            _router = routerAddress;
            ProviderName = "ChainlinkFunctionsOASIS";
            ProviderDescription = "Chainlink Functions subscriptions read from the Functions Router contract.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ChainlinkFunctionsOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
        }

        private async Task<string> CallAsync(string data)
        {
            var body = JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0", id = 1, method = "eth_call",
                @params = new object[] { new { to = _router, data }, "latest" }
            });
            using var response = await _http.PostAsync("", new StringContent(body, Encoding.UTF8, "application/json"));
            var text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"JSON-RPC eth_call failed: {(int)response.StatusCode} {response.ReasonPhrase}");
            using var document = JsonDocument.Parse(text);
            if (document.RootElement.TryGetProperty("error", out var error))
                throw new InvalidOperationException("Functions Router call reverted: " + (error.TryGetProperty("message", out var m) ? m.GetString() : error.GetRawText()));
            var result = document.RootElement.GetProperty("result").GetString();
            if (string.IsNullOrEmpty(result) || result == "0x") throw new InvalidOperationException("The Functions Router returned no data.");
            return result[2..];
        }

        private static string Word(string hex, int index) => hex.Substring(index * 64, 64);
        private static BigInteger Uint(string word) => BigInteger.Parse("0" + word, NumberStyles.HexNumber);
        private static string Address(string word) => "0x" + word[24..];

        internal sealed record Subscription(string Id, BigInteger Balance, string Owner, BigInteger BlockedBalance, string ProposedOwner, IReadOnlyList<string> Consumers, string Flags);

        /// <summary>Decodes getSubscription's ABI tuple (uint96 balance, address owner, uint96 blockedBalance, address proposedOwner, address[] consumers, bytes32 flags).</summary>
        internal static Subscription Decode(string id, string hex)
        {
            var tuple = (int)(Uint(Word(hex, 0)) / 32);
            var owner = Address(Word(hex, tuple + 1));
            var consumersAt = tuple + (int)(Uint(Word(hex, tuple + 4)) / 32);
            var count = (int)Uint(Word(hex, consumersAt));
            var consumers = new List<string>(count);
            for (var i = 0; i < count; i++) consumers.Add(Address(Word(hex, consumersAt + 1 + i)));
            return new Subscription(id, Uint(Word(hex, tuple)), owner, Uint(Word(hex, tuple + 2)), Address(Word(hex, tuple + 3)), consumers, "0x" + Word(hex, tuple + 5));
        }

        private async Task<Subscription> ReadSubscriptionAsync(string subscriptionId)
        {
            if (!ulong.TryParse(subscriptionId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id == 0)
                throw new ArgumentException("A Chainlink Functions subscription id is a positive integer.", nameof(subscriptionId));
            var subscription = Decode(subscriptionId, await CallAsync(GetSubscriptionSelector + id.ToString("x64")));
            if (subscription.Owner == "0x0000000000000000000000000000000000000000")
                throw new KeyNotFoundException($"Chainlink Functions subscription {subscriptionId} does not exist.");
            return subscription;
        }

        private static void Describe(Dictionary<string, object> metaData, Subscription s, string router)
        {
            metaData["subscription_id"] = s.Id;
            metaData["balance"] = s.Balance.ToString(CultureInfo.InvariantCulture); // LINK juels
            metaData["blocked_balance"] = s.BlockedBalance.ToString(CultureInfo.InvariantCulture);
            metaData["owner"] = s.Owner;
            metaData["proposed_owner"] = s.ProposedOwner;
            metaData["consumers"] = JsonSerializer.Serialize(s.Consumers);
            metaData["flags"] = s.Flags;
            metaData["router"] = router;
        }

        private static OASISResult<T> Unsupported<T>(string message)
        {
            var r = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref r, message);
            return r;
        }

        private const string RequestsAreEvents = "Chainlink Functions requests exist only as Functions Router events; this adapter reads subscriptions, not requests.";
        private const string ReadOnly = "ChainlinkFunctionsOASIS is read-only: subscriptions and requests change only through LINK-funded on-chain transactions.";

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                var hex = await CallAsync(TypeAndVersionSelector);
                var length = (int)Uint(Word(hex, 1));
                var name = Encoding.UTF8.GetString(Convert.FromHexString(hex.Substring(128, length * 2)));
                if (!name.StartsWith("Functions Router", StringComparison.Ordinal))
                {
                    OASISErrorHandling.HandleError(ref r, $"{_router} is not a Chainlink Functions Router ({name}).");
                    return r;
                }
                IsProviderActivated = true;
                r.Result = true;
                r.Message = $"ChainlinkFunctionsOASIS activated against {name}.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions activation failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool> { Result = true, Message = "ChainlinkFunctionsOASIS deactivated." });
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        /// <summary>Number of subscriptions the router has created.</summary>
        public async Task<OASISResult<ulong>> GetSubscriptionCountAsync()
        {
            var r = new OASISResult<ulong>();
            try { r.Result = (ulong)Uint(Word(await CallAsync(GetSubscriptionCountSelector), 0)); }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions subscription count failed: {ex.Message}", ex); }
            return r;
        }

        // Avatar = Chainlink Functions subscription
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string subscriptionId, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var s = await ReadSubscriptionAsync(subscriptionId);
                var avatar = new Avatar { Username = s.Id };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ChainlinkFunctionsOASIS] = s.Id;
                Describe(avatar.MetaData, s, _router);
                r.Result = avatar;
                r.IsLoaded = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions LoadAvatarByProviderKey failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => LoadAvatarByProviderKeyAsync(u, v);
        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) => Task.FromResult(Unsupported<IAvatar>("Chainlink subscriptions are identified by subscription id, not OASIS GUID."));
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) => Task.FromResult(Unsupported<IAvatar>("Chainlink Functions subscriptions have no email."));
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) => Task.FromResult(Unsupported<IEnumerable<IAvatar>>("Load subscriptions by id; GetSubscriptionCountAsync gives the id range."));
        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) => Task.FromResult(Unsupported<IAvatar>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) => Task.FromResult(Unsupported<bool>(ReadOnly));
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) => Task.FromResult(Unsupported<IAvatarDetail>("Chainlink subscription detail requires a subscription id, not OASIS GUID."));
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) => Task.FromResult(Unsupported<IAvatarDetail>("Chainlink Functions subscriptions have no email."));

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string subscriptionId, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var s = await ReadSubscriptionAsync(subscriptionId);
                var detail = new AvatarDetail { Username = s.Id };
                Describe(detail.MetaData, s, _router);
                r.Result = detail;
                r.IsLoaded = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions LoadAvatarDetailByUsername failed: {ex.Message}", ex); }
            return r;
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) => Task.FromResult(Unsupported<IAvatarDetail>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) => Task.FromResult(Unsupported<IEnumerable<IAvatarDetail>>("Load subscription details by id."));

        // Requests are router events, not readable state.
        public override Task<OASISResult<IHolon>> LoadHolonAsync(string requestId, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IHolon>(RequestsAreEvents));
        public override Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IHolon>(RequestsAreEvents));
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) => Task.FromResult(Unsupported<IHolon>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(RequestsAreEvents));
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => Task.FromResult(Unsupported<IHolon>(ReadOnly));
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string k) => Task.FromResult(Unsupported<IHolon>(ReadOnly));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(RequestsAreEvents));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string subscriptionId, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(RequestsAreEvents));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(RequestsAreEvents));
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(RequestsAreEvents));

        /// <summary>Search by subscription id (FilterByMetaData["query"]).</summary>
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            var query = sp?.FilterByMetaData?.ContainsKey("query") == true ? sp.FilterByMetaData["query"] : "";
            if (string.IsNullOrEmpty(query)) { OASISErrorHandling.HandleError(ref r, "Search needs a subscription id in FilterByMetaData[\"query\"]."); return r; }
            var subscription = await LoadAvatarByProviderKeyAsync(query);
            if (subscription.IsError) { OASISErrorHandling.HandleError(ref r, subscription.Message); return r; }
            r.Result = new SearchResults { SearchResultAvatars = new List<IAvatar> { subscription.Result } };
            return r;
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(RequestsAreEvents));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string subscriptionId, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(RequestsAreEvents));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(RequestsAreEvents));
        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) => Task.FromResult(Unsupported<IEnumerable<IHolon>>(RequestsAreEvents));
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
