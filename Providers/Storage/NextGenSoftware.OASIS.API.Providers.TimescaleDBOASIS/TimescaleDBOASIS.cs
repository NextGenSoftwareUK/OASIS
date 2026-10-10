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

namespace NextGenSoftware.OASIS.API.Providers.TimescaleDBOASIS
{
    /// <summary>
    /// OASIS provider for TimescaleDB — time-series extension to PostgreSQL, accessed via Npgsql ADO.NET.
    ///
    /// Schema (plain PostgreSQL — TimescaleDB hypertables are optional and left to the DBA):
    ///   oasis_avatars   (id UUID PK, username TEXT, email TEXT, is_deleted BOOLEAN, data_json TEXT, updated_at TIMESTAMPTZ)
    ///   oasis_avatar_details (id UUID PK, username TEXT, email TEXT, data_json TEXT, updated_at TIMESTAMPTZ)
    ///   oasis_holons    (id UUID PK, parent_holon_id UUID, holon_type INT, is_deleted BOOLEAN, data_json TEXT, updated_at TIMESTAMPTZ)
    ///
    /// Constructor parameter:
    ///   connectionString — standard PostgreSQL / TimescaleDB connection string
    /// </summary>
    public class TimescaleDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _connStr;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public TimescaleDBOASIS(string connectionString)
        {
            _connStr = connectionString;
            ProviderName = "TimescaleDBOASIS";
            ProviderDescription = "TimescaleDB provider (Npgsql ADO.NET — time-series PostgreSQL extension for OASIS holons and avatars)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.TimescaleDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocal);
        }

