using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
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

namespace NextGenSoftware.OASIS.API.Providers.LivepeerOASIS
{
    /// <summary>
    /// OASIS provider for Livepeer — decentralised video transcoding infrastructure.
    /// Video assets and streams → Holons. API: https://livepeer.studio/api
    /// </summary>
    public class LivepeerOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly HttpClient _httpClient;
        private bool _isActivated;

        public LivepeerOASIS(string apiKey = "")
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("https://livepeer.studio") };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            if (!string.IsNullOrEmpty(apiKey))
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            ProviderName = "LivepeerOASIS";
            ProviderDescription = "Livepeer Decentralised Video Transcoding Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.LivepeerOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var response = await _httpClient.GetAsync("/api/user/me");
                if (response.IsSuccessStatusCode) { _isActivated = true; result.Result = true; result.Message = "LivepeerOASIS activated."; }
                else OASISErrorHandling.HandleError(ref result, $"LivepeerOASIS: Auth check failed ({response.StatusCode}). Provide a valid API key.");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _isActivated = false; return new OASISResult<bool>(true); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                bool isStream = holon.MetaData.ContainsKey("livepeer_is_stream") && holon.MetaData["livepeer_is_stream"]?.ToString() == "true";
                if (isStream)
                {
                    var streamReq = new { name = holon.Name ?? "OASIS Stream", profiles = new[] { new { name = "720p", bitrate = 2000000, fps = 30, width = 1280, height = 720 }, new { name = "480p", bitrate = 1000000, fps = 30, width = 854, height = 480 } } };
                    var resp = await _httpClient.PostAsJsonAsync("/api/stream", streamReq);
                    if (resp.IsSuccessStatusCode)
                    {
                        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
                        holon.MetaData["livepeer_stream_id"] = body.TryGetProperty("id", out var sid) ? sid.GetString() : "";
                        holon.MetaData["livepeer_stream_key"] = body.TryGetProperty("streamKey", out var sk) ? sk.GetString() : "";
                        holon.MetaData["livepeer_playback_id"] = body.TryGetProperty("playbackId", out var pid) ? pid.GetString() : "";
                        result.Result = holon;
                    }
                    else OASISErrorHandling.HandleError(ref result, $"LivepeerOASIS: Create stream failed ({resp.StatusCode}).");
                }
                else
                {
                    var uploadReq = new { name = holon.Name ?? "OASIS Asset" };
                    var resp = await _httpClient.PostAsJsonAsync("/api/asset/request-upload", uploadReq);
                    if (resp.IsSuccessStatusCode)
                    {
                        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
                        holon.MetaData["livepeer_asset_id"] = body.TryGetProperty("asset", out var a) && a.TryGetProperty("id", out var aid) ? aid.GetString() : "";
                        holon.MetaData["livepeer_upload_url"] = body.TryGetProperty("url", out var u) ? u.GetString() : "";
                        result.Result = holon;
                        result.Message = "LivepeerOASIS: PUT your video file to livepeer_upload_url.";
                    }
                    else OASISErrorHandling.HandleError(ref result, $"LivepeerOASIS: Request upload failed ({resp.StatusCode}).");
                }
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Batch upload not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var response = await _httpClient.GetAsync($"/api/asset/{providerKey}");
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                    var holon = new Holon();
                    holon.Name = body.TryGetProperty("name", out var n) ? n.GetString() : providerKey;
                    holon.MetaData["livepeer_asset_id"] = providerKey;
                    holon.MetaData["livepeer_status"] = body.TryGetProperty("status", out var s) && s.TryGetProperty("phase", out var p) ? p.GetString() : "";
                    holon.MetaData["livepeer_playback_id"] = body.TryGetProperty("playbackId", out var pid) ? pid.GetString() : "";
                    holon.MetaData["livepeer_created_at"] = body.TryGetProperty("createdAt", out var ca) ? ca.ToString() : "";
                    result.Result = holon;
                }
                else OASISErrorHandling.HandleError(ref result, $"LivepeerOASIS: Asset '{providerKey}' not found ({response.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>(); var holons = new List<IHolon>();
            try
            {
                var response = await _httpClient.GetAsync("/api/asset");
                if (response.IsSuccessStatusCode)
                {
                    var assets = await response.Content.ReadFromJsonAsync<JsonElement>();
                    foreach (var asset in assets.EnumerateArray())
                    {
                        var holon = new Holon();
                        holon.Name = asset.TryGetProperty("name", out var n) ? n.GetString() : "";
                        holon.MetaData["livepeer_asset_id"] = asset.TryGetProperty("id", out var id) ? id.GetString() : "";
                        holon.MetaData["livepeer_status"] = asset.TryGetProperty("status", out var s) && s.TryGetProperty("phase", out var p) ? p.GetString() : "";
                        holon.MetaData["livepeer_playback_id"] = asset.TryGetProperty("playbackId", out var pid) ? pid.GetString() : "";
                        holons.Add(holon);
                    }
                    result.Result = holons;
                }
                else OASISErrorHandling.HandleError(ref result, $"LivepeerOASIS: List assets failed ({response.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Use LoadHolonAsync(assetId)."); return await Task.FromResult(r); }
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await LoadHolonsForParentAsync("", type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Use LoadHolonsForParentAsync(string)."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: MetaData search not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: MetaData search not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Use DeleteHolonAsync(assetId)."); return await Task.FromResult(r); }
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/asset/{providerKey}");
                if (response.IsSuccessStatusCode) { result.Result = new Holon(); result.Message = $"LivepeerOASIS: Asset {providerKey} deleted."; }
                else OASISErrorHandling.HandleError(ref result, $"LivepeerOASIS: Delete failed ({response.StatusCode}).");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;
        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        { var r = new OASISResult<ISearchResults>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Use LoadHolonsForParentAsync to list all assets."); return await Task.FromResult(r); }
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: No on-chain identity."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: No on-chain identity."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: No on-chain identity."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: No on-chain identity."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;
        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: No on-chain identity."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;
        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        { var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: No on-chain identity."); return await Task.FromResult(r); }
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar) => SaveAvatarDetailAsync(avatar).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: No on-chain identity."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: No on-chain identity."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Import not supported."); return await Task.FromResult(r); }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Export not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Geolocation not supported."); return r; }
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "LivepeerOASIS: Geolocation not supported."); return r; }
    }
}
