using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
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

namespace NextGenSoftware.OASIS.API.Providers.PrivyServerWalletsOASIS
{
    public class PrivyServerWalletsOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _appId;
        private bool _isActivated;

        public PrivyServerWalletsOASIS(string appId = "", string appSecret = "")
        {
            _appId = appId;
            _http = new HttpClient { BaseAddress = new Uri("https://auth.privy.io/api/v1/") };
            if (!string.IsNullOrEmpty(appId) && !string.IsNullOrEmpty(appSecret))
            {
                var encoded = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{appId}:{appSecret}"));
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encoded);
                _http.DefaultRequestHeaders.Add("privy-app-id", appId);
            }
            ProviderName = "PrivyServerWalletsOASIS";
            ProviderDescription = "Privy server-side embedded wallet and user identity provider.";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PrivyServerWalletsOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        }

        private void EnsureApiKey()
        {
            if (!_http.DefaultRequestHeaders.Contains("privy-app-id"))
                throw new InvalidOperationException("PrivyServerWalletsOASIS requires appId and appSecret. Provide them in the constructor.");
        }

        private async Task<JObject> GetAsync(string path)
        {
            var r = await _http.GetAsync(path);
            r.EnsureSuccessStatusCode();
            return JObject.Parse(await r.Content.ReadAsStringAsync());
        }

        private async Task<JObject> PostAsync(string path, object body)
        {
            var resp = await _http.PostAsync(path, new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json"));
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        private async Task<JObject> PatchAsync(string path, object body)
        {
            var req = new HttpRequestMessage(new HttpMethod("PATCH"), path)
            {
                Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            };
            var resp = await _http.SendAsync(req);
            resp.EnsureSuccessStatusCode();
            return JObject.Parse(await resp.Content.ReadAsStringAsync());
        }

        private async Task DeleteHttpAsync(string path)
        {
            var resp = await _http.DeleteAsync(path);
            resp.EnsureSuccessStatusCode();
        }

        private static IAvatar MapUser(JObject json)
        {
            var userId = json["id"]?.ToString() ?? "";
            var avatar = new Avatar { Username = userId };
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyServerWalletsOASIS] = userId;
            avatar.Email = json["email"]?["address"]?.ToString() ?? "";
            avatar.MetaData["privy_did"] = userId;
            avatar.MetaData["wallet_address"] = json["wallet"]?["address"]?.ToString() ?? "";
            avatar.MetaData["linked_accounts"] = json["linked_accounts"]?.ToString(Formatting.None) ?? "[]";
            avatar.MetaData["created_at"] = json["created_at"]?.ToString() ?? "";
            return avatar;
        }

        private static IHolon MapWallet(JObject json)
        {
            var walletId = json["id"]?.ToString() ?? "";
            var holon = new Holon { Name = json["address"]?.ToString() ?? walletId };
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyServerWalletsOASIS] = walletId;
            holon.MetaData["wallet_id"] = walletId;
            holon.MetaData["address"] = json["address"]?.ToString() ?? "";
            holon.MetaData["chain_type"] = json["chain_type"]?.ToString() ?? "ethereum";
            holon.MetaData["policy_ids"] = json["policy_ids"]?.ToString(Formatting.None) ?? "[]";
            return holon;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                EnsureApiKey();
                await GetAsync("apps/wallets?limit=1");
                _isActivated = true;
                r.Result = true;
                r.Message = "PrivyServerWalletsOASIS activated";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy activation failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _isActivated = false;
            _http.Dispose();
            return new OASISResult<bool> { Result = true };
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // Avatar = Privy user (GET /users/{userId})
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string userId, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync($"users/{Uri.EscapeDataString(userId)}");
                r.Result = MapUser(json);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy LoadAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0)
            => await LoadAvatarByProviderKeyAsync(u, v);

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref r, "PrivyServerWallets does not support loading avatars by Guid; use LoadAvatarByProviderKey with the Privy user DID.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync("users?limit=100");
                var users = new List<IAvatar>();
                var data = json["data"] as JArray ?? new JArray();
                foreach (var u in data)
                    if (u is JObject obj) users.Add(MapUser(obj));
                r.Result = users;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy LoadAllAvatars failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync($"users/{Uri.EscapeDataString(u)}");
                var detail = new AvatarDetail { Username = u };
                detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyServerWalletsOASIS] = json["id"]?.ToString() ?? u;
                detail.Email = json["email"]?["address"]?.ToString() ?? "";
                detail.MetaData["privy_did"] = json["id"]?.ToString() ?? "";
                detail.MetaData["wallet_address"] = json["wallet"]?["address"]?.ToString() ?? "";
                detail.MetaData["linked_accounts"] = json["linked_accounts"]?.ToString(Formatting.None) ?? "[]";
                detail.MetaData["created_at"] = json["created_at"]?.ToString() ?? "";
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy LoadAvatarDetail failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref r, "PrivyServerWallets does not support loading avatar details by Guid.");
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                EnsureApiKey();
                // Privy supports resolving by linked email
                var json = await GetAsync($"users?email={Uri.EscapeDataString(e)}&limit=1");
                var data = json["data"] as JArray;
                if (data == null || data.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref r, $"No Privy user found for email: {e}");
                    return r;
                }
                var u = (JObject)data[0];
                var detail = new AvatarDetail { Email = e };
                detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyServerWalletsOASIS] = u["id"]?.ToString() ?? e;
                detail.MetaData["privy_did"] = u["id"]?.ToString() ?? "";
                detail.MetaData["wallet_address"] = u["wallet"]?["address"]?.ToString() ?? "";
                r.Result = detail;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy LoadAvatarDetailByEmail failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync("users?limit=100");
                var details = new List<IAvatarDetail>();
                var data = json["data"] as JArray ?? new JArray();
                foreach (var u in data)
                {
                    if (u is JObject obj)
                    {
                        var detail = new AvatarDetail { Username = obj["id"]?.ToString() ?? "" };
                        detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyServerWalletsOASIS] = obj["id"]?.ToString() ?? "";
                        detail.Email = obj["email"]?["address"]?.ToString() ?? "";
                        detail.MetaData["wallet_address"] = obj["wallet"]?["address"]?.ToString() ?? "";
                        details.Add(detail);
                    }
                }
                r.Result = details;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy LoadAllAvatarDetails failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync($"users?email={Uri.EscapeDataString(e)}&limit=1");
                var data = json["data"] as JArray;
                if (data == null || data.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref r, $"No Privy user found for email: {e}");
                    return r;
                }
                r.Result = MapUser((JObject)data[0]);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy LoadAvatarByEmail failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                EnsureApiKey();
                if (a.Id == Guid.Empty) a.Id = Guid.NewGuid();
                var existingId = a.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.PrivyServerWalletsOASIS)
                    ? a.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyServerWalletsOASIS] : "";
                if (!string.IsNullOrEmpty(existingId))
                {
                    // Update existing user metadata
                    var body = new { custom_metadata = new { oasis_id = a.Id.ToString(), username = a.Username } };
                    var json = await PatchAsync($"users/{Uri.EscapeDataString(existingId)}", body);
                    r.Result = MapUser(json);
                }
                else
                {
                    // Privy creates users via authentication flows, not direct API creation
                    OASISErrorHandling.HandleError(ref r, "Privy creates users through authentication flows (OAuth, email, wallet). Use LoadAvatarByProviderKey to load an existing user.");
                }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy SaveAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad)
        {
            var r = new OASISResult<IAvatarDetail>();
            try
            {
                EnsureApiKey();
                var existingId = ad.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.PrivyServerWalletsOASIS)
                    ? ad.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyServerWalletsOASIS] : "";
                if (string.IsNullOrEmpty(existingId))
                {
                    OASISErrorHandling.HandleError(ref r, "PrivyServerWallets requires a Privy user DID in ProviderUniqueStorageKey to save avatar details.");
                    return r;
                }
                var body = new { custom_metadata = new { description = ad.Description, oasis_id = ad.Id.ToString() } };
                await PatchAsync($"users/{Uri.EscapeDataString(existingId)}", body);
                r.Result = ad;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy SaveAvatarDetail failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "PrivyServerWallets does not support deleting users by Guid; use DeleteAvatar with the Privy DID string.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true)
        {
            var r = new OASISResult<bool>();
            try
            {
                EnsureApiKey();
                await DeleteHttpAsync($"users/{Uri.EscapeDataString(k)}");
                r.Result = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy DeleteAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true)
        {
            var r = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref r, "PrivyServerWallets does not support deleting users by email; use DeleteAvatar with the Privy DID string.");
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true)
            => await DeleteAvatarAsync(u, s);

        // Holon = Privy server wallet (GET /apps/wallets/{walletId})
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string key, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync($"apps/wallets/{Uri.EscapeDataString(key)}");
                r.Result = MapWallet(json);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy LoadHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "PrivyServerWallets does not support loading wallets by Guid; use LoadHolon with the Privy wallet ID.");
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync("apps/wallets");
                var wallets = new List<IHolon>();
                var data = json["data"] as JArray ?? new JArray();
                foreach (var w in data)
                    if (w is JObject obj) wallets.Add(MapWallet(obj));
                r.Result = wallets;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy LoadAllHolons failed: {ex.Message}", ex); }
            return r;
        }

        // LoadHolonsForParent: get all wallets for a user (GET /users/{userId}/wallets)
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync($"users/{Uri.EscapeDataString(k)}");
                var wallets = new List<IHolon>();
                var linked = json["linked_accounts"] as JArray ?? new JArray();
                foreach (var account in linked)
                {
                    if (account["type"]?.ToString() == "wallet")
                    {
                        var holon = new Holon { Name = account["address"]?.ToString() ?? "" };
                        holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyServerWalletsOASIS] = account["address"]?.ToString() ?? "";
                        holon.MetaData["address"] = account["address"]?.ToString() ?? "";
                        holon.MetaData["chain_type"] = account["chain_type"]?.ToString() ?? "ethereum";
                        holon.MetaData["wallet_client"] = account["wallet_client"]?.ToString() ?? "";
                        holon.MetaData["connector_type"] = account["connector_type"]?.ToString() ?? "";
                        wallets.Add(holon);
                    }
                }
                r.Result = wallets;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy LoadHolonsForParent failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "PrivyServerWallets does not support Guid-based parent queries; use LoadHolonsForParent with a Privy user DID.");
            r.Result = new List<IHolon>();
            return r;
        }

        // LoadHolonsByMetaData: filter wallets by chain_type
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync("apps/wallets");
                var wallets = new List<IHolon>();
                var data = json["data"] as JArray ?? new JArray();
                foreach (var w in data)
                {
                    if (w is JObject obj)
                    {
                        var val = obj[mk]?.ToString() ?? obj["metadata"]?[mk]?.ToString() ?? "";
                        if (val.Equals(mv, StringComparison.OrdinalIgnoreCase))
                            wallets.Add(MapWallet(obj));
                    }
                }
                r.Result = wallets;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy LoadHolonsByMetaData failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                EnsureApiKey();
                var allR = await LoadAllHolonsAsync(t, lc, rec, md, cd, coe, lcfp, v);
                if (allR.IsError) return allR;
                var filtered = new List<IHolon>();
                foreach (var h in allR.Result ?? new List<IHolon>())
                {
                    bool match = mm == MetaKeyValuePairMatchMode.All;
                    foreach (var kv in m)
                    {
                        var val = h.MetaData.ContainsKey(kv.Key) ? h.MetaData[kv.Key]?.ToString() ?? "" : "";
                        bool kvMatch = val.Equals(kv.Value, StringComparison.OrdinalIgnoreCase);
                        if (mm == MetaKeyValuePairMatchMode.All) match = match && kvMatch;
                        else if (kvMatch) { match = true; break; }
                    }
                    if (match) filtered.Add(h);
                }
                r.Result = filtered;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy LoadHolonsByMetaData failed: {ex.Message}", ex); }
            return r;
        }

        // SaveHolon: create a new Privy server wallet (POST /apps/wallets)
        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                EnsureApiKey();
                if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
                var existingId = h.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.PrivyServerWalletsOASIS)
                    ? h.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyServerWalletsOASIS] : "";
                if (!string.IsNullOrEmpty(existingId))
                {
                    // Update wallet policies
                    var updateBody = new { policy_ids = h.MetaData.ContainsKey("policy_ids") ? JsonConvert.DeserializeObject(h.MetaData["policy_ids"]?.ToString() ?? "[]") : new string[0] };
                    var updated = await PatchAsync($"apps/wallets/{Uri.EscapeDataString(existingId)}", updateBody);
                    r.Result = MapWallet(updated);
                }
                else
                {
                    // Create new server wallet
                    var chainType = h.MetaData.ContainsKey("chain_type") ? h.MetaData["chain_type"]?.ToString() ?? "ethereum" : "ethereum";
                    var createBody = new { chain_type = chainType };
                    var json = await PostAsync("apps/wallets", createBody);
                    h.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyServerWalletsOASIS] = json["id"]?.ToString() ?? h.Id.ToString();
                    h.MetaData["wallet_id"] = json["id"]?.ToString() ?? "";
                    h.MetaData["address"] = json["address"]?.ToString() ?? "";
                    r.Result = h;
                }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy SaveHolon failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            foreach (var h in holons)
            {
                var sr = await SaveHolonAsync(h, sc, rec, md, coe, scop);
                if (sr.IsError) { OASISErrorHandling.HandleError(ref r, sr.Message); return r; }
                if (sr.Result != null) saved.Add(sr.Result);
            }
            r.Result = saved;
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var r = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref r, "PrivyServerWallets does not support deleting wallets by Guid; use DeleteHolon with the Privy wallet ID string.");
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                EnsureApiKey();
                await DeleteHttpAsync($"apps/wallets/{Uri.EscapeDataString(k)}");
                r.Result = new Holon();
                r.Result.ProviderUniqueStorageKey[Core.Enums.ProviderType.PrivyServerWalletsOASIS] = k;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy DeleteHolon failed: {ex.Message}", ex); }
            return r;
        }

        // SearchAsync: search users by email or wallet address
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0)
        {
            var r = new OASISResult<ISearchResults>();
            try
            {
                EnsureApiKey();
                var q = sp?.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery ?? "";
                var holons = new List<IHolon>();
                // Try searching users by email
                var json = await GetAsync($"users?limit=50");
                var data = json["data"] as JArray ?? new JArray();
                foreach (var u in data)
                {
                    if (u is JObject obj)
                    {
                        var email = obj["email"]?["address"]?.ToString() ?? "";
                        var did = obj["id"]?.ToString() ?? "";
                        if (email.Contains(q, StringComparison.OrdinalIgnoreCase) || did.Contains(q, StringComparison.OrdinalIgnoreCase))
                        {
                            holons.Add(MapWallet(new JObject {
                                ["id"] = did,
                                ["address"] = obj["wallet"]?["address"] ?? "",
                                ["chain_type"] = "ethereum"
                            }));
                        }
                    }
                }
                var results = new SearchResults();
                results.SearchResultHolons = holons;
                r.Result = results;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy SearchAsync failed: {ex.Message}", ex); }
            return r;
        }

        // ExportAllDataForAvatarByUsername: get user + all their wallets
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync($"users/{Uri.EscapeDataString(u)}");
                var holons = new List<IHolon>();
                // Add user as a holon
                var userHolon = new Holon { Name = json["id"]?.ToString() ?? u };
                userHolon.MetaData["type"] = "user";
                userHolon.MetaData["privy_did"] = json["id"]?.ToString() ?? "";
                userHolon.MetaData["email"] = json["email"]?["address"]?.ToString() ?? "";
                userHolon.MetaData["linked_accounts"] = json["linked_accounts"]?.ToString(Formatting.None) ?? "[]";
                holons.Add(userHolon);
                // Add wallets
                var walletsJson = await GetAsync("apps/wallets");
                var walletsData = walletsJson["data"] as JArray ?? new JArray();
                foreach (var w in walletsData)
                    if (w is JObject obj) holons.Add(MapWallet(obj));
                r.Result = holons;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy ExportAllDataForAvatar failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref r, "PrivyServerWallets does not support export by Guid; use ExportAllDataForAvatarByUsername with the Privy DID.");
            r.Result = new List<IHolon>();
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync($"users?email={Uri.EscapeDataString(e)}&limit=1");
                var data = json["data"] as JArray;
                if (data == null || data.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref r, $"No Privy user found for email: {e}");
                    r.Result = new List<IHolon>();
                    return r;
                }
                var userId = data[0]["id"]?.ToString() ?? "";
                return await ExportAllDataForAvatarByUsernameAsync(userId, v);
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy ExportAllDataForAvatarByEmail failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                EnsureApiKey();
                var json = await GetAsync("apps/wallets");
                var wallets = new List<IHolon>();
                var data = json["data"] as JArray ?? new JArray();
                foreach (var w in data)
                    if (w is JObject obj) wallets.Add(MapWallet(obj));
                r.Result = wallets;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy ExportAll failed: {ex.Message}", ex); }
            return r;
        }

        // ImportAsync: create server wallets from holons
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h)
        {
            var r = new OASISResult<bool>();
            try
            {
                EnsureApiKey();
                foreach (var holon in h)
                {
                    var sr = await SaveHolonAsync(holon);
                    if (sr.IsError) { OASISErrorHandling.HandleError(ref r, $"Privy ImportAsync failed on holon '{holon.Name}': {sr.Message}"); return r; }
                }
                r.Result = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"Privy ImportAsync failed: {ex.Message}", ex); }
            return r;
        }

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
        public override OASISResult<ISearchResults> Search(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0) => SearchAsync(sp, lc, rec, md, coe, v).Result;
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override OASISResult<IHolon> DeleteHolon(string k) => DeleteHolonAsync(k).Result;
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
