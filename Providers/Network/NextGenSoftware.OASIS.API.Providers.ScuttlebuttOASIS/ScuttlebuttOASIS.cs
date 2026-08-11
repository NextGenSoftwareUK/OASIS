using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS
{
    /// <summary>
    /// Secure Scuttlebutt OASIS Provider.
    /// Connects to a local ssb-server HTTP bridge (e.g. ssb-server-http, manyverse) at the configured base URL.
    /// Scuttlebutt is an offline-first gossip-protocol P2P social database — data syncs peer-to-peer when connections are available.
    /// </summary>
    public class ScuttlebuttOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISSuperStar
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public ScuttlebuttOASIS(string apiBaseUrl = "http://localhost:8008/api/v1", string apiKey = "")
        {
            this.ProviderName = "ScuttlebuttOASIS";
            this.ProviderDescription = "Scuttlebutt OASIS Provider — offline-first, gossip-protocol P2P social database";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ScuttlebuttOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);

            _apiBaseUrl = (apiBaseUrl ?? "http://localhost:8008/api/v1").TrimEnd('/');
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

            if (!string.IsNullOrEmpty(apiKey))
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        #region IOASISStorageProvider

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/whoami");
                result.Result = response.IsSuccessStatusCode;
                result.IsError = !result.Result;
                result.Message = result.Result
                    ? "Scuttlebutt provider activated successfully"
                    : $"Failed to connect to Scuttlebutt server: {response.StatusCode}";
                if (result.Result) IsProviderActivated = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error activating Scuttlebutt provider: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                IsProviderActivated = false;
                result.Result = true;
                result.Message = "Scuttlebutt provider deactivated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deactivating Scuttlebutt provider: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/{id}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    result.Result = ParseJsonToAvatar(content);
                    result.IsError = result.Result == null;
                    result.Message = result.Result != null ? "Avatar loaded from Scuttlebutt" : "Failed to parse avatar response";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-key/{Uri.EscapeDataString(providerKey)}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    result.Result = ParseJsonToAvatar(await response.Content.ReadAsStringAsync());
                    result.IsError = result.Result == null;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-email/{Uri.EscapeDataString(avatarEmail)}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    result.Result = ParseJsonToAvatar(await response.Content.ReadAsStringAsync());
                    result.IsError = result.Result == null;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-username/{Uri.EscapeDataString(avatarUsername)}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    result.Result = ParseJsonToAvatar(await response.Content.ReadAsStringAsync());
                    result.IsError = result.Result == null;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/{id}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    result.Result = JsonSerializer.Deserialize<AvatarDetail>(content, _jsonOptions);
                    result.IsError = result.Result == null;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/by-email/{Uri.EscapeDataString(avatarEmail)}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    result.Result = JsonSerializer.Deserialize<AvatarDetail>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.IsError = result.Result == null;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/by-username/{Uri.EscapeDataString(avatarUsername)}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    result.Result = JsonSerializer.Deserialize<AvatarDetail>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.IsError = result.Result == null;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatars = JsonSerializer.Deserialize<List<Avatar>>(content, _jsonOptions);
                    result.Result = avatars?.Cast<IAvatar>() ?? Enumerable.Empty<IAvatar>();
                    result.Message = $"Loaded {avatars?.Count ?? 0} avatars from Scuttlebutt";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading all avatars: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var details = JsonSerializer.Deserialize<List<AvatarDetail>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.Result = details?.Cast<IAvatarDetail>() ?? Enumerable.Empty<IAvatarDetail>();
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, "Avatar cannot be null"); return result; }
                var json = JsonSerializer.Serialize(avatar, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = avatar.Id == Guid.Empty
                    ? await _httpClient.PostAsync($"{_apiBaseUrl}/avatars", content)
                    : await _httpClient.PutAsync($"{_apiBaseUrl}/avatars/{avatar.Id}", content);
                if (response.IsSuccessStatusCode)
                {
                    result.Result = ParseJsonToAvatar(await response.Content.ReadAsStringAsync()) ?? avatar;
                    result.Message = "Avatar saved to Scuttlebutt successfully";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                if (avatarDetail == null) { OASISErrorHandling.HandleError(ref result, "AvatarDetail cannot be null"); return result; }
                var json = JsonSerializer.Serialize(avatarDetail, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = avatarDetail.Id == Guid.Empty
                    ? await _httpClient.PostAsync($"{_apiBaseUrl}/avatar-details", content)
                    : await _httpClient.PutAsync($"{_apiBaseUrl}/avatar-details/{avatarDetail.Id}", content);
                if (response.IsSuccessStatusCode)
                {
                    result.Result = JsonSerializer.Deserialize<AvatarDetail>(await response.Content.ReadAsStringAsync(), _jsonOptions) ?? avatarDetail;
                    result.Message = "AvatarDetail saved to Scuttlebutt successfully";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/avatars/{id}?soft={softDelete}");
                result.Result = response.IsSuccessStatusCode;
                result.IsError = !result.Result;
                result.Message = result.Result ? "Avatar deleted from Scuttlebutt" : $"Scuttlebutt API error: {response.StatusCode}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/avatars/by-key/{Uri.EscapeDataString(providerKey)}?soft={softDelete}");
                result.Result = response.IsSuccessStatusCode;
                result.IsError = !result.Result;
                result.Message = result.Result ? "Avatar deleted from Scuttlebutt" : $"Scuttlebutt API error: {response.StatusCode}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/avatars/by-username/{Uri.EscapeDataString(username)}?soft={softDelete}");
                result.Result = response.IsSuccessStatusCode;
                result.IsError = !result.Result;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/avatars/by-email/{Uri.EscapeDataString(email)}?soft={softDelete}");
                result.Result = response.IsSuccessStatusCode;
                result.IsError = !result.Result;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) => DeleteAvatarByEmailAsync(email, softDelete).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/{id}?version={version}&loadChildren={loadChildren}&recursive={recursive}&maxDepth={maxChildDepth}");
                if (response.IsSuccessStatusCode)
                {
                    result.Result = JsonSerializer.Deserialize<Holon>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.IsError = result.Result == null;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading holon: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/by-key/{Uri.EscapeDataString(providerKey)}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    result.Result = JsonSerializer.Deserialize<Holon>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.IsError = result.Result == null;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/for-parent/{id}?type={type}&version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var holons = JsonSerializer.Deserialize<List<Holon>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.Result = holons?.Cast<IHolon>() ?? Enumerable.Empty<IHolon>();
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/for-parent/by-key/{Uri.EscapeDataString(providerKey)}?type={type}&version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var holons = JsonSerializer.Deserialize<List<Holon>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.Result = holons?.Cast<IHolon>() ?? Enumerable.Empty<IHolon>();
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by key: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/search?metaKey={Uri.EscapeDataString(metaKey)}&metaValue={Uri.EscapeDataString(metaValue)}&type={type}&version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var holons = JsonSerializer.Deserialize<List<Holon>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.Result = holons?.Cast<IHolon>() ?? Enumerable.Empty<IHolon>();
                    result.Message = $"Loaded {holons?.Count ?? 0} holons by metadata";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var body = JsonSerializer.Serialize(new { metaKeyValuePairs, matchMode = metaKeyValuePairMatchMode.ToString(), type = type.ToString(), version });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons/search-multi", content);
                if (response.IsSuccessStatusCode)
                {
                    var holons = JsonSerializer.Deserialize<List<Holon>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.Result = holons?.Cast<IHolon>() ?? Enumerable.Empty<IHolon>();
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons?type={type}&version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var holons = JsonSerializer.Deserialize<List<Holon>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.Result = holons?.Cast<IHolon>() ?? Enumerable.Empty<IHolon>();
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading all holons: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                if (holon == null) { OASISErrorHandling.HandleError(ref result, "Holon cannot be null"); return result; }
                var json = JsonSerializer.Serialize(holon, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = holon.Id == Guid.Empty
                    ? await _httpClient.PostAsync($"{_apiBaseUrl}/holons", content)
                    : await _httpClient.PutAsync($"{_apiBaseUrl}/holons/{holon.Id}", content);
                if (response.IsSuccessStatusCode)
                {
                    result.Result = JsonSerializer.Deserialize<Holon>(await response.Content.ReadAsStringAsync(), _jsonOptions) ?? holon;
                    result.Message = "Holon saved to Scuttlebutt";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving holon: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                if (holons == null || !holons.Any()) { OASISErrorHandling.HandleError(ref result, "Holons cannot be null or empty"); return result; }
                var json = JsonSerializer.Serialize(holons, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons/batch", content);
                if (response.IsSuccessStatusCode)
                {
                    var saved = JsonSerializer.Deserialize<List<Holon>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.Result = saved?.Cast<IHolon>() ?? Enumerable.Empty<IHolon>();
                    result.Message = $"Saved {saved?.Count ?? 0} holons to Scuttlebutt";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving holons: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/holons/{id}");
                result.IsError = !response.IsSuccessStatusCode;
                result.Message = response.IsSuccessStatusCode ? "Holon deleted from Scuttlebutt" : $"Scuttlebutt API error: {response.StatusCode}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.DeleteAsync($"{_apiBaseUrl}/holons/by-key/{Uri.EscapeDataString(providerKey)}");
                result.IsError = !response.IsSuccessStatusCode;
                result.Message = response.IsSuccessStatusCode ? "Holon deleted from Scuttlebutt" : $"Scuttlebutt API error: {response.StatusCode}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                if (searchParams == null) { OASISErrorHandling.HandleError(ref result, "SearchParams cannot be null"); return result; }
                var body = JsonSerializer.Serialize(new { searchParams, version });
                var content = new StringContent(body, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/search", content);
                if (response.IsSuccessStatusCode)
                {
                    result.Result = JsonSerializer.Deserialize<SearchResults>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.IsError = result.Result == null;
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error searching Scuttlebutt: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                if (holons == null || !holons.Any()) { OASISErrorHandling.HandleError(ref result, "Holons cannot be null or empty"); return result; }
                var json = JsonSerializer.Serialize(holons, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/import", content);
                result.Result = response.IsSuccessStatusCode;
                result.IsError = !result.Result;
                result.Message = result.Result ? $"Imported {holons.Count()} holons to Scuttlebutt" : $"Scuttlebutt API error: {response.StatusCode}";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error importing holons: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/avatar/{avatarId}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var holons = JsonSerializer.Deserialize<List<Holon>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.Result = holons?.Cast<IHolon>() ?? Enumerable.Empty<IHolon>();
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/avatar/username/{Uri.EscapeDataString(avatarUsername)}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var holons = JsonSerializer.Deserialize<List<Holon>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.Result = holons?.Cast<IHolon>() ?? Enumerable.Empty<IHolon>();
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by username: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/avatar/email/{Uri.EscapeDataString(avatarEmailAddress)}?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var holons = JsonSerializer.Deserialize<List<Holon>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.Result = holons?.Cast<IHolon>() ?? Enumerable.Empty<IHolon>();
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error exporting avatar data by email: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/export/all?version={version}");
                if (response.IsSuccessStatusCode)
                {
                    var holons = JsonSerializer.Deserialize<List<Holon>>(await response.Content.ReadAsStringAsync(), _jsonOptions);
                    result.Result = holons?.Cast<IHolon>() ?? Enumerable.Empty<IHolon>();
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Scuttlebutt API error: {response.StatusCode}");
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error exporting all data: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        #endregion

        #region IOASISNETProvider

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }
                double centerLat = geoLat / 1e6d;
                double centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();
                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null
                        && avatar.MetaData.TryGetValue("Latitude", out var latObj)
                        && avatar.MetaData.TryGetValue("Longitude", out var lngObj)
                        && double.TryParse(latObj?.ToString(), out var lat)
                        && double.TryParse(lngObj?.ToString(), out var lng)
                        && HaversineDistanceMeters(centerLat, centerLng, lat, lng) <= radiusInMeters)
                    {
                        nearby.Add(avatar);
                    }
                }
                result.Result = nearby;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me: {ex.Message}", ex); }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated) { OASISErrorHandling.HandleError(ref result, "Provider not activated"); return result; }
                var holonsResult = LoadAllHolons(type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }
                double centerLat = geoLat / 1e6d;
                double centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();
                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null
                        && holon.MetaData.TryGetValue("Latitude", out var latObj)
                        && holon.MetaData.TryGetValue("Longitude", out var lngObj)
                        && double.TryParse(latObj?.ToString(), out var lat)
                        && double.TryParse(lngObj?.ToString(), out var lng)
                        && HaversineDistanceMeters(centerLat, centerLng, lat, lng) <= radiusInMeters)
                    {
                        nearby.Add(holon);
                    }
                }
                result.Result = nearby;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error getting holons near me: {ex.Message}", ex); }
            return result;
        }

        #endregion

        #region IOASISSuperStar

        public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
        {
            try
            {
                if (string.IsNullOrEmpty(outputFolder))
                    return false;

                string folder = System.IO.Path.Combine(outputFolder, "Scuttlebutt");
                if (!System.IO.Directory.Exists(folder))
                    System.IO.Directory.CreateDirectory(folder);

                if (!string.IsNullOrEmpty(nativeSource))
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "manifest.json"), nativeSource);
                    return true;
                }

                if (celestialBody == null)
                    return true;

                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"@context\": \"https://www.w3.org/ns/activitystreams\",");
                sb.AppendLine($"  \"name\": \"{celestialBody.Name ?? "OAPP"}\",");
                sb.AppendLine("  \"type\": \"Application\",");
                sb.AppendLine("  \"items\": [");

                var zomes = celestialBody.CelestialBodyCore?.Zomes;
                bool first = true;
                if (zomes != null)
                {
                    foreach (var zome in zomes)
                    {
                        if (zome?.Children == null) continue;
                        foreach (var holon in zome.Children)
                        {
                            if (holon == null || string.IsNullOrWhiteSpace(holon.Name)) continue;
                            if (!first) sb.AppendLine(",");
                            first = false;
                            sb.AppendLine("    {");
                            sb.AppendLine($"      \"type\": \"{holon.Name}\",");
                            sb.AppendLine($"      \"id\": \"{holon.Id}\",");
                            sb.Append($"      \"name\": \"{holon.Name}\"");
                            if (!string.IsNullOrWhiteSpace(holon.Description))
                            {
                                sb.AppendLine(",");
                                sb.Append($"      \"summary\": \"{holon.Description}\"");
                            }
                            sb.AppendLine();
                            sb.Append("    }");
                        }
                    }
                }

                sb.AppendLine();
                sb.AppendLine("  ]");
                sb.AppendLine("}");

                System.IO.File.WriteAllText(System.IO.Path.Combine(folder, "manifest.json"), sb.ToString());
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Private helpers

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        private IAvatar ParseJsonToAvatar(string json)
        {
            try { return JsonSerializer.Deserialize<Avatar>(json, _jsonOptions); }
            catch { return null; }
        }

        private static double HaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            double dLat = ToRad(lat2 - lat1);
            double dLon = ToRad(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                     + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
                     * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

        private static double ToRad(double deg) => deg * Math.PI / 180.0;

        #endregion
    }
}
