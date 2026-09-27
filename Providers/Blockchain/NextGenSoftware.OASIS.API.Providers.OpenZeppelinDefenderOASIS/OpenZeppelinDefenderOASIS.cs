using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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

namespace NextGenSoftware.OASIS.API.Providers.OpenZeppelinDefenderOASIS
{
    public class OpenZeppelinDefenderOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _apiSecret;

        public OpenZeppelinDefenderOASIS(string apiKey = "", string apiSecret = "",
            string baseUrl = "https://defender-api.openzeppelin.com")
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
            if (!string.IsNullOrEmpty(apiKey))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
            ProviderName = "OpenZeppelinDefenderOASIS";
            ProviderDescription = "OpenZeppelin Defender smart contract security and automation operations provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.OpenZeppelinDefenderOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain);
        }

        private async Task<JToken> GetAsync(string path)
        {
            var resp = await _http.GetAsync(path);
            resp.EnsureSuccessStatusCode();
            return JToken.Parse(await resp.Content.ReadAsStringAsync());
        }

        private async Task<JToken> PostAsync(string path, object body)
        {
            var resp = await _http.PostAsync(path,
                new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            return JToken.Parse(await resp.Content.ReadAsStringAsync());
        }

        private async Task<JToken> DeleteAsync(string path)
        {
            var resp = await _http.DeleteAsync(path);
            resp.EnsureSuccessStatusCode();
            return JToken.Parse(await resp.Content.ReadAsStringAsync());
        }

        private void EnsureApiKey()
        {
            if (string.IsNullOrEmpty(_apiKey))
                throw new InvalidOperationException("OpenZeppelin Defender requires an API key. Construct OpenZeppelinDefenderOASIS with your apiKey and apiSecret.");
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                EnsureApiKey();
                await GetAsync("autotask/v1/autotasks");
                r.Result = true;
                r.Message = "OpenZeppelinDefenderOASIS activated.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender activation failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _http.Dispose(); return new OASISResult<bool> { Result = true, Message = "OpenZeppelinDefenderOASIS deactivated." }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = Defender autotask or account
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string autotaskId, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync($"autotask/v1/autotasks/{autotaskId}");
                var avatar = new Avatar { Username = autotaskId };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.OpenZeppelinDefenderOASIS] = autotaskId;
                avatar.MetaData["autotask_id"] = autotaskId;
                avatar.MetaData["name"] = json["name"]?.ToString() ?? "";
                avatar.MetaData["status"] = json["status"]?.ToString() ?? "";
                avatar.MetaData["trigger"] = json["trigger"]?.ToString(Formatting.None) ?? "{}";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender LoadAvatarByProviderKey failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => await LoadAvatarByProviderKeyAsync(u, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "Defender autotasks are identified by autotask ID string, not OASIS GUID."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "Defender does not support email-based lookup."); return r; }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync("autotask/v1/autotasks");
                var tasks = (json["items"] as JArray) ?? (json as JArray);
                var avatars = new List<IAvatar>();
                if (tasks != null)
                    foreach (var t in tasks)
                    {
                        var id = t["autotaskId"]?.ToString() ?? "";
                        var avatar = new Avatar { Username = id };
                        avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.OpenZeppelinDefenderOASIS] = id;
                        avatar.MetaData["autotask_id"] = id;
                        avatar.MetaData["name"] = t["name"]?.ToString() ?? "";
                        avatar.MetaData["status"] = t["status"]?.ToString() ?? "";
                        avatars.Add(avatar);
                    }
                r.Result = avatars;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender LoadAllAvatars failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                EnsureApiKey();
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                var body = new
                {
                    name = avatar.Username ?? "OASIS Autotask",
                    trigger = new { type = "schedule", frequencyMinutes = 60 },
                    paused = false
                };
                var json = await PostAsync("autotask/v1/autotasks", body);
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.OpenZeppelinDefenderOASIS] = json["autotaskId"]?.ToString() ?? avatar.Id.ToString();
                avatar.MetaData["autotask_id"] = json["autotaskId"]?.ToString() ?? "";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender SaveAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Use DeleteAvatar(string autotaskId) to delete a Defender autotask."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string autotaskId, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            try
            {
                EnsureApiKey();
                await DeleteAsync($"autotask/v1/autotasks/{autotaskId}");
                r.Result = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender DeleteAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Defender does not support email-based deletion."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string autotaskId, bool s = true) => await DeleteAvatarAsync(autotaskId, s);
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Defender avatar detail requires an autotask ID."); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Defender does not support email-based lookup."); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string autotaskId, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync($"autotask/v1/autotasks/{autotaskId}");
                var detail = new AvatarDetail { Username = autotaskId };
                detail.MetaData["autotask_id"] = autotaskId;
                detail.MetaData["name"] = json["name"]?.ToString() ?? "";
                detail.MetaData["status"] = json["status"]?.ToString() ?? "";
                detail.MetaData["trigger"] = json["trigger"]?.ToString(Formatting.None) ?? "{}";
                detail.MetaData["network"] = json["network"]?.ToString() ?? "";
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender LoadAvatarDetailByUsername failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Use SaveAvatar to create/update Defender autotasks."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync("autotask/v1/autotasks");
                var tasks = (json["items"] as JArray) ?? (json as JArray);
                var details = new List<IAvatarDetail>();
                if (tasks != null)
                    foreach (var t in tasks)
                    {
                        var detail = new AvatarDetail { Username = t["autotaskId"]?.ToString() ?? "" };
                        detail.MetaData["autotask_id"] = t["autotaskId"]?.ToString() ?? "";
                        detail.MetaData["name"] = t["name"]?.ToString() ?? "";
                        detail.MetaData["status"] = t["status"]?.ToString() ?? "";
                        details.Add(detail);
                    }
                r.Result = details;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender LoadAllAvatarDetails failed: {ex.Message}", ex); }
            return r;
        }

        // Holon = Defender proposal (governance/multisig action)
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string proposalId, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync($"approval-process/v1/proposals/{proposalId}");
                var holon = new Holon { Name = json["title"]?.ToString() ?? $"Defender Proposal {proposalId}" };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.OpenZeppelinDefenderOASIS] = proposalId;
                holon.MetaData["proposal_id"] = proposalId;
                holon.MetaData["title"] = json["title"]?.ToString() ?? "";
                holon.MetaData["status"] = json["status"]?.ToString() ?? "";
                holon.MetaData["type"] = json["type"]?.ToString() ?? "";
                holon.MetaData["network"] = json["network"]?.ToString() ?? "";
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "Defender proposals are identified by proposal ID string, not OASIS GUID."); return r; }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync("approval-process/v1/proposals");
                var proposals = (json["items"] as JArray) ?? (json as JArray);
                var holons = new List<IHolon>();
                if (proposals != null)
                    foreach (var p in proposals)
                    {
                        var h = new Holon { Name = p["title"]?.ToString() ?? "" };
                        h.ProviderUniqueStorageKey[Core.Enums.ProviderType.OpenZeppelinDefenderOASIS] = p["proposalId"]?.ToString() ?? "";
                        h.MetaData["proposal_id"] = p["proposalId"]?.ToString() ?? "";
                        h.MetaData["title"] = p["title"]?.ToString() ?? "";
                        h.MetaData["status"] = p["status"]?.ToString() ?? "";
                        holons.Add(h);
                    }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender LoadAllHolons failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                EnsureApiKey();
                if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
                var body = new
                {
                    title = h.Name ?? "OASIS Proposal",
                    type = h.MetaData.ContainsKey("type") ? h.MetaData["type"]?.ToString() : "custom",
                    network = h.MetaData.ContainsKey("network") ? h.MetaData["network"]?.ToString() : "mainnet",
                    via = h.MetaData.ContainsKey("via") ? h.MetaData["via"]?.ToString() : "",
                    viaType = h.MetaData.ContainsKey("via_type") ? h.MetaData["via_type"]?.ToString() : "EOA"
                };
                var json = await PostAsync("approval-process/v1/proposals", body);
                h.ProviderUniqueStorageKey[Core.Enums.ProviderType.OpenZeppelinDefenderOASIS] = json["proposalId"]?.ToString() ?? h.Id.ToString();
                h.MetaData["proposal_id"] = json["proposalId"]?.ToString() ?? "";
                r.Result = h;
                r.Message = $"Defender proposal created: {json["proposalId"]}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender SaveHolon failed: {ex.Message}", ex); }
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

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "Use DeleteHolon(string proposalId) to delete a Defender proposal."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string proposalId)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                EnsureApiKey();
                await DeleteAsync($"approval-process/v1/proposals/{proposalId}");
                var h = new Holon { Name = proposalId };
                h.ProviderUniqueStorageKey[Core.Enums.ProviderType.OpenZeppelinDefenderOASIS] = proposalId;
                r.Result = h;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender DeleteHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Use LoadHolonsForParent(string autotaskId) to load runs for an autotask."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string autotaskId, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync($"autotask/v1/autotasks/{autotaskId}/runs");
                var runs = (json["items"] as JArray) ?? (json as JArray);
                var holons = new List<IHolon>();
                if (runs != null)
                    foreach (var run in runs)
                    {
                        var h = new Holon { Name = $"Autotask Run {run["autotaskRunId"]}" };
                        h.MetaData["run_id"] = run["autotaskRunId"]?.ToString() ?? "";
                        h.MetaData["status"] = run["status"]?.ToString() ?? "";
                        h.MetaData["autotask_id"] = autotaskId;
                        holons.Add(h);
                    }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            if (mk == "proposal_id") { var h = await LoadHolonAsync(mv); return new OASISResult<IEnumerable<IHolon>> { Result = h.Result != null ? new List<IHolon> { h.Result } : new List<IHolon>(), IsError = h.IsError, Message = h.Message }; }
            if (mk == "autotask_id") return await LoadHolonsForParentAsync(mv, t);
            var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"Defender does not support metadata search by '{mk}'. Supported: proposal_id, autotask_id."); return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        { foreach (var kv in m) { var p = await LoadHolonsByMetaDataAsync(kv.Key, kv.Value, t); if (!p.IsError) return p; } return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var results = new SearchResults();
                string query = sp?.FilterByMetaData?.ContainsKey("query") == true ? sp.FilterByMetaData["query"] : "";
                if (!string.IsNullOrEmpty(query))
                {
                    try { var proposal = await LoadHolonAsync(query); if (!proposal.IsError) results.SearchResultHolons = new List<IHolon> { proposal.Result }; } catch { }
                    try { var autotask = await LoadAvatarByProviderKeyAsync(query); if (!autotask.IsError) results.SearchResultAvatars = new List<IAvatar> { autotask.Result }; } catch { }
                }
                r.Result = results;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenZeppelin Defender Search failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Defender export requires an autotask ID string."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string autotaskId, int v = 0) => await LoadHolonsForParentAsync(autotaskId);
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Defender does not support email-based export."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) => await LoadAllHolonsAsync();
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var r = new OASISResult<bool>(); try { foreach (var h in holons) { var s = await SaveHolonAsync(h); if (s.IsError) { OASISErrorHandling.HandleError(ref r, s.Message); return r; } } r.Result = true; } catch (Exception ex) { OASISErrorHandling.HandleError(ref r, ex.Message, ex); } return r; }

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
