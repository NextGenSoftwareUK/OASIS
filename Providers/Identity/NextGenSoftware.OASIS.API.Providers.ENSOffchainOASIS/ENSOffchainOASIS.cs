using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
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

namespace NextGenSoftware.OASIS.API.Providers.ENSOffchainOASIS
{
    public class ENSOffchainOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;
        private bool _isActivated;

        public ENSOffchainOASIS()
        {
            _http = new HttpClient { BaseAddress = new Uri("https://api.web3.bio/") };
            _http.DefaultRequestHeaders.Add("User-Agent", "OASISAPI/1.0");
            ProviderName = "ENSOffchainOASIS";
            ProviderDescription = "ENS offchain name resolution and Web3 identity profile provider via web3.bio.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ENSOffchainOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        }

        private async Task<JObject> GetAsync(string path)
        {
            var r = await _http.GetAsync(path);
            r.EnsureSuccessStatusCode();
            return JObject.Parse(await r.Content.ReadAsStringAsync());
        }

        private async Task<JArray> GetArrayAsync(string path)
        {
            var r = await _http.GetAsync(path);
            r.EnsureSuccessStatusCode();
            return JArray.Parse(await r.Content.ReadAsStringAsync());
        }

        private static IAvatar MapProfile(JObject json, string key)
        {
            var avatar = new Avatar { Username = key };
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ENSOffchainOASIS] = key;
            avatar.FirstName = json["displayName"]?.ToString() ?? key;
            avatar.MetaData["address"] = json["address"]?.ToString() ?? "";
            avatar.MetaData["avatar"] = json["avatar"]?.ToString() ?? "";
            avatar.MetaData["description"] = json["description"]?.ToString() ?? "";
            avatar.MetaData["twitter"] = json["links"]?["twitter"]?["handle"]?.ToString() ?? "";
            avatar.MetaData["github"] = json["links"]?["github"]?["handle"]?.ToString() ?? "";
            avatar.MetaData["platform"] = json["platform"]?.ToString() ?? "ens";
            return avatar;
        }

        private static IHolon MapProfileHolon(JObject json, string key)
        {
            var holon = new Holon { Name = json["displayName"]?.ToString() ?? key };
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ENSOffchainOASIS] = key;
            holon.MetaData["identity"] = json["identity"]?.ToString() ?? key;
            holon.MetaData["address"] = json["address"]?.ToString() ?? key;
            holon.MetaData["platform"] = json["platform"]?.ToString() ?? "ens";
            holon.MetaData["avatar"] = json["avatar"]?.ToString() ?? "";
            holon.MetaData["description"] = json["description"]?.ToString() ?? "";
            return holon;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                await GetAsync("ns/ens/vitalik.eth");
                _isActivated = true;
                r.Result = true;
                r.Message = "ENSOffchainOASIS activated";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ENSOffchain activation failed: {ex.Message}", ex); }
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

