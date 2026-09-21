using System;using System.Collections.Generic;using System.Net.Http;using System.Text;using System.Threading.Tasks;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using NextGenSoftware.OASIS.API.Core;using NextGenSoftware.OASIS.API.Core.Enums;using NextGenSoftware.OASIS.API.Core.Helpers;using NextGenSoftware.OASIS.API.Core.Holons;using NextGenSoftware.OASIS.API.Core.Interfaces;using NextGenSoftware.OASIS.API.Core.Interfaces.Search;using NextGenSoftware.OASIS.API.Core.Objects;using NextGenSoftware.OASIS.API.Core.Objects.Search;using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS
{
    public class IntelOpenVINOOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _defaultModel;
        private bool _isActivated;
        public IntelOpenVINOOASIS(string baseUrl = "http://localhost:9000", string defaultModel = "oasis_model")
        {
            _defaultModel = defaultModel;
            _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
            ProviderName = "IntelOpenVINOOASIS"; ProviderDescription = "Intel OpenVINO Model Server REST API provider for AI inference.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.IntelOpenVINOOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.AI);
        }
        private async Task<JObject> GetAsync(string path) { var r = await _http.GetAsync(path); r.EnsureSuccessStatusCode(); return JObject.Parse(await r.Content.ReadAsStringAsync()); }
        private async Task<JObject> PostAsync(string path, object body) { var resp = await _http.PostAsync(path, new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")); resp.EnsureSuccessStatusCode(); return JObject.Parse(await resp.Content.ReadAsStringAsync()); }
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                var json = await GetAsync($"v1/models/{_defaultModel}");
                _isActivated = true; r.Result = true; r.Message = $"IntelOpenVINOOASIS activated — model {json["model_version_status"]?[0]?["version"]}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenVINO activation failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _isActivated = false; _http.Dispose(); return new OASISResult<bool> { Result = true }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string modelName, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var json = await GetAsync($"v1/models/{Uri.EscapeDataString(modelName)}");
                var status = json["model_version_status"]?[0];
                var avatar = new Avatar { Username = modelName };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.IntelOpenVINOOASIS] = modelName;
                avatar.MetaData["model_name"] = modelName;
                avatar.MetaData["version"] = status?["version"]?.ToString() ?? "1";
                avatar.MetaData["state"] = status?["state"]?.ToString() ?? "UNKNOWN";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenVINO LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                // key = model name, optionally model_name:version
                var parts = key.Split(':');
                var model = parts[0];
                var version = parts.Length > 1 ? parts[1] : "1";
                var json = await GetAsync($"v1/models/{Uri.EscapeDataString(model)}/versions/{version}/metadata");
                var holon = new Holon { Name = $"OpenVINO Model {key}" };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.IntelOpenVINOOASIS] = key;
                holon.MetaData["model_name"] = model;
                holon.MetaData["version"] = version;
                holon.MetaData["inputs"] = json["metadata"]?["inputs"]?.ToString(Formatting.None) ?? "{}";
                holon.MetaData["outputs"] = json["metadata"]?["outputs"]?.ToString(Formatting.None) ?? "{}";
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenVINO LoadHolon failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
                var model = h.MetaData.ContainsKey("model_name") ? h.MetaData["model_name"] : _defaultModel;
                var version = h.MetaData.ContainsKey("version") ? h.MetaData["version"] : "1";
                var inputData = h.MetaData.ContainsKey("inputs") ? JsonConvert.DeserializeObject(h.MetaData["inputs"]) : new { };
                var body = new { inputs = inputData };
                var json = await PostAsync($"v1/models/{model}/versions/{version}:predict", body);
                h.ProviderUniqueStorageKey[Core.Enums.ProviderType.IntelOpenVINOOASIS] = h.Id.ToString();
                h.MetaData["outputs"] = json["outputs"]?.ToString(Formatting.None) ?? json.ToString(Formatting.None);
                r.Result = h;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"OpenVINO SaveHolon (predict) failed: {ex.Message}", ex); }
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
