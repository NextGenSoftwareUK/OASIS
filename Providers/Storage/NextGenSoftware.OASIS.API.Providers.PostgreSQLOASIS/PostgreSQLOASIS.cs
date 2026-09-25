using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Npgsql;
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

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS
{
    /// <summary>
    /// OASIS provider for PostgreSQL (and compatible managed services: Supabase, Neon, AWS RDS, Azure).
    ///
    /// Uses Npgsql ADO.NET with three tables:
    ///   oasis_avatars        — id, username, email, is_deleted, data_json (TEXT)
    ///   oasis_avatar_details — id, username, email, data_json (TEXT)
    ///   oasis_holons         — id, parent_holon_id, holon_type, is_deleted, data_json (TEXT)
    ///
    /// Upserts use INSERT ... ON CONFLICT DO UPDATE (standard Postgres idiom).
    /// Indexed columns (id, username, email, parent_holon_id) serve fast key lookups.
    /// </summary>
    public class PostgreSQLOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _connectionString;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private const string CreateAvatarsTable = @"
CREATE TABLE IF NOT EXISTS oasis_avatars (
    id              UUID         NOT NULL PRIMARY KEY,
    username        TEXT         NOT NULL DEFAULT '',
    email           TEXT         NOT NULL DEFAULT '',
    is_deleted      BOOLEAN      NOT NULL DEFAULT FALSE,
    created_date    TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    modified_date   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    data_json       TEXT         NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_oasis_avatars_username ON oasis_avatars(username);
CREATE INDEX IF NOT EXISTS ix_oasis_avatars_email    ON oasis_avatars(email);";

        private const string CreateAvatarDetailsTable = @"
CREATE TABLE IF NOT EXISTS oasis_avatar_details (
    id          UUID NOT NULL PRIMARY KEY,
    username    TEXT NOT NULL DEFAULT '',
    email       TEXT NOT NULL DEFAULT '',
    data_json   TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_oasis_avatar_details_username ON oasis_avatar_details(username);
CREATE INDEX IF NOT EXISTS ix_oasis_avatar_details_email    ON oasis_avatar_details(email);";

        private const string CreateHolonsTable = @"
CREATE TABLE IF NOT EXISTS oasis_holons (
    id               UUID        NOT NULL PRIMARY KEY,
    parent_holon_id  UUID        NULL,
    holon_type       INT         NOT NULL DEFAULT 0,
    is_deleted       BOOLEAN     NOT NULL DEFAULT FALSE,
    created_date     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modified_date    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    data_json        TEXT        NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_oasis_holons_parent ON oasis_holons(parent_holon_id);
CREATE INDEX IF NOT EXISTS ix_oasis_holons_type   ON oasis_holons(holon_type);";

        public PostgreSQLOASIS(string connectionString)
        {
            _connectionString = connectionString;
            ProviderName = "PostgreSQLOASIS";
            ProviderDescription = "PostgreSQL provider (Npgsql ADO.NET, JSON blob storage per row)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PostgreSQLOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                foreach (string ddl in new[] { CreateAvatarsTable, CreateAvatarDetailsTable, CreateHolonsTable })
                {
                    await using var cmd = new NpgsqlCommand(ddl, conn);
                    await cmd.ExecuteNonQueryAsync();
                }
                result.Result = true;
                result.IsError = false;
                result.Message = "PostgreSQLOASIS activated — tables created/verified.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error activating provider — {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
            => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "PostgreSQLOASIS deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static string Serialize(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null)
                    avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.PostgreSQLOASIS] = avatar.Id.ToString();

                const string sql = @"
INSERT INTO oasis_avatars (id, username, email, is_deleted, data_json)
VALUES (@id, @username, @email, @is_deleted, @data_json)
ON CONFLICT (id) DO UPDATE SET
    username = EXCLUDED.username,
    email = EXCLUDED.email,
    is_deleted = EXCLUDED.is_deleted,
    modified_date = NOW(),
    data_json = EXCLUDED.data_json";

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", avatar.Id);
                cmd.Parameters.AddWithValue("@username", avatar.Username ?? "");
                cmd.Parameters.AddWithValue("@email", avatar.Email ?? "");
                cmd.Parameters.AddWithValue("@is_deleted", avatar.IsDeleted);
                cmd.Parameters.AddWithValue("@data_json", Serialize(avatar));
                await cmd.ExecuteNonQueryAsync();

                result.Result = avatar;
                result.IsError = false;
                result.Message = $"PostgreSQLOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error saving avatar '{avatar.Username}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private async Task<Avatar?> LoadAvatarByColumnAsync(string column, object value)
        {
            string sql = $"SELECT data_json FROM oasis_avatars WHERE {column}=@val AND is_deleted=FALSE LIMIT 1";
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@val", value);
            var scalar = await cmd.ExecuteScalarAsync();
            if (scalar == null) return null;
            return Deserialize<Avatar>(scalar.ToString()!);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await LoadAvatarByColumnAsync("id", id);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: No avatar found with ID '{id}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"PostgreSQLOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading avatar by ID '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await LoadAvatarByColumnAsync("username", username);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: No avatar found with username '{username}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"PostgreSQLOASIS: Avatar loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await LoadAvatarByColumnAsync("email", avatarEmail);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: No avatar found with email '{avatarEmail}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"PostgreSQLOASIS: Avatar loaded for email '{avatarEmail}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading avatar by email '{avatarEmail}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var avatars = new List<IAvatar>();
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_avatars WHERE is_deleted=FALSE", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var a = Deserialize<Avatar>(reader.GetString(0));
                    if (a != null) avatars.Add(a);
                }
                result.Result = avatars; result.IsError = false; result.Message = $"PostgreSQLOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                string sql = softDelete
                    ? "UPDATE oasis_avatars SET is_deleted=TRUE, modified_date=NOW() WHERE id=@id"
                    : "DELETE FROM oasis_avatars WHERE id=@id";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                result.Result = rows > 0; result.IsError = !result.Result;
                result.Message = result.Result
                    ? $"PostgreSQLOASIS: Avatar '{id}' {(softDelete ? "soft" : "hard")}-deleted."
                    : $"PostgreSQLOASIS: No avatar found with ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error deleting avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                string sql = softDelete
                    ? "UPDATE oasis_avatars SET is_deleted=TRUE, modified_date=NOW() WHERE username=@username"
                    : "DELETE FROM oasis_avatars WHERE username=@username";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@username", username);
                int rows = await cmd.ExecuteNonQueryAsync();
                result.Result = rows > 0; result.IsError = !result.Result;
                result.Message = result.Result
                    ? $"PostgreSQLOASIS: Avatar '{username}' {(softDelete ? "soft" : "hard")}-deleted."
                    : $"PostgreSQLOASIS: No avatar found with username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error deleting avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                string sql = softDelete
                    ? "UPDATE oasis_avatars SET is_deleted=TRUE, modified_date=NOW() WHERE email=@email"
                    : "DELETE FROM oasis_avatars WHERE email=@email";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@email", email);
                int rows = await cmd.ExecuteNonQueryAsync();
                result.Result = rows > 0; result.IsError = !result.Result;
                result.Message = result.Result
                    ? $"PostgreSQLOASIS: Avatar with email '{email}' {(softDelete ? "soft" : "hard")}-deleted."
                    : $"PostgreSQLOASIS: No avatar found with email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error deleting avatar by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) => DeleteAvatarByEmailAsync(email, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteAvatarAsync(id, softDelete);
            return await DeleteAvatarByUsernameAsync(providerKey, softDelete);
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (avatarDetail.Id == Guid.Empty) avatarDetail.Id = Guid.NewGuid();
                const string sql = @"
INSERT INTO oasis_avatar_details (id, username, email, data_json)
VALUES (@id, @username, @email, @data_json)
ON CONFLICT (id) DO UPDATE SET
    username = EXCLUDED.username,
    email = EXCLUDED.email,
    data_json = EXCLUDED.data_json";
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", avatarDetail.Id);
                cmd.Parameters.AddWithValue("@username", avatarDetail.Username ?? "");
                cmd.Parameters.AddWithValue("@email", avatarDetail.Email ?? "");
                cmd.Parameters.AddWithValue("@data_json", Serialize(avatarDetail));
                await cmd.ExecuteNonQueryAsync();
                result.Result = avatarDetail; result.IsError = false; result.Message = "PostgreSQLOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_avatar_details WHERE id=@id LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@id", id);
                var scalar = await cmd.ExecuteScalarAsync();
                if (scalar == null) { OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: No avatar detail found for ID '{id}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(scalar.ToString()!); result.IsError = false;
                result.Message = $"PostgreSQLOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading avatar detail for '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_avatar_details WHERE username=@username LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@username", username);
                var scalar = await cmd.ExecuteScalarAsync();
                if (scalar == null) { OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: No avatar detail found for username '{username}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(scalar.ToString()!); result.IsError = false;
                result.Message = $"PostgreSQLOASIS: AvatarDetail loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading avatar detail by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_avatar_details WHERE email=@email LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@email", email);
                var scalar = await cmd.ExecuteScalarAsync();
                if (scalar == null) { OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: No avatar detail found for email '{email}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(scalar.ToString()!); result.IsError = false;
                result.Message = $"PostgreSQLOASIS: AvatarDetail loaded for email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading avatar detail by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var details = new List<IAvatarDetail>();
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_avatar_details", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var d = Deserialize<AvatarDetail>(reader.GetString(0));
                    if (d != null) details.Add(d);
                }
                result.Result = details; result.IsError = false; result.Message = $"PostgreSQLOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading all avatar details: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                if (holon.ProviderUniqueStorageKey == null)
                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.PostgreSQLOASIS] = holon.Id.ToString();

                const string sql = @"
INSERT INTO oasis_holons (id, parent_holon_id, holon_type, is_deleted, data_json)
VALUES (@id, @parent_holon_id, @holon_type, @is_deleted, @data_json)
ON CONFLICT (id) DO UPDATE SET
    parent_holon_id = EXCLUDED.parent_holon_id,
    holon_type = EXCLUDED.holon_type,
    is_deleted = EXCLUDED.is_deleted,
    modified_date = NOW(),
    data_json = EXCLUDED.data_json";

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", holon.Id);
                cmd.Parameters.AddWithValue("@parent_holon_id", holon.ParentHolonId == Guid.Empty ? (object)DBNull.Value : holon.ParentHolonId);
                cmd.Parameters.AddWithValue("@holon_type", (int)holon.HolonType);
                cmd.Parameters.AddWithValue("@is_deleted", holon.IsDeleted);
                cmd.Parameters.AddWithValue("@data_json", Serialize(holon));
                await cmd.ExecuteNonQueryAsync();

                result.Result = holon; result.IsError = false; result.Message = $"PostgreSQLOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error saving holon '{holon.Name}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var holon in holons)
            {
                var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (r.IsError) errors.Add(r.Message ?? r.Exception?.Message ?? "Unknown error");
                else if (r.Result != null) saved.Add(r.Result);
            }
            result.Result = saved; result.IsError = errors.Count > 0;
            result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"PostgreSQLOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private async Task<Holon?> LoadHolonByIdAsync(Guid id)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_holons WHERE id=@id AND is_deleted=FALSE LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@id", id);
            var scalar = await cmd.ExecuteScalarAsync();
            if (scalar == null) return null;
            return Deserialize<Holon>(scalar.ToString()!);
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holon = await LoadHolonByIdAsync(id);
                if (holon == null) { OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: No holon found with ID '{id}'."); return result; }
                result.Result = holon; result.IsError = false; result.Message = $"PostgreSQLOASIS: Holon loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading holon '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = type == HolonType.All
                    ? "SELECT data_json FROM oasis_holons WHERE is_deleted=FALSE"
                    : "SELECT data_json FROM oasis_holons WHERE is_deleted=FALSE AND holon_type=@holon_type";
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@holon_type", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsError = false; result.Message = $"PostgreSQLOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading all holons: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = type == HolonType.All
                    ? "SELECT data_json FROM oasis_holons WHERE parent_holon_id=@pid AND is_deleted=FALSE"
                    : "SELECT data_json FROM oasis_holons WHERE parent_holon_id=@pid AND is_deleted=FALSE AND holon_type=@holon_type";
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@pid", id);
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@holon_type", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsError = false; result.Message = $"PostgreSQLOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading holons for parent '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonByIdAsync(id);
                if (loaded == null) { OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: No holon found with ID '{id}'."); return result; }
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("UPDATE oasis_holons SET is_deleted=TRUE, modified_date=NOW() WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                result.Result = loaded; result.IsError = false; result.Message = $"PostgreSQLOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonAsync(id);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                var holons = new List<IHolon>();
                string? query = searchParams.SearchGroups?
                    .OfType<SearchTextGroup>()
                    .FirstOrDefault()?.SearchQuery;
                string sql = string.IsNullOrEmpty(query)
                    ? "SELECT data_json FROM oasis_holons WHERE is_deleted=FALSE"
                    : "SELECT data_json FROM oasis_holons WHERE is_deleted=FALSE AND (data_json::jsonb->>'name' ILIKE @q OR data_json::jsonb->>'description' ILIKE @q)";
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(query)) cmd.Parameters.AddWithValue("@q", $"%{query}%");
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                var sr = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count };
                result.Result = sr; result.IsError = false; result.Message = $"PostgreSQLOASIS: Found {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Metadata queries ─────────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = $"SELECT data_json FROM oasis_holons WHERE is_deleted=FALSE AND data_json::jsonb->'metaData'->>@key = @val";
                if (type != HolonType.All) sql += " AND holon_type=@holon_type";
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@key", metaKey);
                cmd.Parameters.AddWithValue("@val", metaValue);
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@holon_type", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsError = false;
                result.Message = $"PostgreSQLOASIS: Loaded {holons.Count} holon(s) where {metaKey}={metaValue}.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0) { OASISErrorHandling.HandleError(ref result, "PostgreSQLOASIS: No metadata filters provided."); return result; }
            try
            {
                var conditions = metaKeyValuePairs.Select((kvp, i) => $"data_json::jsonb->'metaData'->>@k{i} = @v{i}").ToList();
                string join = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? " OR " : " AND ";
                string sql = $"SELECT data_json FROM oasis_holons WHERE is_deleted=FALSE AND ({string.Join(join, conditions)})";
                if (type != HolonType.All) sql += " AND holon_type=@holon_type";
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                int idx = 0;
                foreach (var kvp in metaKeyValuePairs)
                {
                    cmd.Parameters.AddWithValue($"@k{idx}", kvp.Key);
                    cmd.Parameters.AddWithValue($"@v{idx}", kvp.Value);
                    idx++;
                }
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@holon_type", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsError = false;
                result.Message = $"PostgreSQLOASIS: Loaded {holons.Count} holon(s) matching metadata filter.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var saved = await SaveHolonsAsync(holons);
            return new OASISResult<bool> { Result = !saved.IsError, IsError = saved.IsError, Message = saved.Message };
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync();

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                const string sql = "SELECT data_json FROM oasis_holons WHERE is_deleted=FALSE AND data_json::jsonb->>'avatarId'=@aid";
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@aid", avatarId.ToString());
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsError = false; result.Message = $"PostgreSQLOASIS: Exported {holons.Count} holon(s) for avatar '{avatarId}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var r = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref r, $"PostgreSQLOASIS: Avatar '{avatarUsername}' not found.");
                return r;
            }
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var r = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref r, $"PostgreSQLOASIS: Avatar with email '{avatarEmailAddress}' not found.");
                return r;
            }
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