        // Avatar = ENS identity/profile (web3.bio /ns/ens/{name} or /profile/{address})
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string ensName, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var json = await GetAsync($"ns/ens/{Uri.EscapeDataString(ensName)}");
                r.Result = MapProfile(json, ensName);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ENSOffchain LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0)
            => await LoadAvatarByProviderKeyAsync(u, v);

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support loading avatars by Guid; use LoadAvatarByProviderKey with the ENS name.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support listing all ENS names; query specific names via LoadAvatarByProviderKey.");
            r.Result = new List<IAvatar>();
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                // web3.bio profile endpoint returns rich social/linked account data
                var profiles = await GetArrayAsync($"profile/{Uri.EscapeDataString(u)}");
                var detail = new AvatarDetail { Username = u };
                detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.ENSOffchainOASIS] = u;
                var allLinks = new System.Text.StringBuilder();
                foreach (var p in profiles)
                {
                    var platform = p["platform"]?.ToString() ?? "";
                    var identity = p["identity"]?.ToString() ?? "";
                    detail.MetaData[$"platform_{platform}"] = identity;
                    if (!string.IsNullOrEmpty(p["address"]?.ToString()))
                        detail.MetaData["address"] = p["address"]!.ToString();
                    allLinks.Append($"{platform}:{identity} ");
                }
                detail.Description = allLinks.ToString().Trim();
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ENSOffchain LoadAvatarDetail failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support loading avatar details by Guid; use LoadAvatarDetailByUsername with the ENS name.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support email-based lookups.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support listing all avatar details.");
            r.Result = new List<IAvatarDetail>();
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support email-based avatar lookups.");
            return r;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain is a read-only name resolver; avatar writes are not supported.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain is a read-only name resolver; avatar detail writes are not supported.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain is a read-only name resolver; deletion is not supported.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain is a read-only name resolver; deletion is not supported.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain is a read-only name resolver; deletion is not supported.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain is a read-only name resolver; deletion is not supported.");
            return r;
        }

        // Holon = Web3 profile record (web3.bio /profile/{handle})
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                // /profile/{address} returns array of platform profiles for an address
                var arr = await GetArrayAsync($"profile/{Uri.EscapeDataString(key)}");
                if (arr.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref r, $"No web3 profile found for: {key}");
                    return r;
                }
                var json = (JObject)arr[0];
                r.Result = MapProfileHolon(json, key);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ENSOffchain LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support loading holons by Guid; use LoadHolon with an ENS name or address.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support listing all holons; query specific addresses via LoadHolon.");
            r.Result = new List<IHolon>();
            return r;
        }

        // LoadHolonsForParent(key): get all platform profiles for an address/ENS name
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var arr = await GetArrayAsync($"profile/{Uri.EscapeDataString(k)}");
                var holons = new List<IHolon>();
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        var identity = obj["identity"]?.ToString() ?? k;
                        holons.Add(MapProfileHolon(obj, identity));
                    }
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ENSOffchain LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support Guid-based parent queries; use LoadHolonsForParent with an ENS name or address.");
            r.Result = new List<IHolon>();
            return r;
        }

        // LoadHolonsByMetaData: search by platform
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Query web3.bio by platform handle: /ns/{platform}/{identity}
                if (mk.Equals("platform", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(mv))
                {
                    var parts = mv.Split('/', 2);
                    if (parts.Length == 2)
                    {
                        var json = await GetAsync($"ns/{parts[0]}/{Uri.EscapeDataString(parts[1])}");
                        r.Result = new List<IHolon> { MapProfileHolon(json, mv) };
                        return r;
                    }
                }
                OASISErrorHandling.HandleError(ref r, $"ENSOffchain LoadHolonsByMetaData: use metaKey='platform', metaValue='platform/identity' (e.g. 'ens/vitalik.eth').");
                r.Result = new List<IHolon>();
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ENSOffchain LoadHolonsByMetaData failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            r.Result = new List<IHolon>();
            if (m.TryGetValue("platform", out var pv))
                return await LoadHolonsByMetaDataAsync("platform", pv, t, lc, rec, md, cd, coe, lcfp, v);
            OASISErrorHandling.HandleError(ref r, "ENSOffchain LoadHolonsByMetaData: provide 'platform' key with value 'platform/identity'.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain is a read-only name resolver; holon writes are not supported.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain is a read-only name resolver; holon writes are not supported.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain is a read-only name resolver; deletion is not supported.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain is a read-only name resolver; deletion is not supported.");
            return r;
        }

        // SearchAsync: web3.bio /search?q={query}
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var q = sp?.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery ?? "";
                var arr = await GetArrayAsync($"search?q={Uri.EscapeDataString(q)}");
                var holons = new List<IHolon>();
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        var identity = obj["identity"]?.ToString() ?? q;
                        holons.Add(MapProfileHolon(obj, identity));
                    }
                }
                var results = new SearchResults();
                results.SearchResultHolons = holons;
                r.Result = results;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ENSOffchain SearchAsync failed: {ex.Message}", ex); }
            return r;
        }

        // ExportAllDataForAvatarByUsername: get all platform profiles for a user
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var arr = await GetArrayAsync($"profile/{Uri.EscapeDataString(u)}");
                var holons = new List<IHolon>();
                foreach (var item in arr)
                {
                    if (item is JObject obj)
                    {
                        var identity = obj["identity"]?.ToString() ?? u;
                        holons.Add(MapProfileHolon(obj, identity));
                    }
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"ENSOffchain ExportAllDataForAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support export by Guid; use ExportAllDataForAvatarByUsername with ENS name or address.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support email-based export.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain does not support bulk export; query specific ENS names individually.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "ENSOffchain is a read-only name resolver; import is not supported.");
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
