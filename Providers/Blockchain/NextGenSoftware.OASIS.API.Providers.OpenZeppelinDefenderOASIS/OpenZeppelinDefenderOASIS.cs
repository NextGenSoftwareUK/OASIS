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
        /// <summary>OpenZeppelin Defender is a hosted operations platform (relayers, autotasks, approval proposals), not a
        /// record store. The storage members used to create a scheduled autotask per avatar save and a governance proposal per
        /// holon save on the live account; they now fail explicitly instead of causing those side effects.</summary>
        private static OASISResult<T> NoRecordStore<T>()
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, "OpenZeppelinDefenderOASIS has no OASIS record store; Defender manages relayers, autotasks and approval proposals.");
            return result;
        }

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
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string autotaskId, int v = 0) { await Task.CompletedTask; return NoRecordStore<IAvatar>(); }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) { await Task.CompletedTask; return NoRecordStore<IAvatar>(); }
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { await Task.CompletedTask; return NoRecordStore<IAvatar>(); }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { await Task.CompletedTask; return NoRecordStore<IAvatar>(); }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IAvatar>>(); }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar) { await Task.CompletedTask; return NoRecordStore<IAvatar>(); }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) { await Task.CompletedTask; return NoRecordStore<bool>(); }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string autotaskId, bool softDelete = true) { await Task.CompletedTask; return NoRecordStore<bool>(); }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { await Task.CompletedTask; return NoRecordStore<bool>(); }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string autotaskId, bool s = true) { await Task.CompletedTask; return NoRecordStore<bool>(); }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { await Task.CompletedTask; return NoRecordStore<IAvatarDetail>(); }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { await Task.CompletedTask; return NoRecordStore<IAvatarDetail>(); }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string autotaskId, int v = 0) { await Task.CompletedTask; return NoRecordStore<IAvatarDetail>(); }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { await Task.CompletedTask; return NoRecordStore<IAvatarDetail>(); }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IAvatarDetail>>(); }

        // Holon = Defender proposal (governance/multisig action)
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string proposalId, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string proposalId) { await Task.CompletedTask; return NoRecordStore<IHolon>(); }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string autotaskId, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0) { await Task.CompletedTask; return NoRecordStore<ISearchResults>(); }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string autotaskId, int v = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) { await Task.CompletedTask; return NoRecordStore<IEnumerable<IHolon>>(); }
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { await Task.CompletedTask; return NoRecordStore<bool>(); }

        // Sync wrappers
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int v = 0) => NoRecordStore<IAvatar>();
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string k, int v = 0) => NoRecordStore<IAvatar>();
        public override OASISResult<IAvatar> LoadAvatarByUsername(string u, int v = 0) => NoRecordStore<IAvatar>();
        public override OASISResult<IAvatar> LoadAvatarByEmail(string e, int v = 0) => NoRecordStore<IAvatar>();
        public override OASISResult<IAvatar> SaveAvatar(IAvatar a) => NoRecordStore<IAvatar>();
        public override OASISResult<bool> DeleteAvatar(Guid id, bool s = true) => NoRecordStore<bool>();
        public override OASISResult<bool> DeleteAvatar(string k, bool s = true) => NoRecordStore<bool>();
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool s = true) => NoRecordStore<bool>();
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool s = true) => NoRecordStore<bool>();
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int v = 0) => NoRecordStore<IEnumerable<IAvatar>>();
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int v = 0) => NoRecordStore<IAvatarDetail>();
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int v = 0) => NoRecordStore<IAvatarDetail>();
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int v = 0) => NoRecordStore<IAvatarDetail>();
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail ad) => NoRecordStore<IAvatarDetail>();
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int v = 0) => NoRecordStore<IEnumerable<IAvatarDetail>>();
        public override OASISResult<IHolon> LoadHolon(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => NoRecordStore<IHolon>();
        public override OASISResult<IHolon> LoadHolon(string k, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => NoRecordStore<IHolon>();
        public override OASISResult<IHolon> SaveHolon(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) => NoRecordStore<IHolon>();
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> h, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) => NoRecordStore<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => NoRecordStore<IEnumerable<IHolon>>();
        public override OASISResult<IHolon> DeleteHolon(Guid id) => NoRecordStore<IHolon>();
        public override OASISResult<IHolon> DeleteHolon(string k) => NoRecordStore<IHolon>();
        public override OASISResult<ISearchResults> Search(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0) => NoRecordStore<ISearchResults>();
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => NoRecordStore<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => NoRecordStore<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => NoRecordStore<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => NoRecordStore<IEnumerable<IHolon>>();
        public override OASISResult<bool> Import(IEnumerable<IHolon> h) => NoRecordStore<bool>();
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int v = 0) => NoRecordStore<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int v = 0) => NoRecordStore<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int v = 0) => NoRecordStore<IEnumerable<IHolon>>();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int v = 0) => NoRecordStore<IEnumerable<IHolon>>();
    }
}
