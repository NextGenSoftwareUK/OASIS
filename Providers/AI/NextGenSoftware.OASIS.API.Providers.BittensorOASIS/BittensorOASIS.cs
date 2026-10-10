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

namespace NextGenSoftware.OASIS.API.Providers.BittensorOASIS
{
    public class BittensorOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;

        public BittensorOASIS(string baseUrl = "https://taostats.io/api/v1")
        {
            _http = new HttpClient { BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/") };
            ProviderName = "BittensorOASIS";
            ProviderDescription = "Bittensor decentralised AI network account and subnet data provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.BittensorOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.AI);
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
            try
            {
                await GetAsync("blockchain/latest");
                r.Result = true;
                r.Message = "BittensorOASIS activated successfully.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor activation failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _http.Dispose();
            return new OASISResult<bool> { Result = true, Message = "BittensorOASIS deactivated." };
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar: maps to Bittensor wallet/account ────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string address, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                var json = await GetAsync($"account/{Uri.EscapeDataString(address)}");
                var acct = json["data"] ?? json;
                var avatar = new Avatar { Username = address };
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.BittensorOASIS] = address;
                avatar.MetaData["address"] = address;
                avatar.MetaData["balance_tao"] = acct["balance"]?.ToString() ?? "0";
                avatar.MetaData["stake_tao"] = acct["stake"]?.ToString() ?? "0";
                avatar.MetaData["type"] = acct["type"]?.ToString() ?? "";
                avatar.MetaData["coldkey"] = acct["coldkey"]?.ToString() ?? "";
                avatar.MetaData["hotkey"] = acct["hotkey"]?.ToString() ?? "";
                r.Result = avatar;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor LoadAvatarByProviderKey failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
            => await LoadAvatarByProviderKeyAsync(username, version);

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "Bittensor does not support loading avatars by OASIS GUID. Use LoadAvatarByProviderKey with a TAO wallet address.");
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "Bittensor does not support email-based avatar lookup.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref r, "Bittensor does not support bulk account enumeration. Use LoadAvatarByProviderKey with a specific TAO address.");
            return r;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "BittensorOASIS is a read-only data provider. Avatar data cannot be written to the Bittensor network via this provider.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "BittensorOASIS is a read-only data provider. Avatars cannot be deleted.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "BittensorOASIS is a read-only data provider. Avatars cannot be deleted.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "BittensorOASIS is a read-only data provider.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "BittensorOASIS is a read-only data provider.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "Bittensor does not support loading avatar details by OASIS GUID.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "Bittensor does not support email-based avatar detail lookup.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                var json = await GetAsync($"account/{Uri.EscapeDataString(username)}");
                var acct = json["data"] ?? json;
                var detail = new AvatarDetail { Username = username };
                detail.MetaData["address"] = username;
                detail.MetaData["balance_tao"] = acct["balance"]?.ToString() ?? "0";
                detail.MetaData["stake_tao"] = acct["stake"]?.ToString() ?? "0";
                detail.MetaData["rank"] = acct["rank"]?.ToString() ?? "";
                detail.MetaData["trust"] = acct["trust"]?.ToString() ?? "";
                detail.MetaData["consensus"] = acct["consensus"]?.ToString() ?? "";
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor LoadAvatarDetailByUsername failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "BittensorOASIS is a read-only data provider.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref r, "Bittensor does not support bulk avatar detail enumeration.");
            return r;
        }

