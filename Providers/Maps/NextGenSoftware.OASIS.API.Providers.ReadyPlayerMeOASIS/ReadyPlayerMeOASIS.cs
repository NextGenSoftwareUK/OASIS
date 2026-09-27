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

namespace NextGenSoftware.OASIS.API.Providers.ReadyPlayerMeOASIS
{
    public class ReadyPlayerMeOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _appId;
        private bool _isActivated;

        public ReadyPlayerMeOASIS(string apiKey = "", string appId = "demo")
        {
            _appId = appId;
            _http = new HttpClient { BaseAddress = new Uri("https://api.readyplayer.me/v2/") };
            if (!string.IsNullOrEmpty(apiKey))
                _http.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            ProviderName = "ReadyPlayerMeOASIS";
            ProviderDescription = "Ready Player Me cross-app 3D avatar creation and management provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ReadyPlayerMeOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Spatial);
        }

        private async Task<JObject> GetAsync(string path)
        {
            var r = await _http.GetAsync(path);
            r.EnsureSuccessStatusCode();
            return JObject.Parse(await r.Content.ReadAsStringAsync());
        }

        private async Task<JObject> PostAsync(string path, object body)
        {
            var resp = await _http.PostAsync(path, new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        private async Task<JObject> PatchAsync(string path, object body)
        {
            var req = new HttpRequestMessage(new HttpMethod("PATCH"), path)
            { Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json") };
            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        private async Task DeleteHttpAsync(string path)
        {
            var resp = await _http.DeleteAsync(path);
            resp.EnsureSuccessStatusCode();
        }

        private static IAvatar MapAvatar(JObject data, string key)
        {
            var avatarId = data["id"]?.ToString() ?? key;
            var avatar = new Avatar { Username = avatarId };
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReadyPlayerMeOASIS] = avatarId;
            avatar.MetaData["avatar_id"] = avatarId;
            avatar.MetaData["model_url"] = data["modelUrl"]?.ToString() ?? $"https://models.readyplayer.me/{avatarId}.glb";
            avatar.MetaData["thumbnail_url"] = data["thumbnailUrl"]?.ToString() ?? "";
            avatar.MetaData["gender"] = data["gender"]?.ToString() ?? "";
            avatar.MetaData["assets"] = data["assets"]?.ToString(Formatting.None) ?? "{}";
            avatar.MetaData["created_at"] = data["createdAt"]?.ToString() ?? "";
            return avatar;
        }

        private static IHolon MapAvatarHolon(JObject data, string key)
        {
            var avatarId = data["id"]?.ToString() ?? key;
            var holon = new Holon { Name = $"RPM Avatar {avatarId}" };
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReadyPlayerMeOASIS] = avatarId;
            holon.MetaData["avatar_id"] = avatarId;
            holon.MetaData["model_url"] = data["modelUrl"]?.ToString() ?? $"https://models.readyplayer.me/{avatarId}.glb";
            holon.MetaData["thumbnail_url"] = data["thumbnailUrl"]?.ToString() ?? "";
            holon.MetaData["gender"] = data["gender"]?.ToString() ?? "";
            return holon;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                await GetAsync("avatars?limit=1");
                _isActivated = true;
                r.Result = true;
                r.Message = "ReadyPlayerMeOASIS activated";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe activation failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _isActivated = false;
            _http.Dispose();
            return new OASISResult<bool> { Result = true };
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = RPM avatar (GET /v2/avatars/{avatarId})
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string avatarId, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var json = await GetAsync($"avatars/{avatarId}");
                var data = json["data"] as JObject ?? json;
                r.Result = MapAvatar(data, avatarId);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0)
            => await LoadAvatarByProviderKeyAsync(u, v);

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe does not support loading avatars by Guid; use LoadAvatarByProviderKey with the RPM avatar ID.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var json = await GetAsync("avatars?limit=100");
                var avatars = new List<IAvatar>();
                var data = json["data"] as JArray ?? new JArray();
                foreach (var a in data)
                    if (a is JObject obj) avatars.Add(MapAvatar(obj, obj["id"]?.ToString() ?? ""));
                r.Result = avatars;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe LoadAllAvatars failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe does not support email-based avatar lookups; use LoadAvatarByProviderKey with the RPM avatar ID.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await GetAsync($"avatars/{u}");
                var data = json["data"] as JObject ?? json;
                var detail = new AvatarDetail { Username = u };
                detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReadyPlayerMeOASIS] = u;
                detail.MetaData["avatar_id"] = u;
                detail.MetaData["model_url"] = data["modelUrl"]?.ToString() ?? $"https://models.readyplayer.me/{u}.glb";
                detail.MetaData["thumbnail_url"] = data["thumbnailUrl"]?.ToString() ?? "";
                detail.MetaData["gender"] = data["gender"]?.ToString() ?? "";
                detail.MetaData["assets"] = data["assets"]?.ToString(Formatting.None) ?? "{}";
                detail.MetaData["outfit"] = data["outfit"]?.ToString(Formatting.None) ?? "{}";
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe LoadAvatarDetail failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe does not support loading avatar details by Guid.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe does not support email-based lookups.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var json = await GetAsync("avatars?limit=100");
                var details = new List<IAvatarDetail>();
                var data = json["data"] as JArray ?? new JArray();
                foreach (var a in data)
                {
                    if (a is JObject obj)
                    {
                        var id = obj["id"]?.ToString() ?? "";
                        var d = new AvatarDetail { Username = id };
                        d.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReadyPlayerMeOASIS] = id;
                        d.MetaData["avatar_id"] = id;
                        d.MetaData["model_url"] = obj["modelUrl"]?.ToString() ?? $"https://models.readyplayer.me/{id}.glb";
                        d.MetaData["gender"] = obj["gender"]?.ToString() ?? "";
                        details.Add(d);
                    }
                }
                r.Result = details;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe LoadAllAvatarDetails failed: {ex.Message}", ex); }
            return r;
        }

        // SaveAvatar: creates a new RPM user and returns their user ID as avatar
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                if (a.Id == Guid.Empty) a.Id = Guid.NewGuid();
                var existingId = a.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.ReadyPlayerMeOASIS)
                    ? a.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReadyPlayerMeOASIS] : "";
                if (!string.IsNullOrEmpty(existingId))
                {
                    // Update avatar assets
                    var assets = a.MetaData.ContainsKey("assets") ? JsonConvert.DeserializeObject(a.MetaData["assets"]?.ToString() ?? "{}") : new { };
                    var updateBody = new { data = new { assets } };
                    var updated = await PatchAsync($"avatars/{existingId}", updateBody);
                    var updData = updated["data"] as JObject ?? updated;
                    r.Result = MapAvatar(updData, existingId);
                }
                else
                {
                    // Create anonymous user
                    var body = new { data = new { applicationId = _appId } };
                    var json = await PostAsync("users", body);
                    var userId = json["data"]?["id"]?.ToString() ?? a.Id.ToString();
                    a.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReadyPlayerMeOASIS] = userId;
                    a.MetaData["user_id"] = userId;
                    r.Result = a;
                }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe SaveAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var existingId = ad.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.ReadyPlayerMeOASIS)
                    ? ad.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReadyPlayerMeOASIS] : "";
                if (string.IsNullOrEmpty(existingId))
                {
                    OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe requires an avatar ID in ProviderUniqueStorageKey to save avatar details.");
                    return r;
                }
                var assets = ad.MetaData.ContainsKey("assets") ? JsonConvert.DeserializeObject(ad.MetaData["assets"]?.ToString() ?? "{}") : new { };
                var body = new { data = new { assets } };
                await PatchAsync($"avatars/{existingId}", body);
                r.Result = ad;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe SaveAvatarDetail failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe does not support deleting avatars by Guid; use DeleteAvatar with the RPM avatar ID string.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true)
        {
            var r = new OASISResult<bool>();
            try
            {
                await DeleteHttpAsync($"avatars/{k}");
                r.Result = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe DeleteAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe does not support email-based deletion.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true)
            => await DeleteAvatarAsync(u, s);

        // Holon = RPM avatar asset/model
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var json = await GetAsync($"avatars/{key}");
                var data = json["data"] as JObject ?? json;
                r.Result = MapAvatarHolon(data, key);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe does not support loading holons by Guid; use LoadHolon with the RPM avatar ID.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var json = await GetAsync("avatars?limit=100");
                var holons = new List<IHolon>();
                var data = json["data"] as JArray ?? new JArray();
                foreach (var a in data)
                    if (a is JObject obj) holons.Add(MapAvatarHolon(obj, obj["id"]?.ToString() ?? ""));
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe LoadAllHolons failed: {ex.Message}", ex); }
            return r;
        }

        // LoadHolonsForParent: get all avatars for a user
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var json = await GetAsync($"users/{k}/avatars");
                var holons = new List<IHolon>();
                var data = json["data"] as JArray ?? new JArray();
                foreach (var a in data)
                    if (a is JObject obj) holons.Add(MapAvatarHolon(obj, obj["id"]?.ToString() ?? ""));
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe does not support Guid-based parent queries; use LoadHolonsForParent with the RPM user ID.");
            r.Result = new List<IHolon>();
            return r;
        }

        // LoadHolonsByMetaData: filter by gender
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var allR = await LoadAllHolonsAsync(t, lc, rec, md, cd, coe, lcfp, v);
                if (allR.IsError) return allR;
                var filtered = new List<IHolon>();
                foreach (var h in allR.Result ?? new List<IHolon>())
                {
                    var val = h.MetaData.ContainsKey(mk) ? h.MetaData[mk]?.ToString() ?? "" : "";
                    if (val.Equals(mv, StringComparison.OrdinalIgnoreCase)) filtered.Add(h);
                }
                r.Result = filtered;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe LoadHolonsByMetaData failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var allR = await LoadAllHolonsAsync(t, lc, rec, md, cd, coe, lcfp, v);
                if (allR.IsError) return allR;
                var filtered = new List<IHolon>();
                foreach (var h in allR.Result ?? new List<IHolon>())
                {
                    bool match = mm == MetaKeyValuePairMatchMode.MatchAll;
                    foreach (var kv in m)
                    {
                        var val = h.MetaData.ContainsKey(kv.Key) ? h.MetaData[kv.Key]?.ToString() ?? "" : "";
                        bool kvMatch = val.Equals(kv.Value, StringComparison.OrdinalIgnoreCase);
                        if (mm == MetaKeyValuePairMatchMode.MatchAll) match = match && kvMatch;
                        else if (kvMatch) { match = true; break; }
                    }
                    if (match) filtered.Add(h);
                }
                r.Result = filtered;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe LoadHolonsByMetaData failed: {ex.Message}", ex); }
            return r;
        }

        // SaveHolon: create a new RPM avatar
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
                var existingId = h.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.ReadyPlayerMeOASIS)
                    ? h.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReadyPlayerMeOASIS] : "";
                if (!string.IsNullOrEmpty(existingId))
                {
                    var assets = h.MetaData.ContainsKey("assets") ? JsonConvert.DeserializeObject(h.MetaData["assets"]?.ToString() ?? "{}") : new { };
                    var updateBody = new { data = new { assets } };
                    var updated = await PatchAsync($"avatars/{existingId}", updateBody);
                    r.Result = MapAvatarHolon(updated["data"] as JObject ?? updated, existingId);
                }
                else
                {
                    // Create avatar for a user
                    var userId = h.MetaData.ContainsKey("user_id") ? h.MetaData["user_id"]?.ToString() ?? "" : "";
                    var gender = h.MetaData.ContainsKey("gender") ? h.MetaData["gender"]?.ToString() ?? "neutral" : "neutral";
                    if (string.IsNullOrEmpty(userId))
                    {
                        OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe SaveHolon: set MetaData['user_id'] to a valid RPM user ID to create an avatar.");
                        return r;
                    }
                    var body = new { data = new { userId, gender } };
                    var json = await PostAsync("avatars", body);
                    var data = json["data"] as JObject ?? json;
                    h.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReadyPlayerMeOASIS] = data["id"]?.ToString() ?? h.Id.ToString();
                    h.MetaData["model_url"] = data["modelUrl"]?.ToString() ?? "";
                    r.Result = h;
                }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe SaveHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            foreach (var h in holons)
            {
                var sr = await SaveHolonAsync(h, sc, rec, md, coe, scop);
                if (sr.IsError) { OASISErrorHandling.HandleError(ref r, sr.Message); return r; }
                if (sr.Result != null) saved.Add(sr.Result);
            }
            r.Result = saved;
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe does not support deleting holons by Guid; use DeleteHolon with the RPM avatar ID string.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                await DeleteHttpAsync($"avatars/{k}");
                r.Result = new Holon();
                r.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.ReadyPlayerMeOASIS] = k;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe DeleteHolon failed: {ex.Message}", ex); }
            return r;
        }

        // SearchAsync: search avatars by id or gender
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var q = sp?.SearchQuery ?? "";
                var holons = new List<IHolon>();
                // Try direct avatar ID lookup
                try
                {
                    var hr = await LoadHolonAsync(q, lc, rec, md, coe, false, v);
                    if (!hr.IsError && hr.Result != null) holons.Add(hr.Result);
                }
                catch { }
                // Also search by gender filter
                if (holons.Count == 0)
                {
                    var allR = await LoadAllHolonsAsync();
                    foreach (var h in allR.Result ?? new List<IHolon>())
                    {
                        var gender = h.MetaData.ContainsKey("gender") ? h.MetaData["gender"]?.ToString() ?? "" : "";
                        if (gender.Contains(q, StringComparison.OrdinalIgnoreCase))
                            holons.Add(h);
                    }
                }
                var results = new SearchResults();
                results.SearchResultHolons = holons;
                r.Result = results;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe SearchAsync failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // u = RPM user ID; get all their avatars
                var json = await GetAsync($"users/{u}/avatars");
                var holons = new List<IHolon>();
                var data = json["data"] as JArray ?? new JArray();
                foreach (var a in data)
                    if (a is JObject obj) holons.Add(MapAvatarHolon(obj, obj["id"]?.ToString() ?? ""));
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe ExportAllDataForAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe does not support export by Guid; use ExportAllDataForAvatarByUsername with the RPM user ID.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "ReadyPlayerMe does not support email-based export.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0)
            => await LoadAllHolonsAsync();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h)
        {
            var r = new OASISResult<bool>();
            try
            {
                foreach (var holon in h)
                {
                    var sr = await SaveHolonAsync(holon);
                    if (sr.IsError) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe ImportAsync failed on '{holon.Name}': {sr.Message}"); return r; }
                }
                r.Result = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ReadyPlayerMe ImportAsync failed: {ex.Message}", ex); }
            return r;
        }

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
        public override OASISResult<ISearchResults> Search(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0) => SearchAsync(sp, lc, rec, md, coe, v).Result;
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override OASISResult<IHolon> DeleteHolon(string k) => DeleteHolonAsync(k).Result;
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
