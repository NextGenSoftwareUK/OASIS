using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

namespace NextGenSoftware.OASIS.API.Providers.ReservoirOASIS
{
    public class ReservoirOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;

        public ReservoirOASIS(string apiKey = "")
        {
            _http = new HttpClient { BaseAddress = new Uri("https://api.reservoir.tools/") };
            if (!string.IsNullOrEmpty(apiKey))
                _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
            ProviderName = "ReservoirOASIS";
            ProviderDescription = "Reservoir NFT aggregation and marketplace API provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ReservoirOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
        }

        private async Task<JObject> GetAsync(string path)
        {
            var resp = await _http.GetAsync(path);
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }
        private async Task<JObject> PostAsync(string path, object body)
        {
            var resp = await _http.PostAsync(path, new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try { var res = await GetAsync("tokens/v7?limit=1"); r.Result = res["tokens"] != null; r.Message = "ReservoirOASIS activated"; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Reservoir activation failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _http.Dispose(); return new OASISResult<bool> { Result = true }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = user's NFT portfolio
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var result = await GetAsync($"users/{address}/tokens/v10?limit=20");
                var tokens = result["tokens"] as JArray ?? new JArray();
                var avatar = new Avatar { Username = address };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReservoirOASIS] = address;
                avatar.MetaData["token_count"] = tokens.Count.ToString();
                avatar.MetaData["tokens"] = tokens.ToString(Formatting.None);
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Reservoir LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        // Holon = individual NFT token
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var result = await GetAsync($"tokens/v7?tokens={Uri.EscapeDataString(key)}");
                var token = result["tokens"]?.First?["token"];
                var holon = new Holon { Name = token?["name"]?.ToString() ?? key };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReservoirOASIS] = key;
                holon.MetaData["collection"] = token?["collection"]?["name"]?.ToString() ?? "";
                holon.MetaData["description"] = token?["description"]?.ToString() ?? "";
                holon.MetaData["image"] = token?["image"]?.ToString() ?? "";
                holon.MetaData["owner"] = token?["owner"]?.ToString() ?? "";
                holon.MetaData["last_buy_value"] = result["tokens"]?.First?["market"]?["lastBuy"]?["value"]?.ToString() ?? "0";
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Reservoir LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        // SaveHolon = list NFT for sale
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var tokenKey = h.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.ReservoirOASIS)
                    ? h.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReservoirOASIS] : "";
                var body = new
                {
                    maker = h.MetaData.ContainsKey("maker") ? h.MetaData["maker"]?.ToString() : "",
                    source = "oasis",
                    params_ = new[] { new { orderKind = "seaport-v1.5", orderbook = "reservoir", token = tokenKey, weiPrice = h.MetaData.ContainsKey("price_wei") ? h.MetaData["price_wei"]?.ToString() : "0" } }
                };
                var result = await PostAsync("execute/list/v5", body);
                if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
                r.Result = h;
                r.Message = result["steps"]?.ToString() ?? "Listing created";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Reservoir SaveHolon failed: {ex.Message}", ex); }
            return r;
        }

        // LoadHolonsForParent = user's tokens
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var result = await GetAsync($"users/{k}/tokens/v10?limit=50");
                var tokens = result["tokens"] as JArray ?? new JArray();
                var holons = new List<IHolon>();
                foreach (var item in tokens)
                {
                    var token = item["token"];
                    var holon = new Holon { Name = token?["name"]?.ToString() ?? token?["tokenId"]?.ToString() ?? "" };
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReservoirOASIS] = $"{token?["contract"]}:{token?["tokenId"]}";
                    holon.MetaData["collection"] = token?["collection"]?["name"]?.ToString() ?? "";
                    holon.MetaData["image"] = token?["image"]?.ToString() ?? "";
                    holons.Add(holon);
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Reservoir LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var result = await GetAsync("tokens/v7?sortBy=floorAskPrice&sortDirection=asc&limit=50");
                var tokens = result["tokens"] as JArray ?? new JArray();
                var holons = new List<IHolon>();
                foreach (var item in tokens)
                {
                    var token = item["token"];
                    var holon = new Holon { Name = token?["name"]?.ToString() ?? $"{token?["collection"]?["name"]} #{token?["tokenId"]}" };
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReservoirOASIS] = $"{token?["contract"]}:{token?["tokenId"]}";
                    holon.MetaData["collection"] = token?["collection"]?["name"]?.ToString() ?? "";
                    holon.MetaData["floor_price"] = item["market"]?["floorAsk"]?["price"]?["amount"]?["decimal"]?.ToString() ?? "0";
                    holons.Add(holon);
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Reservoir LoadAllHolons failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var q = sp?.FilterByMetaData?.ContainsKey("query") == true ? sp.FilterByMetaData["query"] : "";
                var result = await GetAsync($"tokens/v7?name={Uri.EscapeDataString(q)}&limit=20");
                var tokens = result["tokens"] as JArray ?? new JArray();
                var holons = new List<IHolon>();
                foreach (var item in tokens)
                {
                    var token = item["token"];
                    var holon = new Holon { Name = token?["name"]?.ToString() ?? "" };
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReservoirOASIS] = $"{token?["contract"]}:{token?["tokenId"]}";
                    holon.MetaData["collection"] = token?["collection"]?["name"]?.ToString() ?? "";
                    holons.Add(holon);
                }
                r.Result = new SearchResults { SearchResultHolons = holons };
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, ex.Message, ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0)
        {
            return await LoadHolonsForParentAsync(u);
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var r = new OASISResult<bool>();
            try { foreach (var h in holons) await SaveHolonAsync(h); r.Result = true; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Reservoir ImportAsync failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it has no record keyed by an OASIS holon Guid. Load by provider key instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it has no record keyed by an OASIS avatar Guid. Load by provider key (e.g. wallet address) instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => await LoadAvatarByProviderKeyAsync(u, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "Reservoir does not support email-based lookup."); return r; }
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it cannot save or update avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it cannot save or update avatar details."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it has no OASIS avatar index to list."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) { var r = new OASISResult<IEnumerable<IHolon>>(); var l = new List<IHolon>(); foreach (var h in holons) { var sr = await SaveHolonAsync(h, sc, rec, md, coe, scop); if (sr.IsError) { OASISErrorHandling.HandleError(ref r, $"Error saving holon {h.Id}: {sr.Message}"); if (!coe) return r; } else l.Add(sr.Result); } r.Result = l; return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; holons cannot be looked up by OASIS parent Guid. Use LoadHolonsForParentAsync with a provider key."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { if (mk == "owner") return await LoadHolonsForParentAsync(mv, t, lc, rec, md, cd, coe, lcfp, v); return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it has no holon metadata index."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it cannot delete holons."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "Reservoir: NFT listings are cancelled via execute/cancel API, not direct deletion."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it has no OASIS avatar index, so avatar data cannot be exported by id or email."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "ReservoirOASIS is an NFT market data and trading API; it has no OASIS avatar index, so avatar data cannot be exported by id or email."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Reservoir requires a wallet address to export data."); return r; }
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int v = 0) => LoadAvatarAsync(id, v).Result;
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string k, int v = 0) => LoadAvatarByProviderKeyAsync(k, v).Result;
        public override OASISResult<IAvatar> LoadAvatarByUsername(string u, int v = 0) => LoadAvatarByUsernameAsync(u, v).Result;
        public override OASISResult<IAvatar> SaveAvatar(IAvatar a) => SaveAvatarAsync(a).Result;
        public override OASISResult<bool> DeleteAvatar(Guid id, bool s = true) => DeleteAvatarAsync(id, s).Result;
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int v = 0) => LoadAllAvatarsAsync(v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int v = 0) => LoadAvatarDetailAsync(id, v).Result;
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail ad) => SaveAvatarDetailAsync(ad).Result;
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int v = 0) => LoadAllAvatarDetailsAsync(v).Result;
        public override OASISResult<IHolon> LoadHolon(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonAsync(id, lc, rec, md, coe, lcfp, v).Result;
        public override OASISResult<IHolon> LoadHolon(string k, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonAsync(k, lc, rec, md, coe, lcfp, v).Result;
        public override OASISResult<IHolon> SaveHolon(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) => SaveHolonAsync(h, sc, rec, md, coe, scop).Result;
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> h, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) => SaveHolonsAsync(h, sc, rec, md, cd, coe, scop).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadAllHolonsAsync(ht, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<ISearchResults> Search(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0) => SearchAsync(sp, lc, rec, md, coe, v).Result;
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int v = 0) => ExportAllDataForAvatarByIdAsync(id, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int v = 0) => ExportAllDataForAvatarByUsernameAsync(u, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int v = 0) => ExportAllDataForAvatarByEmailAsync(e, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int v = 0) => ExportAllAsync(v).Result;
        public override OASISResult<bool> DeleteAvatar(string k, bool s = true) => DeleteAvatarAsync(k, s).Result;
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool s = true) => DeleteAvatarByEmailAsync(e, s).Result;
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool s = true) => DeleteAvatarByUsernameAsync(u, s).Result;
        public override OASISResult<IAvatar> LoadAvatarByEmail(string e, int v = 0) => LoadAvatarByEmailAsync(e, v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int v = 0) => LoadAvatarDetailByEmailAsync(e, v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int v = 0) => LoadAvatarDetailByUsernameAsync(u, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsForParentAsync(id, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsForParentAsync(k, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsByMetaDataAsync(mk, mv, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsByMetaDataAsync(m, mm, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IHolon> DeleteHolon(string k) { var r = DeleteHolonAsync(k).Result; return new OASISResult<IHolon> { IsError = r.IsError, Message = r.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> h) => ImportAsync(h).Result;
    }
}