        // ─── Holon: maps to Bittensor subnet ─────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                var json = await GetAsync($"subnet/{Uri.EscapeDataString(key)}");
                var subnet = json["data"] ?? json;
                r.Result = MapSubnetToHolon(subnet, key);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor LoadHolon (subnet {key}) failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "Bittensor holons (subnets) are identified by netuid string key, not OASIS GUID. Use LoadHolon(string key).");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var json = await GetAsync("subnet/list");
                var subnets = (json["data"] as JArray) ?? (json as JArray);
                var holons = new List<IHolon>();
                if (subnets != null)
                    foreach (var s in subnets)
                        holons.Add(MapSubnetToHolon(s, s["netuid"]?.ToString() ?? ""));
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor LoadAllHolons failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Bittensor LoadHolonsForParent requires a netuid string key, not OASIS GUID.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string key, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Load validators for a given subnet netuid
                var json = await GetAsync($"validator/list?netuid={Uri.EscapeDataString(key)}");
                var validators = (json["data"] as JArray) ?? (json as JArray);
                var holons = new List<IHolon>();
                if (validators != null)
                {
                    foreach (var v in validators)
                    {
                        var h = new Holon { Name = v["hotkey"]?.ToString() ?? "validator" };
                        h.MetaData["hotkey"] = v["hotkey"]?.ToString() ?? "";
                        h.MetaData["coldkey"] = v["coldkey"]?.ToString() ?? "";
                        h.MetaData["stake"] = v["stake"]?.ToString() ?? "0";
                        h.MetaData["rank"] = v["rank"]?.ToString() ?? "";
                        h.MetaData["trust"] = v["trust"]?.ToString() ?? "";
                        h.MetaData["netuid"] = key;
                        h.ProviderUniqueStorageKey[Core.Enums.ProviderType.BittensorOASIS] = v["hotkey"]?.ToString() ?? "";
                        holons.Add(h);
                    }
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor LoadHolonsForParent (subnet {key}) failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (metaKey == "netuid" || metaKey == "subnet_uid")
                    return await LoadHolonsForParentAsync(metaValue, type);
                // Generic metadata search falls back to listing all and filtering
                var allResult = await LoadAllHolonsAsync(type);
                if (allResult.IsError) return allResult;
                var filtered = new List<IHolon>();
                foreach (var h in allResult.Result)
                    if (h.MetaData.ContainsKey(metaKey) && h.MetaData[metaKey]?.ToString() == metaValue)
                        filtered.Add(h);
                r.Result = filtered;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor LoadHolonsByMetaData failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var allResult = await LoadAllHolonsAsync(type);
                if (allResult.IsError) return allResult;
                var filtered = new List<IHolon>();
                foreach (var h in allResult.Result)
                {
                    bool match = matchMode == MetaKeyValuePairMatchMode.All;
                    foreach (var kv in metaData)
                    {
                        bool has = h.MetaData.ContainsKey(kv.Key) && h.MetaData[kv.Key]?.ToString() == kv.Value;
                        if (matchMode == MetaKeyValuePairMatchMode.Any && has) { match = true; break; }
                        if (matchMode == MetaKeyValuePairMatchMode.All && !has) { match = false; break; }
                    }
                    if (match) filtered.Add(h);
                }
                r.Result = filtered;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor LoadHolonsByMetaData failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "BittensorOASIS is a read-only data provider. Holons cannot be written to the Bittensor network via this provider.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int currentDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "BittensorOASIS is a read-only data provider. Holons cannot be written to the Bittensor network via this provider.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "BittensorOASIS is a read-only data provider. Holons cannot be deleted.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string key)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "BittensorOASIS is a read-only data provider. Holons cannot be deleted.");
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
                    // Search for a specific account by address
                    try
                    {
                        var acctJson = await GetAsync($"account/{Uri.EscapeDataString(query)}");
                        var acct = acctJson["data"] ?? acctJson;
                        var avatar = new Avatar { Username = query };
                        avatar.MetaData["address"] = query;
                        avatar.MetaData["balance_tao"] = acct["balance"]?.ToString() ?? "0";
                        results.SearchResultAvatars = new List<IAvatar> { avatar };
                    }
                    catch { /* not a valid account address, skip */ }

                    // Search for a subnet by netuid
                    try
                    {
                        var subnetJson = await GetAsync($"subnet/{Uri.EscapeDataString(query)}");
                        var subnet = subnetJson["data"] ?? subnetJson;
                        results.SearchResultHolons = new List<IHolon> { MapSubnetToHolon(subnet, query) };
                    }
                    catch { /* not a valid netuid, skip */ }
                }

                r.Result = results;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor Search failed: {ex.Message}", ex); }
            return r;
        }

        // ─── Export / Import ──────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Bittensor does not support export by OASIS GUID. Use ExportAllDataForAvatarByUsername with a TAO address.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string username, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var transfers = await GetAsync($"transfer?account={Uri.EscapeDataString(username)}");
                var items = (transfers["data"] as JArray) ?? (transfers as JArray);
                var holons = new List<IHolon>();
                if (items != null)
                {
                    foreach (var tx in items)
                    {
                        var h = new Holon { Name = $"TAO Transfer {tx["extrinsic_id"]}" };
                        h.MetaData["extrinsic_id"] = tx["extrinsic_id"]?.ToString() ?? "";
                        h.MetaData["from"] = tx["from"]?.ToString() ?? "";
                        h.MetaData["to"] = tx["to"]?.ToString() ?? "";
                        h.MetaData["amount"] = tx["amount"]?.ToString() ?? "0";
                        h.MetaData["block"] = tx["block"]?.ToString() ?? "";
                        holons.Add(h);
                    }
                }
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Bittensor ExportAllDataForAvatarByUsername failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string email, int version = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "Bittensor does not support email-based export.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "BittensorOASIS is a read-only data provider. Import is not supported.");
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

        // ─── Helper ───────────────────────────────────────────────────────────

        private static IHolon MapSubnetToHolon(JToken subnet, string key)
        {
            var holon = new Holon { Name = subnet["name"]?.ToString() ?? $"Bittensor Subnet {key}" };
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.BittensorOASIS] = key;
            holon.MetaData["netuid"] = subnet["netuid"]?.ToString() ?? key;
            holon.MetaData["name"] = subnet["name"]?.ToString() ?? "";
            holon.MetaData["active_validators"] = subnet["activeValidators"]?.ToString() ?? "0";
            holon.MetaData["active_miners"] = subnet["activeMiners"]?.ToString() ?? "0";
            holon.MetaData["emission"] = subnet["emission"]?.ToString() ?? "0";
            holon.MetaData["tempo"] = subnet["tempo"]?.ToString() ?? "";
            holon.MetaData["owner"] = subnet["owner"]?.ToString() ?? "";
            return holon;
        }
    }
}
