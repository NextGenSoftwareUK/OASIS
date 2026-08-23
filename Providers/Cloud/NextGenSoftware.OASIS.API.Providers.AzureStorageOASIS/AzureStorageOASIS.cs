using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS
{
    /// <summary>
    /// OASIS provider for Microsoft Azure Blob Storage.
    /// Holons are stored as JSON blobs in a configurable container.
    /// The blob name (providerKey) is the holon's OASIS ID (GUID) or a caller-supplied key.
    /// Avatars are stored in a separate "avatars" container, keyed by avatar ID or username.
    /// SDK: Azure.Storage.Blobs v12
    /// </summary>
    public class AzureStorageOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly BlobServiceClient _serviceClient;
        private readonly string _holonContainer;
        private readonly string _avatarContainer;
        private bool _isActivated;

        public AzureStorageOASIS(
            string connectionString,
            string holonContainer = "oasis-holons",
            string avatarContainer = "oasis-avatars")
        {
            _serviceClient = new BlobServiceClient(connectionString);
            _holonContainer = holonContainer;
            _avatarContainer = avatarContainer;

            ProviderName = "AzureStorageOASIS";
            ProviderDescription = "Azure Blob Storage Provider — JSON holons and avatars as blobs";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AzureStorageOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Cloud));
            ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var holons = _serviceClient.GetBlobContainerClient(_holonContainer);
                await holons.CreateIfNotExistsAsync(PublicAccessType.None);
                var avatars = _serviceClient.GetBlobContainerClient(_avatarContainer);
                await avatars.CreateIfNotExistsAsync(PublicAccessType.None);
                _isActivated = true; result.Result = true; result.Message = "AzureStorageOASIS activated.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { _isActivated = false; return new OASISResult<bool>(true); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static string HolonKey(IHolon holon)
        {
            var customKey = (holon as Holon)?.CustomKey;
            return !string.IsNullOrEmpty(customKey) ? customKey : holon.Id.ToString();
        }

        private static async Task<string> ReadBlobAsync(BlobClient blob)
        {
            var download = await blob.DownloadContentAsync();
            return download.Value.Content.ToString();
        }

        private static T? Deserialise<T>(string json) =>
            JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        private static string Serialise<T>(T obj) =>
            JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });

        // ─── Holons ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var key = HolonKey(holon);
                var json = Serialise(new
                {
                    id = holon.Id, name = holon.Name, description = holon.Description,
                    holonType = holon.HolonType.ToString(),
                    customKey = (holon as Holon)?.CustomKey,
                    metaData = holon.MetaData
                });
                var container = _serviceClient.GetBlobContainerClient(_holonContainer);
                var blob = container.GetBlobClient(key);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await blob.UploadAsync(stream, overwrite: true);
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS] = key;
                result.Result = holon; result.Message = $"AzureStorageOASIS: Saved holon blob '{key}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            foreach (var h in holons)
            {
                var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (!r.IsError && r.Result != null) saved.Add(r.Result);
            }
            result.Result = saved; return result;
        }
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var blob = _serviceClient.GetBlobContainerClient(_holonContainer).GetBlobClient(providerKey);
                if (!await blob.ExistsAsync()) { OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Blob '{providerKey}' not found."); return result; }
                var json = await ReadBlobAsync(blob);
                var doc = JsonDocument.Parse(json);
                var holon = new Holon();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS] = providerKey;
                holon.Name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() : "";
                holon.Description = doc.RootElement.TryGetProperty("description", out var d) ? d.GetString() : "";
                if (doc.RootElement.TryGetProperty("id", out var id) && Guid.TryParse(id.GetString(), out var guid)) holon.Id = guid;
                result.Result = holon;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await LoadHolonAsync(id.ToString(), loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var holons = new List<IHolon>();
            try
            {
                var container = _serviceClient.GetBlobContainerClient(_holonContainer);
                await foreach (var item in container.GetBlobsAsync())
                {
                    var r = await LoadHolonAsync(item.Name, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
                    if (!r.IsError && r.Result != null) holons.Add(r.Result);
                }
                result.Result = holons;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // providerKey = blob name prefix (e.g. "avatarId/")
            var result = new OASISResult<IEnumerable<IHolon>>();
            var holons = new List<IHolon>();
            try
            {
                var container = _serviceClient.GetBlobContainerClient(_holonContainer);
                await foreach (var item in container.GetBlobsAsync(prefix: providerKey))
                {
                    var r = await LoadHolonAsync(item.Name, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
                    if (!r.IsError && r.Result != null) holons.Add(r.Result);
                }
                result.Result = holons;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await LoadHolonsForParentAsync(id.ToString(), type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var blob = _serviceClient.GetBlobContainerClient(_holonContainer).GetBlobClient(id.ToString());
                await blob.DeleteIfExistsAsync();
                result.Result = new Holon { Id = id };
                result.Message = $"AzureStorageOASIS: Deleted holon blob '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var blob = _serviceClient.GetBlobContainerClient(_holonContainer).GetBlobClient(providerKey);
                await blob.DeleteIfExistsAsync();
                result.Result = new Holon { CustomKey = providerKey };
                result.Message = $"AzureStorageOASIS: Deleted holon blob '{providerKey}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AzureStorageOASIS: MetaData search not supported — use LoadAllHolons and filter client-side."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AzureStorageOASIS: MetaData search not supported."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        { var r = new OASISResult<ISearchResults>(); OASISErrorHandling.HandleError(ref r, "AzureStorageOASIS: Full-text search not supported."); return await Task.FromResult(r); }
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Avatars ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
            => await LoadAvatarByProviderKeyAsync(id.ToString(), version);
        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var blob = _serviceClient.GetBlobContainerClient(_avatarContainer).GetBlobClient(providerKey);
                if (!await blob.ExistsAsync()) { OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Avatar blob '{providerKey}' not found."); return result; }
                var json = await ReadBlobAsync(blob);
                var doc = JsonDocument.Parse(json);
                var avatar = new Avatar();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS] = providerKey;
                avatar.Username = doc.RootElement.TryGetProperty("username", out var u) ? u.GetString() : "";
                avatar.Email = doc.RootElement.TryGetProperty("email", out var e) ? e.GetString() : "";
                if (doc.RootElement.TryGetProperty("id", out var id) && Guid.TryParse(id.GetString(), out var guid)) avatar.Id = guid;
                result.Result = avatar;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => await LoadAvatarByProviderKeyAsync($"username:{avatarUsername}", version);
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0) => LoadAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => await LoadAvatarByProviderKeyAsync($"email:{avatarEmail}", version);
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            var avatars = new List<IAvatar>();
            try
            {
                var container = _serviceClient.GetBlobContainerClient(_avatarContainer);
                await foreach (var item in container.GetBlobsAsync())
                {
                    var r = await LoadAvatarByProviderKeyAsync(item.Name, version);
                    if (!r.IsError && r.Result != null) avatars.Add(r.Result);
                }
                result.Result = avatars;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var key = avatar.Id != Guid.Empty ? avatar.Id.ToString() : $"username:{avatar.Username}";
                var json = Serialise(new { id = avatar.Id, username = avatar.Username, email = avatar.Email });
                var container = _serviceClient.GetBlobContainerClient(_avatarContainer);
                var blob = container.GetBlobClient(key);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
                await blob.UploadAsync(stream, overwrite: true);
                // also index by username and email for lookup
                if (!string.IsNullOrEmpty(avatar.Username))
                {
                    var uBlob = container.GetBlobClient($"username:{avatar.Username}");
                    using var us = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    await uBlob.UploadAsync(us, overwrite: true);
                }
                if (!string.IsNullOrEmpty(avatar.Email))
                {
                    var eBlob = container.GetBlobClient($"email:{avatar.Email}");
                    using var es = new MemoryStream(Encoding.UTF8.GetBytes(json));
                    await eBlob.UploadAsync(es, overwrite: true);
                }
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS] = key;
                result.Result = avatar; result.Message = $"AzureStorageOASIS: Saved avatar blob '{key}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "AzureStorageOASIS: AvatarDetail not separately stored."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "AzureStorageOASIS: AvatarDetail not separately stored."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0) => LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "AzureStorageOASIS: AvatarDetail not separately stored."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0) => LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); OASISErrorHandling.HandleError(ref r, "AzureStorageOASIS: AvatarDetail not separately stored."); return await Task.FromResult(r); }
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "AzureStorageOASIS: AvatarDetail not separately stored."); return await Task.FromResult(r); }
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar) => SaveAvatarDetailAsync(avatar).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try { await _serviceClient.GetBlobContainerClient(_avatarContainer).GetBlobClient(id.ToString()).DeleteIfExistsAsync(); result.Result = true; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try { await _serviceClient.GetBlobContainerClient(_avatarContainer).GetBlobClient(providerKey).DeleteIfExistsAsync(); result.Result = true; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
            => await DeleteAvatarAsync($"email:{avatarEmail}", softDelete);
        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true) => DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
            => await DeleteAvatarAsync($"username:{avatarUsername}", softDelete);
        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true) => DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "AzureStorageOASIS: Use SaveHolonsAsync for bulk import."); return await Task.FromResult(r); }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
            => await LoadHolonsForParentAsync(avatarId, version: version);
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => await LoadHolonsForParentAsync($"username:{avatarUsername}", version: version);
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
            => await LoadHolonsForParentAsync($"email:{avatarEmailAddress}", version: version);
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync(version: version);
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        // ─── IOASISNETProvider ────────────────────────────────────────────────────
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        { var r = new OASISResult<IEnumerable<IAvatar>>(); OASISErrorHandling.HandleError(ref r, "AzureStorageOASIS: Geolocation not supported."); return r; }
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "AzureStorageOASIS: Geolocation not supported."); return r; }
    }
}
