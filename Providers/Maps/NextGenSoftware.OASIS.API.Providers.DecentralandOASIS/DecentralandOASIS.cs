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

namespace NextGenSoftware.OASIS.API.Providers.DecentralandOASIS
{
    public class DecentralandOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _catalyst;
        private readonly HttpClient _content;
        private bool _isActivated;

        public DecentralandOASIS(string catalystUrl = "https://peer.decentraland.org")
        {
            _catalyst = new HttpClient { BaseAddress = new Uri(catalystUrl.TrimEnd('/') + "/") };
            _content = new HttpClient { BaseAddress = new Uri(catalystUrl.TrimEnd('/') + "/content/") };
            ProviderName = "DecentralandOASIS";
            ProviderDescription = "Decentraland Catalyst API: avatar profiles and world scene content provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.DecentralandOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Spatial);
        }

        private async Task<JToken> GetCatalystAsync(string path)
        {
            var r = await _catalyst.GetAsync(path);
            r.EnsureSuccessStatusCode();
            return JToken.Parse(await r.Content.ReadAsStringAsync());
        }

        private async Task<JToken> GetContentAsync(string path)
        {
            var r = await _content.GetAsync(path);
            r.EnsureSuccessStatusCode();
            return JToken.Parse(await r.Content.ReadAsStringAsync());
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                await GetCatalystAsync("about");
                _isActivated = true;
                r.Result = true;
                r.Message = "DecentralandOASIS activated";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Decentraland activation failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _isActivated = false;
            _catalyst.Dispose();
            _content.Dispose();
            return new OASISResult<bool> { Result = true };
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = Decentraland avatar profile (GET /lambdas/profiles/{address})
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var json = await GetCatalystAsync($"lambdas/profiles/{address}");
                var profile = json.Type == JTokenType.Array ? (json as JArray)?[0] as JObject : json as JObject;
                var metadata = profile?["metadata"] ?? profile;
                var avatars = metadata?["avatars"] as JArray;
                var first = avatars?.First as JObject;
                var avatar = new Avatar { Username = address };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.DecentralandOASIS] = address;
                avatar.FirstName = first?["name"]?.ToString() ?? "";
                avatar.MetaData["description"] = first?["description"]?.ToString() ?? "";
                avatar.MetaData["snapshot_face"] = first?["avatar"]?["snapshots"]?["face256"]?.ToString() ?? "";
                avatar.MetaData["wearables"] = first?["avatar"]?["wearables"]?.ToString(Formatting.None) ?? "[]";
                avatar.MetaData["eth_address"] = address;
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Decentraland LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0)
            => await LoadAvatarByProviderKeyAsync(u, v);

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "DecentralandOASIS does not support loading avatars by Guid; use LoadAvatarByProviderKey with the Ethereum address.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref r, "Decentraland does not provide a list-all-avatars endpoint; query specific addresses via LoadAvatarByProviderKey.");
            r.Result = new List<IAvatar>();
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "Decentraland does not support email-based avatar lookups.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await GetCatalystAsync($"lambdas/profiles/{u}");
                var profile = json.Type == JTokenType.Array ? (json as JArray)?[0] as JObject : json as JObject;
                var metadata = profile?["metadata"] ?? profile;
                var avatars = metadata?["avatars"] as JArray;
                var first = avatars?.First as JObject;
                var detail = new AvatarDetail { Username = u };
                detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.DecentralandOASIS] = u;
                detail.Description = first?["description"]?.ToString() ?? "";
                detail.MetaData["name"] = first?["name"]?.ToString() ?? "";
                detail.MetaData["wearables"] = first?["avatar"]?["wearables"]?.ToString(Formatting.None) ?? "[]";
                detail.MetaData["snapshot_face"] = first?["avatar"]?["snapshots"]?["face256"]?.ToString() ?? "";
                detail.MetaData["snapshot_body"] = first?["avatar"]?["snapshots"]?["body"]?.ToString() ?? "";
                detail.MetaData["eth_address"] = u;
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Decentraland LoadAvatarDetail failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "Decentraland does not support loading avatar details by Guid.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "Decentraland does not support email-based lookups.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref r, "Decentraland does not provide a list-all endpoint; query specific addresses.");
            r.Result = new List<IAvatarDetail>();
            return r;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "Decentraland avatar profiles are stored on-chain and deployed via the Decentraland SDK/CLI; writes are not supported via this API.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "Decentraland avatar details are stored on-chain and deployed via the Decentraland SDK/CLI; writes are not supported via this API.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Decentraland avatar profiles are on-chain; deletion is not supported via this API.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Decentraland avatar profiles are on-chain; deletion is not supported via this API.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Decentraland does not support email-based operations.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Decentraland avatar profiles are on-chain; deletion is not supported via this API.");
            return r;
        }

        // Holon = Decentraland scene (GET /content/entities/scene?pointer={x,y})
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var json = await GetContentAsync($"entities/scene?pointer={key}");
                var scenes = json.Type == JTokenType.Array ? (JArray)json : new JArray(json);
                var scene = scenes.First as JObject;
                if (scene == null)
                {
                    OASISErrorHandling.HandleError(ref r, $"No Decentraland scene found at pointer: {key}");
                    return r;
                }
                var holon = new Holon { Name = scene["metadata"]?["display"]?["title"]?.ToString() ?? key };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.DecentralandOASIS] = key;
                holon.MetaData["pointer"] = key;
                holon.MetaData["scene_id"] = scene["id"]?.ToString() ?? "";
                holon.MetaData["description"] = scene["metadata"]?["display"]?["description"]?.ToString() ?? "";
                holon.MetaData["parcels"] = scene["metadata"]?["scene"]?["parcels"]?.ToString(Formatting.None) ?? "[]";
                holon.MetaData["owner"] = scene["metadata"]?["owner"]?.ToString() ?? "";
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Decentraland LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "Decentraland does not support loading scenes by Guid; use LoadHolon with a parcel coordinate like '0,0'.");
            return r;
        }

        // LoadAllHolons: fetch scenes around genesis plaza
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Fetch active scenes in genesis area
                var holons = new List<IHolon>();
                var pointers = new[] { "0,0", "0,1", "1,0", "-1,0", "0,-1", "1,1", "-1,1", "1,-1", "-1,-1", "50,0" };
                foreach (var p in pointers)
                {
                    try
                    {
                        var sr = await LoadHolonAsync(p, lc, rec, md, coe, lcfp, v);
                        if (!sr.IsError && sr.Result != null) holons.Add(sr.Result);
                    }
                    catch { /* skip missing parcels */ }
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Decentraland LoadAllHolons failed: {ex.Message}", ex); }
            return r;
        }

        // LoadHolonsForParent: scenes owned by an address
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Get LAND parcels owned by address via Marketplace subgraph
                var json = await GetCatalystAsync($"lambdas/nfts/land?owner={k}");
                var holons = new List<IHolon>();
                var items = json["assets"] as JArray ?? new JArray();
                foreach (var item in items)
                {
                    if (item is JObject obj)
                    {
                        var coords = obj["data"]?["parcel"]?.ToString() ?? "";
                        var holon = new Holon { Name = $"LAND {coords}" };
                        holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.DecentralandOASIS] = coords;
                        holon.MetaData["coordinates"] = coords;
                        holon.MetaData["owner"] = k;
                        holon.MetaData["token_id"] = obj["id"]?.ToString() ?? "";
                        holons.Add(holon);
                    }
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Decentraland LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Decentraland does not support Guid-based parent queries; use LoadHolonsForParent with an Ethereum address.");
            r.Result = new List<IHolon>();
            return r;
        }

        // LoadHolonsByMetaData: filter scenes by owner or title
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (mk.Equals("owner", StringComparison.OrdinalIgnoreCase))
                    return await LoadHolonsForParentAsync(mv, t, lc, rec, md, cd, coe, lcfp, v);
                if (mk.Equals("pointer", StringComparison.OrdinalIgnoreCase))
                    return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon> { (await LoadHolonAsync(mv, lc, rec, md, coe, lcfp, v)).Result } };
                OASISErrorHandling.HandleError(ref r, "Decentraland LoadHolonsByMetaData: use metaKey='owner' (Ethereum address) or metaKey='pointer' (parcel coords like '0,0').");
                r.Result = new List<IHolon>();
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Decentraland LoadHolonsByMetaData failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            r.Result = new List<IHolon>();
            if (m.TryGetValue("owner", out var owner))
                return await LoadHolonsForParentAsync(owner, t, lc, rec, md, cd, coe, lcfp, v);
            if (m.TryGetValue("pointer", out var ptr))
                return await LoadHolonsByMetaDataAsync("pointer", ptr, t, lc, rec, md, cd, coe, lcfp, v);
            OASISErrorHandling.HandleError(ref r, "Decentraland LoadHolonsByMetaData: provide 'owner' or 'pointer' key.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "Decentraland scenes must be deployed via the Decentraland SDK/CLI and signed transactions; writes are not supported via the Catalyst API.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Decentraland scenes must be deployed via the Decentraland SDK/CLI; writes are not supported via the Catalyst API.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "Decentraland scenes are on-chain; deletion is not supported via the Catalyst API.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "Decentraland scenes are on-chain; deletion is not supported via the Catalyst API.");
            return r;
        }

        // SearchAsync: search avatars by querying the Decentraland profile endpoint
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var q = sp?.SearchQuery ?? "";
                var holons = new List<IHolon>();
                // Try as Ethereum address (profile lookup)
                if (q.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    var hr = await LoadHolonAsync(q, lc, rec, md, coe, false, v);
                    if (!hr.IsError && hr.Result != null) holons.Add(hr.Result);
                }
                else
                {
                    // Try as parcel pointer
                    var hr = await LoadHolonAsync(q, lc, rec, md, coe, false, v);
                    if (!hr.IsError && hr.Result != null) holons.Add(hr.Result);
                }
                var results = new SearchResults();
                results.SearchResultHolons = holons;
                r.Result = results;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Decentraland SearchAsync failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                // Load avatar profile as holon
                var profileR = await LoadAvatarByProviderKeyAsync(u, v);
                if (!profileR.IsError && profileR.Result != null)
                {
                    var profileHolon = new Holon { Name = profileR.Result.FirstName };
                    profileHolon.MetaData["type"] = "avatar_profile";
                    profileHolon.MetaData["eth_address"] = u;
                    foreach (var kv in profileR.Result.MetaData) profileHolon.MetaData[kv.Key] = kv.Value;
                    holons.Add(profileHolon);
                }
                // Load owned LAND
                var landR = await LoadHolonsForParentAsync(u, HolonType.All, lc: true);
                if (!landR.IsError && landR.Result != null) holons.AddRange(landR.Result);
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Decentraland ExportAllDataForAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Decentraland does not support export by Guid; use ExportAllDataForAvatarByUsername with the Ethereum address.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Decentraland does not support email-based export.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0)
            => await LoadAllHolonsAsync();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Decentraland content must be deployed via the Decentraland SDK/CLI; import is not supported.");
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
