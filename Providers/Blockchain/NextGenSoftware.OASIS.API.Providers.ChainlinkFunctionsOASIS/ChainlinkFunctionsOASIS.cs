using System;using System.Collections.Generic;using System.Net.Http;using System.Text;using System.Threading.Tasks;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using NextGenSoftware.OASIS.API.Core;using NextGenSoftware.OASIS.API.Core.Enums;using NextGenSoftware.OASIS.API.Core.Helpers;using NextGenSoftware.OASIS.API.Core.Holons;using NextGenSoftware.OASIS.API.Core.Interfaces;using NextGenSoftware.OASIS.API.Core.Interfaces.Search;using NextGenSoftware.OASIS.API.Core.Objects;using NextGenSoftware.OASIS.API.Core.Objects.Search;using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.ChainlinkFunctionsOASIS
{
    public class ChainlinkFunctionsOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
    {
        private readonly HttpClient _http;
        private bool _isActivated;
        public ChainlinkFunctionsOASIS(string baseUrl = "https://functions-gateway.chain.link/")
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
            ProviderName = "ChainlinkFunctionsOASIS"; ProviderDescription = "Chainlink Functions serverless compute and oracle subscription provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ChainlinkFunctionsOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain);
        }
        private async Task<JObject> GetAsync(string path) { var r = await _http.GetAsync(path); r.EnsureSuccessStatusCode(); return JObject.Parse(await r.Content.ReadAsStringAsync()); }
        private async Task<JObject> PostAsync(string path, object body) { var resp = await _http.PostAsync(path, new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")); resp.EnsureSuccessStatusCode(); return JObject.Parse(await resp.Content.ReadAsStringAsync()); }
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try { await GetAsync("health"); _isActivated = true; r.Result = true; r.Message = "ChainlinkFunctionsOASIS activated"; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions activation failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _isActivated = false; _http.Dispose(); return new OASISResult<bool> { Result = true }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string subId, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                // Chainlink Functions: get subscription info
                var json = await GetAsync($"subscriptions/{subId}");
                var avatar = new Avatar { Username = subId };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ChainlinkFunctionsOASIS] = subId;
                avatar.MetaData["subscription_id"] = json["subscriptionId"]?.ToString() ?? subId;
                avatar.MetaData["balance"] = json["balance"]?.ToString() ?? "0";
                avatar.MetaData["owner"] = json["owner"]?.ToString() ?? "";
                avatar.MetaData["consumers"] = json["consumers"]?.ToString(Formatting.None) ?? "[]";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                // key = request ID
                var json = await GetAsync($"requests/{key}");
                var holon = new Holon { Name = $"Chainlink Functions Request {key[..Math.Min(8, key.Length)]}..." };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.ChainlinkFunctionsOASIS] = key;
                holon.MetaData["request_id"] = key;
                holon.MetaData["status"] = json["status"]?.ToString() ?? "";
                holon.MetaData["response"] = json["response"]?.ToString() ?? "";
                holon.MetaData["error"] = json["error"]?.ToString() ?? "";
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions LoadHolon failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
                var body = new { source = h.MetaData.ContainsKey("source") ? h.MetaData["source"] : "return Functions.encodeString('hello')", subscriptionId = h.MetaData.ContainsKey("subscription_id") ? h.MetaData["subscription_id"] : "1", gasLimit = 300000 };
                var json = await PostAsync("requests", body);
                h.ProviderUniqueStorageKey[Core.Enums.ProviderType.ChainlinkFunctionsOASIS] = json["requestId"]?.ToString() ?? h.Id.ToString();
                r.Result = h;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Chainlink Functions SaveHolon failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatar>(); r.Result = new Avatar { Id = id }; return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => await LoadAvatarByProviderKeyAsync(u, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) { if (a.Id == Guid.Empty) a.Id = Guid.NewGuid(); return new OASISResult<IAvatar> { Result = a }; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) => new OASISResult<bool> { Result = true };
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) => new OASISResult<bool> { Result = true };
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) => new OASISResult<bool> { Result = true };
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) => new OASISResult<bool> { Result = true };
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) => new OASISResult<IEnumerable<IAvatar>> { Result = new List<IAvatar>() };
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); r.Result = new Holon { Id = id }; return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) => new OASISResult<IAvatarDetail> { Result = ad };
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) => new OASISResult<IEnumerable<IAvatarDetail>> { Result = new List<IAvatarDetail>() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) { var l = new List<IHolon>(); foreach (var h in holons) { var sr = await SaveHolonAsync(h); if (sr.Result != null) l.Add(sr.Result); } return new OASISResult<IEnumerable<IHolon>> { Result = l }; }
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0) => new OASISResult<ISearchResults> { Result = new SearchResults() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() };
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) => new OASISResult<IHolon> { Result = new Holon { Id = id } };
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
    }
}
