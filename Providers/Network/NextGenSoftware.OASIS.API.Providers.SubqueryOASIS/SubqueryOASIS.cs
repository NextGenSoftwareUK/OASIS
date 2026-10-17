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
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SubqueryOASIS
{
    public class SubqueryOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;

        public SubqueryOASIS(string endpointUrl = "https://api.subquery.network/sq/subquery/subql-starter/", string apiKey = "")
        {
            _http = new HttpClient { BaseAddress = new Uri(endpointUrl) };
            if (!string.IsNullOrEmpty(apiKey))
                _http.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            ProviderName = "SubqueryOASIS";
            ProviderDescription = "SubQuery GraphQL blockchain indexing provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SubqueryOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
        }

        private async Task<JObject> GraphQLAsync(string query, object variables = null)
        {
            var body = variables != null ? new { query, variables } : (object)new { query };
            var resp = await _http.PostAsync("", new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try { var res = await GraphQLAsync("{_metadata{lastProcessedHeight}}"); r.Result = res["data"] != null; r.Message = "SubqueryOASIS activated"; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"SubQuery activation failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _http.Dispose(); return new OASISResult<bool> { Result = true }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = account/address
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var query = "query($id:String!){accounts(filter:{id:{equalTo:$id}}){nodes{id firstTransferBlock transferCount}}}";
                var result = await GraphQLAsync(query, new { id = address });
                var acct = result["data"]?["accounts"]?["nodes"]?.First;
                if (acct == null || acct.Type == JTokenType.Null)
                { OASISErrorHandling.HandleError(ref r, $"Account {address} not found"); return r; }
                var avatar = new Avatar { Username = address };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SubqueryOASIS] = address;
                avatar.MetaData["first_transfer_block"] = acct["firstTransferBlock"]?.ToString() ?? "";
                avatar.MetaData["transfer_count"] = acct["transferCount"]?.ToString() ?? "0";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"SubQuery LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        // Holon = extrinsic/transaction
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var query = "query($id:String!){extrinsics(filter:{id:{equalTo:$id}}){nodes{id method blockHeight isSigned signer}}}";
                var result = await GraphQLAsync(query, new { id = key });
                var extrinsic = result["data"]?["extrinsics"]?["nodes"]?.First;
                var holon = new Holon { Name = extrinsic?["method"]?.ToString() ?? key };
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SubqueryOASIS] = key;
                holon.MetaData["method"] = extrinsic?["method"]?.ToString() ?? "";
                holon.MetaData["block_height"] = extrinsic?["blockHeight"]?.ToString() ?? "";
                holon.MetaData["signer"] = extrinsic?["signer"]?.ToString() ?? "";
                holon.MetaData["is_signed"] = extrinsic?["isSigned"]?.ToString() ?? "false";
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"SubQuery LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        { if (h.Id == Guid.Empty) h.Id = Guid.NewGuid(); return new OASISResult<IHolon> { Result = h, Message = "SubQuery is a read-only indexer; data must be submitted on-chain." }; }

        // LoadHolonsForParent = extrinsics by signer address
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var query = "query($addr:String!){extrinsics(filter:{signer:{equalTo:$addr}},first:50,orderBy:BLOCK_HEIGHT_DESC){nodes{id method blockHeight isSigned}}}";
                var result = await GraphQLAsync(query, new { addr = k });
                var nodes = result["data"]?["extrinsics"]?["nodes"] as JArray ?? new JArray();
                var holons = new List<IHolon>();
                foreach (var node in nodes)
                {
                    var holon = new Holon { Name = node["method"]?.ToString() ?? node["id"]?.ToString() ?? "" };
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SubqueryOASIS] = node["id"]?.ToString() ?? "";
                    holon.MetaData["method"] = node["method"]?.ToString() ?? "";
                    holon.MetaData["block_height"] = node["blockHeight"]?.ToString() ?? "";
                    holon.MetaData["is_signed"] = node["isSigned"]?.ToString() ?? "false";
                    holons.Add(holon);
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"SubQuery LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        // LoadAllHolons = recent extrinsics
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var query = "{extrinsics(first:50,orderBy:BLOCK_HEIGHT_DESC){nodes{id method blockHeight isSigned signer}}}";
                var result = await GraphQLAsync(query);
                var nodes = result["data"]?["extrinsics"]?["nodes"] as JArray ?? new JArray();
                var holons = new List<IHolon>();
                foreach (var node in nodes)
                {
                    var holon = new Holon { Name = node["method"]?.ToString() ?? node["id"]?.ToString() ?? "" };
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SubqueryOASIS] = node["id"]?.ToString() ?? "";
                    holon.MetaData["method"] = node["method"]?.ToString() ?? "";
                    holon.MetaData["block_height"] = node["blockHeight"]?.ToString() ?? "";
                    holon.MetaData["signer"] = node["signer"]?.ToString() ?? "";
                    holons.Add(holon);
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"SubQuery LoadAllHolons failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var q = sp?.FilterByMetaData?.ContainsKey("query") == true ? sp.FilterByMetaData["query"] : "";
                var query = "query($m:String!){extrinsics(filter:{method:{includesInsensitive:$m}},first:20){nodes{id method blockHeight signer}}}";
                var result = await GraphQLAsync(query, new { m = q });
                var nodes = result["data"]?["extrinsics"]?["nodes"] as JArray ?? new JArray();
                var holons = new List<IHolon>();
                foreach (var node in nodes)
                {
                    var holon = new Holon { Name = node["method"]?.ToString() ?? "" };
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SubqueryOASIS] = node["id"]?.ToString() ?? "";
                    holon.MetaData["block_height"] = node["blockHeight"]?.ToString() ?? "";
                    holon.MetaData["signer"] = node["signer"]?.ToString() ?? "";
                    holons.Add(holon);
                }
                r.Result = new SearchResults { SearchResultHolons = holons };
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, ex.Message, ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0)
        {
            return await LoadHolonsForParentAsync(u);
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "SubQuery is a read-only blockchain indexer; data cannot be imported."); return r; }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it has no record keyed by an OASIS holon Guid. Load by provider key instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it has no record keyed by an OASIS avatar Guid. Load by provider key (e.g. wallet address) instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => await LoadAvatarByProviderKeyAsync(u, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "SubQuery does not support email-based lookup."); return r; }
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it cannot save or update avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it cannot save or update avatar details."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it has no OASIS avatar index to list."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) { var r = new OASISResult<IEnumerable<IHolon>>(); var l = new List<IHolon>(); foreach (var h in holons) { var sr = await SaveHolonAsync(h, sc, rec, md, coe, scop); if (sr.IsError) { OASISErrorHandling.HandleError(ref r, $"Error saving holon {h.Id}: {sr.Message}"); if (!coe) return r; } else l.Add(sr.Result); } r.Result = l; return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; holons cannot be looked up by OASIS parent Guid. Use LoadHolonsForParentAsync with a provider key."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { if (mk == "signer") return await LoadHolonsForParentAsync(mv, t, lc, rec, md, cd, coe, lcfp, v); return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it has no holon metadata index."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it cannot delete holons."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "SubQuery is read-only."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it has no OASIS avatar index, so avatar data cannot be exported by id or email."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "SubqueryOASIS is a read-only blockchain indexing API; it has no OASIS avatar index, so avatar data cannot be exported by id or email."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "SubQuery requires an address to export data."); return r; }
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
