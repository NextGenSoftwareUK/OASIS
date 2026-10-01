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

namespace NextGenSoftware.OASIS.API.Providers.RitualOASIS
{
    public class RitualOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;

        public RitualOASIS(string baseUrl = "http://localhost:4000")
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
            ProviderName = "RitualOASIS";
            ProviderDescription = "Ritual Infernet node REST API for decentralised AI compute jobs.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.RitualOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.AI);
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

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                var status = await GetAsync("api/status");
                r.Result = true;
                r.Message = $"RitualOASIS activated — node status: {status}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual activation failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _http.Dispose();
            return new OASISResult<bool> { Result = true, Message = "RitualOASIS deactivated." };
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar: maps to Infernet AI container ────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string containerId, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var json = await GetAsync("api/containers");
                var containers = (json as JArray) ?? (json["containers"] as JArray);
                JObject match = null;
                if (containers != null)
                    foreach (var c in containers)
                        if (c["id"]?.ToString() == containerId) { match = c as JObject; break; }

                if (match == null)
                {
                    OASISErrorHandling.HandleError(ref r, $"Ritual container '{containerId}' not found.");
                    return r;
                }
                r.Result = MapContainerToAvatar(match, containerId);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual LoadAvatarByProviderKey failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
            => await LoadAvatarByProviderKeyAsync(username, version);

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "Ritual containers are identified by container ID string, not OASIS GUID. Use LoadAvatarByProviderKey.");
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "Ritual does not support email-based container lookup.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var json = await GetAsync("api/containers");
                var containers = (json as JArray) ?? (json["containers"] as JArray);
                var avatars = new List<IAvatar>();
                if (containers != null)
                    foreach (var c in containers)
                        if (c is JObject obj)
                            avatars.Add(MapContainerToAvatar(obj, obj["id"]?.ToString() ?? ""));
                r.Result = avatars;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual LoadAllAvatars (containers) failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "Ritual Infernet containers are deployed via configuration, not via API. Avatar (container) creation is not supported via this provider.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Ritual Infernet containers are managed via configuration, not via API. Container deletion is not supported via this provider.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Ritual Infernet containers are managed via configuration, not via API. Container deletion is not supported via this provider.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Ritual does not support email-based avatar deletion.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Ritual Infernet container deletion is not supported via the REST API.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "Ritual avatar detail requires a container ID string, not OASIS GUID.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "Ritual does not support email-based avatar detail lookup.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string containerId, int version = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await GetAsync("api/containers");
                var containers = (json as JArray) ?? (json["containers"] as JArray);
                JObject match = null;
                if (containers != null)
                    foreach (var c in containers)
                        if (c["id"]?.ToString() == containerId) { match = c as JObject; break; }

                if (match == null)
                {
                    OASISErrorHandling.HandleError(ref r, $"Ritual container '{containerId}' not found.");
                    return r;
                }
                var detail = new AvatarDetail { Username = containerId };
                detail.MetaData["container_id"] = containerId;
                detail.MetaData["image"] = match["image"]?.ToString() ?? "";
                detail.MetaData["description"] = match["description"]?.ToString() ?? "";
                detail.MetaData["external"] = match["external"]?.ToString() ?? "";
                detail.MetaData["port"] = match["port"]?.ToString() ?? "";
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual LoadAvatarDetailByUsername failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "Ritual Infernet container configuration cannot be modified via the REST API.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var json = await GetAsync("api/containers");
                var containers = (json as JArray) ?? (json["containers"] as JArray);
                var details = new List<IAvatarDetail>();
                if (containers != null)
                {
                    foreach (var c in containers)
                    {
                        var detail = new AvatarDetail { Username = c["id"]?.ToString() ?? "" };
                        detail.MetaData["container_id"] = c["id"]?.ToString() ?? "";
                        detail.MetaData["image"] = c["image"]?.ToString() ?? "";
                        detail.MetaData["description"] = c["description"]?.ToString() ?? "";
                        details.Add(detail);
                    }
                }
                r.Result = details;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual LoadAllAvatarDetails failed: {ex.Message}", ex); }
            return r;
        }

        // ─── Holon: maps to Infernet AI job ───────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string jobId, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var json = await GetAsync($"api/jobs/{Uri.EscapeDataString(jobId)}");
                var job = (json as JObject) ?? (json["job"] as JObject) ?? json as JObject;
                r.Result = MapJobToHolon(job ?? new JObject(), jobId);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual LoadHolon (job {jobId}) failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "Ritual jobs are identified by string job ID, not OASIS GUID. Use LoadHolon(string key).");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Ritual Infernet does not support listing all jobs. Use LoadHolon(string jobId) to load a specific job by its ID.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Ritual LoadHolonsForParent requires a container ID string, not OASIS GUID.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string containerId, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Ritual Infernet does not support listing jobs by container. Submit jobs with SaveHolon and retrieve individual jobs with LoadHolon(jobId).");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            if (metaKey == "job_id" || metaKey == "id")
            {
                var holonResult = await LoadHolonAsync(metaValue);
                if (!holonResult.IsError && holonResult.Result != null)
                    r.Result = new List<IHolon> { holonResult.Result };
                else
                    r = new OASISResult<IEnumerable<IHolon>> { IsError = holonResult.IsError, Message = holonResult.Message, Result = new List<IHolon>() };
            }
            else
            {
                OASISErrorHandling.HandleError(ref r, $"Ritual does not support metadata search by '{metaKey}'. Supported: job_id, id.");
            }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            foreach (var kv in metaData)
            {
                var partial = await LoadHolonsByMetaDataAsync(kv.Key, kv.Value, type);
                if (!partial.IsError && partial.Result != null)
                {
                    r.Result = partial.Result;
                    return r;
                }
            }
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                var container = holon.MetaData.ContainsKey("container") ? holon.MetaData["container"]?.ToString() ?? "default" : "default";
                var inputData = holon.MetaData.ContainsKey("input") ? JsonConvert.DeserializeObject(holon.MetaData["input"]?.ToString() ?? "{}") : new { };

                var body = new
                {
                    containers = new[] { container },
                    data = inputData
                };
                var json = await PostAsync("api/jobs", body);
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.RitualOASIS] = json["id"]?.ToString() ?? holon.Id.ToString();
                holon.MetaData["job_id"] = json["id"]?.ToString() ?? "";
                r.Result = holon;
                r.Message = $"Ritual job submitted with ID: {json["id"]}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual SaveHolon (submit job) failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var results = new List<IHolon>();
            var r = new OASISResult<IEnumerable<IHolon>>();
            foreach (var h in holons)
            {
                var sr = await SaveHolonAsync(h);
                if (sr.Result != null) results.Add(sr.Result);
                else if (!continueOnError) { OASISErrorHandling.HandleError(ref r, sr.Message); return r; }
            }
            r.Result = results;
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "Ritual Infernet jobs cannot be deleted via the REST API.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string key)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "Ritual Infernet jobs cannot be deleted via the REST API.");
            return r;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var results = new SearchResults();
                string query = searchParams?.FilterByMetaData?.ContainsKey("query") == true
                    ? searchParams.FilterByMetaData["query"] : "";

                if (!string.IsNullOrEmpty(query))
                {
                    // Search containers matching query
                    var json = await GetAsync("api/containers");
                    var containers = (json as JArray) ?? (json["containers"] as JArray);
                    var matchingAvatars = new List<IAvatar>();
                    if (containers != null)
                        foreach (var c in containers)
                            if (c["id"]?.ToString()?.Contains(query, StringComparison.OrdinalIgnoreCase) == true
                                || c["description"]?.ToString()?.Contains(query, StringComparison.OrdinalIgnoreCase) == true)
                                matchingAvatars.Add(MapContainerToAvatar(c as JObject, c["id"]?.ToString() ?? ""));

                    results.SearchResultAvatars = matchingAvatars;

                    // Also try loading as a job ID
                    try
                    {
                        var jobResult = await LoadHolonAsync(query);
                        if (!jobResult.IsError && jobResult.Result != null)
                            results.SearchResultHolons = new List<IHolon> { jobResult.Result };
                    }
                    catch { /* not a valid job ID, skip */ }
                }
                r.Result = results;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual Search failed: {ex.Message}", ex); }
            return r;
        }

        // ─── Export / Import ──────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Ritual export requires a container ID string, not OASIS GUID.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string containerId, int version = 0)
        {
            // Export container configuration as a holon
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var avatarResult = await LoadAvatarByProviderKeyAsync(containerId);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref r, avatarResult.Message);
                    return r;
                }
                var holon = new Holon { Name = $"Ritual Container Export: {containerId}" };
                foreach (var kv in avatarResult.Result.MetaData)
                    holon.MetaData[kv.Key] = kv.Value;
                r.Result = new List<IHolon> { holon };
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual ExportAllDataForAvatarByUsername failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Ritual does not support email-based export.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Export all containers as holons
                var json = await GetAsync("api/containers");
                var containers = (json as JArray) ?? (json["containers"] as JArray);
                var holons = new List<IHolon>();
                if (containers != null)
                {
                    foreach (var c in containers)
                    {
                        var h = new Holon { Name = $"Ritual Container: {c["id"]}" };
                        foreach (var prop in (c as JObject)?.Properties() ?? new JObject().Properties())
                            h.MetaData[prop.Name] = prop.Value?.ToString() ?? "";
                        holons.Add(h);
                    }
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual ExportAll failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            // Import = submit each holon as a new inference job
            var r = new OASISResult<bool>();
            try
            {
                foreach (var h in holons)
                {
                    var saveResult = await SaveHolonAsync(h);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref r, $"Ritual import failed on holon '{h.Name}': {saveResult.Message}");
                        return r;
                    }
                }
                r.Result = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Ritual ImportAsync failed: {ex.Message}", ex); }
            return r;
        }

        // ─── Sync wrappers ────────────────────────────────────────────────────

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

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static IAvatar MapContainerToAvatar(JObject c, string containerId)
        {
            var avatar = new Avatar { Username = containerId };
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.RitualOASIS] = containerId;
            avatar.MetaData["container_id"] = containerId;
            avatar.MetaData["image"] = c["image"]?.ToString() ?? "";
            avatar.MetaData["description"] = c["description"]?.ToString() ?? "";
            avatar.MetaData["external"] = c["external"]?.ToString() ?? "";
            avatar.MetaData["port"] = c["port"]?.ToString() ?? "";
            return avatar;
        }

        private static IHolon MapJobToHolon(JObject job, string jobId)
        {
            var holon = new Holon { Name = $"Ritual Job {jobId}" };
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.RitualOASIS] = jobId;
            holon.MetaData["job_id"] = jobId;
            holon.MetaData["status"] = job["status"]?.ToString() ?? "";
            holon.MetaData["container"] = job["container"]?.ToString() ?? "";
            holon.MetaData["result"] = job["result"]?.ToString(Formatting.None) ?? "{}";
            holon.MetaData["input"] = job["input"]?.ToString(Formatting.None) ?? "{}";
            return holon;
        }
    }
}
