using System;
using System.Collections.Generic;
using System.Net.Http;
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

namespace NextGenSoftware.OASIS.API.Providers.TheSandboxOASIS
{
    public class TheSandboxOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;
        private bool _isActivated;

        public TheSandboxOASIS(string baseUrl = "https://api.sandbox.game/v1")
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
            ProviderName = "TheSandboxOASIS";
            ProviderDescription = "The Sandbox metaverse LAND and user profile data provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TheSandboxOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Spatial);
        }

        private async Task<JObject> GetAsync(string path)
        {
            var r = await _http.GetAsync(path);
            r.EnsureSuccessStatusCode();
            return JObject.Parse(await r.Content.ReadAsStringAsync());
        }

        private static IAvatar MapUser(JToken user, string key)
        {
            var username = user["username"]?.ToString() ?? key;
            var avatar = new Avatar { Username = username };
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.TheSandboxOASIS] = username;
            avatar.FirstName = user["name"]?.ToString() ?? username;
            avatar.MetaData["wallet_address"] = user["ethAddress"]?.ToString() ?? "";
            avatar.MetaData["avatar_url"] = user["avatarUrl"]?.ToString() ?? "";
            avatar.MetaData["land_count"] = user["landCount"]?.ToString() ?? "0";
            avatar.MetaData["description"] = user["description"]?.ToString() ?? "";
            return avatar;
        }

        private static IHolon MapLand(JToken land, string key)
        {
            var tokenId = land["id"]?.ToString() ?? land["tokenId"]?.ToString() ?? key;
            var holon = new Holon { Name = land["name"]?.ToString() ?? $"LAND #{tokenId}" };
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.TheSandboxOASIS] = tokenId;
            holon.MetaData["token_id"] = tokenId;
            holon.MetaData["x"] = land["x"]?.ToString() ?? "0";
            holon.MetaData["y"] = land["y"]?.ToString() ?? "0";
            holon.MetaData["owner"] = land["owner"]?["ethAddress"]?.ToString() ?? land["ownerAddress"]?.ToString() ?? "";
            holon.MetaData["description"] = land["description"]?.ToString() ?? "";
            holon.MetaData["image"] = land["image"]?.ToString() ?? "";
            return holon;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                await GetAsync("lands?limit=1");
                _isActivated = true;
                r.Result = true;
                r.Message = "TheSandboxOASIS activated";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox activation failed: {ex.Message}", ex); }
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

        // Avatar = The Sandbox user/player (GET /users/{username})
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string username, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var json = await GetAsync($"users/{Uri.EscapeDataString(username)}");
                var user = json["user"] ?? json;
                r.Result = MapUser(user, username);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0)
            => await LoadAvatarByProviderKeyAsync(u, v);

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "TheSandbox does not support loading avatars by Guid; use LoadAvatarByProviderKey with the Sandbox username.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not provide a list-all-users endpoint; query specific users via LoadAvatarByProviderKey.");
            r.Result = new List<IAvatar>();
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not support email-based user lookups.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await GetAsync($"users/{Uri.EscapeDataString(u)}");
                var user = json["user"] ?? json;
                var detail = new AvatarDetail { Username = u };
                detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.TheSandboxOASIS] = u;
                detail.Description = user["description"]?.ToString() ?? "";
                detail.MetaData["name"] = user["name"]?.ToString() ?? u;
                detail.MetaData["wallet_address"] = user["ethAddress"]?.ToString() ?? "";
                detail.MetaData["avatar_url"] = user["avatarUrl"]?.ToString() ?? "";
                detail.MetaData["land_count"] = user["landCount"]?.ToString() ?? "0";
                detail.MetaData["twitter"] = user["twitter"]?.ToString() ?? "";
                detail.MetaData["website"] = user["website"]?.ToString() ?? "";
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox LoadAvatarDetail failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not support loading avatar details by Guid.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not support email-based lookups.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not provide a list-all-users endpoint.");
            r.Result = new List<IAvatarDetail>();
            return r;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox user profiles are managed through the Sandbox platform; writes are not supported via this API.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox user profiles are managed through the Sandbox platform; writes are not supported via this API.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not support deleting user accounts via the API.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not support deleting user accounts via the API.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not support email-based operations.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not support deleting user accounts via the API.");
            return r;
        }

        // Holon = The Sandbox LAND (GET /lands/{tokenId})
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var json = await GetAsync($"lands/{Uri.EscapeDataString(key)}");
                var land = json["land"] ?? json;
                r.Result = MapLand(land, key);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not support loading LAND by Guid; use LoadHolon with the LAND token ID.");
            return r;
        }

        // LoadAllHolons: list LAND parcels
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var json = await GetAsync("lands?limit=50");
                var holons = new List<IHolon>();
                var items = json["lands"] as JArray ?? json["data"] as JArray ?? new JArray();
                foreach (var item in items)
                    if (item is JObject obj) holons.Add(MapLand(obj, obj["id"]?.ToString() ?? ""));
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox LoadAllHolons failed: {ex.Message}", ex); }
            return r;
        }

        // LoadHolonsForParent: get all LAND owned by a user
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // k = username or wallet address
                var json = await GetAsync($"lands?owner={Uri.EscapeDataString(k)}&limit=100");
                var holons = new List<IHolon>();
                var items = json["lands"] as JArray ?? json["data"] as JArray ?? new JArray();
                foreach (var item in items)
                    if (item is JObject obj) holons.Add(MapLand(obj, obj["id"]?.ToString() ?? ""));
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not support Guid-based parent queries; use LoadHolonsForParent with a username or wallet address.");
            r.Result = new List<IHolon>();
            return r;
        }

        // LoadHolonsByMetaData: filter by owner or coordinates
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (mk.Equals("owner", StringComparison.OrdinalIgnoreCase))
                    return await LoadHolonsForParentAsync(mv, t, lc, rec, md, cd, coe, lcfp, v);
                if (mk.Equals("token_id", StringComparison.OrdinalIgnoreCase))
                    return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon> { (await LoadHolonAsync(mv, lc, rec, md, coe, lcfp, v)).Result } };
                // Filter from full list
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
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox LoadHolonsByMetaData failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (m.TryGetValue("owner", out var owner))
                    return await LoadHolonsForParentAsync(owner, t, lc, rec, md, cd, coe, lcfp, v);
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
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox LoadHolonsByMetaData failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox LAND ownership is governed by blockchain transactions; writes are not supported via this API.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox LAND ownership is governed by blockchain transactions; writes are not supported via this API.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox LAND is on-chain; deletion is not supported.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox LAND is on-chain; deletion is not supported.");
            return r;
        }

        // SearchAsync: search LAND by name or owner
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var q = sp?.SearchQuery ?? "";
                var holons = new List<IHolon>();
                // Try as token ID
                try
                {
                    var hr = await LoadHolonAsync(q, lc, rec, md, coe, false, v);
                    if (!hr.IsError && hr.Result != null) holons.Add(hr.Result);
                }
                catch { }
                // Try as owner
                if (holons.Count == 0)
                {
                    var ownerR = await LoadHolonsForParentAsync(q, HolonType.All, lc: true);
                    if (!ownerR.IsError && ownerR.Result != null) holons.AddRange(ownerR.Result);
                }
                // Filter all by name match
                if (holons.Count == 0)
                {
                    var allR = await LoadAllHolonsAsync();
                    foreach (var h in allR.Result ?? new List<IHolon>())
                        if (h.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) holons.Add(h);
                }
                var results = new SearchResults();
                results.SearchResultHolons = holons;
                r.Result = results;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox SearchAsync failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                // Load user profile as holon
                var profileR = await LoadAvatarByProviderKeyAsync(u, v);
                if (!profileR.IsError && profileR.Result != null)
                {
                    var profileHolon = new Holon { Name = profileR.Result.FirstName };
                    profileHolon.MetaData["type"] = "user_profile";
                    profileHolon.MetaData["username"] = u;
                    foreach (var kv in profileR.Result.MetaData) profileHolon.MetaData[kv.Key] = kv.Value;
                    holons.Add(profileHolon);
                }
                // Load owned LAND
                var landR = await LoadHolonsForParentAsync(u, HolonType.All, lc: true);
                if (!landR.IsError && landR.Result != null) holons.AddRange(landR.Result);
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"The Sandbox ExportAllDataForAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not support export by Guid; use ExportAllDataForAvatarByUsername.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox does not support email-based export.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0)
            => await LoadAllHolonsAsync();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "The Sandbox LAND is on-chain; import is not supported via this API.");
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
