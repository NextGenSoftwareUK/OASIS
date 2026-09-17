using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
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

namespace NextGenSoftware.OASIS.API.Providers.LoomOASIS
{
    /// <summary>
    /// OASIS provider for the Loom video messaging platform.
    /// Uses the Loom REST API v1 (https://api.loom.com/v1/) for data access.
    /// Loom workspace users map to OASIS Avatars; Loom videos map to OASIS Holons.
    /// Provider key for avatars is the Loom user ID; for holons it is the Loom video ID.
    /// Authentication uses a personal access token or OAuth 2.0 bearer token (LOOM_ACCESS_TOKEN).
    /// Video upload is supported via the two-step create-then-upload flow; the presigned upload URL
    /// is returned in MetaData["LoomUploadUrl"] after SaveHolonAsync.
    /// </summary>
    public class LoomOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _httpClient;
        private const string ApiBase = "https://api.loom.com/v1";
        private string _currentUserId;
        private string _workspaceId;
        private bool _isActivated;

        public LoomOASIS(string accessToken = null)
        {
            _httpClient = new HttpClient();
            var token = accessToken
                        ?? Environment.GetEnvironmentVariable("LOOM_ACCESS_TOKEN")
                        ?? string.Empty;
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            ProviderName = "LoomOASIS";
            ProviderDescription = "Loom video messaging platform provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.LoomOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        // ─── Activation ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var me = await GetAsync<LoomUser>("/me");
                if (me != null)
                {
                    _currentUserId = me.Id;
                    _workspaceId = me.WorkspaceId;
                    _isActivated = true;
                    result.Result = true;
                    result.Message = $"LoomOASIS activated for user {me.Name} (workspace: {me.WorkspaceName}).";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "LoomOASIS: /me returned null — check LOOM_ACCESS_TOKEN.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error activating provider: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _isActivated = false;
            _currentUserId = null;
            _workspaceId = null;
            return await Task.FromResult(new OASISResult<bool> { Result = true, Message = "LoomOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar: Load ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var user = await GetAsync<LoomUser>($"/users/{id}");
                if (user != null)
                    result.Result = MapUserToAvatar(user);
                else
                    OASISErrorHandling.HandleError(ref result, $"LoomOASIS: User {id} not found.");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"LoomOASIS: Error loading avatar {id}: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) =>
            LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var user = await GetAsync<LoomUser>($"/users/{providerKey}");
                if (user != null)
                    result.Result = MapUserToAvatar(user);
                else
                    OASISErrorHandling.HandleError(ref result, $"LoomOASIS: User '{providerKey}' not found.");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error loading avatar by key '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) =>
            LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var members = await GetAsync<LoomMembersResponse>($"/workspaces/{_workspaceId}/members");
                if (members?.Members != null)
                {
                    var match = members.Members.Find(m =>
                        string.Equals(m.Name, avatarUsername, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        result.Result = MapUserToAvatar(match);
                    else
                        OASISErrorHandling.HandleError(ref result,
                            $"LoomOASIS: No workspace member with name '{avatarUsername}'.");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "LoomOASIS: Failed to retrieve workspace members.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error loading avatar by username '{avatarUsername}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var members = await GetAsync<LoomMembersResponse>($"/workspaces/{_workspaceId}/members");
                if (members?.Members != null)
                {
                    var match = members.Members.Find(m =>
                        string.Equals(m.Email, avatarEmail, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        result.Result = MapUserToAvatar(match);
                    else
                        OASISErrorHandling.HandleError(ref result,
                            $"LoomOASIS: No workspace member with email '{avatarEmail}'.");
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result,
                        "LoomOASIS: Failed to retrieve workspace members.");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error loading avatar by email '{avatarEmail}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var members = await GetAsync<LoomMembersResponse>($"/workspaces/{_workspaceId}/members");
                var avatars = new List<IAvatar>();
                if (members?.Members != null)
                    foreach (var m in members.Members)
                        avatars.Add(MapUserToAvatar(m));

                result.Result = avatars;
                result.Message = $"LoomOASIS: Loaded {avatars.Count} workspace members.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error loading all avatars: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) =>
            LoadAllAvatarsAsync(version).Result;

        // ─── Avatar: Save / Delete ────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (string.IsNullOrEmpty(_currentUserId))
                    throw new InvalidOperationException("Provider not activated — call ActivateProviderAsync first.");

                if (avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.LoomOASIS)
                    && avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.LoomOASIS] != _currentUserId)
                {
                    OASISErrorHandling.HandleError(ref result,
                        "LoomOASIS: Can only update the authenticated user's own profile.");
                    return result;
                }

                var patch = new { name = avatar.Username };
                var content = new StringContent(JsonSerializer.Serialize(patch), Encoding.UTF8, "application/json");
                var response = await _httpClient.PatchAsync($"{ApiBase}/me/profile", content);

                if (response.IsSuccessStatusCode)
                {
                    var updated = await GetAsync<LoomUser>("/me");
                    result.Result = updated != null ? MapUserToAvatar(updated) : avatar;
                    result.Message = "LoomOASIS: Profile updated successfully.";
                }
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"LoomOASIS: Profile update failed ({response.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"LoomOASIS: Error saving avatar: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "LoomOASIS: Account deletion is not supported via the Loom public API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) =>
            DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "LoomOASIS: Account deletion is not supported via the Loom public API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) =>
            DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "LoomOASIS: Account deletion is not supported via the Loom public API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) =>
            DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result,
                "LoomOASIS: Account deletion is not supported via the Loom public API.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) =>
            DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;

        // ─── Avatar Detail ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var user = await GetAsync<LoomUser>($"/users/{id}");
                if (user != null)
                    result.Result = MapUserToAvatarDetail(user);
                else
                    OASISErrorHandling.HandleError(ref result, $"LoomOASIS: User {id} not found.");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error loading avatar detail {id}: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) =>
            LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername, version);
            var result = new OASISResult<IAvatarDetail>();
            if (!avatar.IsError && avatar.Result != null)
            {
                var detail = await LoadAvatarDetailAsync(avatar.Result.Id, version);
                result.Result = detail.Result;
                result.IsError = detail.IsError;
                result.Message = detail.Message;
                result.Exception = detail.Exception;
            }
            else
            {
                result.IsError = avatar.IsError;
                result.Message = avatar.Message;
                result.Exception = avatar.Exception;
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) =>
            LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var avatar = await LoadAvatarByEmailAsync(avatarEmail, version);
            var result = new OASISResult<IAvatarDetail>();
            if (!avatar.IsError && avatar.Result != null)
            {
                var detail = await LoadAvatarDetailAsync(avatar.Result.Id, version);
                result.Result = detail.Result;
                result.IsError = detail.IsError;
                result.Message = detail.Message;
                result.Exception = detail.Exception;
            }
            else
            {
                result.IsError = avatar.IsError;
                result.Message = avatar.Message;
                result.Exception = avatar.Exception;
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) =>
            LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var members = await GetAsync<LoomMembersResponse>($"/workspaces/{_workspaceId}/members");
                var details = new List<IAvatarDetail>();
                if (members?.Members != null)
                    foreach (var m in members.Members)
                        details.Add(MapUserToAvatarDetail(m));

                result.Result = details;
                result.Message = $"LoomOASIS: Loaded {details.Count} avatar details.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error loading all avatar details: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) =>
            LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            OASISErrorHandling.HandleError(ref result,
                "LoomOASIS: Avatar detail save is not separately supported — use SaveAvatarAsync to update the profile.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) =>
            SaveAvatarDetailAsync(avatarDetail).Result;

        // ─── Holon: Load ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var video = await GetAsync<LoomVideo>($"/videos/{id}");
                if (video != null)
                    result.Result = MapVideoToHolon(video);
                else
                    OASISErrorHandling.HandleError(ref result, $"LoomOASIS: Video {id} not found.");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"LoomOASIS: Error loading holon {id}: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError,
                loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var video = await GetAsync<LoomVideo>($"/videos/{providerKey}");
                if (video != null)
                    result.Result = MapVideoToHolon(video);
                else
                    OASISErrorHandling.HandleError(ref result, $"LoomOASIS: Video '{providerKey}' not found.");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error loading holon by key '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError,
                loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                string nextCursor = null;
                do
                {
                    var url = "/videos?limit=50" + (nextCursor != null ? $"&cursor={nextCursor}" : "");
                    var page = await GetAsync<LoomVideosResponse>(url);
                    if (page?.Videos != null)
                        foreach (var v in page.Videos)
                            holons.Add(MapVideoToHolon(v));
                    nextCursor = page?.NextCursor;
                }
                while (!string.IsNullOrEmpty(nextCursor));

                result.Result = holons;
                result.Message = $"LoomOASIS: Loaded {holons.Count} videos.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error loading all holons: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) =>
            LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth,
                continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            await LoadHolonsForParentAsync(id.ToString(), type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth,
                continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                string nextCursor = null;
                do
                {
                    var url = $"/folders/{providerKey}/videos?limit=50"
                              + (nextCursor != null ? $"&cursor={nextCursor}" : "");
                    var page = await GetAsync<LoomVideosResponse>(url);
                    if (page?.Videos != null)
                        foreach (var v in page.Videos)
                            holons.Add(MapVideoToHolon(v));
                    nextCursor = page?.NextCursor;
                }
                while (!string.IsNullOrEmpty(nextCursor));

                result.Result = holons;
                result.Message = $"LoomOASIS: Loaded {holons.Count} videos for folder '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error loading holons for parent '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── MetaData queries ─────────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey,
            string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoomOASIS: LoadHolonsByMetaData is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth,
                curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(
            Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, "LoomOASIS: LoadHolonsByMetaData is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(
            Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode,
            HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true,
            int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true,
            bool loadChildrenFromProvider = false, int version = 0) =>
            LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren,
                recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon: Save ──────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                bool hasKey = holon.ProviderUniqueStorageKey != null
                    && holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.LoomOASIS)
                    && !string.IsNullOrEmpty(holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.LoomOASIS]);

                if (hasKey)
                {
                    var videoId = holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.LoomOASIS];
                    var patch = new { title = holon.Name, description = holon.Description };
                    var content = new StringContent(JsonSerializer.Serialize(patch), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PatchAsync($"{ApiBase}/videos/{videoId}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var updated = await GetAsync<LoomVideo>($"/videos/{videoId}");
                        result.Result = updated != null ? MapVideoToHolon(updated) : holon;
                        result.Message = "LoomOASIS: Video updated successfully.";
                    }
                    else
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref result,
                            $"LoomOASIS: Video update failed ({response.StatusCode}): {body}");
                    }
                }
                else
                {
                    string folderId = holon.ParentHolonId != Guid.Empty
                        ? holon.ParentHolonId.ToString() : null;

                    var createBody = new { title = holon.Name, folder_id = folderId };
                    var content = new StringContent(JsonSerializer.Serialize(createBody), Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync($"{ApiBase}/videos", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var created = await response.Content.ReadFromJsonAsync<LoomVideoCreateResponse>(
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (created != null)
                        {
                            var mapped = MapVideoToHolon(created.Video);
                            mapped.ProviderUniqueStorageKey[Core.Enums.ProviderType.LoomOASIS] = created.Video.Id;
                            mapped.MetaData["LoomUploadUrl"] = created.UploadUrl;
                            result.Result = mapped;
                            result.Message = $"LoomOASIS: Video slot created (ID: {created.Video.Id}). " +
                                             "Upload video binary to MetaData[\"LoomUploadUrl\"] via HTTP PUT.";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result,
                                "LoomOASIS: Video create returned null response.");
                        }
                    }
                    else
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        OASISErrorHandling.HandleError(ref result,
                            $"LoomOASIS: Video create failed ({response.StatusCode}): {body}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"LoomOASIS: Error saving holon: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true,
            bool recursive = true, int maxChildDepth = 0, bool continueOnError = true,
            bool saveChildrenOnProvider = false) =>
            SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            try
            {
                foreach (var holon in holons)
                {
                    var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth,
                        continueOnError, saveChildrenOnProvider);
                    if (!r.IsError && r.Result != null)
                        saved.Add(r.Result);
                    else if (!continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result,
                            $"LoomOASIS: Error saving holon '{holon.Name}': {r.Message}");
                        return result;
                    }
                }
                result.Result = saved;
                result.Message = $"LoomOASIS: Saved {saved.Count} holons.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"LoomOASIS: Error saving holons: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons,
            bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0,
            bool continueOnError = true, bool saveChildrenOnProvider = false) =>
            SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth,
                continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon: Delete ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var response = await _httpClient.DeleteAsync($"{ApiBase}/videos/{id}");
                if (response.IsSuccessStatusCode)
                    result.Message = $"LoomOASIS: Video {id} deleted.";
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"LoomOASIS: Delete video {id} failed ({response.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"LoomOASIS: Error deleting holon {id}: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var response = await _httpClient.DeleteAsync($"{ApiBase}/videos/{providerKey}");
                if (response.IsSuccessStatusCode)
                    result.Message = $"LoomOASIS: Video '{providerKey}' deleted.";
                else
                {
                    var body = await response.Content.ReadAsStringAsync();
                    OASISErrorHandling.HandleError(ref result,
                        $"LoomOASIS: Delete video '{providerKey}' failed ({response.StatusCode}): {body}");
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error deleting holon '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                var searchResults = new SearchResults();

                // Extract text query from the first SearchTextGroup if present
                string queryText = string.Empty;
                if (searchParams.SearchGroups != null)
                    foreach (var group in searchParams.SearchGroups)
                        if (group is NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup textGroup
                            && !string.IsNullOrEmpty(textGroup.SearchQuery))
                        {
                            queryText = textGroup.SearchQuery;
                            break;
                        }

                if (!string.IsNullOrWhiteSpace(queryText))
                {
                    var q = Uri.EscapeDataString(queryText);
                    var page = await GetAsync<LoomVideosResponse>($"/videos?limit=100&search={q}");
                    if (page?.Videos != null)
                        foreach (var v in page.Videos)
                            searchResults.SearchResultHolons.Add(MapVideoToHolon(v));

                    if (!string.IsNullOrWhiteSpace(_workspaceId))
                    {
                        var members = await GetAsync<LoomMembersResponse>($"/workspaces/{_workspaceId}/members");
                        if (members?.Members != null)
                            foreach (var m in members.Members)
                                if ((m.Name?.Contains(queryText, StringComparison.OrdinalIgnoreCase) ?? false)
                                    || (m.Email?.Contains(queryText, StringComparison.OrdinalIgnoreCase) ?? false))
                                    searchResults.SearchResultAvatars.Add(MapUserToAvatar(m));
                    }
                }

                result.Result = searchResults;
                result.Message = $"LoomOASIS: Found {searchResults.SearchResultHolons.Count} videos and " +
                                 $"{searchResults.SearchResultAvatars.Count} users for '{queryText}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"LoomOASIS: Error searching: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams,
            bool loadChildren = true, bool recursive = true, int maxChildDepth = 0,
            bool continueOnError = true, int version = 0) =>
            SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "LoomOASIS: Import is not supported.");
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                string nextCursor = null;
                do
                {
                    var url = $"/users/{avatarId}/videos?limit=50"
                              + (nextCursor != null ? $"&cursor={nextCursor}" : "");
                    var page = await GetAsync<LoomVideosResponse>(url);
                    if (page?.Videos != null)
                        foreach (var v in page.Videos)
                            holons.Add(MapVideoToHolon(v));
                    nextCursor = page?.NextCursor;
                }
                while (!string.IsNullOrEmpty(nextCursor));

                result.Result = holons;
                result.Message = $"LoomOASIS: Exported {holons.Count} videos for avatar {avatarId}.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"LoomOASIS: Error exporting data for avatar {avatarId}: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) =>
            ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var err = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref err,
                    $"LoomOASIS: Could not resolve username '{avatarUsername}': {avatarResult.Message}");
                return err;
            }
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) =>
            ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var err = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref err,
                    $"LoomOASIS: Could not resolve email '{avatarEmailAddress}': {avatarResult.Message}");
                return err;
            }
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) =>
            ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) =>
            await LoadAllHolonsAsync(version: version);

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) =>
            ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            OASISErrorHandling.HandleError(ref result,
                "LoomOASIS: Geolocation lookup is not supported by the Loom platform.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result,
                "LoomOASIS: Geolocation lookup is not supported by the Loom platform.");
            return result;
        }

        // ─── HTTP helpers ─────────────────────────────────────────────────────────

        private async Task<T> GetAsync<T>(string path)
        {
            var response = await _httpClient.GetAsync($"{ApiBase}{path}");
            if (!response.IsSuccessStatusCode) return default;
            return await response.Content.ReadFromJsonAsync<T>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // ─── Mapping ──────────────────────────────────────────────────────────────

        private static Avatar MapUserToAvatar(LoomUser user)
        {
            var avatar = new Avatar
            {
                Id = TryParseGuid(user.Id),
                Username = user.Name ?? user.Id,
                Email = user.Email ?? string.Empty,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            };
            avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.LoomOASIS] = user.Id;
            avatar.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["workspace_id"] = user.WorkspaceId ?? string.Empty;
            avatar.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["workspace_name"] = user.WorkspaceName ?? string.Empty;
            avatar.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["picture"] = user.Picture ?? string.Empty;
            return avatar;
        }

        private static AvatarDetail MapUserToAvatarDetail(LoomUser user)
        {
            var detail = new AvatarDetail
            {
                Id = TryParseGuid(user.Id),
                Username = user.Name ?? user.Id,
                Email = user.Email ?? string.Empty,
                Description = $"Loom workspace member — {user.WorkspaceName}",
            };
            detail.ProviderUniqueStorageKey[Core.Enums.ProviderType.LoomOASIS] = user.Id;
            detail.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["picture"] = user.Picture ?? string.Empty;
            detail.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["workspace_id"] = user.WorkspaceId ?? string.Empty;
            return detail;
        }

        private static Holon MapVideoToHolon(LoomVideo video)
        {
            var holon = new Holon
            {
                Id = TryParseGuid(video.Id),
                Name = video.Title ?? string.Empty,
                Description = video.Description ?? string.Empty,
                HolonType = HolonType.Holon,
                CreatedDate = video.CreatedAt,
                ModifiedDate = video.UpdatedAt,
            };
            holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.LoomOASIS] = video.Id;
            holon.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["owner_id"] = video.OwnerId ?? string.Empty;
            holon.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["folder_id"] = video.FolderId ?? string.Empty;
            holon.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["space_id"] = video.SpaceId ?? string.Empty;
            holon.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["embed_url"] = video.EmbedUrl ?? string.Empty;
            holon.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["share_url"] = video.ShareUrl ?? string.Empty;
            holon.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["thumbnail_url"] = video.ThumbnailUrl ?? string.Empty;
            holon.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["duration_seconds"] = video.Duration.ToString();
            holon.ProviderMetaData[Core.Enums.ProviderType.LoomOASIS]["privacy"] = video.Privacy ?? string.Empty;

            if (!string.IsNullOrEmpty(video.FolderId) && Guid.TryParse(video.FolderId, out var parentGuid))
                holon.ParentHolonId = parentGuid;

            return holon;
        }

        private static Guid TryParseGuid(string id)
        {
            if (Guid.TryParse(id, out var g)) return g;
            if (!string.IsNullOrEmpty(id))
            {
                using var md5 = System.Security.Cryptography.MD5.Create();
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(id));
                return new Guid(hash);
            }
            return Guid.NewGuid();
        }

        // ─── Loom API DTOs ────────────────────────────────────────────────────────

        private class LoomUser
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public string Picture { get; set; }
            public string WorkspaceId { get; set; }
            public string WorkspaceName { get; set; }
        }

        private class LoomMembersResponse
        {
            public List<LoomUser> Members { get; set; }
            public string NextCursor { get; set; }
        }

        private class LoomVideo
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Description { get; set; }
            public string OwnerId { get; set; }
            public string FolderId { get; set; }
            public string SpaceId { get; set; }
            public string Privacy { get; set; }
            public int Duration { get; set; }
            public string EmbedUrl { get; set; }
            public string ShareUrl { get; set; }
            public string ThumbnailUrl { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        private class LoomVideosResponse
        {
            public List<LoomVideo> Videos { get; set; }
            public string NextCursor { get; set; }
        }

        private class LoomVideoCreateResponse
        {
            public LoomVideo Video { get; set; }
            public string UploadUrl { get; set; }
        }
    }
}
