using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.TypesenseOASIS
{
    /// <summary>
    /// OASIS provider for Typesense — a fast, typo-tolerant search engine with a Typesense REST API.
    ///
    /// Collections: oasis_avatars, oasis_avatar_details, oasis_holons
    /// Upsert via POST /collections/{name}/documents?action=upsert
    /// Primary key field: "id" (string GUID)
    ///
    /// Constructor parameters:
    ///   host   — Typesense host URL, e.g. "http://localhost:8108"
    ///   apiKey — Typesense API key (X-TYPESENSE-API-KEY header)
    /// </summary>
    public partial class TypesenseOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        /// <summary>
        /// Historical Holon writes and CQL are not implemented and reject when enabled.
        /// This flag must not imply a historical revision was stored.
        /// </summary>
        public bool IsVersionControlEnabled { get; set; }

        private readonly HttpClient _http;
        private readonly string _host;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private const string ColAvatars = "oasis_avatars";
        private const string ColDetails = "oasis_avatar_details";
        private const string ColHolons = "oasis_holons";

        public TypesenseOASIS(string host, string apiKey) : this(new HttpClient(), host, apiKey) { }

        // Supply a dedicated caller-owned client.
        public TypesenseOASIS(HttpClient client, string host, string apiKey)
        {
            _host = host.TrimEnd('/');
            _http = client ?? throw new ArgumentNullException(nameof(client));
            _http.DefaultRequestHeaders.Add("X-TYPESENSE-API-KEY", apiKey);
            ProviderName = "TypesenseOASIS";
            ProviderDescription = "Typesense provider (fast fuzzy-search engine via REST API)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TypesenseOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private StringContent Json(object obj) => new StringContent(Ser(obj), Encoding.UTF8, "application/json");

        private static T? DeserializeDocument<T>(string json) => typeof(T) == typeof(AvatarDetail)
            ? StoredRecordJson.DeserializeStored<T>(json)
            : typeof(T) == typeof(Holon) ? (T)(object)StoredRecordJson.DeserializeStoredHolon(json)
            : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private async Task<T?> GetDocAsync<T>(string collection, string id)
        {
            using var resp = await _http.GetAsync($"{_host}/collections/{collection}/documents/{id}");
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return default;
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            return DeserializeDocument<T>(json);
        }

        private async Task<List<T>> SearchAllAsync<T>(string collection)
        {
            using var deadline = new System.Threading.CancellationTokenSource(_http.Timeout);
            using var response = await _http.GetAsync(
                $"{_host}/collections/{collection}/documents/export", HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var reader = new System.IO.StreamReader(stream);
            var documents = new List<T>();
            while (await reader.ReadLineAsync(deadline.Token) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                var document = DeserializeDocument<T>(line);
                if (document is null)
                    throw new JsonException("Typesense exported a null document.");
                documents.Add(document);
            }

            return documents;
        }

        private async Task<List<T>> SearchByFieldAsync<T>(string collection, string field, string value)
        {
            using var resp = await _http.GetAsync($"{_host}/collections/{collection}/documents/search?q={Uri.EscapeDataString(value)}&query_by={field}&filter_by={field}:={Uri.EscapeDataString(value)}&per_page=1");
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var hits = doc.RootElement.GetProperty("hits");
            var list = new List<T>();
            foreach (var hit in hits.EnumerateArray())
            {
                if (!hit.TryGetProperty("document", out var d)) continue;
                var item = DeserializeDocument<T>(d.GetRawText());
                if (item != null) list.Add(item);
            }
            return list;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                IsProviderActivated = false;
                await EnsureCollectionAsync(ColAvatars);
                await EnsureCollectionAsync(ColDetails);
                await EnsureCollectionAsync(ColHolons);
                IsProviderActivated = true; result.Result = true; result.IsError = false; result.Message = "TypesenseOASIS activated.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { IsProviderActivated = false; return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "TypesenseOASIS deactivated." }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                ArgumentNullException.ThrowIfNull(avatar);
                var id = avatar.Id == Guid.Empty ? Guid.NewGuid() : avatar.Id;
                var keys = avatar.ProviderUniqueStorageKey == null
                    ? new Dictionary<Core.Enums.ProviderType, string>()
                    : new Dictionary<Core.Enums.ProviderType, string>(avatar.ProviderUniqueStorageKey);
                keys[Core.Enums.ProviderType.TypesenseOASIS] = id.ToString("D");
                var document = JsonSerializer.SerializeToNode(avatar, avatar.GetType(), _jsonOpts)!.AsObject();
                document["id"] = id;
                document["providerUniqueStorageKey"] = JsonSerializer.SerializeToNode(keys, _jsonOpts);
                await UpsertDocAsync(ColAvatars, document);

                avatar.Id = id;
                avatar.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.TypesenseOASIS] = id.ToString("D");
                result.Result = avatar;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.Message = $"TypesenseOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var a = await GetDocAsync<Avatar>(ColAvatars, id.ToString());
                if (a == null || a.IsDeleted) { result.ErrorCode = OASISResultErrorCodes.RecordNotFound; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No avatar for ID '{id}'."); return result; }
                result.Result = a; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "TypesenseOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var hits = await SearchByFieldAsync<Avatar>(ColAvatars, "username", username);
                var a = hits.Count > 0 ? hits[0] : null;
                if (a == null || a.IsDeleted) { result.ErrorCode = OASISResultErrorCodes.RecordNotFound; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No avatar for username '{username}'."); return result; }
                result.Result = a; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "TypesenseOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var hits = await SearchByFieldAsync<Avatar>(ColAvatars, "email", avatarEmail);
                var a = hits.Count > 0 ? hits[0] : null;
                if (a == null || a.IsDeleted) { result.ErrorCode = OASISResultErrorCodes.RecordNotFound; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No avatar for email '{avatarEmail}'."); return result; }
                result.Result = a; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "TypesenseOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"TypesenseOASIS: Invalid GUID '{providerKey}'."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var all = await SearchAllAsync<Avatar>(ColAvatars);
                var list = new List<IAvatar>(); foreach (var a in all) { if (!a.IsDeleted) list.Add(a); }
                result.Result = list; result.IsLoaded = true; result.LoadedCount = list.Count; result.Message = $"TypesenseOASIS: Loaded {list.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    var loaded = await LoadAvatarAsync(id);
                    if (loaded.IsError || loaded.Result is not Avatar avatar)
                        throw new InvalidOperationException(loaded.Message, loaded.Exception);

                    avatar.DeletedDate = DateTime.UtcNow;
                    var saved = await SaveAvatarAsync(avatar);
                    if (saved.IsError || !saved.IsSaved)
                        throw new InvalidOperationException(saved.Message, saved.Exception);
                }
                else
                {
                    await DeleteDocAsync(ColDetails, id.ToString("D"), allowMissing: true);
                    await DeleteDocAsync(ColAvatars, id.ToString("D"));
                }

                result.Result = true;
                result.IsDeleted = true;
                result.DeletedCount = 1;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"TypesenseOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "TypesenseOASIS: Avatar not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool softDelete = true) => DeleteAvatarByEmailAsync(e, softDelete).Result;
        public override Task<OASISResult<bool>> DeleteAvatarAsync(string pk, bool softDelete = true)
            => GuidProviderKeyDeletion.DeleteAvatarAsync(pk, softDelete, DeleteAvatarAsync);
        public override OASISResult<bool> DeleteAvatar(string pk, bool softDelete = true) => DeleteAvatarAsync(pk, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail d)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                ArgumentNullException.ThrowIfNull(d);
                var id = d.Id == Guid.Empty ? Guid.NewGuid() : d.Id;
                var document = JsonSerializer.SerializeToNode(d, d.GetType(), _jsonOpts)!.AsObject();
                document["id"] = id;
                await UpsertDocAsync(ColDetails, document);
                d.Id = id;
                result.Result = d;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.Message = "TypesenseOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await GetDocAsync<AvatarDetail>(ColDetails, id.ToString()); if (d == null) { result.ErrorCode = OASISResultErrorCodes.RecordNotFound; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No detail for ID '{id}'."); return result; } result.Result = d; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "TypesenseOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var hits = await SearchByFieldAsync<AvatarDetail>(ColDetails, "username", u); var d = hits.Count > 0 ? hits[0] : null; if (d == null) { result.ErrorCode = OASISResultErrorCodes.RecordNotFound; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "TypesenseOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var hits = await SearchByFieldAsync<AvatarDetail>(ColDetails, "email", e); var d = hits.Count > 0 ? hits[0] : null; if (d == null) { result.ErrorCode = OASISResultErrorCodes.RecordNotFound; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "TypesenseOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var all = await SearchAllAsync<AvatarDetail>(ColDetails); result.Result = new List<IAvatarDetail>(all); result.IsLoaded = true; result.LoadedCount = all.Count; result.Message = $"TypesenseOASIS: Loaded {all.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holons ───────────────────────────────────────────────────────────────

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonGraphAsync(holon, saveChildren, recursive, maxChildDepth, 0, continueOnError);

        private async Task<OASISResult<IHolon>> SaveHolonGraphAsync(IHolon holon, bool saveChildren, bool recursive,
            int maxChildDepth, int currentChildDepth, bool continueOnError)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (IsVersionControlEnabled)
                    throw new NotSupportedException("Typesense historical writes are not implemented.");

                var plan = HolonGraphWritePlan.CreateForSave(new[] { holon }, saveChildren, recursive, maxChildDepth, currentChildDepth);
                var failed = new HashSet<Guid>();
                foreach (var node in plan.Nodes)
                {
                    if (failed.Contains(node.Snapshot.ParentHolonId))
                    {
                        failed.Add(node.Snapshot.Id);
                        continue;
                    }

                    try
                    {
                        await SavePlannedHolonAsync(node);
                        if (ReferenceEquals(node.Source, holon))
                            result.Result = holon;
                        result.SavedCount++;
                    }
                    catch (Exception ex)
                    {
                        failed.Add(node.Snapshot.Id);
                        OASISErrorHandling.HandleError(ref result, $"Typesense did not save holon '{node.Snapshot.Id}'.", ex);
                        if (!continueOnError)
                            break;
                    }
                }

                result.IsSaved = result.SavedCount == plan.Nodes.Count;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}", ex);
            }
            return result;
        }

        private async Task SavePlannedHolonAsync(PlannedHolonWrite node)
        {
            var snapshot = node.Snapshot;
            var id = snapshot.Id.ToString("D");
            var keys = snapshot.ProviderUniqueStorageKey ?? new Dictionary<Core.Enums.ProviderType, string>();
            keys[Core.Enums.ProviderType.TypesenseOASIS] = id;
            var captured = JsonNode.Parse(node.CapturedIntentJson)!.AsObject();
            var document = new JsonObject();
            foreach (var property in captured)
            {
                if (property.Key != "$type")
                    document.Add(JsonNamingPolicy.CamelCase.ConvertName(property.Key), property.Value?.DeepClone());
            }
            document["id"] = id;
            document["parentHolonId"] = snapshot.ParentHolonId;
            document["providerUniqueStorageKey"] = JsonSerializer.SerializeToNode(keys, _jsonOpts);
            await UpsertDocAsync(ColHolons, document);

            node.Source.Id = snapshot.Id;
            node.Source.ParentHolonId = snapshot.ParentHolonId;
            node.Source.ProviderUniqueStorageKey = keys;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            var errors = new List<string>();
            foreach (var holon in holons)
            {
                var write = await SaveHolonGraphAsync(holon, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError);
                result.SavedCount += write.SavedCount;
                if (write.IsError) { errors.Add(write.Message); if (!continueOnError) break; }
                else if (write.IsSaved && write.Result != null) saved.Add(write.Result);
            }
            result.Result = saved; result.IsError = errors.Count > 0; result.ErrorCount = errors.Count;
            result.IsSaved = result.SavedCount > 0 && !result.IsError;
            result.Message = string.Join("; ", errors);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var h = await GetDocAsync<Holon>(ColHolons, id.ToString());
                if (h == null || h.IsDeleted)
                {
                    OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: No holon for ID '{id}'.");
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    return result;
                }

                result.Result = h;
                result.IsLoaded = true;
                result.LoadedCount = 1;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"TypesenseOASIS: Invalid GUID '{providerKey}'."); return r; }
        public OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await SearchAllAsync<Holon>(ColHolons); var list = new List<IHolon>();
                foreach (var h in all) { if (h.IsDeleted) continue; if (holonType != HolonType.All && h.HolonType != holonType) continue; list.Add(h); }
                result.Result = list; result.IsLoaded = true; result.LoadedCount = list.Count; result.Message = $"TypesenseOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var all = await SearchAllAsync<Holon>(ColHolons); var list = new List<IHolon>();
                foreach (var h in all) { if (h.IsDeleted || h.ParentHolonId != id) continue; if (holonType != HolonType.All && h.HolonType != holonType) continue; list.Add(h); }
                result.Result = list; result.IsLoaded = true; result.LoadedCount = list.Count; result.Message = $"TypesenseOASIS: Loaded {list.Count} child holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"TypesenseOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    var loaded = await LoadHolonAsync(id, loadChildren: false);
                    if (loaded.IsError || loaded.Result is not Holon holon)
                        throw new InvalidOperationException(loaded.Message, loaded.Exception);

                    holon.DeletedDate = DateTime.UtcNow;
                    var saved = await SaveHolonAsync(holon, saveChildren: false);
                    if (saved.IsError || !saved.IsSaved || saved.SavedCount != 1)
                        throw new InvalidOperationException(saved.Message, saved.Exception);
                }
                else
                {
                    await DeleteDocAsync(ColHolons, id.ToString());
                }

                result.Result = true;
                result.IsDeleted = true;
                result.DeletedCount = 1;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: {ex.Message}"); }
            return result;
        }

        public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true) => DeleteHolonSoftAsync(id, softDelete).Result;
        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonSoftAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"TypesenseOASIS: Invalid GUID '{pk}'."); return r; }
        public OASISResult<bool> DeleteHolonSoft(string pk, bool softDelete = true) => DeleteHolonSoftAsync(pk, softDelete).Result;

        // ─── Search ───────────────────────────────────────────────────────────────



        public string GetProviderVersion() => "1.0.0";
        public Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");

        // ─── Remaining IOASISStorageProvider surface ─────────────────────────────



        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            if (Guid.TryParse(providerKey, out var id))
                return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);

            var all = await LoadAllHolonsAsync();
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            foreach (var holon in all.Result)
            {
                if (holon.ProviderUniqueStorageKey != null
                    && holon.ProviderUniqueStorageKey.TryGetValue(ProviderType.Value, out var key)
                    && key == providerKey)
                {
                    result.Result = holon;
                    return result;
                }
            }

            OASISErrorHandling.HandleError(ref result, $"Holon with provider key '{providerKey}' not found.");
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
            => DeleteHolonAsync(id).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            var loaded = await LoadHolonAsync(id);
            if (loaded.IsError || loaded.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, loaded.Message);
                return result;
            }

            var deleted = await DeleteHolonSoftAsync(id, true);
            if (deleted.IsError)
                OASISErrorHandling.HandleError(ref result, deleted.Message);
            else
                result.Result = loaded.Result;

            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
            => DeleteHolonAsync(providerKey).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            var holon = await LoadHolonAsync(providerKey);
            if (holon.IsError || holon.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, holon.Message);
                return result;
            }
            return await DeleteHolonAsync(holon.Result.Id);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            var matches = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.MetaData != null
                    && holon.MetaData.TryGetValue(metaKey, out var value)
                    && value?.ToString() == metaValue)
                    matches.Add(holon);
            }

            result.Result = matches;
            result.IsLoaded = true;
            result.LoadedCount = matches.Count;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0)
            {
                return all;
            }

            var matches = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.MetaData == null) continue;

                var matched = 0;
                foreach (var pair in metaKeyValuePairs)
                {
                    if (holon.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value)
                        matched++;
                }

                var isMatch = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All
                    ? matched == metaKeyValuePairs.Count
                    : matched > 0;

                if (isMatch) matches.Add(holon);
            }

            result.Result = matches;
            result.IsLoaded = true;
            result.LoadedCount = matches.Count;
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
            => ImportAsync(holons).GetAwaiter().GetResult();

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            var saved = await SaveHolonsAsync(holons);
            if (saved.IsError)
                OASISErrorHandling.HandleError(ref result, saved.Message);
            else
                result.Result = saved.IsSaved;
            result.IsSaved = saved.IsSaved;
            result.SavedCount = saved.SavedCount;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => ExportAllAsync(version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
            => ExportAllDataForAvatarByIdAsync(avatarId, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var all = await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
            if (all.IsError || all.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, all.Message);
                return result;
            }

            var owned = new List<IHolon>();
            foreach (var holon in all.Result)
            {
                if (holon.CreatedByAvatarId == avatarId)
                    owned.Add(holon);
            }

            result.Result = owned;
            result.IsLoaded = true;
            result.LoadedCount = owned.Count;
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var avatar = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
            => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var avatar = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            if (avatar.IsError || avatar.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, avatar.Message);
                return result;
            }
            return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version);
        }

        // ─── Search ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            var searchResults = new SearchResults();

            try
            {
                var groups = searchParams?.SearchGroups ?? new List<ISearchGroupBase>();
                var wantAvatars = groups.Count == 0 || groups.Exists(g => g.SearchAvatars);
                var wantHolons = groups.Count == 0 || groups.Exists(g => g.SearchHolons);

                var matchedAvatars = new Dictionary<Guid, IAvatar>();
                var matchedHolons = new Dictionary<Guid, IHolon>();

                // ── Avatars ──────────────────────────────────────────────────
                if (wantAvatars)
                {
                    var avatars = await LoadAllAvatarsAsync(version);
                    if (avatars.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, avatars.Message);
                        return result;
                    }

                    foreach (var avatar in avatars.Result ?? new List<IAvatar>())
                    {
                        if (avatar == null) continue;
                        if (searchParams != null && searchParams.SearchOnlyForCurrentAvatar
                            && searchParams.AvatarId != Guid.Empty && avatar.Id != searchParams.AvatarId)
                            continue;

                        if (groups.Count == 0 || AvatarMatchesAnyGroup(avatar, groups))
                            matchedAvatars[avatar.Id] = avatar;
                    }
                }

                // ── Holons ───────────────────────────────────────────────────
                if (wantHolons)
                {
                    var holons = await LoadAllHolonsAsync(HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, false, version);
                    if (holons.IsError && !continueOnError)
                    {
                        OASISErrorHandling.HandleError(ref result, holons.Message);
                        return result;
                    }

                    foreach (var holon in holons.Result ?? new List<IHolon>())
                    {
                        if (holon == null) continue;

                        if (searchParams != null)
                        {
                            if (searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId != Guid.Empty
                                && holon.CreatedByAvatarId != searchParams.AvatarId)
                                continue;

                            if (searchParams.ParentId != Guid.Empty && holon.ParentHolonId != searchParams.ParentId)
                                continue;

                            if (!MetaDataMatches(holon, searchParams.FilterByMetaData, searchParams.MetaKeyValuePairMatchMode))
                                continue;
                        }

                        if (groups.Count == 0 || HolonMatchesAnyGroup(holon, groups))
                            matchedHolons[holon.Id] = holon;
                    }
                }

                searchResults.SearchResultAvatars = new List<IAvatar>(matchedAvatars.Values);
                searchResults.SearchResultHolons = new List<IHolon>(matchedHolons.Values);
                searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;

                result.Result = searchResults;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"TypesenseOASIS: SearchAsync failed: {ex.Message}");
            }

            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).GetAwaiter().GetResult();

        private static bool Contains(string source, string query)
            => !string.IsNullOrEmpty(source) && source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool MetaDataMatches(IHolon holon, Dictionary<string, string> filter, MetaKeyValuePairMatchMode mode)
        {
            if (filter == null || filter.Count == 0) return true;
            if (holon.MetaData == null) return false;

            var matched = 0;
            foreach (var pair in filter)
            {
                if (holon.MetaData.TryGetValue(pair.Key, out var value) && value?.ToString() == pair.Value)
                    matched++;
            }

            return mode == MetaKeyValuePairMatchMode.All ? matched == filter.Count : matched > 0;
        }

        private static bool AvatarMatchesAnyGroup(IAvatar avatar, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchAvatars) continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.AvatarSearchParams;

                // No field flags set - match the natural identity fields.
                if (p == null)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query)
                        || Contains(avatar.FirstName, query) || Contains(avatar.LastName, query))
                        return true;
                    continue;
                }

                if (p.Username && Contains(avatar.Username, query)) return true;
                if (p.Email && Contains(avatar.Email, query)) return true;
                if (p.FirstName && Contains(avatar.FirstName, query)) return true;
                if (p.LastName && Contains(avatar.LastName, query)) return true;
                if (p.Title && Contains(avatar.Title, query)) return true;
                if (p.AvatarId && Contains(avatar.Id.ToString(), query)) return true;
                if (text != null && text.SearchIds && Contains(avatar.Id.ToString(), query)) return true;

                if (text != null && text.SearchProviderKeys && avatar.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in avatar.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                // Flags present but none of them matched a searchable field - fall
                // back to identity fields so a query is never silently dropped.
                if (!p.Username && !p.Email && !p.FirstName && !p.LastName && !p.Title && !p.AvatarId)
                {
                    if (Contains(avatar.Username, query) || Contains(avatar.Email, query))
                        return true;
                }
            }

            return false;
        }

        private static bool HolonMatchesAnyGroup(IHolon holon, List<ISearchGroupBase> groups)
        {
            foreach (var group in groups)
            {
                if (!group.SearchHolons) continue;

                if (group.HolonType != HolonType.All && holon.HolonType != group.HolonType)
                    continue;

                var text = group as ISearchTextGroup;
                var query = text?.SearchQuery;
                if (string.IsNullOrWhiteSpace(query)) return true;

                var p = group.HolonSearchParams;

                if (p == null)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                    continue;
                }

                if (p.Name && Contains(holon.Name, query)) return true;
                if (p.Description && Contains(holon.Description, query)) return true;
                if (text != null && text.SearchIds && Contains(holon.Id.ToString(), query)) return true;

                if (p.MetaData && holon.MetaData != null)
                {
                    foreach (var kvp in holon.MetaData)
                        if (Contains(kvp.Key, query) || Contains(kvp.Value?.ToString(), query)) return true;
                }

                if ((p.ProviderUniqueStorageKey || (text != null && text.SearchProviderKeys))
                    && holon.ProviderUniqueStorageKey != null)
                {
                    foreach (var key in holon.ProviderUniqueStorageKey.Values)
                        if (Contains(key, query)) return true;
                }

                if (!p.Name && !p.Description && !p.MetaData && !p.ProviderUniqueStorageKey)
                {
                    if (Contains(holon.Name, query) || Contains(holon.Description, query))
                        return true;
                }
            }

            return false;
        }

    }
}
