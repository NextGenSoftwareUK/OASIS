using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.FarcasterOASIS
{
    /// <summary>
    /// OASIS provider for the Farcaster decentralised social protocol.
    /// Uses the Neynar REST API (https://api.neynar.com/v2/farcaster/) for data access.
    /// Farcaster IDs (FIDs) are stored as the provider key for avatars.
    /// Casts (posts) are mapped to Holons; channels map to Holons of type Collection.
    /// Writing requires a registered Farcaster signer — use PublishCastAsync with a Neynar signer UUID.
    /// </summary>
    public class FarcasterOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _httpClient;
        private const string ApiBase = "https://api.neynar.com/v2/farcaster";
        private bool _isActivated;

        public FarcasterOASIS(string neynarApiKey = null)
        {
            _httpClient = new HttpClient();
            var key = neynarApiKey
                      ?? Environment.GetEnvironmentVariable("NEYNAR_API_KEY")
                      ?? string.Empty;
            _httpClient.DefaultRequestHeaders.Add("api_key", key);

            ProviderName = "FarcasterOASIS";
            ProviderDescription = "Farcaster decentralised social protocol provider (via Neynar REST API)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.FarcasterOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Activation ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBase}/user/bulk?fids=1");
                if (response.IsSuccessStatusCode)
                {
                    _isActivated = true;
                    result.Result = true;
                    result.Message = "FarcasterOASIS provider activated successfully.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"FarcasterOASIS: Neynar API health check failed ({response.StatusCode}).");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"FarcasterOASIS: Error activating provider: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _isActivated = false;
            return await Task.FromResult(new OASISResult<bool> { Result = true, Message = "FarcasterOASIS provider deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var keysResult = KeyManager.Instance.GetProviderPublicKeysForAvatarById(
                    id, Core.Enums.ProviderType.FarcasterOASIS);

                if (keysResult.IsError || keysResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result,
                        "FarcasterOASIS: No Farcaster FID found for this avatar GUID. " +
                        "Use LoadAvatarByUsernameAsync or LoadAvatarByProviderKeyAsync instead.");
                    return result;
                }

                string fid = System.Linq.Enumerable.FirstOrDefault(keysResult.Result) ?? string.Empty;
                return await LoadAvatarByProviderKeyAsync(fid, version);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"FarcasterOASIS: Error in LoadAvatarAsync: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
            => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{ApiBase}/user/by_username?username={Uri.EscapeDataString(avatarUsername)}");

                if (!response.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"FarcasterOASIS: Failed to load user by username '{avatarUsername}' ({response.StatusCode}).");
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("user", out var userEl))
                {
                    result.Result = MapUserToAvatar(userEl);
                    result.IsError = false;
                    result.Message = $"Avatar loaded from Farcaster for username '{avatarUsername}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"FarcasterOASIS: User '{avatarUsername}' not found on Farcaster.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"FarcasterOASIS: Error loading avatar by username: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
            => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS: Email lookup is not supported by Farcaster. " +
                "Use LoadAvatarByUsernameAsync or LoadAvatarByProviderKeyAsync (FID) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
            => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var response = await _httpClient.GetAsync(
                    $"{ApiBase}/user/bulk?fids={Uri.EscapeDataString(providerKey)}");

                if (!response.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"FarcasterOASIS: Failed to load user with FID '{providerKey}' ({response.StatusCode}).");
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("users", out var usersEl)
                    && usersEl.GetArrayLength() > 0)
                {
                    result.Result = MapUserToAvatar(usersEl[0]);
                    result.IsError = false;
                    result.Message = $"Avatar loaded from Farcaster for FID '{providerKey}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"FarcasterOASIS: No user found with FID '{providerKey}'.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"FarcasterOASIS: Error loading avatar by provider key: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
            => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS: LoadAllAvatars is not supported — Farcaster has millions of users. " +
                "Use LoadAvatarByUsernameAsync or LoadAvatarByProviderKeyAsync (FID) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => LoadAllAvatarsAsync(version).Result;

        // ─── AvatarDetail loading ────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS: LoadAvatarDetail is not supported. Use LoadAvatarByProviderKeyAsync (FID) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
            => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: LoadAvatarDetailByEmail is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
            => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: LoadAvatarDetailByUsername is not supported. Use LoadAvatarByUsernameAsync instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
            => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: LoadAllAvatarDetails is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
            => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Save / Delete Avatar ─────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS: Writing requires a registered Farcaster signer. " +
                "Use the Neynar managed signer API to create one, then call PublishCastAsync.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar) => SaveAvatarAsync(Avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS: Writing requires a registered Farcaster signer. " +
                "Use the Neynar managed signer API to create one, then call PublishCastAsync.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar) => SaveAvatarDetailAsync(Avatar).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS does not support deletion — Farcaster hubs retain history.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS does not support deletion — Farcaster hubs retain history.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS does not support deletion — Farcaster hubs retain history.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS does not support deletion — Farcaster hubs retain history.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        // ─── Holon loading ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS: Use LoadHolonAsync(string providerKey) with a cast hash to load a specific cast.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                // providerKey = cast hash (e.g. 0x...)
                var response = await _httpClient.GetAsync(
                    $"{ApiBase}/cast?identifier={Uri.EscapeDataString(providerKey)}&type=hash");

                if (!response.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"FarcasterOASIS: Failed to load cast '{providerKey}' ({response.StatusCode}).");
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("cast", out var castEl))
                {
                    result.Result = MapCastToHolon(castEl);
                    result.IsError = false;
                    result.Message = $"Holon (cast) loaded for hash '{providerKey}'.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"FarcasterOASIS: Cast '{providerKey}' not found.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"FarcasterOASIS: Error loading holon by provider key: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var response = await _httpClient.GetAsync($"{ApiBase}/feed/trending?limit=25");

                if (!response.IsSuccessStatusCode)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"FarcasterOASIS: Failed to load trending casts ({response.StatusCode}).");
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);
                var holons = new List<IHolon>();

                if (doc.RootElement.TryGetProperty("casts", out var castsEl))
                {
                    foreach (var castEl in castsEl.EnumerateArray())
                        holons.Add(MapCastToHolon(castEl));
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Loaded {holons.Count} trending casts from Farcaster.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"FarcasterOASIS: Error loading all holons: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: LoadHolonsForParent is not supported.");
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
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: LoadHolonsForParent is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── MetaData queries ─────────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: LoadHolonsByMetaData is not supported.");
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
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: LoadHolonsByMetaData is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs,
            MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Save / Delete Holon ──────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS: Writing requires a registered Farcaster signer. " +
                "Use the Neynar managed signer API to create one, then call PublishCastAsync.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS: Writing requires a registered Farcaster signer. " +
                "Use the Neynar managed signer API to create one, then call PublishCastAsync.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS does not support deletion — Farcaster hubs retain history.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS does not support deletion — Farcaster hubs retain history.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS: Search is not yet implemented. Use LoadAvatarByUsernameAsync or LoadHolonAsync (cast hash) instead.");
            return await Task.FromResult(result);
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: Import is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: Export is not yet implemented.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
            => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: Export is not yet implemented.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: Export is not yet implemented.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
            => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "FarcasterOASIS: Export is not yet implemented.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS: Geolocation lookup is not supported by the Farcaster protocol.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result,
                "FarcasterOASIS: Geolocation lookup is not supported by the Farcaster protocol.");
            return result;
        }

        // ─── Publishing (outside interface) ──────────────────────────────────────

        /// <summary>
        /// Publishes a new cast (post) to Farcaster using a Neynar managed signer.
        /// </summary>
        /// <param name="signerUuid">The UUID of a registered Neynar managed signer.</param>
        /// <param name="text">The cast text (max 320 characters).</param>
        /// <param name="channelId">Optional Farcaster channel ID to post into.</param>
        /// <returns>The hash of the newly created cast.</returns>
        public async Task<OASISResult<string>> PublishCastAsync(string signerUuid, string text, string? channelId = null)
        {
            var result = new OASISResult<string>();
            try
            {
                var body = new Dictionary<string, object?>
                {
                    ["signer_uuid"] = signerUuid,
                    ["text"] = text
                };
                if (channelId != null)
                    body["channel_id"] = channelId;

                var jsonBody = JsonSerializer.Serialize(body);
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{ApiBase}/cast", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"FarcasterOASIS: Failed to publish cast ({response.StatusCode}): {errorBody}");
                    return result;
                }

                var json = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("cast", out var castEl)
                    && castEl.TryGetProperty("hash", out var hashEl))
                {
                    result.Result = hashEl.GetString() ?? string.Empty;
                    result.IsError = false;
                    result.Message = "Cast published successfully.";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "FarcasterOASIS: Cast published but hash not found in response.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"FarcasterOASIS: Error publishing cast: {ex.Message}");
            }
            return result;
        }

        // ─── Private helpers ──────────────────────────────────────────────────────

        private static Avatar MapUserToAvatar(JsonElement userEl)
        {
            var avatar = new Avatar();
            avatar.Username = TryGetString(userEl, "username") ?? string.Empty;
            avatar.Description = TryGetString(userEl, "profile", "bio", "text")
                              ?? TryGetString(userEl, "bio")
                              ?? string.Empty;

            string fid = string.Empty;
            if (userEl.TryGetProperty("fid", out var fidEl))
                fid = fidEl.ToString();

            string displayName = TryGetString(userEl, "display_name") ?? string.Empty;

            if (avatar.MetaData == null)
                avatar.MetaData = new Dictionary<string, object>();

            avatar.MetaData["FarcasterFid"] = fid;
            avatar.MetaData["DisplayName"] = displayName;

            return avatar;
        }

        private static Holon MapCastToHolon(JsonElement castEl)
        {
            var holon = new Holon();
            string text = TryGetString(castEl, "text") ?? string.Empty;
            holon.Name = text.Length > 80 ? text[..80] : text;
            holon.Description = text;

            if (holon.MetaData == null)
                holon.MetaData = new Dictionary<string, object>();

            holon.MetaData["CastHash"] = TryGetString(castEl, "hash") ?? string.Empty;

            string authorFid = string.Empty;
            if (castEl.TryGetProperty("author", out var authorEl)
                && authorEl.TryGetProperty("fid", out var fidEl))
            {
                authorFid = fidEl.ToString();
            }
            holon.MetaData["AuthorFid"] = authorFid;

            return holon;
        }

        private static string? TryGetString(JsonElement el, params string[] path)
        {
            JsonElement current = el;
            foreach (var key in path)
            {
                if (!current.TryGetProperty(key, out current))
                    return null;
            }
            return current.ValueKind == JsonValueKind.String ? current.GetString() : current.ToString();
        }
    }
}
