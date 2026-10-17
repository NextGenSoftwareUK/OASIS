using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
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

namespace NextGenSoftware.OASIS.API.Providers.DuneAnalyticsOASIS
{
    public class DuneAnalyticsOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;

        public DuneAnalyticsOASIS(string apiKey)
        {
            _http = new HttpClient { BaseAddress = new Uri("https://api.dune.com/api/v1/") };
            _http.DefaultRequestHeaders.Add("X-Dune-Api-Key", apiKey);
            ProviderName = "DuneAnalyticsOASIS";
            ProviderDescription = "Dune Analytics blockchain data query and execution provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.DuneAnalyticsOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
        }

        private async Task<JObject> PostAsync(string path, object body = null)
        {
            var content = body != null ? new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json") : new StringContent("{}", Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(path, content);
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }
        private async Task<JObject> GetAsync(string path)
        {
            var resp = await _http.GetAsync(path);
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        private async Task<JObject> ExecuteAndPollAsync(string queryId, int maxWaitSeconds = 60)
        {
            var execResult = await PostAsync($"query/{queryId}/execute");
            var execId = execResult["execution_id"]?.ToString();
            if (string.IsNullOrEmpty(execId)) throw new Exception("No execution_id returned");
            var deadline = DateTime.UtcNow.AddSeconds(maxWaitSeconds);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(3000);
                var status = await GetAsync($"execution/{execId}/status");
                var state = status["state"]?.ToString();
                if (state == "QUERY_STATE_COMPLETED") return await GetAsync($"execution/{execId}/results");
                if (state == "QUERY_STATE_FAILED") throw new Exception($"Dune query failed: {status["error"]?.ToString()}");
            }
            throw new TimeoutException($"Dune query {queryId} timed out after {maxWaitSeconds}s");
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try { var res = await GetAsync("echo/test"); r.Result = res != null; r.Message = "DuneAnalyticsOASIS activated"; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Dune activation failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _http.Dispose(); return new OASISResult<bool> { Result = true }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = list of all Dune queries for this API key (no direct per-user wallet endpoint)
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string userId, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var result = await GetAsync($"query/");
                var avatar = new Avatar { Username = userId };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.DuneAnalyticsOASIS] = userId;
                avatar.MetaData["queries"] = result["result"]?["rows"]?.ToString(Formatting.None) ?? "[]";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Dune LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        // Holon = Dune query (by query ID) or query result
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var holon = new Holon();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.DuneAnalyticsOASIS] = key;
                if (key.All(char.IsDigit))
                {
                    var results = await ExecuteAndPollAsync(key);
                    var rows = results["result"]?["rows"] as JArray ?? new JArray();
                    holon.Name = $"Dune Query {key}";
                    holon.MetaData["row_count"] = rows.Count.ToString();
                    holon.MetaData["rows"] = rows.ToString(Formatting.None);
                    holon.MetaData["metadata"] = results["result"]?["metadata"]?.ToString(Formatting.None) ?? "{}";
                }
                else
                {
                    var status = await GetAsync($"execution/{key}/status");
                    holon.Name = $"Dune Execution {key}";
                    holon.MetaData["state"] = status["state"]?.ToString() ?? "";
                    holon.MetaData["query_id"] = status["query_id"]?.ToString() ?? "";
                }
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Dune LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        // SaveHolon = create a new Dune query
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var sql = h.MetaData.ContainsKey("sql") ? h.MetaData["sql"]?.ToString() : $"SELECT * FROM ethereum.transactions LIMIT 10";
                var body = new { name = h.Name ?? "OASIS Query", query_sql = sql, description = h.Description ?? "" };
                var result = await PostAsync("query/", body);
                h.Id = h.Id == Guid.Empty ? Guid.NewGuid() : h.Id;
                h.ProviderUniqueStorageKey[Core.Enums.ProviderType.DuneAnalyticsOASIS] = result["query_id"]?.ToString() ?? "";
                r.Result = h;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Dune SaveHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // k = query ID; execute the query and return each row as a holon
                if (k.All(char.IsDigit))
                {
                    var results = await ExecuteAndPollAsync(k);
                    var rows = results["result"]?["rows"] as JArray ?? new JArray();
                    var holons = new List<IHolon>();
                    for (int i = 0; i < rows.Count; i++)
                    {
                        var holon = new Holon { Name = $"Row {i}" };
                        foreach (var prop in (rows[i] as JObject)?.Properties() ?? System.Linq.Enumerable.Empty<JProperty>())
                            holon.MetaData[prop.Name] = prop.Value.ToString();
                        holons.Add(holon);
                    }
                    r.Result = holons;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref r, "Dune LoadHolonsForParent expects a numeric query ID.");
                }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Dune LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Dune Analytics requires a query ID to load holons; use LoadHolonsForParentAsync with a numeric query ID.");
            return r;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var q = sp?.FilterByMetaData?.ContainsKey("query_id") == true ? sp.FilterByMetaData["query_id"] : "";
                var holons = new List<IHolon>();
                if (!string.IsNullOrEmpty(q))
                {
                    var hr = await LoadHolonAsync(q);
                    if (!hr.IsError && hr.Result != null) holons.Add(hr.Result);
                }
                r.Result = new SearchResults { SearchResultHolons = holons };
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, ex.Message, ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Dune Analytics requires a numeric query ID to export data; use LoadHolonsForParentAsync with a query ID.");
            return r;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var r = new OASISResult<bool>();
            try
            {
                foreach (var h in holons)
                    await SaveHolonAsync(h);
                r.Result = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Dune ImportAsync failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it has no record keyed by an OASIS holon Guid. Load by provider key instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it has no record keyed by an OASIS avatar Guid. Load by provider key (e.g. wallet address) instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => await LoadAvatarByProviderKeyAsync(u, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "Dune does not support email-based lookup."); return r; }
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it cannot save or update avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it cannot delete avatars."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "Dune does not support listing all avatars."); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it cannot save or update avatar details."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it has no OASIS avatar index to list."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) { var r = new OASISResult<IEnumerable<IHolon>>(); var l = new List<IHolon>(); foreach (var h in holons) { var sr = await SaveHolonAsync(h, sc, rec, md, coe, scop); if (sr.IsError) { OASISErrorHandling.HandleError(ref r, $"Error saving holon {h.Id}: {sr.Message}"); if (!coe) return r; } else l.Add(sr.Result); } r.Result = l; return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); holons cannot be looked up by OASIS parent Guid. Use LoadHolonsForParentAsync with a provider key."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { if (mk == "query_id") return await LoadHolonsForParentAsync(mv, t, lc, rec, md, cd, coe, lcfp, v); return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it has no holon metadata index."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it cannot delete holons."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "Dune Analytics does not support individual holon deletion."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it has no OASIS avatar index, so avatar data cannot be exported by id or email."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "DuneAnalyticsOASIS is an on-chain SQL analytics API (queries, not avatar or holon storage); it has no OASIS avatar index, so avatar data cannot be exported by id or email."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Dune Analytics requires a query ID to export data."); return r; }
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
