using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

namespace NextGenSoftware.OASIS.API.Providers.TursoOASIS
{
    /// <summary>
    /// OASIS provider for Turso (libSQL) via the libSQL HTTP pipeline API.
    ///
    /// Turso is an edge-native SQLite-compatible database built on libSQL.
    /// Uses the pipeline HTTP API — no SDK required.
    ///
    /// Constructor parameters:
    ///   url      — e.g. "https://mydb-myorg.turso.io" (from Turso dashboard) or "http://localhost:8080" (sqld)
    ///   authToken — JWT auth token from `turso db tokens create mydb`
    ///
    /// Tables are auto-created on first ActivateProvider call.
    /// </summary>
    public class TursoOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly HttpClient _http;
        private readonly string _pipelineUrl;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public TursoOASIS(string url, string authToken)
        {
            _pipelineUrl = url.TrimEnd('/') + "/v2/pipeline";
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            ProviderName = "TursoOASIS";
            ProviderDescription = "Turso (libSQL) provider via libSQL HTTP pipeline API";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TursoOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ─── Pipeline execution ───────────────────────────────────────────────────

        private class PipelineRequest { [JsonPropertyName("requests")] public List<PipelineStmt> Requests { get; set; } = new(); }
        private class PipelineStmt
        {
            [JsonPropertyName("type")] public string Type { get; set; } = "execute";
            [JsonPropertyName("stmt")] public LibSqlStmt? Stmt { get; set; }
            [JsonPropertyName("sql")] public string? Sql { get; set; }
        }
        private class LibSqlStmt { [JsonPropertyName("sql")] public string Sql { get; set; } = ""; [JsonPropertyName("args")] public List<LibSqlArg>? Args { get; set; } }
        private class LibSqlArg { [JsonPropertyName("type")] public string Type { get; set; } = "text"; [JsonPropertyName("value")] public string? Value { get; set; } }
        private class PipelineResponse { [JsonPropertyName("results")] public List<PipelineResult>? Results { get; set; } }
        private class PipelineResult { [JsonPropertyName("type")] public string Type { get; set; } = ""; [JsonPropertyName("response")] public PipelineResultBody? Response { get; set; } }
        private class PipelineResultBody { [JsonPropertyName("type")] public string Type { get; set; } = ""; [JsonPropertyName("result")] public LibSqlResult? Result { get; set; } }
        private class LibSqlResult
        {
            [JsonPropertyName("cols")] public List<LibSqlCol>? Cols { get; set; }
            [JsonPropertyName("rows")] public List<List<LibSqlValue>>? Rows { get; set; }
            [JsonPropertyName("affected_row_count")] public ulong? AffectedRowCount { get; set; }
        }
        private class LibSqlCol { [JsonPropertyName("name")] public string Name { get; set; } = ""; }
        private class LibSqlValue { [JsonPropertyName("type")] public string Type { get; set; } = ""; [JsonPropertyName("value")] public string? Value { get; set; } }

        private static LibSqlArg Arg(string? value) => new LibSqlArg { Type = value == null ? "null" : "text", Value = value };
        private static LibSqlArg ArgInt(long? value) => new LibSqlArg { Type = value == null ? "null" : "integer", Value = value?.ToString() };

        private async Task<LibSqlResult> ExecuteSqlAsync(string sql, List<LibSqlArg>? args = null)
        {
            var response = await ExecutePipelineAsync(new PipelineStmt
            {
                Stmt = new LibSqlStmt { Sql = sql, Args = args }
            }).ConfigureAwait(false);
            return response.Result ?? throw new InvalidOperationException("Turso did not return the SQL statement result.");
        }

        private async Task<PipelineResultBody> ExecutePipelineAsync(PipelineStmt request)
        {
            var payload = new PipelineRequest
            {
                Requests = new List<PipelineStmt>
                {
                    request,
                    new PipelineStmt { Type = "close" }
                }
            };
            using var content = new StringContent(JsonSerializer.Serialize(payload, _jsonOpts), Encoding.UTF8, "application/json");
            using var response = await _http.PostAsync(_pipelineUrl, content).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var results = JsonSerializer.Deserialize<PipelineResponse>(json, _jsonOpts)?.Results;
            if (results?.Count != 2)
                throw new InvalidOperationException("Turso returned an incomplete SQL pipeline result.");
            if (results[0].Type == "error")
                throw new InvalidOperationException("Turso rejected the SQL statement.");
            if (results[0] is not { Type: "ok", Response: { } statement } || statement.Type != request.Type)
                throw new InvalidOperationException("Turso did not acknowledge SQL execution.");
            if (results[1] is not { Type: "ok", Response.Type: "close" })
                throw new InvalidOperationException("Turso did not acknowledge closure of the SQL stream.");
            return statement;
        }

        private async Task WriteOneAsync(string sql, List<LibSqlArg> args)
        {
            var write = await ExecuteSqlAsync(sql, args).ConfigureAwait(false);
            if (write.AffectedRowCount != 1)
                throw new InvalidOperationException("Turso did not acknowledge exactly one saved record.");
        }

        private List<Dictionary<string, string?>> ResultToRows(LibSqlResult? result)
        {
            if (result?.Cols == null || result.Rows == null) return new List<Dictionary<string, string?>>();
            var cols = result.Cols.Select(c => c.Name).ToList();
            return result.Rows.Select(row => cols.Zip(row, (col, val) => (col, val.Value)).ToDictionary(x => x.col, x => x.Value)).ToList();
        }

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private static OASISResult<T> MissingRecord<T>(string message) => new()
        {
            IsError = true, ErrorCount = 1, ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = message
        };

        // ─── Schema bootstrap ─────────────────────────────────────────────────────

        private async Task EnsureTablesAsync()
        {
            await ExecutePipelineAsync(new PipelineStmt { Type = "sequence", Sql = """
                BEGIN;
                CREATE TABLE IF NOT EXISTS oasis_avatars (
                    id TEXT PRIMARY KEY, username TEXT, email TEXT, is_deleted INTEGER DEFAULT 0, data_json TEXT);
                CREATE TABLE IF NOT EXISTS oasis_avatar_details (
                    id TEXT PRIMARY KEY, username TEXT, email TEXT, data_json TEXT);
                CREATE TABLE IF NOT EXISTS oasis_holons (
                    id TEXT PRIMARY KEY, parent_holon_id TEXT, holon_type INTEGER DEFAULT 0, is_deleted INTEGER DEFAULT 0, data_json TEXT);
                CREATE INDEX IF NOT EXISTS idx_avatars_username ON oasis_avatars(username);
                CREATE INDEX IF NOT EXISTS idx_avatars_email ON oasis_avatars(email);
                CREATE INDEX IF NOT EXISTS idx_holons_parent ON oasis_holons(parent_holon_id);
                CREATE INDEX IF NOT EXISTS idx_holons_type ON oasis_holons(holon_type);
                CREATE INDEX IF NOT EXISTS idx_holons_owner
                    ON oasis_holons(json_extract(data_json,'$.createdByAvatarId') COLLATE NOCASE)
                    WHERE is_deleted=0;
                CREATE TRIGGER IF NOT EXISTS oasis_avatars_delete_detail
                    AFTER DELETE ON oasis_avatars
                    BEGIN
                        DELETE FROM oasis_avatar_details WHERE id=OLD.id;
                    END;
                COMMIT;
                """ }).ConfigureAwait(false);
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureTablesAsync();
                IsProviderActivated = true;
                result.Result = true; result.IsError = false;
                result.Message = "TursoOASIS activated — tables ready in Turso (libSQL).";
            }
            catch (Exception ex) { IsProviderActivated = false; result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error activating provider — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool> { Result = true, Message = "TursoOASIS deactivated." });
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
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.TursoOASIS] = avatar.Id.ToString();
                await WriteOneAsync(
                    "INSERT INTO oasis_avatars(id,username,email,is_deleted,data_json) VALUES(?,?,?,?,?) ON CONFLICT(id) DO UPDATE SET username=excluded.username,email=excluded.email,is_deleted=excluded.is_deleted,data_json=excluded.data_json",
                    new List<LibSqlArg> { Arg(avatar.Id.ToString()), Arg(avatar.Username), Arg(avatar.Email), ArgInt(avatar.IsDeleted ? 1 : 0), Arg(Ser(avatar)) });
                result.Result = avatar;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.Message = $"TursoOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private async Task<Avatar?> QueryAvatarAsync(string sql, List<LibSqlArg> args)
        {
            var res = await ExecuteSqlAsync(sql, args);
            var rows = ResultToRows(res);
            if (rows.Count == 0) return null;
            return Des<Avatar>(rows[0].GetValueOrDefault("data_json"));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE id=? AND is_deleted=0 LIMIT 1", new List<LibSqlArg> { Arg(id.ToString()) });
                if (avatar == null)
                    return MissingRecord<IAvatar>($"TursoOASIS: No avatar found for ID '{id}'.");
                result.Result = avatar;
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.Message = $"TursoOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error loading avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE username=? AND is_deleted=0 LIMIT 1", new List<LibSqlArg> { Arg(username) });
                if (avatar == null)
                    return MissingRecord<IAvatar>($"TursoOASIS: No avatar found for username '{username}'.");
                result.Result = avatar;
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.Message = $"TursoOASIS: Avatar loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error loading avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE email=? AND is_deleted=0 LIMIT 1", new List<LibSqlArg> { Arg(avatarEmail) });
                if (avatar == null)
                    return MissingRecord<IAvatar>($"TursoOASIS: No avatar found for email '{avatarEmail}'.");
                result.Result = avatar;
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.Message = $"TursoOASIS: Avatar loaded for email '{avatarEmail}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error loading avatar by email '{avatarEmail}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"TursoOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var res = await ExecuteSqlAsync("SELECT data_json FROM oasis_avatars WHERE is_deleted=0");
                var avatars = ResultToRows(res).Select(r => Des<Avatar>(r.GetValueOrDefault("data_json"))).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"TursoOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var deleted = await ExecuteSqlAsync(
                    softDelete ? "UPDATE oasis_avatars SET is_deleted=1 WHERE id=? AND is_deleted=0"
                        : "DELETE FROM oasis_avatars WHERE id=?",
                    new List<LibSqlArg> { Arg(id.ToString()) }).ConfigureAwait(false);
                if (deleted.AffectedRowCount == 0)
                    return new OASISResult<bool>
                    {
                        IsError = true, ErrorCount = 1, ErrorCode = OASISResultErrorCodes.RecordNotFound,
                        Message = "Turso found no matching avatar to delete."
                    };
                if (deleted.AffectedRowCount != 1)
                    throw new InvalidOperationException("Turso did not acknowledge exactly one avatar deletion.");
                result.Result = true;
                result.IsDeleted = true;
                result.DeletedCount = 1;
                result.Message = $"TursoOASIS: Avatar '{id}' {(softDelete ? "soft" : "hard")}-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error deleting avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var a = await LoadAvatarByUsernameAsync(username);
            if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"TursoOASIS: Avatar '{username}' not found."); return r; }
            return await DeleteAvatarAsync(a.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var a = await LoadAvatarByEmailAsync(email);
            if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"TursoOASIS: Avatar with email '{email}' not found."); return r; }
            return await DeleteAvatarAsync(a.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) => DeleteAvatarByEmailAsync(email, softDelete).Result;

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
            => GuidProviderKeyDeletion.DeleteAvatarAsync(providerKey, softDelete, DeleteAvatarAsync);

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (avatarDetail.Id == Guid.Empty) avatarDetail.Id = Guid.NewGuid();
                await WriteOneAsync(
                    "INSERT INTO oasis_avatar_details(id,username,email,data_json) VALUES(?,?,?,?) ON CONFLICT(id) DO UPDATE SET username=excluded.username,email=excluded.email,data_json=excluded.data_json",
                    new List<LibSqlArg> { Arg(avatarDetail.Id.ToString()), Arg(avatarDetail.Username), Arg(avatarDetail.Email), Arg(StoredRecordJson.Serialize(avatarDetail)) });
                result.Result = avatarDetail;
                result.IsSaved = true;
                result.SavedCount = 1;
                result.Message = "TursoOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var res = await ExecuteSqlAsync("SELECT data_json FROM oasis_avatar_details WHERE id=? LIMIT 1", new List<LibSqlArg> { Arg(id.ToString()) });
                var rows = ResultToRows(res);
                if (rows.Count == 0)
                    return MissingRecord<IAvatarDetail>($"TursoOASIS: No avatar detail found for ID '{id}'.");
                result.Result = StoredRecordJson.DeserializeStored<AvatarDetail>(rows[0].GetValueOrDefault("data_json")!)
                    ?? throw new InvalidOperationException("Turso returned an empty avatar detail payload.");
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.Message = $"TursoOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error loading avatar detail '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var res = await ExecuteSqlAsync("SELECT data_json FROM oasis_avatar_details WHERE username=? LIMIT 1", new List<LibSqlArg> { Arg(username) });
                var rows = ResultToRows(res);
                if (rows.Count == 0)
                    return MissingRecord<IAvatarDetail>($"TursoOASIS: No avatar detail found for username '{username}'.");
                result.Result = StoredRecordJson.DeserializeStored<AvatarDetail>(rows[0].GetValueOrDefault("data_json")!)
                    ?? throw new InvalidOperationException("Turso returned an empty avatar detail payload.");
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.Message = $"TursoOASIS: AvatarDetail loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error loading avatar detail by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var res = await ExecuteSqlAsync("SELECT data_json FROM oasis_avatar_details WHERE email=? LIMIT 1", new List<LibSqlArg> { Arg(email) });
                var rows = ResultToRows(res);
                if (rows.Count == 0)
                    return MissingRecord<IAvatarDetail>($"TursoOASIS: No avatar detail found for email '{email}'.");
                result.Result = StoredRecordJson.DeserializeStored<AvatarDetail>(rows[0].GetValueOrDefault("data_json")!)
                    ?? throw new InvalidOperationException("Turso returned an empty avatar detail payload.");
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.Message = $"TursoOASIS: AvatarDetail loaded for email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error loading avatar detail by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var res = await ExecuteSqlAsync("SELECT data_json FROM oasis_avatar_details");
                var details = ResultToRows(res).Select(row =>
                    (IAvatarDetail)(StoredRecordJson.DeserializeStored<AvatarDetail>(row.GetValueOrDefault("data_json")!)
                        ?? throw new InvalidOperationException("Turso returned an empty avatar detail payload."))).ToList();
                result.Result = details;
                result.IsLoaded = true;
                result.LoadedCount = details.Count;
                result.Message = $"TursoOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error loading all avatar details: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var saved = await SaveHolonsAsync(new[] { holon }, saveChildren, recursive, maxChildDepth,
                continueOnError: continueOnError, saveChildrenOnProvider: saveChildrenOnProvider).ConfigureAwait(false);
            return new OASISResult<IHolon>
            {
                Result = saved.Result?.SingleOrDefault(), IsSaved = saved.IsSaved, SavedCount = saved.SavedCount,
                IsError = saved.IsError, ErrorCount = saved.ErrorCount, ErrorCode = saved.ErrorCode,
                Exception = saved.Exception, Message = saved.Message
            };
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var plan = HolonGraphWritePlan.CreateForSave(holons, saveChildren, recursive, maxChildDepth, curentChildDepth);
                var rows = plan.Nodes.Select(node =>
                {
                    var snapshot = node.Snapshot;
                    snapshot.ProviderUniqueStorageKey ??= new Dictionary<ProviderType, string>();
                    snapshot.ProviderUniqueStorageKey[Core.Enums.ProviderType.TursoOASIS] = snapshot.Id.ToString();
                    return new
                    {
                        id = snapshot.Id,
                        parent = snapshot.ParentHolonId == Guid.Empty ? (Guid?)null : snapshot.ParentHolonId,
                        type = (int)snapshot.HolonType,
                        deleted = snapshot.IsDeleted ? 1 : 0,
                        payload = Ser(snapshot)
                    };
                }).ToArray();

                if (rows.Length > 0)
                {
                    var saved = await ExecuteSqlAsync("""
                        INSERT INTO oasis_holons(id,parent_holon_id,holon_type,is_deleted,data_json)
                        SELECT json_extract(value,'$.id'), json_extract(value,'$.parent'),
                            json_extract(value,'$.type'), json_extract(value,'$.deleted'), json_extract(value,'$.payload')
                        FROM json_each(?) WHERE 1
                        ON CONFLICT(id) DO UPDATE SET parent_holon_id=excluded.parent_holon_id,
                            holon_type=excluded.holon_type,is_deleted=excluded.is_deleted,data_json=excluded.data_json
                        """, new List<LibSqlArg> { Arg(Ser(rows)) }).ConfigureAwait(false);
                    if (saved.AffectedRowCount != (ulong)rows.Length)
                        throw new InvalidOperationException("Turso did not acknowledge every planned holon write.");
                }

                foreach (var node in plan.Nodes)
                {
                    node.Source.Id = node.Snapshot.Id;
                    node.Source.ParentHolonId = node.Snapshot.ParentHolonId;
                    node.Source.ProviderUniqueStorageKey = node.Snapshot.ProviderUniqueStorageKey;
                }
                result.Result = plan.Roots;
                result.IsSaved = true;
                result.SavedCount = rows.Length;
                result.Message = $"TursoOASIS: {rows.Length} holon(s) saved.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error saving holon graph: {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private async Task<List<IHolon>> QueryHolonsAsync(string sql, List<LibSqlArg>? args = null)
        {
            var res = await ExecuteSqlAsync(sql, args);
            return ResultToRows(res).Select(r => Des<Holon>(r.GetValueOrDefault("data_json"))).Where(h => h != null).Cast<IHolon>().ToList();
        }

        private async Task<IHolon?> LoadHolonTreeAsync(Guid id, int maxDepth)
        {
            var holons = await QueryHolonsAsync("""
                WITH RECURSIVE graph(id, data_json, depth, visited) AS (
                    SELECT id, data_json, 0, ',' || id || ','
                    FROM oasis_holons WHERE id=? AND is_deleted=0
                    UNION ALL
                    SELECT child.id, child.data_json, graph.depth+1, graph.visited || child.id || ','
                    FROM graph JOIN oasis_holons child ON child.parent_holon_id=graph.id
                    WHERE child.is_deleted=0 AND (?=0 OR graph.depth<?)
                        AND instr(graph.visited, ',' || child.id || ',')=0
                )
                SELECT data_json FROM graph ORDER BY depth
                """, new List<LibSqlArg> { Arg(id.ToString()), ArgInt(maxDepth), ArgInt(maxDepth) }).ConfigureAwait(false);
            if (holons.Count == 0)
                return null;
            AttachDescendants(holons, holons.Skip(1));
            return holons[0];
        }

        private static void AttachDescendants(List<IHolon> holons, IEnumerable<IHolon> descendants)
        {
            var byId = holons.ToDictionary(holon => holon.Id);
            foreach (var holon in holons)
                holon.Children = new List<IHolon>();
            foreach (var child in descendants)
                byId[child.ParentHolonId].Children.Add(child);
        }

        private async Task<List<IHolon>> LoadParentTreesAsync(Guid parentId, HolonType type, int maxDepth, int currentDepth)
        {
            var holons = await QueryHolonsAsync("""
                WITH RECURSIVE graph(id, data_json, depth, visited) AS (
                    SELECT id, data_json, ?+1, ',' || parent_holon_id || ',' || id || ','
                    FROM oasis_holons WHERE parent_holon_id=? AND is_deleted=0 AND id<>parent_holon_id
                        AND (? IS NULL OR holon_type=?)
                    UNION ALL
                    SELECT child.id, child.data_json, graph.depth+1, graph.visited || child.id || ','
                    FROM graph JOIN oasis_holons child ON child.parent_holon_id=graph.id
                    WHERE child.is_deleted=0 AND (?=0 OR graph.depth<?)
                        AND instr(graph.visited, ',' || child.id || ',')=0
                )
                SELECT data_json FROM graph ORDER BY depth
                """, new List<LibSqlArg>
                {
                    ArgInt(currentDepth), Arg(parentId.ToString()), ArgInt(type == HolonType.All ? null : (int)type),
                    ArgInt((int)type), ArgInt(maxDepth), ArgInt(maxDepth)
                }).ConfigureAwait(false);
            var roots = holons.Where(holon => holon.ParentHolonId == parentId).ToList();
            AttachDescendants(holons, holons.Where(holon => holon.ParentHolonId != parentId));
            return roots;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holon = loadChildren
                    ? await LoadHolonTreeAsync(id, recursive ? maxChildDepth : 1).ConfigureAwait(false)
                    : (await QueryHolonsAsync("SELECT data_json FROM oasis_holons WHERE id=? AND is_deleted=0 LIMIT 1",
                        new List<LibSqlArg> { Arg(id.ToString()) }).ConfigureAwait(false)).SingleOrDefault();
                if (holon == null)
                    return MissingRecord<IHolon>($"TursoOASIS: No holon found for ID '{id}'.");
                result.Result = holon;
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.Message = $"TursoOASIS: Holon loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error loading holon '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"TursoOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = type == HolonType.All ? "SELECT data_json FROM oasis_holons WHERE is_deleted=0" : "SELECT data_json FROM oasis_holons WHERE is_deleted=0 AND holon_type=?";
                var args = type == HolonType.All ? null : new List<LibSqlArg> { ArgInt((int)type) };
                var holons = await QueryHolonsAsync(sql, args);
                result.Result = holons;
                result.IsLoaded = true;
                result.LoadedCount = holons.Count;
                result.Message = $"TursoOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error loading all holons: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = type == HolonType.All ? "SELECT data_json FROM oasis_holons WHERE parent_holon_id=? AND is_deleted=0" : "SELECT data_json FROM oasis_holons WHERE parent_holon_id=? AND is_deleted=0 AND holon_type=?";
                var args = type == HolonType.All ? new List<LibSqlArg> { Arg(id.ToString()) } : new List<LibSqlArg> { Arg(id.ToString()), ArgInt((int)type) };
                var holons = loadChildren && recursive
                    ? await LoadParentTreesAsync(id, type, maxChildDepth, curentChildDepth).ConfigureAwait(false)
                    : await QueryHolonsAsync(sql, args).ConfigureAwait(false);
                result.Result = holons;
                result.IsLoaded = true;
                result.LoadedCount = holons.Count;
                result.Message = $"TursoOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error loading holons for parent '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"TursoOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var deleted = await ExecuteSqlAsync(
                    "UPDATE oasis_holons SET is_deleted=1 WHERE id=? AND is_deleted=0 RETURNING data_json",
                    new List<LibSqlArg> { Arg(id.ToString()) }).ConfigureAwait(false);
                if (deleted.AffectedRowCount == 0)
                    return new OASISResult<IHolon>
                    {
                        IsError = true, ErrorCount = 1, ErrorCode = OASISResultErrorCodes.RecordNotFound,
                        Message = "Turso found no matching live holon to delete."
                    };

                var rows = ResultToRows(deleted);
                if (deleted.AffectedRowCount != 1 || rows.Count != 1)
                    throw new InvalidOperationException("Turso did not acknowledge exactly one holon deletion.");

                result.Result = Des<Holon>(rows[0].GetValueOrDefault("data_json"))
                    ?? throw new InvalidOperationException("Turso did not return the deleted holon.");
                result.IsDeleted = true;
                result.DeletedCount = 1;
                result.Message = $"TursoOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonAsync(id);
            var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"TursoOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                StoredRecordSearch.Validate(searchParams, version, maxChildDepth);
                searchParams = StoredRecordJson.Project<SearchParams>(searchParams);
                StoredRecordSearch.Validate(searchParams, version, maxChildDepth);
                var groups = searchParams.SearchGroups ?? new List<ISearchGroupBase>();
                var searchAvatars = groups.Count == 0 || groups.Any(group => group.SearchAvatars || !group.SearchHolons);
                var searchHolons = groups.Count == 0 || groups.Any(group => group.SearchHolons || !group.SearchAvatars);
                var avatars = new List<IAvatar>();
                var holons = new List<IHolon>();
                if (searchAvatars)
                {
                    var all = await LoadAllAvatarsAsync(version).ConfigureAwait(false);
                    if (all.IsError)
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IAvatar>, ISearchResults>(all);
                    avatars = all.Result.ToList();
                }
                if (searchHolons || searchParams.ParentId != Guid.Empty)
                {
                    var all = await LoadAllHolonsAsync(loadChildren: loadChildren, recursive: recursive,
                        maxChildDepth: maxChildDepth, continueOnError: continueOnError, version: version).ConfigureAwait(false);
                    if (all.IsError)
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>, ISearchResults>(all);
                    holons = all.Result.ToList();
                }

                var matches = new SearchResults();
                matches.SearchResultAvatars.AddRange(avatars.Where(avatar => StoredRecordSearch.Matches(avatar, true, searchParams, holons)));
                if (searchHolons)
                    matches.SearchResultHolons.AddRange(holons.Where(holon => StoredRecordSearch.Matches(holon, false, searchParams, holons)));
                if (searchParams.SearchOnlyForCurrentAvatar)
                {
                    var scoped = new OASISResult<IEnumerable<IHolon>> { Result = matches.SearchResultHolons, IsLoaded = true };
                    var visible = IOASISStorageProvider.ProjectVisibleHolons(scoped, searchParams.AvatarId, false);
                    if (visible.IsError)
                        return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>, ISearchResults>(visible);
                    matches.SearchResultHolons = visible.Result.ToList();
                }

                matches.NumberOfResults = matches.SearchResultAvatars.Count + matches.SearchResultHolons.Count;
                result.Result = matches;
                result.IsLoaded = true;
                result.LoadedCount = matches.NumberOfResults;
                result.Message = $"TursoOASIS: Found {matches.NumberOfResults} record(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Metadata ─────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var all = await LoadAllHolonsAsync(type);
            if (all.IsError)
                return all;
            var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>();
            return new OASISResult<IEnumerable<IHolon>>
            {
                Result = holons, IsLoaded = true, LoadedCount = holons.Count,
                Message = $"TursoOASIS: Loaded {holons.Count} holon(s) where {metaKey}={metaValue}."
            };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var all = await LoadAllHolonsAsync(type);
            if (all.IsError)
                return all;
            bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKeyValuePairs.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); }
            var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>();
            return new OASISResult<IEnumerable<IHolon>>
            {
                Result = holons, IsLoaded = true, LoadedCount = holons.Count,
                Message = $"TursoOASIS: Loaded {holons.Count} holon(s) matching metadata filter."
            };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var saved = await SaveHolonsAsync(holons).ConfigureAwait(false);
            return new OASISResult<bool>
            {
                Result = saved.IsSaved, IsSaved = saved.IsSaved, SavedCount = saved.SavedCount,
                IsError = saved.IsError, ErrorCount = saved.ErrorCount, ErrorCode = saved.ErrorCode,
                Exception = saved.Exception, Message = saved.Message
            };
        }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = await QueryHolonsAsync("""
                    SELECT data_json FROM oasis_holons
                    WHERE is_deleted=0 AND json_extract(data_json,'$.createdByAvatarId') COLLATE NOCASE = ?
                    """, new List<LibSqlArg> { Arg(avatarId.ToString()) }).ConfigureAwait(false);
                result.Result = holons;
                result.IsLoaded = true;
                result.LoadedCount = holons.Count;
                result.Message = $"TursoOASIS: Exported {holons.Count} holon(s) for avatar '{avatarId}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"TursoOASIS: Error exporting holons for avatar '{avatarId}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"TursoOASIS: Avatar '{u}' not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"TursoOASIS: Avatar with email '{e}' not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
