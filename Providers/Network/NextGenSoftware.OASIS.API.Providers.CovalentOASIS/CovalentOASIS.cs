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

namespace NextGenSoftware.OASIS.API.Providers.CovalentOASIS
{
    public class CovalentOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _chainName;
        private bool _isActivated;

        public CovalentOASIS(string apiKey = "", string chainName = "eth-mainnet")
        {
            _apiKey = apiKey;
            _chainName = chainName;
            _http = new HttpClient { BaseAddress = new Uri("https://api.covalenthq.com/v1/") };
            if (!string.IsNullOrEmpty(apiKey))
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            ProviderName = "CovalentOASIS";
            ProviderDescription = "Covalent Unified Blockchain Data API — multi-chain token balances, transactions and NFT data.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CovalentOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
        }

        private async Task<JObject> GetJsonAsync(string path)
        {
            var resp = await _http.GetAsync(path);
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                var json = await GetJsonAsync("chains/");
                _isActivated = true;
                r.Result = true;
                r.Message = "CovalentOASIS activated — " + (json["data"]?["items"]?.HasValues == true ? "chains loaded" : "connected");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"CovalentOASIS activation failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            _isActivated = false;
            _http.Dispose();
            r.Result = true;
            return r;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = blockchain address; ProviderUniqueStorageKey = address
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                // Load by stored id — need key lookup; return empty Avatar
                r.Result = new Avatar { Id = id };
                r.Message = "Covalent: load by GUID requires ProviderUniqueStorageKey (address). Use LoadAvatarByProviderKeyAsync.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, ex.Message, ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var json = await GetJsonAsync($"{_chainName}/address/{address}/balances_v2/");
                var data = json["data"];
                var avatar = new Avatar
                {
                    Username = address,
                    ProviderUniqueStorageKey = new Dictionary<ProviderType, string> { { Core.Enums.ProviderType.CovalentOASIS, address } }
                };
                avatar.MetaData["chain"] = _chainName;
                avatar.MetaData["quote_currency"] = data?["quote_currency"]?.ToString() ?? "USD";
                avatar.MetaData["items_count"] = data?["items"]?.HasValues == true ? data["items"].ToString() : "[]";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Covalent LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string address, int v = 0) =>
            await LoadAvatarByProviderKeyAsync(address, v);

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "Covalent does not support email-based lookup");
            return r;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                // Covalent is read-only; record the address as the provider key
                if (!string.IsNullOrEmpty(avatar.Username))
                    avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.CovalentOASIS] = avatar.Username;
                r.Result = avatar;
                r.Message = "Covalent is a read-only data API; avatar address registered locally.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, ex.Message, ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool soft = true)
        {
            var r = new OASISResult<bool>();
            r.Result = true;
            r.Message = "Covalent is read-only; no data deleted.";
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            r.Result = new List<IAvatar>();
            r.Message = "Covalent does not support listing all avatars.";
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            r.Result = new Holon { Id = id };
            r.Message = "Use LoadHolonAsync(string key) with a blockchain address or tx hash.";
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                // key = address → fetch token balances; key starting with 0x and 66 chars = tx hash
                JObject json;
                var holon = new Holon();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.CovalentOASIS] = key;

                if (key.StartsWith("0x") && key.Length == 66)
                {
                    // Transaction hash
                    json = await GetJsonAsync($"{_chainName}/transaction_v2/{key}/");
                    var tx = json["data"]?["items"]?.First;
                    holon.Name = $"Transaction {key[..10]}...";
                    holon.MetaData["tx_hash"] = key;
                    holon.MetaData["from"] = tx?["from_address"]?.ToString() ?? "";
                    holon.MetaData["to"] = tx?["to_address"]?.ToString() ?? "";
                    holon.MetaData["value"] = tx?["value"]?.ToString() ?? "0";
                    holon.MetaData["block_height"] = tx?["block_height"]?.ToString() ?? "";
                }
                else
                {
                    // Address → token balances
                    json = await GetJsonAsync($"{_chainName}/address/{key}/balances_v2/");
                    var data = json["data"];
                    holon.Name = $"Covalent Portfolio {key[..8]}...";
                    holon.MetaData["address"] = key;
                    holon.MetaData["chain"] = _chainName;
                    holon.MetaData["balances"] = data?["items"]?.ToString() ?? "[]";
                }
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Covalent LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
            r.Result = h;
            r.Message = "Covalent is a read-only data API; holon not persisted externally.";
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false)
        {
            var results = new List<IHolon>();
            foreach (var h in holons) { var sr = await SaveHolonAsync(h); if (sr.Result != null) results.Add(sr.Result); }
            return new OASISResult<IEnumerable<IHolon>> { Result = results };
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                // Search by address
                var holons = new List<IHolon>();
                if (!string.IsNullOrEmpty(sp?.SearchQuery))
                {
                    var hr = await LoadHolonAsync(sp.SearchQuery);
                    if (!hr.IsError && hr.Result != null) holons.Add(hr.Result);
                }
                r.Result = new SearchResults { Holons = new Holons(holons) };
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, ex.Message, ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "Not supported by Covalent");
            return r;
        }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { var r = new OASISResult<IAvatarDetail>(); r.Result = ad; return r; }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) { return new OASISResult<IEnumerable<IAvatarDetail>> { Result = new List<IAvatarDetail>() }; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) { return new OASISResult<bool> { Result = true }; }
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { return new OASISResult<bool> { Result = true }; }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) { return new OASISResult<bool> { Result = true }; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { return new OASISResult<IHolon> { Result = new Holon { Id = id } }; }

        // Sync wrappers
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
