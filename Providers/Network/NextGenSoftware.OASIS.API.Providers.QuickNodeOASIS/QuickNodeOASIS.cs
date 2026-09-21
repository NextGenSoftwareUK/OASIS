using System;using System.Collections.Generic;using System.Net.Http;using System.Text;using System.Threading.Tasks;using Newtonsoft.Json;using Newtonsoft.Json.Linq;using NextGenSoftware.OASIS.API.Core;using NextGenSoftware.OASIS.API.Core.Enums;using NextGenSoftware.OASIS.API.Core.Helpers;using NextGenSoftware.OASIS.API.Core.Holons;using NextGenSoftware.OASIS.API.Core.Interfaces;using NextGenSoftware.OASIS.API.Core.Interfaces.Search;using NextGenSoftware.OASIS.API.Core.Objects;using NextGenSoftware.OASIS.API.Core.Objects.Search;using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.QuickNodeOASIS
{
    public class QuickNodeOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
    {
        private readonly HttpClient _http;
        private bool _isActivated;
        public QuickNodeOASIS(string endpointUrl = "https://api.quicknode.com/")
        {
            _http = new HttpClient { BaseAddress = new Uri(endpointUrl.TrimEnd('/') + "/") };
            ProviderName = "QuickNodeOASIS"; ProviderDescription = "QuickNode blockchain RPC and API infrastructure provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.QuickNodeOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network);
        }
        private async Task<JObject> RpcAsync(string method, params object[] p)
        {
            var body = new { id = 1, jsonrpc = "2.0", method, @params = p };
            var resp = await _http.PostAsync("", new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try { var res = await RpcAsync("eth_blockNumber"); _isActivated = res["result"] != null; r.Result = _isActivated; r.Message = "QuickNodeOASIS activated"; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"QuickNode activation failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _isActivated = false; _http.Dispose(); return new OASISResult<bool> { Result = true }; }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var balance = await RpcAsync("eth_getBalance", address, "latest");
                var tokenBalance = await RpcAsync("qn_getWalletTokenBalance", address, new { pageSize = 20 });
                var avatar = new Avatar { Username = address };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.QuickNodeOASIS] = address;
                var balHex = balance["result"]?.ToString() ?? "0x0";
                avatar.MetaData["eth_balance_wei"] = balHex;
                avatar.MetaData["tokens"] = tokenBalance["result"]?.ToString(Formatting.None) ?? "[]";
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
                    var tx = await RpcAsync("eth_getTransactionByHash", key);
                    var txObj = tx["result"];
                    holon.Name = $"QuickNode Tx {key[..10]}...";
                    holon.MetaData["hash"] = key;
                    holon.MetaData["from"] = txObj?["from"]?.ToString() ?? "";
                    holon.MetaData["to"] = txObj?["to"]?.ToString() ?? "";
                    holon.MetaData["value"] = txObj?["value"]?.ToString() ?? "0x0";
                    holon.MetaData["block"] = txObj?["blockNumber"]?.ToString() ?? "";
                }
                else
                {
                    var block = await RpcAsync("eth_getBlockByHash", key, false);
                    var blk = block["result"];
                    holon.Name = $"Block {blk?["number"]?.ToString() ?? key}";
                    holon.MetaData["hash"] = key;
                    holon.MetaData["tx_count"] = (blk?["transactions"] as JArray)?.Count.ToString() ?? "0";
                    holon.MetaData["timestamp"] = blk?["timestamp"]?.ToString() ?? "";
                }
                r.Result = holon;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"QuickNode LoadHolon failed: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) { if (h.Id == Guid.Empty) h.Id = Guid.NewGuid(); return new OASISResult<IHolon> { Result = h, Message = "QuickNode: use eth_sendRawTransaction to persist data on-chain." }; }
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
