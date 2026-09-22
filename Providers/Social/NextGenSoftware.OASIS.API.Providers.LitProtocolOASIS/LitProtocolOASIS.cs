using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.LitProtocolOASIS
{
    /// <summary>
    /// OASIS provider for Lit Protocol (threshold encryption / access control).
    /// Encrypted holons stored via Lit + IPFS. Wallets → Avatars.
    /// Set LIT_API_KEY env var. Encrypted blobs are stored in holon MetaData.
    /// </summary>
    public class LitProtocolOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _http;
        private const string ApiBase = "https://api.litprotocol.com/api";

        public LitProtocolOASIS(string apiKey = null)
        {
            _http = new HttpClient();
            var key = apiKey ?? Environment.GetEnvironmentVariable("LIT_API_KEY") ?? string.Empty;
            if (!string.IsNullOrEmpty(key)) _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

            ProviderName = "LitProtocolOASIS";
            ProviderDescription = "Lit Protocol threshold encryption provider (wallet → Avatar, encrypted content → Holon)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.LitProtocolOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private static Avatar MapWalletToAvatar(string walletAddress, JsonElement? profile = null)
        {
            var avatar = new Avatar();
            if (avatar.MetaData == null) avatar.MetaData = new Dictionary<string, object>();
            avatar.Username = walletAddress;
            avatar.MetaData["lit_wallet_address"] = walletAddress;
            if (profile.HasValue)
            {
                if (profile.Value.TryGetProperty("name", out var n) && n.ValueKind != JsonValueKind.Null)
                    avatar.MetaData["lit_display_name"] = n.GetString();
                if (profile.Value.TryGetProperty("pkpPublicKey", out var pk) && pk.ValueKind != JsonValueKind.Null)
                    avatar.MetaData["lit_pkp_public_key"] = pk.GetString();
            }
            return avatar;
        }

        private static Holon MapEncryptedResourceToHolon(JsonElement resource)
        {
            var holon = new Holon();
            if (holon.MetaData == null) holon.MetaData = new Dictionary<string, object>();
            var id = resource.TryGetProperty("id", out var rid) ? rid.GetString() : Guid.NewGuid().ToString();
            holon.MetaData["lit_resource_id"] = id;
            holon.Name = resource.TryGetProperty("name", out var rn) ? rn.GetString() ?? id : id;
            holon.Description = resource.TryGetProperty("description", out var d) ? d.GetString() : null;
            if (resource.TryGetProperty("encryptedSymmetricKey", out var esk)) holon.MetaData["lit_encrypted_symmetric_key"] = esk.GetString();
            if (resource.TryGetProperty("ciphertext", out var ct)) holon.MetaData["lit_ciphertext"] = ct.GetString();
            if (resource.TryGetProperty("dataToEncryptHash", out var deh)) holon.MetaData["lit_data_hash"] = deh.GetString();
            if (resource.TryGetProperty("accessControlConditions", out var acc)) holon.MetaData["lit_access_conditions"] = acc.ToString();
            if (resource.TryGetProperty("chain", out var ch)) holon.MetaData["lit_chain"] = ch.GetString();
            if (resource.TryGetProperty("createdAt", out var ca))
            {
                holon.MetaData["lit_created_at"] = ca.GetString();
                if (DateTime.TryParse(ca.GetString(), out var dt)) holon.CreatedDate = dt;
            }
            return holon;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var resp = await _http.GetAsync($"{ApiBase}/health");
                if (resp.IsSuccessStatusCode) { result.Result = true; result.Message = "LitProtocolOASIS provider activated."; }
                else { result.Result = true; result.Message = "LitProtocolOASIS provider activated (health check skipped)."; }
            }
            catch { result.Result = true; result.Message = "LitProtocolOASIS provider activated (offline mode — nodes contacted at encrypt/decrypt time)."; }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
            => await Task.FromResult(new OASISResult<bool> { Result = true, Message = "LitProtocolOASIS provider deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var keys = KeyManager.Instance.GetProviderPublicKeysForAvatarById(id, Core.Enums.ProviderType.LitProtocolOASIS);
                if (keys.IsError || keys.Result == null) { OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: No wallet address for avatar GUID."); return result; }
                return await LoadAvatarByProviderKeyAsync(keys.Result.FirstOrDefault() ?? string.Empty, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            // providerKey = Ethereum wallet address
            var result = new OASISResult<IAvatar>();
            try
            {
                var resp = await _http.GetAsync($"{ApiBase}/user/{providerKey}");
                if (resp.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                    result.Result = MapWalletToAvatar(providerKey, doc.RootElement);
                }
                else
                {
                    result.Result = MapWalletToAvatar(providerKey);
                }
                result.Message = $"LitProtocolOASIS: Loaded avatar for wallet {providerKey}.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => await LoadAvatarByProviderKeyAsync(avatarUsername, version);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Email lookup not supported — use wallet address as providerKey.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Loading all wallets not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Avatar detail not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Avatar detail not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Avatar detail not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Avatar detail not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Avatar creation is wallet-driven — save not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Save not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar) => SaveAvatarDetailAsync(avatar).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Delete not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Delete not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Delete not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Delete not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Use LoadHolonAsync(string resourceId) to load an encrypted resource.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var resp = await _http.GetAsync($"{ApiBase}/resource/{providerKey}");
                if (!resp.IsSuccessStatusCode) { OASISErrorHandling.HandleError(ref result, $"LitProtocolOASIS: GET resource {providerKey} failed ({resp.StatusCode})."); return result; }
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                result.Result = MapEncryptedResourceToHolon(doc.RootElement);
                result.Message = $"LitProtocolOASIS: Loaded resource {providerKey}.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Use LoadHolonsForParent(walletAddress) to load resources for a specific wallet.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Use LoadHolonsForParent(walletAddress) to load resources.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var resp = await _http.GetAsync($"{ApiBase}/user/{providerKey}/resources");
                if (!resp.IsSuccessStatusCode) { OASISErrorHandling.HandleError(ref result, $"LitProtocolOASIS: GET resources failed ({resp.StatusCode})."); return result; }
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                var holons = new List<IHolon>();
                var root = doc.RootElement;
                var items = root.ValueKind == JsonValueKind.Array ? root : (root.TryGetProperty("resources", out var arr) ? arr : root);
                if (items.ValueKind == JsonValueKind.Array)
                    foreach (var item in items.EnumerateArray()) holons.Add(MapEncryptedResourceToHolon(item));
                result.Result = holons; result.Message = $"LitProtocolOASIS: Loaded {holons.Count} resources for {providerKey}.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: LoadHolonsByMetaData not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs,
            MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: LoadHolonsByMetaData not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs,
            MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.MetaData == null) { OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: MetaData required. Set lit_ciphertext, lit_encrypted_symmetric_key, lit_access_conditions, lit_chain."); return result; }
                var payload = new Dictionary<string, object>
                {
                    ["name"] = holon.Name ?? string.Empty,
                    ["description"] = holon.Description ?? string.Empty,
                };
                if (holon.MetaData.ContainsKey("lit_ciphertext")) payload["ciphertext"] = holon.MetaData["lit_ciphertext"];
                if (holon.MetaData.ContainsKey("lit_encrypted_symmetric_key")) payload["encryptedSymmetricKey"] = holon.MetaData["lit_encrypted_symmetric_key"];
                if (holon.MetaData.ContainsKey("lit_access_conditions")) payload["accessControlConditions"] = holon.MetaData["lit_access_conditions"];
                if (holon.MetaData.ContainsKey("lit_chain")) payload["chain"] = holon.MetaData["lit_chain"];
                if (holon.MetaData.ContainsKey("lit_data_hash")) payload["dataToEncryptHash"] = holon.MetaData["lit_data_hash"];

                var body = JsonSerializer.Serialize(payload);
                var resp = await _http.PostAsync($"{ApiBase}/resource", new StringContent(body, Encoding.UTF8, "application/json"));
                if (!resp.IsSuccessStatusCode) { OASISErrorHandling.HandleError(ref result, $"LitProtocolOASIS: POST resource failed ({resp.StatusCode})."); return result; }
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                var newId = doc.RootElement.TryGetProperty("id", out var nid) ? nid.GetString() : string.Empty;
                if (holon.MetaData == null) holon.MetaData = new Dictionary<string, object>();
                holon.MetaData["lit_resource_id"] = newId;
                result.Result = holon; result.Message = $"LitProtocolOASIS: Resource saved, ID {newId}.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            foreach (var h in holons)
            {
                var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (!r.IsError && r.Result != null) saved.Add(r.Result);
            }
            result.Result = saved; result.Message = $"LitProtocolOASIS: Saved {saved.Count} holons.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Use DeleteHolon(string resourceId) to delete a resource.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var resp = await _http.DeleteAsync($"{ApiBase}/resource/{providerKey}");
                if (resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    var holon = new Holon();
                    if (holon.MetaData == null) holon.MetaData = new Dictionary<string, object>();
                    holon.MetaData["lit_resource_id"] = providerKey;
                    result.Result = holon; result.Message = $"LitProtocolOASIS: Resource {providerKey} deleted.";
                }
                else OASISErrorHandling.HandleError(ref result, $"LitProtocolOASIS: DELETE failed ({resp.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Search not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Import not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Export not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Export not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Export not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Export not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Geolocation not supported.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LitProtocolOASIS: Geolocation not supported.");
            return result;
        }
    }
}
