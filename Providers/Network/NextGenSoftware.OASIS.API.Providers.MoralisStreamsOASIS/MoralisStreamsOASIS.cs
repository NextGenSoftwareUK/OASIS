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

namespace NextGenSoftware.OASIS.API.Providers.MoralisStreamsOASIS
{
    public class MoralisStreamsOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;

        public MoralisStreamsOASIS(string apiKey)
        {
            _http = new HttpClient { BaseAddress = new Uri("https://api.moralis-streams.com/") };
            _http.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            ProviderName = "MoralisStreamsOASIS";
            ProviderDescription = "Moralis Streams webhook event streaming provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.MoralisStreamsOASIS);
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
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(path, content);
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }
        private async Task<JObject> PutAsync(string path, object body)
        {
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var resp = await _http.PutAsync(path, content);
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try { var res = await GetAsync("beta/streams?limit=1"); r.Result = res["result"] != null; r.Message = "MoralisStreamsOASIS activated"; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"MoralisStreams activation failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _http.Dispose(); return new OASISResult<bool> { Result = true }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = summary of all streams (treating the account's stream set as an "avatar")
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var result = await GetAsync("beta/streams?limit=20");
                var streams = result["result"] as JArray ?? new JArray();
                var avatar = new Avatar { Username = address };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MoralisStreamsOASIS] = address;
                avatar.MetaData["stream_count"] = streams.Count.ToString();
                avatar.MetaData["streams"] = streams.ToString(Formatting.None);
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"MoralisStreams LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        // Holon = a single stream by ID
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var result = await GetAsync($"beta/streams/{key}");
                var holon = new Holon { Name = result["tag"]?.ToString() ?? result["description"]?.ToString() ?? key };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.MoralisStreamsOASIS] = key;
                holon.MetaData["status"] = result["status"]?.ToString() ?? "";
                holon.MetaData["webhook_url"] = result["webhookUrl"]?.ToString() ?? "";
                holon.MetaData["chains"] = result["chainIds"]?.ToString(Formatting.None) ?? "[]";
                holon.MetaData["addresses"] = result["allAddresses"]?.ToString(Formatting.None) ?? "[]";
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"MoralisStreams LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        // SaveHolon = create or update a stream
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var existingId = h.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.MoralisStreamsOASIS)
                    ? h.ProviderUniqueStorageKey[Core.Enums.ProviderType.MoralisStreamsOASIS] : null;
                var body = new
                {
                    webhookUrl = h.MetaData.ContainsKey("webhook_url") ? h.MetaData["webhook_url"] : "",
                    description = h.Description ?? h.Name ?? "",
                    tag = h.Name ?? "OASIS Stream",
                    chainIds = h.MetaData.ContainsKey("chains") ? JsonConvert.DeserializeObject(h.MetaData["chains"]?.ToString() ?? "[]") : new[] { "0x1" },
                    includeNativeTxs = true
                };
                JObject result;
                if (!string.IsNullOrEmpty(existingId))
                    result = await PutAsync($"beta/streams/{existingId}", body);
                else
                    result = await PostAsync("beta/streams", body);
                if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
                h.ProviderUniqueStorageKey[Core.Enums.ProviderType.MoralisStreamsOASIS] = result["id"]?.ToString() ?? existingId ?? "";
                r.Result = h;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"MoralisStreams SaveHolon failed: {ex.Message}", ex); }
            return r;
        }

        // LoadHolonsForParent = all streams (filtered by tag/address if available)
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var result = await GetAsync($"beta/streams?limit=100");
                var streams = result["result"] as JArray ?? new JArray();
                var holons = new List<IHolon>();
                foreach (var stream in streams)
                {
                    var addresses = stream["allAddresses"] as JArray ?? new JArray();
                    bool match = string.IsNullOrEmpty(k) || stream["tag"]?.ToString() == k;
                    if (!match) foreach (var addr in addresses) if (addr.ToString().ToLower() == k.ToLower()) { match = true; break; }
                    if (!match) continue;
                    var holon = new Holon { Name = stream["tag"]?.ToString() ?? stream["id"]?.ToString() ?? "" };
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.MoralisStreamsOASIS] = stream["id"]?.ToString() ?? "";
                    holon.MetaData["status"] = stream["status"]?.ToString() ?? "";
                    holon.MetaData["webhook_url"] = stream["webhookUrl"]?.ToString() ?? "";
                    holons.Add(holon);
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"MoralisStreams LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        // LoadAllHolons = all streams
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var result = await GetAsync("beta/streams?limit=100");
                var streams = result["result"] as JArray ?? new JArray();
                var holons = new List<IHolon>();
                foreach (var stream in streams)
                {
                    var holon = new Holon { Name = stream["tag"]?.ToString() ?? stream["id"]?.ToString() ?? "" };
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.MoralisStreamsOASIS] = stream["id"]?.ToString() ?? "";
                    holon.MetaData["status"] = stream["status"]?.ToString() ?? "";
                    holon.MetaData["webhook_url"] = stream["webhookUrl"]?.ToString() ?? "";
                    holon.MetaData["chains"] = stream["chainIds"]?.ToString(Formatting.None) ?? "[]";
                    holons.Add(holon);
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"MoralisStreams LoadAllHolons failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var q = sp?.FilterByMetaData?.ContainsKey("query") == true ? sp.FilterByMetaData["query"] : "";
                var holons = new List<IHolon>();
                var allResult = await LoadAllHolonsAsync();
                if (!allResult.IsError && allResult.Result != null)
                    foreach (var h in allResult.Result)
                        if (string.IsNullOrEmpty(q) || h.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) == true)
                            holons.Add(h);
                r.Result = new SearchResults { SearchResultHolons = holons };
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, ex.Message, ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0)
        {
            return await LoadAllHolonsAsync();
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var r = new OASISResult<bool>();
            try { foreach (var h in holons) await SaveHolonAsync(h); r.Result = true; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"MoralisStreams ImportAsync failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); it has no record keyed by an OASIS holon Guid. Load by provider key instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); it has no record keyed by an OASIS avatar Guid. Load by provider key (e.g. wallet address) instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => await LoadAvatarByProviderKeyAsync(u, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "MoralisStreams does not support email-based lookup."); return r; }
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); it cannot save or update avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); it cannot delete avatars."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); it cannot save or update avatar details."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); it has no OASIS avatar index to list."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) { var r = new OASISResult<IEnumerable<IHolon>>(); var l = new List<IHolon>(); foreach (var h in holons) { var sr = await SaveHolonAsync(h, sc, rec, md, coe, scop); if (sr.IsError) { OASISErrorHandling.HandleError(ref r, $"Error saving holon {h.Id}: {sr.Message}"); if (!coe) return r; } else l.Add(sr.Result); } r.Result = l; return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); holons cannot be looked up by OASIS parent Guid. Use LoadHolonsForParentAsync with a provider key."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { if (mk == "tag") return await LoadHolonsForParentAsync(mv, t, lc, rec, md, cd, coe, lcfp, v); return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); it has no holon metadata index."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "MoralisStreamsOASIS is a blockchain event-stream service (streams, not avatar or holon storage); it cannot delete holons."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k)
        {
            var r = new OASISResult<IHolon>();
            try { await _http.DeleteAsync($"beta/streams/{k}"); r.Result = new Holon(); r.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.MoralisStreamsOASIS] = k; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"MoralisStreams DeleteHolon failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) => await LoadAllHolonsAsync();
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) => await LoadAllHolonsAsync();
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) => await LoadAllHolonsAsync();
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
        public override OASISResult<IHolon> DeleteHolon(string k) => DeleteHolonAsync(k).Result;
        public override OASISResult<bool> Import(IEnumerable<IHolon> h) => ImportAsync(h).Result;
    }
}
