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

namespace NextGenSoftware.OASIS.API.Providers.QuickNodeOASIS
{
    public class QuickNodeOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;

        public QuickNodeOASIS(string endpointUrl = "https://api.quicknode.com/")
        {
            _http = new HttpClient { BaseAddress = new Uri(endpointUrl.TrimEnd('/') + "/") };
            ProviderName = "QuickNodeOASIS";
            ProviderDescription = "QuickNode blockchain node JSON-RPC and API provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.QuickNodeOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
        }

        private async Task<JObject> RpcAsync(string method, params object[] @params)
        {
            var body = new { id = 1, jsonrpc = "2.0", method, @params };
            var resp = await _http.PostAsync("", new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try { var res = await RpcAsync("eth_blockNumber"); r.Result = res["result"] != null; r.Message = "QuickNodeOASIS activated"; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"QuickNode activation failed: {ex.Message}", ex); }
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
                var balResult = await RpcAsync("eth_getBalance", address, "latest");
                var tokenResult = await RpcAsync("qn_getWalletTokenBalance", new { wallet = address, page = 1, perPage = 10 });
                var avatar = new Avatar { Username = address };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.QuickNodeOASIS] = address;
                avatar.MetaData["eth_balance_wei"] = balResult["result"]?.ToString() ?? "0x0";
                avatar.MetaData["tokens"] = tokenResult["result"]?["result"]?.ToString(Formatting.None) ?? "[]";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"QuickNode LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var holon = new Holon();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.QuickNodeOASIS] = key;
                if (key.StartsWith("0x") && key.Length == 66)
                {
                    var result = await RpcAsync("eth_getTransactionByHash", key);
                    var tx = result["result"];
                    holon.Name = $"Tx {key[..10]}...";
                    holon.MetaData["from"] = tx?["from"]?.ToString() ?? "";
                    holon.MetaData["to"] = tx?["to"]?.ToString() ?? "";
                    holon.MetaData["value"] = tx?["value"]?.ToString() ?? "0x0";
                    holon.MetaData["block_number"] = tx?["blockNumber"]?.ToString() ?? "";
                    holon.MetaData["gas"] = tx?["gas"]?.ToString() ?? "";
                }
                else
                {
                    var result = await RpcAsync("eth_getBlockByHash", key, false);
                    var block = result["result"];
                    holon.Name = $"Block {block?["number"]?.ToString() ?? key}";
                    holon.MetaData["hash"] = block?["hash"]?.ToString() ?? "";
                    holon.MetaData["tx_count"] = (block?["transactions"] as JArray)?.Count.ToString() ?? "0";
                    holon.MetaData["timestamp"] = block?["timestamp"]?.ToString() ?? "";
                    holon.MetaData["miner"] = block?["miner"]?.ToString() ?? "";
                }
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"QuickNode LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it cannot save holons."); return r; }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var result = await RpcAsync("qn_getTransactionsByAddress", new { address = k, page = 1, perPage = 50 });
                var txns = result["result"]?["paginatedItems"] as JArray ?? new JArray();
                var holons = new List<IHolon>();
                foreach (var tx in txns)
                {
                    var holon = new Holon { Name = $"Tx {tx["transactionHash"]?.ToString()?[..10]}..." };
                    holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.QuickNodeOASIS] = tx["transactionHash"]?.ToString() ?? "";
                    holon.MetaData["from"] = tx["fromAddress"]?.ToString() ?? "";
                    holon.MetaData["to"] = tx["toAddress"]?.ToString() ?? "";
                    holon.MetaData["value"] = tx["value"]?.ToString() ?? "0";
                    holon.MetaData["block_number"] = tx["blockNumber"]?.ToString() ?? "";
                    holons.Add(holon);
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"QuickNode LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "QuickNode requires an address; use LoadHolonsForParentAsync with an Ethereum address.");
            return r;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var q = sp?.FilterByMetaData?.ContainsKey("query") == true ? sp.FilterByMetaData["query"] : "";
                var holons = new List<IHolon>();
                if (!string.IsNullOrEmpty(q))
                {
                    var hr = await LoadHolonAsync(q);
                    if (!hr.IsError && hr.Result != null) holons.Add(hr.Result);
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
                var tokenResult = await RpcAsync("qn_getWalletTokenBalance", new { wallet = u, page = 1, perPage = 50 });
                if (tokenResult["result"]?["result"] is JArray tokens)
                {
                    foreach (var token in tokens)
                    {
                        var holon = new Holon { Name = token["name"]?.ToString() ?? token["symbol"]?.ToString() ?? "" };
                        holon.MetaData["symbol"] = token["symbol"]?.ToString() ?? "";
                        holon.MetaData["balance"] = token["amount"]?.ToString() ?? "0";
                        holons.Add(holon);
                    }
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"QuickNode ExportAllData failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "QuickNode: use eth_sendRawTransaction to submit data on-chain."); return r; }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it has no record keyed by an OASIS holon Guid. Load by provider key instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it has no record keyed by an OASIS avatar Guid. Load by provider key (e.g. wallet address) instead."); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0) => await LoadAvatarByProviderKeyAsync(u, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "QuickNode does not support email-based lookup."); return r; }
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a) { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it cannot save or update avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it cannot delete avatars."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it cannot save or update avatar details."); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it has no OASIS avatar index to list."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) { var r = new OASISResult<IEnumerable<IHolon>>(); var l = new List<IHolon>(); foreach (var h in holons) { var sr = await SaveHolonAsync(h, sc, rec, md, coe, scop); if (sr.IsError) { OASISErrorHandling.HandleError(ref r, $"Error saving holon {h.Id}: {sr.Message}"); if (!coe) return r; } else l.Add(sr.Result); } r.Result = l; return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; holons cannot be looked up by OASIS parent Guid. Use LoadHolonsForParentAsync with a provider key."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { if (mk == "address") return await LoadHolonsForParentAsync(mv, t, lc, rec, md, cd, coe, lcfp, v); return new OASISResult<IEnumerable<IHolon>> { Result = new List<IHolon>() }; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it has no holon metadata index."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it cannot delete holons."); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "QuickNode: on-chain data cannot be deleted."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it has no OASIS avatar index, so avatar data cannot be exported by id or email."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "QuickNodeOASIS is a read-only blockchain RPC endpoint; it has no OASIS avatar index, so avatar data cannot be exported by id or email."); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "QuickNode requires a wallet address to export data."); return r; }
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
