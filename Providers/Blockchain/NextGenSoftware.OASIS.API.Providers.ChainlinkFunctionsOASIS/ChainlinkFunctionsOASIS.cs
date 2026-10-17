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

namespace NextGenSoftware.OASIS.API.Providers.ChainlinkFunctionsOASIS
{
    public class ChainlinkFunctionsOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;

        public ChainlinkFunctionsOASIS(string baseUrl = "https://functions-gateway.chain.link/")
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
            ProviderName = "ChainlinkFunctionsOASIS";
            ProviderDescription = "Chainlink Functions serverless compute and oracle subscription provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ChainlinkFunctionsOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain);
        }

        private async Task<JObject> GetAsync(string path)
        {
            var resp = await _http.GetAsync(path);
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        private async Task<JObject> PostAsync(string path, object body)
        {
            var resp = await _http.PostAsync(path,
                new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                await GetAsync("health");
                r.Result = true;
                r.Message = "ChainlinkFunctionsOASIS activated.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions activation failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _http.Dispose();
            return new OASISResult<bool> { Result = true, Message = "ChainlinkFunctionsOASIS deactivated." };
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = Chainlink Functions subscription
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string subscriptionId, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var json = await GetAsync($"subscriptions/{subscriptionId}");
                var avatar = new Avatar { Username = subscriptionId };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ChainlinkFunctionsOASIS] = subscriptionId;
                avatar.MetaData["subscription_id"] = json["subscriptionId"]?.ToString() ?? subscriptionId;
                avatar.MetaData["balance"] = json["balance"]?.ToString() ?? "0";
                avatar.MetaData["owner"] = json["owner"]?.ToString() ?? "";
                avatar.MetaData["consumers"] = json["consumers"]?.ToString(Formatting.None) ?? "[]";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions LoadAvatarByProviderKey failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => await LoadAvatarByProviderKeyAsync(u, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "Chainlink subscriptions are identified by subscription ID, not OASIS GUID."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "Chainlink Functions does not support email-based lookup."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "Chainlink Functions does not support bulk subscription enumeration."); return r; }
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "Chainlink subscription creation requires on-chain transaction with LINK tokens."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Chainlink subscriptions cannot be deleted via REST API."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Chainlink subscriptions cannot be deleted via REST API."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Chainlink does not support email-based deletion."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Chainlink subscriptions cannot be deleted via REST API."); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Chainlink avatar detail requires a subscription ID, not OASIS GUID."); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Chainlink Functions does not support email-based lookup."); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string subscriptionId, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await GetAsync($"subscriptions/{subscriptionId}");
                var detail = new AvatarDetail { Username = subscriptionId };
                detail.MetaData["subscription_id"] = json["subscriptionId"]?.ToString() ?? subscriptionId;
                detail.MetaData["balance"] = json["balance"]?.ToString() ?? "0";
                detail.MetaData["owner"] = json["owner"]?.ToString() ?? "";
                detail.MetaData["consumers"] = json["consumers"]?.ToString(Formatting.None) ?? "[]";
                detail.MetaData["router"] = json["router"]?.ToString() ?? "";
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions LoadAvatarDetailByUsername failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Chainlink subscription configuration is managed on-chain."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "Chainlink does not support bulk subscription detail enumeration."); return r; }

        // Holon = Chainlink Functions request/job
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string requestId, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var json = await GetAsync($"requests/{requestId}");
                var holon = new Holon { Name = $"Chainlink Request {requestId[..Math.Min(8, requestId.Length)]}..." };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ChainlinkFunctionsOASIS] = requestId;
                holon.MetaData["request_id"] = requestId;
                holon.MetaData["status"] = json["status"]?.ToString() ?? "";
                holon.MetaData["response"] = json["response"]?.ToString() ?? "";
                holon.MetaData["error"] = json["error"]?.ToString() ?? "";
                holon.MetaData["subscription_id"] = json["subscriptionId"]?.ToString() ?? "";
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "Chainlink requests are identified by request ID string, not OASIS GUID."); return r; }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
                var body = new
                {
                    source = h.MetaData.ContainsKey("source") ? h.MetaData["source"]?.ToString() : "return Functions.encodeString('hello')",
                    subscriptionId = h.MetaData.ContainsKey("subscription_id") ? h.MetaData["subscription_id"]?.ToString() : "1",
                    gasLimit = 300000
                };
                var json = await PostAsync("requests", body);
                h.ProviderUniqueStorageKey[Core.Enums.ProviderType.ChainlinkFunctionsOASIS] = json["requestId"]?.ToString() ?? h.Id.ToString();
                h.MetaData["request_id"] = json["requestId"]?.ToString() ?? "";
                r.Result = h;
                r.Message = $"Chainlink Functions request submitted: {json["requestId"]}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions SaveHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false)
        {
            var results = new List<IHolon>();
            var r = new OASISResult<IEnumerable<IHolon>>();
            foreach (var h in holons) { var sr = await SaveHolonAsync(h); if (sr.Result != null) results.Add(sr.Result); else if (!coe) { OASISErrorHandling.HandleError(ref r, sr.Message); return r; } }
            r.Result = results;
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Chainlink Functions does not support listing all requests. Use LoadHolon(requestId) for a specific request."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "Chainlink Functions requests are immutable on-chain."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "Chainlink Functions requests are immutable on-chain."); return r; }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Use LoadHolonsForParent(string subscriptionId) to load requests for a subscription."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string subscriptionId, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var json = await GetAsync($"subscriptions/{subscriptionId}/requests");
                var reqs = json["requests"] as JArray ?? new JArray();
                var holons = new List<IHolon>();
                if (reqs != null)
                    foreach (var req in reqs)
                    {
                        var h = new Holon { Name = $"Chainlink Request {req["requestId"]?.ToString()}" };
                        h.MetaData["request_id"] = req["requestId"]?.ToString() ?? "";
                        h.MetaData["status"] = req["status"]?.ToString() ?? "";
                        h.MetaData["subscription_id"] = subscriptionId;
                        h.ProviderUniqueStorageKey[Core.Enums.ProviderType.ChainlinkFunctionsOASIS] = req["requestId"]?.ToString() ?? "";
                        holons.Add(h);
                    }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            if (mk == "subscription_id" || mk == "subscriptionId") return await LoadHolonsForParentAsync(mv, t);
            if (mk == "request_id") { var h = await LoadHolonAsync(mv); var r2 = new OASISResult<IEnumerable<IHolon>>(); r2.Result = h.Result != null ? new List<IHolon> { h.Result } : new List<IHolon>(); r2.IsError = h.IsError; r2.Message = h.Message; return r2; }
            var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"Chainlink Functions does not support metadata search by '{mk}'. Supported: subscription_id, request_id."); return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        { foreach (var kv in m) { var p = await LoadHolonsByMetaDataAsync(kv.Key, kv.Value, t); if (!p.IsError && p.Result != null) return p; } return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var results = new SearchResults();
                string query = sp?.FilterByMetaData?.ContainsKey("query") == true ? sp.FilterByMetaData["query"] : "";
                if (!string.IsNullOrEmpty(query))
                {
                    try { var sub = await LoadAvatarByProviderKeyAsync(query); if (!sub.IsError) results.SearchResultAvatars = new List<IAvatar> { sub.Result }; } catch { }
                    try { var req = await LoadHolonAsync(query); if (!req.IsError) results.SearchResultHolons = new List<IHolon> { req.Result }; } catch { }
                }
                r.Result = results;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions Search failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Chainlink export requires a subscription ID string."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string subscriptionId, int v = 0) => await LoadHolonsForParentAsync(subscriptionId);
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Chainlink Functions does not support email-based export."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Chainlink Functions does not support full data export."); return r; }
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h) { var r2 = new OASISResult<bool>(); try { foreach (var holon in h) { var s = await SaveHolonAsync(holon); if (s.IsError) { OASISErrorHandling.HandleError(ref r2, s.Message); return r2; } } r2.Result = true; } catch (Exception ex) { OASISErrorHandling.HandleError(ref r2, ex.Message, ex); } return r2; }

        // Sync wrappers
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