        private static string Ser(object o) => JsonSerializer.Serialize(o, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private NpgsqlConnection OpenConnection()
        {
            var conn = new NpgsqlConnection(_connStr);
            conn.Open();
            return conn;
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new NpgsqlConnection(_connStr);
                await conn.OpenAsync();
                var ddl = @"
CREATE TABLE IF NOT EXISTS oasis_avatars (
    id UUID PRIMARY KEY,
    username TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '',
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    data_json TEXT NOT NULL DEFAULT '',
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_oasis_avatars_username ON oasis_avatars (username);
CREATE INDEX IF NOT EXISTS idx_oasis_avatars_email    ON oasis_avatars (email);

CREATE TABLE IF NOT EXISTS oasis_avatar_details (
    id UUID PRIMARY KEY,
    username TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '',
    data_json TEXT NOT NULL DEFAULT '',
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_oasis_details_username ON oasis_avatar_details (username);
CREATE INDEX IF NOT EXISTS idx_oasis_details_email    ON oasis_avatar_details (email);

CREATE TABLE IF NOT EXISTS oasis_holons (
    id UUID PRIMARY KEY,
    parent_holon_id UUID,
    holon_type INT NOT NULL DEFAULT 0,
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    data_json TEXT NOT NULL DEFAULT '',
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_oasis_holons_parent ON oasis_holons (parent_holon_id);
CREATE INDEX IF NOT EXISTS idx_oasis_holons_type   ON oasis_holons (holon_type);
";
                await using var cmd = new NpgsqlCommand(ddl, conn);
                await cmd.ExecuteNonQueryAsync();
                result.Result = true; result.IsError = false; result.Message = "TimescaleDBOASIS activated — schema ready.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "TimescaleDBOASIS deactivated." });
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.TimescaleDBOASIS] = avatar.Id.ToString();
                await using var conn = new NpgsqlConnection(_connStr); await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(@"
INSERT INTO oasis_avatars (id, username, email, is_deleted, data_json, updated_at)
VALUES (@id, @username, @email, @is_deleted, @data_json, NOW())
ON CONFLICT (id) DO UPDATE SET
    username   = EXCLUDED.username,
    email      = EXCLUDED.email,
    is_deleted = EXCLUDED.is_deleted,
    data_json  = EXCLUDED.data_json,
    updated_at = NOW()", conn);
                cmd.Parameters.AddWithValue("id", avatar.Id);
                cmd.Parameters.AddWithValue("username", (object?)avatar.Username ?? DBNull.Value);
                cmd.Parameters.AddWithValue("email", (object?)avatar.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("is_deleted", avatar.IsDeleted);
                cmd.Parameters.AddWithValue("data_json", Ser(avatar));
                await cmd.ExecuteNonQueryAsync();
                result.Result = avatar; result.IsError = false; result.Message = $"TimescaleDBOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private async Task<Avatar?> LoadAvatarRowAsync(string where, Action<NpgsqlCommand> bind)
        {
            await using var conn = new NpgsqlConnection(_connStr); await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"SELECT data_json FROM oasis_avatars WHERE {where} AND is_deleted = FALSE LIMIT 1", conn);
            bind(cmd);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return Des<Avatar>(reader.GetString(0));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await LoadAvatarRowAsync("id = @p", c => c.Parameters.AddWithValue("p", id)); if (a == null) { OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: No avatar for ID '{id}'."); return result; } result.Result = a; result.IsError = false; result.Message = "TimescaleDBOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await LoadAvatarRowAsync("username = @p", c => c.Parameters.AddWithValue("p", username)); if (a == null) { OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: No avatar for username '{username}'."); return result; } result.Result = a; result.IsError = false; result.Message = "TimescaleDBOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await LoadAvatarRowAsync("email = @p", c => c.Parameters.AddWithValue("p", email)); if (a == null) { OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: No avatar for email '{email}'."); return result; } result.Result = a; result.IsError = false; result.Message = "TimescaleDBOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"TimescaleDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                await using var conn = new NpgsqlConnection(_connStr); await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_avatars WHERE is_deleted = FALSE", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                var avatars = new List<IAvatar>();
                while (await reader.ReadAsync()) { var a = Des<Avatar>(reader.GetString(0)); if (a != null) avatars.Add(a); }
                result.Result = avatars; result.IsError = false; result.Message = $"TimescaleDBOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new NpgsqlConnection(_connStr); await conn.OpenAsync();
                if (softDelete)
                {
                    var loaded = await LoadAvatarAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: Avatar '{id}' not found."); return result; }
                    var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                    await using var cmd = new NpgsqlCommand("UPDATE oasis_avatars SET is_deleted = TRUE, data_json = @dj, updated_at = NOW() WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("dj", Ser(av)); cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync();
                }
                else { await using var cmd = new NpgsqlCommand("DELETE FROM oasis_avatars WHERE id = @id", conn); cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync(); }
                result.Result = true; result.IsError = false; result.Message = $"TimescaleDBOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool softDelete = true) => DeleteAvatarByEmailAsync(e, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteAvatarAsync(id, softDelete); return await DeleteAvatarByUsernameAsync(pk, softDelete); }
        public override OASISResult<bool> DeleteAvatar(string pk, bool softDelete = true) => DeleteAvatarAsync(pk, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail d)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (d.Id == Guid.Empty) d.Id = Guid.NewGuid();
                await using var conn = new NpgsqlConnection(_connStr); await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(@"
INSERT INTO oasis_avatar_details (id, username, email, data_json, updated_at)
VALUES (@id, @username, @email, @data_json, NOW())
ON CONFLICT (id) DO UPDATE SET
    username   = EXCLUDED.username,
    email      = EXCLUDED.email,
    data_json  = EXCLUDED.data_json,
    updated_at = NOW()", conn);
                cmd.Parameters.AddWithValue("id", d.Id);
                cmd.Parameters.AddWithValue("username", (object?)d.Username ?? DBNull.Value);
                cmd.Parameters.AddWithValue("email", (object?)d.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("data_json", Ser(d));
                await cmd.ExecuteNonQueryAsync();
                result.Result = d; result.IsError = false; result.Message = "TimescaleDBOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        private async Task<AvatarDetail?> LoadDetailRowAsync(string where, Action<NpgsqlCommand> bind)
        {
            await using var conn = new NpgsqlConnection(_connStr); await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"SELECT data_json FROM oasis_avatar_details WHERE {where} LIMIT 1", conn);
            bind(cmd);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return Des<AvatarDetail>(reader.GetString(0));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await LoadDetailRowAsync("id = @p", c => c.Parameters.AddWithValue("p", id)); if (d == null) { OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: No detail for ID '{id}'."); return result; } result.Result = d; result.IsError = false; result.Message = "TimescaleDBOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await LoadDetailRowAsync("username = @p", c => c.Parameters.AddWithValue("p", u)); if (d == null) { OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "TimescaleDBOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await LoadDetailRowAsync("email = @p", c => c.Parameters.AddWithValue("p", e)); if (d == null) { OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "TimescaleDBOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { await using var conn = new NpgsqlConnection(_connStr); await conn.OpenAsync(); await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_avatar_details", conn); await using var reader = await cmd.ExecuteReaderAsync(); var details = new List<IAvatarDetail>(); while (await reader.ReadAsync()) { var d = Des<AvatarDetail>(reader.GetString(0)); if (d != null) details.Add(d); } result.Result = details; result.IsError = false; result.Message = $"TimescaleDBOASIS: Loaded {details.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
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
                if (holon.ProviderUniqueStorageKey == null) holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.TimescaleDBOASIS] = holon.Id.ToString();
                await using var conn = new NpgsqlConnection(_connStr); await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(@"
INSERT INTO oasis_holons (id, parent_holon_id, holon_type, is_deleted, data_json, updated_at)
VALUES (@id, @parent_holon_id, @holon_type, @is_deleted, @data_json, NOW())
ON CONFLICT (id) DO UPDATE SET
    parent_holon_id = EXCLUDED.parent_holon_id,
    holon_type      = EXCLUDED.holon_type,
    is_deleted      = EXCLUDED.is_deleted,
    data_json       = EXCLUDED.data_json,
    updated_at      = NOW()", conn);
                cmd.Parameters.AddWithValue("id", holon.Id);
                cmd.Parameters.AddWithValue("parent_holon_id", holon.ParentHolonId == Guid.Empty ? (object)DBNull.Value : holon.ParentHolonId);
                cmd.Parameters.AddWithValue("holon_type", (int)holon.HolonType);
                cmd.Parameters.AddWithValue("is_deleted", holon.IsDeleted);
                cmd.Parameters.AddWithValue("data_json", Ser(holon));
                await cmd.ExecuteNonQueryAsync();
                result.Result = holon; result.IsError = false; result.Message = $"TimescaleDBOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"TimescaleDBOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private async Task<List<IHolon>> LoadHolonRowsAsync(string where, Action<NpgsqlCommand>? bind = null)
        {
            await using var conn = new NpgsqlConnection(_connStr); await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand($"SELECT data_json FROM oasis_holons WHERE {where}", conn);
            bind?.Invoke(cmd);
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<IHolon>();
            while (await reader.ReadAsync()) { var h = Des<Holon>(reader.GetString(0)); if (h != null) list.Add(h); }
            return list;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var list = await LoadHolonRowsAsync("id = @p AND is_deleted = FALSE LIMIT 1", c => c.Parameters.AddWithValue("p", id));
                if (!list.Any()) { OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: No holon for ID '{id}'."); return result; }
                result.Result = list[0]; result.IsError = false; result.Message = "TimescaleDBOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"TimescaleDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var where = type == HolonType.All ? "is_deleted = FALSE" : "is_deleted = FALSE AND holon_type = @ht";
                var holons = await LoadHolonRowsAsync(where, type == HolonType.All ? null : c => c.Parameters.AddWithValue("ht", (int)type));
                result.Result = holons; result.IsError = false; result.Message = $"TimescaleDBOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var where = type == HolonType.All ? "parent_holon_id = @pid AND is_deleted = FALSE" : "parent_holon_id = @pid AND is_deleted = FALSE AND holon_type = @ht";
                var holons = await LoadHolonRowsAsync(where, c => { c.Parameters.AddWithValue("pid", id); if (type != HolonType.All) c.Parameters.AddWithValue("ht", (int)type); });
                result.Result = holons; result.IsError = false; result.Message = $"TimescaleDBOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"TimescaleDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: Holon '{id}' not found."); return result; }
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                await using var conn = new NpgsqlConnection(_connStr); await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("UPDATE oasis_holons SET is_deleted = TRUE, data_json = @dj, updated_at = NOW() WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("dj", Ser(holon)); cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync();
                result.Result = holon; result.IsError = false; result.Message = $"TimescaleDBOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"TimescaleDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"TimescaleDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"TimescaleDBOASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"TimescaleDBOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"TimescaleDBOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(); var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = h, IsError = false, Message = $"TimescaleDBOASIS: {h.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
