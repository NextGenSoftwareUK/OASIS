using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.KeyValue;
using Couchbase.Query;
using Couchbase.Management.Collections;
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

namespace NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS
{
    /// <summary>
    /// OASIS provider for Couchbase document storage through the native .NET SDK.
    ///
    /// Uses three collections inside a single bucket (default scope):
    ///   avatars         — stores Avatar documents
    ///   avatar_details  — stores AvatarDetail documents
    ///   holons          — stores Holon documents
    ///
    /// Documents are stored as JSON with the OASIS ID as the document key.
    /// N1QL queries are used for lookups by username, email, and parent holon ID.
    ///
    /// Constructor parameters:
    ///   connectionString — Couchbase connection string, e.g. "couchbase://localhost" or "couchbases://cluster.cloud.couchbase.com"
    ///   username         — Couchbase administrator username
    ///   password         — Couchbase administrator password
    ///   bucketName       — name of the target bucket (default: "oasis")
    /// </summary>
    public partial class CouchbaseOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _connectionString;
        private readonly string _username;
        private readonly string _password;
        private readonly string _bucketName;
        private ICluster? _cluster;
        private IBucket? _bucket;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public CouchbaseOASIS(string connectionString, string username, string password, string bucketName = "oasis")
        {
            if (string.IsNullOrEmpty(bucketName) || bucketName.Length > 100 || bucketName[0] == '.' ||
                bucketName.Any(c => !((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') || c == '_' || c == '-' || c == '.' || c == '%')))
                throw new ArgumentException("Use a valid Couchbase bucket identifier, at most 100 characters.", nameof(bucketName));
            _connectionString = connectionString; _username = username; _password = password; _bucketName = bucketName;
            ProviderName = "CouchbaseOASIS";
            ProviderDescription = "Couchbase document database provider (CouchbaseNetClient SDK and SQL++ queries)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CouchbaseOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocal);
        }

        // Canonical OASIS record JSON keeps concrete types for interface members and skips holon navigation.
        private static string Ser(object o) => NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(o);
        private static T? Des<T>(string? json) => json == null ? default : NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<T>(json);

        /// <summary>Native envelope document as written to the avatars, avatar_details and holons collections.</summary>
        private sealed class StoredDocument
        {
            public string? Id { get; set; }
            public string? Username { get; set; }
            public string? Email { get; set; }
            public string? ParentHolonId { get; set; }
            public int HolonType { get; set; }
            public bool IsDeleted { get; set; }
            public string? DataJson { get; set; }
        }

        private async Task<IBucket> GetBucketAsync()
        {
            if (_cluster == null) _cluster = await Couchbase.Cluster.ConnectAsync(_connectionString, _username, _password);
            if (_bucket == null) _bucket = await _cluster.BucketAsync(_bucketName);
            return _bucket;
        }

        private async Task<ICouchbaseCollection> GetCollectionAsync(string name)
        {
            var bucket = await GetBucketAsync();
            var scope = await bucket.DefaultScopeAsync();
            return await scope.CollectionAsync(name);
        }

        private async Task<List<T>> N1qlAsync<T>(string query, Dictionary<string, object>? args = null)
        {
            if (_cluster == null) _cluster = await Couchbase.Cluster.ConnectAsync(_connectionString, _username, _password);
            // Lookups must observe acknowledged writes; the default NotBounded scan can miss them.
            var options = new QueryOptions().ScanConsistency(QueryScanConsistency.RequestPlus);
            if (args != null) foreach (var kv in args) options.Parameter(kv.Key, kv.Value);
            var result = await _cluster.QueryAsync<T>(query, options);
            var rows = new List<T>();
            await foreach (var row in result.Rows)
                rows.Add(row);
            return rows;
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            IsProviderActivated = false;
            try
            {
                var bucket = await GetBucketAsync();
                var scopes = await bucket.Collections.GetAllScopesAsync();
                var scope = scopes.Single(item => item.Name == "_default");
                foreach (var name in new[] { "avatars", "avatar_details", "holons" })
                    if (!scope.Collections.Any(item => item.Name == name))
                        await bucket.Collections.CreateCollectionAsync("_default", name, new CreateCollectionSettings());
                // Create the indexes needed by the existing general lookup APIs.
                if (_cluster != null)
                {
                    foreach (var coll in new[] { "avatars", "avatar_details", "holons" })
                    {
                        using var index = await _cluster.QueryAsync<object>($"CREATE PRIMARY INDEX IF NOT EXISTS ON `{_bucketName}`.`_default`.`{coll}`",
                            new QueryOptions().Timeout(TimeSpan.FromSeconds(15)));
                        await foreach (var ignored in index.Rows) { }
                        if (index.MetaData.Status != QueryStatus.Success)
                            throw new InvalidOperationException("Native index creation did not complete successfully.");
                    }
                }
                IsProviderActivated = true; result.Result = true; result.IsError = false; result.Message = $"CouchbaseOASIS activated — bucket '{_bucketName}' ready.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            if (_cluster != null) { await _cluster.DisposeAsync(); _cluster = null; _bucket = null; }
            return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "CouchbaseOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.CouchbaseOASIS] = avatar.Id.ToString();
                var coll = await GetCollectionAsync("avatars");
                var doc = new { id = avatar.Id.ToString(), username = avatar.Username ?? "", email = avatar.Email ?? "", isDeleted = avatar.IsDeleted, dataJson = Ser(avatar) };
                await coll.UpsertAsync(avatar.Id.ToString(), doc);
                result.Result = avatar; result.IsError = false; result.IsSaved = true; result.SavedCount = 1; result.Message = $"CouchbaseOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var coll = await GetCollectionAsync("avatars");
                IGetResult? get = null;
                try { get = await coll.GetAsync(id.ToString()); } catch (DocumentNotFoundException) { }
                if (get == null) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: No avatar for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                var doc = get.ContentAs<StoredDocument>();
                if (doc.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: Avatar '{id}' is deleted."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                var avatar = Des<Avatar>(doc.DataJson); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "CouchbaseOASIS: Deserialise failed."); return result; }
                result.Result = avatar; result.IsError = false; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "CouchbaseOASIS: loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        private async Task<Avatar?> QueryAvatarAsync(string field, string value)
        {
            var rows = await N1qlAsync<StoredDocument>($"SELECT dataJson FROM `{_bucketName}`.`_default`.`avatars` WHERE {field} = $val AND isDeleted = false LIMIT 1", new Dictionary<string, object> { ["val"] = value });
            var row = rows.FirstOrDefault();
            return row == null ? null : Des<Avatar>(row.DataJson);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarAsync("username", username); if (a == null) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: No avatar for username '{username}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; } result.Result = a; result.IsError = false; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "CouchbaseOASIS: loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarAsync("email", email); if (a == null) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: No avatar for email '{email}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; } result.Result = a; result.IsError = false; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "CouchbaseOASIS: loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"CouchbaseOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var rows = await N1qlAsync<StoredDocument>($"SELECT dataJson FROM `{_bucketName}`.`_default`.`avatars` WHERE isDeleted = false");
                var avatars = rows.Select(r => Des<Avatar>(r.DataJson)).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.IsLoaded = true; result.LoadedCount = avatars.Count; result.Message = $"CouchbaseOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var coll = await GetCollectionAsync("avatars");
                if (softDelete)
                {
                    var loaded = await LoadAvatarAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: Avatar '{id}' not found."); return result; }
                    var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                    await coll.UpsertAsync(id.ToString(), new { id = id.ToString(), username = av.Username ?? "", email = av.Email ?? "", isDeleted = true, dataJson = Ser(av) });
                }
                else
                {
                    try { await coll.RemoveAsync(id.ToString()); }
                    catch (DocumentNotFoundException) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: No avatar for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                    var details = await GetCollectionAsync("avatar_details");
                    try { await details.RemoveAsync(id.ToString()); } catch (DocumentNotFoundException) { }
                }
                result.Result = true; result.IsError = false; result.IsDeleted = true; result.DeletedCount = 1; result.Message = $"CouchbaseOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                if (d.Id == Guid.Empty) d.Id = Guid.NewGuid();
                var coll = await GetCollectionAsync("avatar_details");
                await coll.UpsertAsync(d.Id.ToString(), new { id = d.Id.ToString(), username = d.Username ?? "", email = d.Email ?? "", dataJson = Ser(d) });
                result.Result = d; result.IsError = false; result.IsSaved = true; result.SavedCount = 1; result.Message = "CouchbaseOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var coll = await GetCollectionAsync("avatar_details");
                IGetResult? get = null; try { get = await coll.GetAsync(id.ToString()); } catch (DocumentNotFoundException) { }
                if (get == null) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: No detail for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                var doc = get.ContentAs<StoredDocument>();
                var detail = Des<AvatarDetail>(doc.DataJson); if (detail == null) { OASISErrorHandling.HandleError(ref result, "CouchbaseOASIS: Deserialise failed."); return result; }
                result.Result = detail; result.IsError = false; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "CouchbaseOASIS: loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        private async Task<AvatarDetail?> QueryDetailAsync(string field, string value)
        {
            var rows = await N1qlAsync<StoredDocument>($"SELECT dataJson FROM `{_bucketName}`.`_default`.`avatar_details` WHERE {field} = $val LIMIT 1", new Dictionary<string, object> { ["val"] = value });
            var row = rows.FirstOrDefault();
            return row == null ? null : Des<AvatarDetail>(row.DataJson);
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await QueryDetailAsync("username", u); if (d == null) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: No detail for username '{u}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; } result.Result = d; result.IsError = false; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "CouchbaseOASIS: loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await QueryDetailAsync("email", e); if (d == null) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: No detail for email '{e}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; } result.Result = d; result.IsError = false; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "CouchbaseOASIS: loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var rows = await N1qlAsync<StoredDocument>($"SELECT dataJson FROM `{_bucketName}`.`_default`.`avatar_details`"); var details = rows.Select(r => Des<AvatarDetail>(r.DataJson)).Where(d => d != null).Cast<IAvatarDetail>().ToList(); result.Result = details; result.IsError = false; result.IsLoaded = true; result.LoadedCount = details.Count; result.Message = $"CouchbaseOASIS: Loaded {details.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon == null) throw new ArgumentNullException(nameof(holon));
                var id = holon.Id == Guid.Empty ? Guid.NewGuid() : holon.Id;
                var keys = holon.ProviderUniqueStorageKey == null
                    ? new Dictionary<Core.Enums.ProviderType, string>()
                    : new Dictionary<Core.Enums.ProviderType, string>(holon.ProviderUniqueStorageKey);
                keys[Core.Enums.ProviderType.CouchbaseOASIS] = id.ToString("D");
                var payload = System.Text.Json.Nodes.JsonNode.Parse(Ser(holon))!.AsObject();
                payload["Id"] = id.ToString("D");
                payload["ProviderUniqueStorageKey"] = JsonSerializer.SerializeToNode(keys);
                var cas = await PersistHolonAsync(id, holon.ParentHolonId, (int)holon.HolonType, holon.IsDeleted, payload.ToJsonString());
                if (cas == 0) throw new InvalidOperationException("Native write did not acknowledge a CAS revision.");
                holon.Id = id;
                holon.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.CouchbaseOASIS] = id.ToString("D");
                var savedCount = 1;
                if (saveChildren && holon.Children != null)
                {
                    foreach (var child in holon.Children)
                    {
                        if (child == null) continue;
                        child.ParentHolonId = id;
                        var childResult = await SaveHolonAsync(child, recursive, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                        if (childResult.IsError || !childResult.IsSaved)
                            throw new System.IO.InvalidDataException($"Child holon {child.Id} was not saved: {childResult.Message}");
                        savedCount += childResult.SavedCount;
                    }
                }
                result.Result = holon;
                result.IsSaved = true;
                result.SavedCount = savedCount;
                // Preserve exact native unsigned CAS precision; this is an acknowledgement, not a conditional write.
                result.MetaData["Couchbase.Cas"] = cas.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        /// <summary>Single native storage boundary; caller publication follows its acknowledgement.</summary>
        protected virtual async Task<ulong> PersistHolonAsync(Guid id, Guid parentId, int type, bool deleted, string json)
        {
            var collection = await GetCollectionAsync("holons");
            var result = await collection.UpsertAsync(id.ToString("D"), new {
                id = id.ToString("D"), parentHolonId = parentId.ToString("D"), holonType = type,
                isDeleted = deleted, dataJson = json });
            return result.Cas;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>(); var savedTotal = 0;
            foreach (var h in holons)
            {
                var write = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (write.IsError)
                {
                    errors.Add(write.Message);
                    if (!continueOnError) break;
                }
                else if (write.IsSaved && write.Result != null) { saved.Add(write.Result); savedTotal += write.SavedCount; }
            }
            result.Result = saved;
            result.IsError = errors.Count > 0;
            result.ErrorCount = errors.Count;
            result.IsSaved = saved.Count > 0;
            result.SavedCount = savedTotal;
            result.Message = string.Join("; ", errors);
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var coll = await GetCollectionAsync("holons");
                IGetResult? get = null; try { get = await coll.GetAsync(id.ToString()); } catch (DocumentNotFoundException) { }
                if (get == null) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: No holon for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                var doc = get.ContentAs<StoredDocument>();
                if (doc.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: Holon '{id}' is deleted."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                var holon = Des<Holon>(doc.DataJson); if (holon == null) { OASISErrorHandling.HandleError(ref result, "CouchbaseOASIS: Deserialise failed."); return result; }
                result.Result = holon; result.IsError = false; result.IsLoaded = true; result.LoadedCount = 1; result.Message = "CouchbaseOASIS: loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"CouchbaseOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        private async Task<List<IHolon>> QueryHolonsAsync(string where, Dictionary<string, object>? args = null)
        {
            var rows = await N1qlAsync<StoredDocument>($"SELECT dataJson FROM `{_bucketName}`.`_default`.`holons` WHERE {where}", args);
            return rows.Select(r => Des<Holon>(r.DataJson)).Where(h => h != null).Cast<IHolon>().ToList();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var where = type == HolonType.All ? "isDeleted = false" : "isDeleted = false AND holonType = $ht";
                var args = type == HolonType.All ? null : new Dictionary<string, object> { ["ht"] = (int)type };
                var holons = await QueryHolonsAsync(where, args);
                result.Result = holons; result.IsError = false; result.IsLoaded = true; result.LoadedCount = holons.Count; result.Message = $"CouchbaseOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var where = type == HolonType.All ? "parentHolonId = $pid AND isDeleted = false" : "parentHolonId = $pid AND isDeleted = false AND holonType = $ht";
                var args = new Dictionary<string, object> { ["pid"] = id.ToString() };
                if (type != HolonType.All) args["ht"] = (int)type;
                var holons = await QueryHolonsAsync(where, args);
                result.Result = holons; result.IsError = false; result.IsLoaded = true; result.LoadedCount = holons.Count; result.Message = $"CouchbaseOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"CouchbaseOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id, loadChildren: false); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: Holon '{id}' not found."); result.ErrorCode = loaded.ErrorCode; return result; }
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                var coll = await GetCollectionAsync("holons");
                await coll.UpsertAsync(id.ToString(), new { id = id.ToString(), parentHolonId = holon.ParentHolonId.ToString(), holonType = (int)holon.HolonType, isDeleted = true, dataJson = Ser(holon) });
                result.Result = holon; result.IsError = false; result.IsDeleted = true; result.DeletedCount = 1; result.Message = $"CouchbaseOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        /// <summary>Soft delete (tombstone document) or permanent removal of one native holon document.</summary>
        public async Task<OASISResult<bool>> DeleteHolonSoftAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    var deleted = await DeleteHolonAsync(id);
                    if (deleted.IsError || !deleted.IsDeleted) { OASISErrorHandling.HandleError(ref result, deleted.Message); result.ErrorCode = deleted.ErrorCode; return result; }
                }
                else
                {
                    var coll = await GetCollectionAsync("holons");
                    try { await coll.RemoveAsync(id.ToString()); }
                    catch (DocumentNotFoundException) { OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: No holon for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                }
                result.Result = true; result.IsDeleted = true; result.DeletedCount = 1;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CouchbaseOASIS: {ex.Message}"); }
            return result;
        }

        public OASISResult<bool> DeleteHolonSoft(Guid id, bool softDelete = true) => DeleteHolonSoftAsync(id, softDelete).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"CouchbaseOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"CouchbaseOASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>(); if (all.IsError) { var failed = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref failed, all.Message); return failed; } return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, IsLoaded = true, LoadedCount = holons.Count, Message = $"CouchbaseOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); if (all.IsError) { var failed = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref failed, all.Message); return failed; } return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, IsLoaded = true, LoadedCount = holons.Count, Message = $"CouchbaseOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons, saveChildren: false, continueOnError: false); var ok = !s.IsError && s.IsSaved; return new OASISResult<bool> { Result = ok, IsError = !ok, IsSaved = ok, SavedCount = s.SavedCount, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(); var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); if (all.IsError) { var failed = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref failed, all.Message); return failed; } return new OASISResult<IEnumerable<IHolon>> { Result = h, IsError = false, IsLoaded = true, LoadedCount = h.Count, Message = $"CouchbaseOASIS: {h.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
