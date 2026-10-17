using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
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

namespace NextGenSoftware.OASIS.API.Providers.NansenOASIS
{
    public class NansenOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;

        public NansenOASIS(string apiKey)
        {
            _http = new HttpClient { BaseAddress = new Uri("https://api.nansen.ai/v1/") };
            _http.DefaultRequestHeaders.Add("apiKey", apiKey);
            ProviderName = "NansenOASIS";
            ProviderDescription = "Nansen on-chain analytics and wallet intelligence provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.NansenOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
        }

        private async Task<JObject> GetAsync(string path)
        {
            var resp = await _http.GetAsync(path);
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try { var res = await GetAsync("wallet/0x000000000000000000000000000000000000dEaD"); r.Result = res != null; r.Message = "NansenOASIS activated"; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Nansen activation failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _http.Dispose(); return new OASISResult<bool> { Result = true }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var profile = await GetAsync($"wallet/{address}");
                var portfolio = await GetAsync($"wallet/{address}/portfolio");
                var avatar = new Avatar { Username = address };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.NansenOASIS] = address;
                avatar.MetaData["labels"] = profile["data"]?["labels"]?.ToString() ?? "[]";
                avatar.MetaData["portfolio_value_usd"] = portfolio["data"]?["totalValueUSD"]?.ToString() ?? "0";
                avatar.MetaData["portfolio_items"] = portfolio["data"]?["items"]?.ToString() ?? "[]";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Nansen LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        // Holon = transaction history for an address
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var result = await GetAsync($"wallet/{key}/transactions?limit=50");
                var holon = new Holon { Name = $"Nansen Wallet {key[..8]}..." };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.NansenOASIS] = key;
                holon.MetaData["transactions"] = result["data"]?.ToString() ?? "[]";
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Nansen LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it cannot save holons."); return r; }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var result = await GetAsync($"wallet/{k}/transactions?limit=50");
                var txns = result["data"] as JArray ?? new JArray();
                var holons = new List<IHolon>();
                foreach (var tx in txns)
                {
                    var holon = new Holon { Name = $"Tx {tx["hash"]?.ToString()?[..10]}..." };
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.NansenOASIS] = tx["hash"]?.ToString() ?? "";
                    holon.MetaData["from"] = tx["from"]?.ToString() ?? "";
                    holon.MetaData["to"] = tx["to"]?.ToString() ?? "";
                    holon.MetaData["value"] = tx["value"]?.ToString() ?? "0";
                    holon.MetaData["timestamp"] = tx["timestamp"]?.ToString() ?? "";
                    holons.Add(holon);
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Nansen LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Nansen requires a wallet address; use LoadHolonsForParentAsync with an Ethereum address.");
            return r;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var q = sp?.FilterByMetaData?.ContainsKey("address") == true ? sp.FilterByMetaData["address"] :
                        sp?.FilterByMetaData?.ContainsKey("query") == true ? sp.FilterByMetaData["query"] : "";
                var holons = new List<IHolon>();
                if (!string.IsNullOrEmpty(q))
                {
                    var hr = await LoadAvatarByProviderKeyAsync(q);
                    if (!hr.IsError && hr.Result != null)
                    {
                        var holon = new Holon { Name = $"Nansen Wallet {q[..8]}..." };
                        holon.MetaData["labels"] = hr.Result.MetaData["labels"]?.ToString() ?? "[]";
                        holon.MetaData["portfolio_value_usd"] = hr.Result.MetaData["portfolio_value_usd"]?.ToString() ?? "0";
                        holons.Add(holon);
                    }
                }
                r.Result = new SearchResults { SearchResultHolons = holons };
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, ex.Message, ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                var txResult = await LoadHolonsForParentAsync(u);
                if (!txResult.IsError && txResult.Result != null) holons.AddRange(txResult.Result);
                var portfolio = await GetAsync($"wallet/{u}/portfolio");
                if (portfolio["data"]?["items"] is JArray items)
                {
                    foreach (var item in items)
                    {
                        var holon = new Holon { Name = item["symbol"]?.ToString() ?? item["name"]?.ToString() ?? "" };
                        holon.MetaData["balance"] = item["balance"]?.ToString() ?? "0";
                        holon.MetaData["value_usd"] = item["valueUSD"]?.ToString() ?? "0";
                        holons.Add(holon);
                    }
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Nansen ExportAllData failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Nansen is a read-only analytics API; data cannot be imported."); return r; }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it has no record keyed by an OASIS holon Guid. Load by provider key instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it has no record keyed by an OASIS avatar Guid. Load by provider key (e.g. wallet address) instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => await LoadAvatarByProviderKeyAsync(u, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "Nansen does not support email-based lookup."); return r; }
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it cannot save or update avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it cannot save or update avatar details."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it has no OASIS avatar index to list."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) { var r = new OASISResult<IEnumerable<IHolon>>(); var l = new List<IHolon>(); foreach (var h in holons) { var sr = await SaveHolonAsync(h, sc, rec, md, coe, scop); if (sr.IsError) { OASISErrorHandling.HandleError(ref r, $"Error saving holon {h.Id}: {sr.Message}"); if (!coe) return r; } else l.Add(sr.Result); } r.Result = l; return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; holons cannot be looked up by OASIS parent Guid. Use LoadHolonsForParentAsync with a provider key."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { if (mk == "address") return await LoadHolonsForParentAsync(mv, t, lc, rec, md, cd, coe, lcfp, v); return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it has no holon metadata index."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it cannot delete holons."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "Nansen is read-only."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it has no OASIS avatar index, so avatar data cannot be exported by id or email."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "NansenOASIS is a read-only wallet analytics API; it has no OASIS avatar index, so avatar data cannot be exported by id or email."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Nansen requires a wallet address to export data."); return r; }
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
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int v = 0) => ExportAllDataForAvatarByIdAsync(id, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int v = 0) => ExportAllDataForAvatarByUsernameAsync(u, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int v = 0) => ExportAllDataForAvatarByEmailAsync(e, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int v = 0) => ExportAllAsync(v).Result;
        public override OASISResult<bool> DeleteAvatar(string k, bool s = true) => DeleteAvatarAsync(k, s).Result;
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool s = true) => DeleteAvatarByEmailAsync(e, s).Result;
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool s = true) => DeleteAvatarByUsernameAsync(u, s).Result;
        public override OASISResult<IAvatar> LoadAvatarByEmail(string e, int v = 0) => LoadAvatarByEmailAsync(e, v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int v = 0) => LoadAvatarDetailByEmailAsync(e, v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int v = 0) => LoadAvatarDetailByUsernameAsync(u, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsForParentAsync(id, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsForParentAsync(k, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsByMetaDataAsync(mk, mv, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsByMetaDataAsync(m, mm, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IHolon> DeleteHolon(string k) { var r = DeleteHolonAsync(k).Result; return new OASISResult<IHolon> { IsError = r.IsError, Message = r.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> h) => ImportAsync(h).Result;
    }
}
