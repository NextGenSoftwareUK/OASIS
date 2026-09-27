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
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EigenLayerOASIS
{
    public class EigenLayerOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;

        public EigenLayerOASIS(string baseUrl = "https://api.eigenlayer.xyz/api/v1")
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
            ProviderName = "EigenLayerOASIS";
            ProviderDescription = "EigenLayer restaking protocol staker and operator data provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EigenLayerOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain);
        }

        private async Task<JToken> GetAsync(string path)
        {
            var resp = await _http.GetAsync(path);
            resp.EnsureSuccessStatusCode();
            return JToken.Parse(await resp.Content.ReadAsStringAsync());
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try { await GetAsync("operators?limit=1"); r.Result = true; r.Message = "EigenLayerOASIS activated."; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer activation failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _http.Dispose(); return new OASISResult<bool> { Result = true, Message = "EigenLayerOASIS deactivated." }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = EigenLayer staker
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var json = await GetAsync($"stakers/{address}");
                var avatar = new Avatar { Username = address };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.EigenLayerOASIS] = address;
                avatar.MetaData["address"] = address;
                avatar.MetaData["total_shares"] = json["totalShares"]?.ToString() ?? "0";
                avatar.MetaData["shares"] = json["shares"]?.ToString(Formatting.None) ?? "[]";
                avatar.MetaData["withdrawals"] = json["withdrawals"]?.ToString(Formatting.None) ?? "[]";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer LoadAvatarByProviderKey failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => await LoadAvatarByProviderKeyAsync(u, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "EigenLayer stakers are identified by Ethereum address, not OASIS GUID."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "EigenLayer does not support email-based lookup."); return r; }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var json = await GetAsync("stakers?limit=50");
                var stakers = (json["stakers"] as JArray) ?? (json as JArray);
                var avatars = new List<IAvatar>();
                if (stakers != null)
                    foreach (var s in stakers)
                    {
                        var addr = s["address"]?.ToString() ?? "";
                        var avatar = new Avatar { Username = addr };
                        avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.EigenLayerOASIS] = addr;
                        avatar.MetaData["address"] = addr;
                        avatar.MetaData["total_shares"] = s["totalShares"]?.ToString() ?? "0";
                        avatars.Add(avatar);
                    }
                r.Result = avatars;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer LoadAllAvatars failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "EigenLayer is a read-only data provider. Staking requires on-chain transactions via smart contract."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "EigenLayer staker accounts are on-chain and cannot be deleted."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "EigenLayer staker accounts are on-chain and cannot be deleted."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "EigenLayer does not support email-based operations."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "EigenLayer staker accounts are on-chain and cannot be deleted."); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "EigenLayer avatar detail requires an Ethereum address."); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "EigenLayer does not support email-based lookup."); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await GetAsync($"stakers/{address}");
                var detail = new AvatarDetail { Username = address };
                detail.MetaData["address"] = address;
                detail.MetaData["total_shares"] = json["totalShares"]?.ToString() ?? "0";
                detail.MetaData["shares"] = json["shares"]?.ToString(Formatting.None) ?? "[]";
                detail.MetaData["delegated_to"] = json["delegatedTo"]?.ToString() ?? "";
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer LoadAvatarDetailByUsername failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "EigenLayer is a read-only data provider."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var json = await GetAsync("stakers?limit=50");
                var stakers = (json["stakers"] as JArray) ?? (json as JArray);
                var details = new List<IAvatarDetail>();
                if (stakers != null)
                    foreach (var s in stakers)
                    {
                        var detail = new AvatarDetail { Username = s["address"]?.ToString() ?? "" };
                        detail.MetaData["address"] = s["address"]?.ToString() ?? "";
                        detail.MetaData["total_shares"] = s["totalShares"]?.ToString() ?? "0";
                        details.Add(detail);
                    }
                r.Result = details;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer LoadAllAvatarDetails failed: {ex.Message}", ex); }
            return r;
        }

        // Holon = EigenLayer operator
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var json = await GetAsync($"operators/{key}");
                var holon = new Holon { Name = json["name"]?.ToString() ?? key };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.EigenLayerOASIS] = key;
                holon.MetaData["address"] = key;
                holon.MetaData["name"] = json["name"]?.ToString() ?? "";
                holon.MetaData["total_stakers"] = json["totalStakers"]?.ToString() ?? "0";
                holon.MetaData["tvl_eth"] = json["tvl"]?.ToString() ?? "0";
                holon.MetaData["website"] = json["website"]?.ToString() ?? "";
                holon.MetaData["description"] = json["description"]?.ToString() ?? "";
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "EigenLayer operators are identified by Ethereum address, not OASIS GUID."); return r; }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var json = await GetAsync("operators?limit=50");
                var operators = (json["operators"] as JArray) ?? (json as JArray);
                var holons = new List<IHolon>();
                if (operators != null)
                    foreach (var op in operators)
                    {
                        var h = new Holon { Name = op["name"]?.ToString() ?? op["address"]?.ToString() ?? "" };
                        h.ProviderUniqueStorageKey[Core.Enums.ProviderType.EigenLayerOASIS] = op["address"]?.ToString() ?? "";
                        h.MetaData["address"] = op["address"]?.ToString() ?? "";
                        h.MetaData["name"] = op["name"]?.ToString() ?? "";
                        h.MetaData["total_stakers"] = op["totalStakers"]?.ToString() ?? "0";
                        h.MetaData["tvl_eth"] = op["tvl"]?.ToString() ?? "0";
                        holons.Add(h);
                    }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer LoadAllHolons failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "EigenLayer is a read-only data provider. Operator registration requires on-chain transactions."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> h, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "EigenLayer is a read-only data provider."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "EigenLayer on-chain data is immutable."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "EigenLayer on-chain data is immutable."); return r; }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Use LoadHolonsForParent(string operatorAddress) to load AVS list for an operator."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string operatorAddress, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var json = await GetAsync($"operators/{operatorAddress}/avs");
                var avsList = (json["avs"] as JArray) ?? (json as JArray);
                var holons = new List<IHolon>();
                if (avsList != null)
                    foreach (var avs in avsList)
                    {
                        var h = new Holon { Name = avs["name"]?.ToString() ?? avs["address"]?.ToString() ?? "" };
                        h.MetaData["address"] = avs["address"]?.ToString() ?? "";
                        h.MetaData["name"] = avs["name"]?.ToString() ?? "";
                        h.MetaData["operator"] = operatorAddress;
                        holons.Add(h);
                    }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            if (mk == "address") { var h = await LoadHolonAsync(mv); return new OASISResult<IEnumerable<IHolon>> { Result = h.Result != null ? new List<IHolon> { h.Result } : new List<IHolon>(), IsError = h.IsError, Message = h.Message }; }
            var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"EigenLayer does not support metadata search by '{mk}'. Supported: address."); return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        { foreach (var kv in m) { var p = await LoadHolonsByMetaDataAsync(kv.Key, kv.Value, t); if (!p.IsError) return p; } return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var results = new SearchResults();
                string query = sp?.FilterByMetaData?.ContainsKey("query") == true ? sp.FilterByMetaData["query"] : "";
                if (!string.IsNullOrEmpty(query))
                {
                    try { var op = await LoadHolonAsync(query); if (!op.IsError) results.SearchResultHolons = new List<IHolon> { op.Result }; } catch { }
                    try { var staker = await LoadAvatarByProviderKeyAsync(query); if (!staker.IsError) results.SearchResultAvatars = new List<IAvatar> { staker.Result }; } catch { }
                }
                r.Result = results;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"EigenLayer Search failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "EigenLayer export requires an Ethereum address."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string address, int v = 0) => await LoadHolonsForParentAsync(address);
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "EigenLayer does not support email-based export."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) => await LoadAllHolonsAsync();
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "EigenLayer is a read-only data provider. Operator registration requires on-chain transactions."); return r; }

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
    }
}
