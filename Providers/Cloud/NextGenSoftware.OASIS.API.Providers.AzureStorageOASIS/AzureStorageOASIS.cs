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
    ///
    /// Distinct from AzureCosmosDBOASIS (Cosmos DB document database).
    /// This provider targets Azure Blob Storage — massively scalable object storage
    /// with 11 nines of durability and global replication.
    ///
    /// Storage layout:
    ///   Container "oasis-avatars"   → one blob per avatar, name = avatar.Id.ToString()
    ///   Container "oasis-holons"    → one blob per holon,  name = holon.Id.ToString()
    ///   Each blob is UTF-8 JSON of the full OASIS object.
    ///   Blob metadata tags carry indexed fields (Username, Email, ProviderKey)
    ///   for lookup without deserialising every blob.
    ///
    /// Pass the Azure Storage connection string to the constructor.
    /// Obtain it from the Azure portal: Storage account → Access keys → Connection string.
    /// </summary>
    public class AzureStorageOASIS : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly string _connectionString;
        private BlobServiceClient? _serviceClient;
        private BlobContainerClient? _avatarContainer;
        private BlobContainerClient? _holonContainer;

        private const string AvatarContainerName = "oasis-avatars";
        private const string HolonContainerName = "oasis-holons";

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public AzureStorageOASIS(string connectionString)
        {
            _connectionString = connectionString;

            ProviderName = "AzureStorageOASIS";
            ProviderDescription = "Microsoft Azure Blob Storage provider (object storage, distinct from AzureCosmosDBOASIS)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AzureStorageOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                _serviceClient = new BlobServiceClient(_connectionString);
                _avatarContainer = _serviceClient.GetBlobContainerClient(AvatarContainerName);
                _holonContainer = _serviceClient.GetBlobContainerClient(HolonContainerName);

                await _avatarContainer.CreateIfNotExistsAsync(PublicAccessType.None);
                await _holonContainer.CreateIfNotExistsAsync(PublicAccessType.None);

                result.Result = true;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS activated. Containers '{AvatarContainerName}' and '{HolonContainerName}' ready.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AzureStorageOASIS: Error activating provider — {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _serviceClient = null;
            _avatarContainer = null;
            _holonContainer = null;
            return await Task.FromResult(new OASISResult<bool>
            {
                Result = true, IsError = false, Message = "AzureStorageOASIS deactivated."
            });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Container guard ──────────────────────────────────────────────────────

        private OASISResult<T> NotActivated<T>()
        {
            var r = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref r,
                "AzureStorageOASIS: Provider is not activated. Call ActivateProvider() first.");
            return r;
        }

        // ─── Blob helpers ─────────────────────────────────────────────────────────

        private async Task UploadJsonAsync(BlobContainerClient container, string blobName, object obj,
            Dictionary<string, string>? tags = null)
        {
            string json = JsonSerializer.Serialize(obj, _jsonOpts);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using var stream = new MemoryStream(bytes);

            var client = container.GetBlobClient(blobName);
            await client.UploadAsync(stream, overwrite: true);

            if (tags != null && tags.Count > 0)
                await client.SetTagsAsync(tags);
        }

        private async Task<T?> DownloadJsonAsync<T>(BlobContainerClient container, string blobName)
        {
            var client = container.GetBlobClient(blobName);
            if (!await client.ExistsAsync()) return default;

            var download = await client.DownloadContentAsync();
            string json = download.Value.Content.ToString();
            return JsonSerializer.Deserialize<T>(json, _jsonOpts);
        }

        private async Task<string?> FindBlobByTagAsync(BlobContainerClient container, string tagKey, string tagValue)
        {
            string filter = $"\"{tagKey}\" = '{tagValue}'";
            await foreach (var item in _serviceClient!.FindBlobsByTagsAsync(filter))
            {
                if (item.BlobContainerName == container.Name)
                    return item.BlobName;
            }
            return null;
        }

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            if (_avatarContainer == null) return NotActivated<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();

                string blobName = avatar.Id.ToString();
                var tags = new Dictionary<string, string>
                {
                    ["Username"] = avatar.Username ?? "",
                    ["Email"] = avatar.Email ?? "",
                    ["IsDeleted"] = avatar.IsDeleted.ToString()
                };

                if (avatar.ProviderUniqueStorageKey == null)
                    avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS] = blobName;

                await UploadJsonAsync(_avatarContainer, blobName, avatar, tags);

                result.Result = avatar;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: Avatar '{avatar.Username}' saved (blob: {blobName}).";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AzureStorageOASIS: Error saving avatar '{avatar.Username}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
            => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            if (_avatarContainer == null) return NotActivated<IAvatar>();
            try
            {
                var avatar = await DownloadJsonAsync<Avatar>(_avatarContainer, id.ToString());
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"AzureStorageOASIS: No avatar found with ID '{id}'.");
                    return result;
                }
                result.Result = avatar;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AzureStorageOASIS: Error loading avatar by ID '{id}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
            => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            if (_avatarContainer == null) return NotActivated<IAvatar>();
            try
            {
                string? blobName = await FindBlobByTagAsync(_avatarContainer, "Username", username);
                if (blobName == null)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"AzureStorageOASIS: No avatar found with username '{username}'.");
                    return result;
                }
                var avatar = await DownloadJsonAsync<Avatar>(_avatarContainer, blobName);
                result.Result = avatar;
                result.IsError = avatar == null;
                result.Message = avatar != null
                    ? $"AzureStorageOASIS: Avatar loaded for username '{username}'."
                    : $"AzureStorageOASIS: Blob '{blobName}' found by tag but could not be deserialised.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AzureStorageOASIS: Error loading avatar by username '{username}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
            => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            // providerKey = blob name (avatar GUID)
            var result = new OASISResult<IAvatar>();
            if (_avatarContainer == null) return NotActivated<IAvatar>();
            try
            {
                var avatar = await DownloadJsonAsync<Avatar>(_avatarContainer, providerKey);
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result,
                        $"AzureStorageOASIS: No avatar found with provider key '{providerKey}'.");
                    return result;
                }
                result.Result = avatar;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: Avatar loaded for provider key '{providerKey}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AzureStorageOASIS: Error loading avatar by provider key '{providerKey}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
            => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            if (_avatarContainer == null) return NotActivated<IEnumerable<IAvatar>>();
            try
            {
                var avatars = new List<IAvatar>();
                await foreach (var blobItem in _avatarContainer.GetBlobsAsync())
                {
                    var avatar = await DownloadJsonAsync<Avatar>(_avatarContainer, blobItem.Name);
                    if (avatar != null) avatars.Add(avatar);
                }
                result.Result = avatars;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AzureStorageOASIS: Error loading all avatars: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            if (_avatarContainer == null) return NotActivated<bool>();
            try
            {
                if (softDelete)
                {
                    var avatar = await DownloadJsonAsync<Avatar>(_avatarContainer, id.ToString());
                    if (avatar != null)
                    {
                        avatar.IsDeleted = true;
                        await SaveAvatarAsync(avatar);
                        result.Result = true;
                        result.IsError = false;
                        result.Message = $"AzureStorageOASIS: Avatar '{id}' soft-deleted.";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: No avatar found with ID '{id}' to delete.");
                    }
                }
                else
                {
                    var client = _avatarContainer.GetBlobClient(id.ToString());
                    await client.DeleteIfExistsAsync();
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"AzureStorageOASIS: Avatar '{id}' hard-deleted.";
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AzureStorageOASIS: Error deleting avatar '{id}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
            => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            if (_avatarContainer == null) return NotActivated<bool>();
            try
            {
                string? blobName = await FindBlobByTagAsync(_avatarContainer, "Username", username);
                if (blobName == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: No avatar found with username '{username}'.");
                    return result;
                }
                if (Guid.TryParse(blobName, out Guid id))
                    return await DeleteAvatarAsync(id, softDelete);

                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Blob name '{blobName}' is not a valid GUID.");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error deleting avatar by username '{username}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
            => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            if (_avatarContainer == null) return NotActivated<bool>();
            try
            {
                string? blobName = await FindBlobByTagAsync(_avatarContainer, "Email", email);
                if (blobName == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: No avatar found with email '{email}'.");
                    return result;
                }
                if (Guid.TryParse(blobName, out Guid id))
                    return await DeleteAvatarAsync(id, softDelete);

                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Blob name '{blobName}' is not a valid GUID.");
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error deleting avatar by email '{email}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
            => DeleteAvatarByEmailAsync(email, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            if (_avatarContainer == null) return NotActivated<IAvatarDetail>();
            try
            {
                var detail = await DownloadJsonAsync<AvatarDetail>(_avatarContainer, $"detail-{id}");
                if (detail == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: No avatar detail found for ID '{id}'.");
                    return result;
                }
                result.Result = detail;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error loading avatar detail for '{id}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
            => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            if (_avatarContainer == null) return NotActivated<IAvatarDetail>();
            try
            {
                string? blobName = await FindBlobByTagAsync(_avatarContainer, "Username", username);
                if (blobName == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: No avatar found with username '{username}' to load detail.");
                    return result;
                }
                // Avatar detail is stored as detail-<id>
                var avatar = await DownloadJsonAsync<Avatar>(_avatarContainer, blobName);
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Could not load avatar for username '{username}'.");
                    return result;
                }
                return await LoadAvatarDetailAsync(avatar.Id, version);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error loading avatar detail by username '{username}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
            => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            if (_avatarContainer == null) return NotActivated<IAvatarDetail>();
            try
            {
                string? blobName = await FindBlobByTagAsync(_avatarContainer, "Email", email);
                if (blobName == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: No avatar found with email '{email}' to load detail.");
                    return result;
                }
                var avatar = await DownloadJsonAsync<Avatar>(_avatarContainer, blobName);
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Could not load avatar for email '{email}'.");
                    return result;
                }
                return await LoadAvatarDetailAsync(avatar.Id, version);
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error loading avatar detail by email '{email}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
            => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            if (_avatarContainer == null) return NotActivated<IEnumerable<IAvatarDetail>>();
            try
            {
                var details = new List<IAvatarDetail>();
                await foreach (var blobItem in _avatarContainer.GetBlobsAsync(prefix: "detail-"))
                {
                    var detail = await DownloadJsonAsync<AvatarDetail>(_avatarContainer, blobItem.Name);
                    if (detail != null) details.Add(detail);
                }
                result.Result = details;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error loading all avatar details: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
            => LoadAllAvatarDetailsAsync(version).Result;

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            if (_avatarContainer == null) return NotActivated<IAvatarDetail>();
            try
            {
                if (avatarDetail.Id == Guid.Empty) avatarDetail.Id = Guid.NewGuid();
                string blobName = $"detail-{avatarDetail.Id}";
                var tags = new Dictionary<string, string>
                {
                    ["Username"] = avatarDetail.Username ?? "",
                    ["Email"] = avatarDetail.Email ?? ""
                };
                await UploadJsonAsync(_avatarContainer, blobName, avatarDetail, tags);
                result.Result = avatarDetail;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: AvatarDetail saved (blob: {blobName}).";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error saving avatar detail: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
            => SaveAvatarDetailAsync(avatarDetail).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            if (_holonContainer == null) return NotActivated<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();

                string blobName = holon.Id.ToString();
                var tags = new Dictionary<string, string>
                {
                    ["HolonType"] = holon.HolonType.ToString(),
                    ["IsDeleted"] = holon.IsDeleted.ToString()
                };

                if (holon.ProviderUniqueStorageKey == null)
                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureStorageOASIS] = blobName;

                await UploadJsonAsync(_holonContainer, blobName, holon, tags);

                result.Result = holon;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: Holon '{holon.Name}' saved (blob: {blobName}).";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result,
                    $"AzureStorageOASIS: Error saving holon '{holon.Name}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            var errors = new List<string>();
            foreach (var holon in holons)
            {
                var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (r.IsError) errors.Add(r.Message);
                else saved.Add(r.Result!);
            }
            result.Result = saved;
            result.IsError = errors.Count > 0;
            result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"AzureStorageOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            if (_holonContainer == null) return NotActivated<IHolon>();
            try
            {
                var holon = await DownloadJsonAsync<Holon>(_holonContainer, id.ToString());
                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: No holon found with ID '{id}'.");
                    return result;
                }
                result.Result = holon;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: Holon loaded for ID '{id}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error loading holon '{id}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // providerKey = blob name (holon GUID)
            if (Guid.TryParse(providerKey, out Guid id))
                return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (_holonContainer == null) return NotActivated<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                await foreach (var blobItem in _holonContainer.GetBlobsAsync())
                {
                    var holon = await DownloadJsonAsync<Holon>(_holonContainer, blobItem.Name);
                    if (holon != null && (holonType == HolonType.All || holon.HolonType == holonType))
                        holons.Add(holon);
                }
                result.Result = holons;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error loading all holons: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (_holonContainer == null) return NotActivated<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                string parentIdStr = id.ToString();
                await foreach (var blobItem in _holonContainer.GetBlobsAsync())
                {
                    var holon = await DownloadJsonAsync<Holon>(_holonContainer, blobItem.Name);
                    if (holon != null && holon.ParentHolonId == id &&
                        (holonType == HolonType.All || holon.HolonType == holonType))
                        holons.Add(holon);
                }
                result.Result = holons;
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error loading holons for parent '{id}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            if (Guid.TryParse(providerKey, out Guid id))
                return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider);

            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            if (_holonContainer == null) return NotActivated<bool>();
            try
            {
                if (softDelete)
                {
                    var holon = await DownloadJsonAsync<Holon>(_holonContainer, id.ToString());
                    if (holon != null)
                    {
                        holon.IsDeleted = true;
                        await SaveHolonAsync(holon);
                        result.Result = true;
                        result.IsError = false;
                        result.Message = $"AzureStorageOASIS: Holon '{id}' soft-deleted.";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: No holon found with ID '{id}' to delete.");
                    }
                }
                else
                {
                    var client = _holonContainer.GetBlobClient(id.ToString());
                    await client.DeleteIfExistsAsync();
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"AzureStorageOASIS: Holon '{id}' hard-deleted.";
                }
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error deleting holon '{id}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true)
            => DeleteHolonAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            if (Guid.TryParse(providerKey, out Guid id))
                return await DeleteHolonAsync(id, softDelete);

            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<bool> DeleteHolon(string providerKey, bool softDelete = true)
            => DeleteHolonAsync(providerKey, softDelete).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            if (_holonContainer == null) return NotActivated<ISearchResults>();
            try
            {
                string query = searchParams.SearchQuery?.ToLowerInvariant() ?? string.Empty;
                var matched = new List<IHolon>();

                await foreach (var blobItem in _holonContainer.GetBlobsAsync())
                {
                    var holon = await DownloadJsonAsync<Holon>(_holonContainer, blobItem.Name);
                    if (holon != null &&
                        ((holon.Name?.ToLowerInvariant().Contains(query) == true) ||
                         (holon.Description?.ToLowerInvariant().Contains(query) == true)))
                    {
                        matched.Add(holon);
                    }
                }

                result.Result = new SearchResults { Holons = matched };
                result.IsError = false;
                result.Message = $"AzureStorageOASIS: Found {matched.Count} holon(s) matching '{searchParams.SearchQuery}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"AzureStorageOASIS: Error during search: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
    }
}
