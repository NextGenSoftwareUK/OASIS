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

namespace NextGenSoftware.OASIS.API.Providers.GaladrielOASIS
{
    public class GaladrielOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;
        private int _rpcId = 1;

        public GaladrielOASIS(string rpcUrl = "https://devnet.galadriel.com")
        {
            _http = new HttpClient { BaseAddress = new Uri(rpcUrl.TrimEnd('/') + "/") };
            ProviderName = "GaladrielOASIS";
            ProviderDescription = "Galadriel EVM-compatible AI oracle blockchain provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.GaladrielOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.AI);
        }

        private async Task<JObject> RpcAsync(string method, object[] parms = null)
        {
            var body = JsonConvert.SerializeObject(new
            {
                jsonrpc = "2.0",
                id = _rpcId++,
                method,
                @params = parms ?? Array.Empty<object>()
            });
            var resp = await _http.PostAsync("", new StringContent(body, Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                var json = await RpcAsync("eth_blockNumber");
                if (json["result"] != null)
                {
                    r.Result = true;
                    r.Message = $"GaladrielOASIS activated — latest block {json["result"]}";
                }
                else OASISErrorHandling.HandleError(ref r, $"Galadriel RPC returned error: {json["error"]}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Galadriel activation failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _http.Dispose();
            return new OASISResult<bool> { Result = true, Message = "GaladrielOASIS deactivated." };
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar: maps to Galadriel wallet/account ────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var balJson = await RpcAsync("eth_getBalance", new object[] { address, "latest" });
                var cntJson = await RpcAsync("eth_getTransactionCount", new object[] { address, "latest" });
                var codeJson = await RpcAsync("eth_getCode", new object[] { address, "latest" });

                var avatar = new Avatar { Username = address };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.GaladrielOASIS] = address;
                avatar.MetaData["address"] = address;
                avatar.MetaData["balance_wei"] = balJson["result"]?.ToString() ?? "0x0";
                avatar.MetaData["tx_count"] = cntJson["result"]?.ToString() ?? "0x0";
                avatar.MetaData["is_contract"] = (codeJson["result"]?.ToString() ?? "0x") != "0x" ? "true" : "false";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Galadriel LoadAvatarByProviderKey failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
            => await LoadAvatarByProviderKeyAsync(username, version);

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "Galadriel avatars (accounts) are identified by Ethereum address, not OASIS GUID. Use LoadAvatarByProviderKey.");
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "Galadriel does not support email-based account lookup.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref r, "Galadriel does not support bulk account enumeration. Use LoadAvatarByProviderKey with a specific Ethereum address.");
            return r;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "GaladrielOASIS is a read-only data provider. Account state cannot be directly written; use eth_sendRawTransaction with a signed transaction.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Galadriel blockchain accounts cannot be deleted.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Galadriel blockchain accounts cannot be deleted.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Galadriel blockchain accounts cannot be deleted.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "Galadriel blockchain accounts cannot be deleted.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "Galadriel avatar details require an Ethereum address, not OASIS GUID.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "Galadriel does not support email-based avatar detail lookup.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string address, int version = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var balJson = await RpcAsync("eth_getBalance", new object[] { address, "latest" });
                var cntJson = await RpcAsync("eth_getTransactionCount", new object[] { address, "latest" });
                var codeJson = await RpcAsync("eth_getCode", new object[] { address, "latest" });

                var detail = new AvatarDetail { Username = address };
                detail.MetaData["address"] = address;
                detail.MetaData["balance_wei"] = balJson["result"]?.ToString() ?? "0x0";
                detail.MetaData["tx_count"] = cntJson["result"]?.ToString() ?? "0x0";
                detail.MetaData["is_contract"] = (codeJson["result"]?.ToString() ?? "0x") != "0x" ? "true" : "false";
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Galadriel LoadAvatarDetailByUsername failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "GaladrielOASIS is a read-only data provider.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref r, "Galadriel does not support bulk avatar detail enumeration.");
            return r;
        }

        // ─── Holon: maps to Galadriel transaction or block ───────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                JObject json;
                if (key.StartsWith("0x") && key.Length == 66)
                    json = await RpcAsync("eth_getTransactionByHash", new object[] { key });
                else
                    json = await RpcAsync("eth_getBlockByNumber", new object[] { key, false });

                var result = json["result"] as JObject;
                if (result == null)
                {
                    OASISErrorHandling.HandleError(ref r, $"Galadriel returned null for key {key}.");
                    return r;
                }
                r.Result = MapToHolon(result, key);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Galadriel LoadHolon ({key}) failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "Galadriel holons (transactions/blocks) are identified by hex hash or block number, not OASIS GUID.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Load the latest N blocks as holons
                var latestJson = await RpcAsync("eth_blockNumber");
                string latestHex = latestJson["result"]?.ToString() ?? "0x0";
                long latestBlock = Convert.ToInt64(latestHex, 16);
                long startBlock = Math.Max(0, latestBlock - 9); // last 10 blocks

                var holons = new List<IHolon>();
                for (long blockNum = startBlock; blockNum <= latestBlock; blockNum++)
                {
                    string hex = "0x" + blockNum.ToString("x");
                    var blockJson = await RpcAsync("eth_getBlockByNumber", new object[] { hex, false });
                    var block = blockJson["result"] as JObject;
                    if (block != null) holons.Add(MapToHolon(block, hex));
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Galadriel LoadAllHolons failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Galadriel LoadHolonsForParent requires an Ethereum address or block number string key, not OASIS GUID.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string key, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Load all transactions in a block (key = block number or hash)
                var blockJson = key.StartsWith("0x") && key.Length == 66
                    ? await RpcAsync("eth_getBlockByHash", new object[] { key, true })
                    : await RpcAsync("eth_getBlockByNumber", new object[] { key, true });

                var block = blockJson["result"] as JObject;
                var txs = block?["transactions"] as JArray;
                var holons = new List<IHolon>();
                if (txs != null)
                    foreach (var tx in txs)
                        holons.Add(MapToHolon(tx as JObject, tx["hash"]?.ToString() ?? ""));
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Galadriel LoadHolonsForParent (block {key}) failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (metaKey == "block" || metaKey == "blockNumber")
                    return await LoadHolonsForParentAsync(metaValue, type);
                if (metaKey == "address" || metaKey == "from" || metaKey == "to")
                {
                    // Use eth_getLogs to find transactions involving this address
                    var logsJson = await RpcAsync("eth_getLogs", new object[]
                    {
                        new { fromBlock = "earliest", toBlock = "latest", address = metaValue }
                    });
                    var logs = logsJson["result"] as JArray;
                    var holons = new List<IHolon>();
                    if (logs != null)
                        foreach (var log in logs)
                            holons.Add(MapLogToHolon(log as JObject));
                    r.Result = holons;
                }
                else
                {
                    OASISErrorHandling.HandleError(ref r, $"Galadriel does not support metadata search by '{metaKey}'. Supported keys: block, blockNumber, address, from, to.");
                }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Galadriel LoadHolonsByMetaData failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            foreach (var kv in metaData)
            {
                var partial = await LoadHolonsByMetaDataAsync(kv.Key, kv.Value, type);
                if (!partial.IsError && partial.Result != null)
                {
                    r.Result = partial.Result;
                    return r;
                }
            }
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "GaladrielOASIS is a read-only data provider. Writing holons to Galadriel requires eth_sendRawTransaction with a signed transaction and private key.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "GaladrielOASIS is a read-only data provider. Writing holons requires eth_sendRawTransaction with a signed transaction.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "Galadriel blockchain data is immutable; transactions and blocks cannot be deleted.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string key)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "Galadriel blockchain data is immutable; transactions and blocks cannot be deleted.");
            return r;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                var results = new SearchResults();
                string query = searchParams?.FilterByMetaData?.ContainsKey("query") == true
                    ? searchParams.FilterByMetaData["query"] : "";

                if (!string.IsNullOrEmpty(query))
                {
                    // Try as address (account)
                    if (query.StartsWith("0x") && query.Length == 42)
                    {
                        var avatarResult = await LoadAvatarByProviderKeyAsync(query);
                        if (!avatarResult.IsError && avatarResult.Result != null)
                            results.SearchResultAvatars = new List<IAvatar> { avatarResult.Result };
                    }
                    // Try as transaction hash
                    else if (query.StartsWith("0x") && query.Length == 66)
                    {
                        var txJson = await RpcAsync("eth_getTransactionByHash", new object[] { query });
                        var tx = txJson["result"] as JObject;
                        if (tx != null)
                            results.SearchResultHolons = new List<IHolon> { MapToHolon(tx, query) };
                    }
                    // Try as block number
                    else
                    {
                        var blockJson = await RpcAsync("eth_getBlockByNumber", new object[] { query, false });
                        var block = blockJson["result"] as JObject;
                        if (block != null)
                            results.SearchResultHolons = new List<IHolon> { MapToHolon(block, query) };
                    }
                }
                r.Result = results;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Galadriel Search failed: {ex.Message}", ex); }
            return r;
        }

        // ─── Export / Import ──────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Galadriel export requires an Ethereum address, not OASIS GUID.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string address, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var logsJson = await RpcAsync("eth_getLogs", new object[]
                {
                    new { fromBlock = "earliest", toBlock = "latest", address = address }
                });
                var logs = logsJson["result"] as JArray;
                var holons = new List<IHolon>();
                if (logs != null)
                    foreach (var log in logs)
                        holons.Add(MapLogToHolon(log as JObject));
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Galadriel ExportAllDataForAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Galadriel does not support email-based export.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "GaladrielOASIS is a read-only data provider. Import requires eth_sendRawTransaction with signed transactions.");
            return r;
        }

        // ─── Sync wrappers ────────────────────────────────────────────────────

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

        // ─── Helpers ──────────────────────────────────────────────────────────

        private static IHolon MapToHolon(JObject obj, string key)
        {
            if (obj == null) return new Holon { Name = key };
            var holon = new Holon { Name = $"Galadriel {(obj["number"] != null ? "Block" : "Tx")} {key}" };
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.GaladrielOASIS] = key;
            holon.MetaData["hash"] = obj["hash"]?.ToString() ?? key;
            holon.MetaData["from"] = obj["from"]?.ToString() ?? "";
            holon.MetaData["to"] = obj["to"]?.ToString() ?? obj["miner"]?.ToString() ?? "";
            holon.MetaData["value"] = obj["value"]?.ToString() ?? "";
            holon.MetaData["blockNumber"] = obj["blockNumber"]?.ToString() ?? obj["number"]?.ToString() ?? "";
            holon.MetaData["gas"] = obj["gas"]?.ToString() ?? obj["gasUsed"]?.ToString() ?? "";
            holon.MetaData["timestamp"] = obj["timestamp"]?.ToString() ?? "";
            return holon;
        }

        private static IHolon MapLogToHolon(JObject log)
        {
            var holon = new Holon { Name = $"Galadriel Log {log?["transactionHash"]}" };
            holon.MetaData["transactionHash"] = log?["transactionHash"]?.ToString() ?? "";
            holon.MetaData["blockNumber"] = log?["blockNumber"]?.ToString() ?? "";
            holon.MetaData["address"] = log?["address"]?.ToString() ?? "";
            holon.MetaData["data"] = log?["data"]?.ToString() ?? "";
            holon.MetaData["topics"] = log?["topics"]?.ToString() ?? "";
            return holon;
        }
    }
}
