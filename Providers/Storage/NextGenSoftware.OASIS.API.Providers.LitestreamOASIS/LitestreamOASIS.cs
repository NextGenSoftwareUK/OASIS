using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
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

namespace NextGenSoftware.OASIS.API.Providers.LitestreamOASIS
{
    /// <summary>
    /// OASIS provider for Litestream — SQLite with continuous S3-compatible replication.
    ///
    /// This provider reads and writes to a local SQLite database file which Litestream
    /// replicates asynchronously to object storage (S3, MinIO, GCS, Azure Blob, etc.).
    /// The OASIS provider layer sees only SQLite; Litestream runs as a sidecar process.
    ///
    /// Schema:
    ///   oasis_avatars       — id TEXT PK, username TEXT, email TEXT, is_deleted INTEGER, data_json TEXT
    ///   oasis_avatar_details — id TEXT PK, username TEXT, email TEXT, data_json TEXT
    ///   oasis_holons        — id TEXT PK, parent_holon_id TEXT, holon_type INTEGER, is_deleted INTEGER, data_json TEXT
    ///
    /// Constructor parameters:
    ///   databasePath — path to the SQLite database file managed by Litestream, e.g. "/data/oasis.db"
    /// </summary>
    public class LitestreamOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _connectionString;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public LitestreamOASIS(string databasePath)
        {
            _connectionString = $"Data Source={databasePath}";
            ProviderName = "LitestreamOASIS";
            ProviderDescription = "Litestream provider (continuously replicated SQLite via Microsoft.Data.Sqlite)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.LitestreamOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private async Task<SqliteConnection> OpenAsync() { var c = new SqliteConnection(_connectionString); await c.OpenAsync(); return c; }
        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private async Task EnsureTablesAsync()
        {
            await using var conn = await OpenAsync();
            async Task Exec(string sql) { await using var cmd = conn.CreateCommand(); cmd.CommandText = sql; await cmd.ExecuteNonQueryAsync(); }
            await Exec("CREATE TABLE IF NOT EXISTS oasis_avatars (id TEXT PRIMARY KEY, username TEXT, email TEXT, is_deleted INTEGER NOT NULL DEFAULT 0, data_json TEXT)");
            await Exec("CREATE INDEX IF NOT EXISTS idx_avatars_username ON oasis_avatars(username)");
            await Exec("CREATE INDEX IF NOT EXISTS idx_avatars_email ON oasis_avatars(email)");
            await Exec("CREATE TABLE IF NOT EXISTS oasis_avatar_details (id TEXT PRIMARY KEY, username TEXT, email TEXT, data_json TEXT)");
            await Exec("CREATE INDEX IF NOT EXISTS idx_details_username ON oasis_avatar_details(username)");
            await Exec("CREATE INDEX IF NOT EXISTS idx_details_email ON oasis_avatar_details(email)");
            await Exec("CREATE TABLE IF NOT EXISTS oasis_holons (id TEXT PRIMARY KEY, parent_holon_id TEXT, holon_type INTEGER DEFAULT 0, is_deleted INTEGER NOT NULL DEFAULT 0, data_json TEXT)");
            await Exec("CREATE INDEX IF NOT EXISTS idx_holons_parent ON oasis_holons(parent_holon_id)");
            await Exec("CREATE INDEX IF NOT EXISTS idx_holons_type ON oasis_holons(holon_type)");
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try { await EnsureTablesAsync(); result.Result = true; IsProviderActivated = true; result.IsError = false; result.Message = "LitestreamOASIS activated."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() { IsProviderActivated = false; return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "LitestreamOASIS deactivated." }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.LitestreamOASIS] = avatar.Id.ToString();
                await using var conn = await OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO oasis_avatars(id,username,email,is_deleted,data_json) VALUES(@id,@u,@e,@del,@data) ON CONFLICT(id) DO UPDATE SET username=excluded.username,email=excluded.email,is_deleted=excluded.is_deleted,data_json=excluded.data_json";
                cmd.Parameters.AddWithValue("@id", avatar.Id.ToString());
                cmd.Parameters.AddWithValue("@u", (object?)avatar.Username ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@e", (object?)avatar.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@del", avatar.IsDeleted ? 1 : 0);
                cmd.Parameters.AddWithValue("@data", Ser(avatar));
                await cmd.ExecuteNonQueryAsync();
                result.Result = avatar; result.IsError = false; result.Message = $"LitestreamOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        private async Task<Avatar?> QueryAvatarAsync(string sql, Action<SqliteCommand> bind)
        {
            await using var conn = await OpenAsync(); await using var cmd = conn.CreateCommand(); cmd.CommandText = sql; bind(cmd);
            await using var r = await cmd.ExecuteReaderAsync(); if (!await r.ReadAsync()) return null; return Des<Avatar>(r.GetString(0));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE id=@id AND is_deleted=0 LIMIT 1", c => c.Parameters.AddWithValue("@id", id.ToString())); if (a == null) { OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: No avatar for ID '{id}'."); return result; } result.Result = a; result.IsError = false; result.Message = "LitestreamOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE username=@u AND is_deleted=0 LIMIT 1", c => c.Parameters.AddWithValue("@u", username)); if (a == null) { OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: No avatar for username '{username}'."); return result; } result.Result = a; result.IsError = false; result.Message = "LitestreamOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE email=@e AND is_deleted=0 LIMIT 1", c => c.Parameters.AddWithValue("@e", avatarEmail)); if (a == null) { OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: No avatar for email '{avatarEmail}'."); return result; } result.Result = a; result.IsError = false; result.Message = "LitestreamOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"LitestreamOASIS: Invalid GUID '{providerKey}'."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                await using var conn = await OpenAsync(); await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT data_json FROM oasis_avatars WHERE is_deleted=0";
                await using var r = await cmd.ExecuteReaderAsync();
                var list = new List<IAvatar>();
                while (await r.ReadAsync()) { var a = Des<Avatar>(r.GetString(0)); if (a != null) list.Add(a); }
                result.Result = list; result.IsError = false; result.Message = $"LitestreamOASIS: Loaded {list.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = await OpenAsync(); await using var cmd = conn.CreateCommand();
                cmd.CommandText = softDelete ? "UPDATE oasis_avatars SET is_deleted=1 WHERE id=@id" : "DELETE FROM oasis_avatars WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", id.ToString()); await cmd.ExecuteNonQueryAsync();
                result.Result = true; result.IsError = false; result.Message = $"LitestreamOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"LitestreamOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"LitestreamOASIS: Avatar not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                await using var conn = await OpenAsync(); await using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO oasis_avatar_details(id,username,email,data_json) VALUES(@id,@u,@e,@data) ON CONFLICT(id) DO UPDATE SET username=excluded.username,email=excluded.email,data_json=excluded.data_json";
                cmd.Parameters.AddWithValue("@id", d.Id.ToString()); cmd.Parameters.AddWithValue("@u", (object?)d.Username ?? DBNull.Value); cmd.Parameters.AddWithValue("@e", (object?)d.Email ?? DBNull.Value); cmd.Parameters.AddWithValue("@data", Ser(d));
                await cmd.ExecuteNonQueryAsync(); result.Result = d; result.IsError = false; result.Message = "LitestreamOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        private async Task<AvatarDetail?> QueryDetailAsync(string sql, Action<SqliteCommand> bind) { await using var conn = await OpenAsync(); await using var cmd = conn.CreateCommand(); cmd.CommandText = sql; bind(cmd); await using var r = await cmd.ExecuteReaderAsync(); if (!await r.ReadAsync()) return null; return Des<AvatarDetail>(r.GetString(0)); }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await QueryDetailAsync("SELECT data_json FROM oasis_avatar_details WHERE id=@id LIMIT 1", c => c.Parameters.AddWithValue("@id", id.ToString())); if (d == null) { OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: No detail for ID '{id}'."); return result; } result.Result = d; result.IsError = false; result.Message = "LitestreamOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await QueryDetailAsync("SELECT data_json FROM oasis_avatar_details WHERE username=@u LIMIT 1", c => c.Parameters.AddWithValue("@u", u)); if (d == null) { OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: No detail for '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "LitestreamOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await QueryDetailAsync("SELECT data_json FROM oasis_avatar_details WHERE email=@e LIMIT 1", c => c.Parameters.AddWithValue("@e", e)); if (d == null) { OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "LitestreamOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { await using var conn = await OpenAsync(); await using var cmd = conn.CreateCommand(); cmd.CommandText = "SELECT data_json FROM oasis_avatar_details"; await using var r = await cmd.ExecuteReaderAsync(); var list = new List<IAvatarDetail>(); while (await r.ReadAsync()) { var d = Des<AvatarDetail>(r.GetString(0)); if (d != null) list.Add(d); } result.Result = list; result.IsError = false; result.Message = $"LitestreamOASIS: Loaded {list.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holons ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                await using var conn = await OpenAsync(); await using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO oasis_holons(id,parent_holon_id,holon_type,is_deleted,data_json) VALUES(@id,@p,@t,@del,@data) ON CONFLICT(id) DO UPDATE SET parent_holon_id=excluded.parent_holon_id,holon_type=excluded.holon_type,is_deleted=excluded.is_deleted,data_json=excluded.data_json";
                cmd.Parameters.AddWithValue("@id", holon.Id.ToString());
                cmd.Parameters.AddWithValue("@p", holon.ParentHolonId != Guid.Empty ? (object)holon.ParentHolonId.ToString() : DBNull.Value);
                cmd.Parameters.AddWithValue("@t", holon.HolonType.HasValue ? (int)holon.HolonType.Value : 0);
                cmd.Parameters.AddWithValue("@del", holon.IsDeleted ? 1 : 0);
                cmd.Parameters.AddWithValue("@data", Ser(holon));
                await cmd.ExecuteNonQueryAsync();
                result.Result = holon; result.IsError = false; result.Message = $"LitestreamOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: Error saving holon: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>(); var saved = new List<IHolon>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version); if (!r.IsError && r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = false; result.Message = $"LitestreamOASIS: Saved {saved.Count} holons."; return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                await using var conn = await OpenAsync(); await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT data_json FROM oasis_holons WHERE id=@id AND is_deleted=0 LIMIT 1";
                cmd.Parameters.AddWithValue("@id", id.ToString()); await using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync()) { OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: No holon for ID '{id}'."); return result; }
                var h = Des<Holon>(r.GetString(0)); if (h == null) { OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: Failed to deserialise holon '{id}'."); return result; }
                result.Result = h; result.IsError = false; result.Message = "LitestreamOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"LitestreamOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await using var conn = await OpenAsync(); await using var cmd = conn.CreateCommand();
                cmd.CommandText = holonType == HolonType.All ? "SELECT data_json FROM oasis_holons WHERE is_deleted=0" : "SELECT data_json FROM oasis_holons WHERE holon_type=@t AND is_deleted=0";
                if (holonType != HolonType.All) cmd.Parameters.AddWithValue("@t", (int)holonType);
                await using var r = await cmd.ExecuteReaderAsync();
                var list = new List<IHolon>(); while (await r.ReadAsync()) { var h = Des<Holon>(r.GetString(0)); if (h != null) list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"LitestreamOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                await using var conn = await OpenAsync(); await using var cmd = conn.CreateCommand();
                cmd.CommandText = holonType == HolonType.All ? "SELECT data_json FROM oasis_holons WHERE parent_holon_id=@p AND is_deleted=0" : "SELECT data_json FROM oasis_holons WHERE parent_holon_id=@p AND holon_type=@t AND is_deleted=0";
                cmd.Parameters.AddWithValue("@p", id.ToString()); if (holonType != HolonType.All) cmd.Parameters.AddWithValue("@t", (int)holonType);
                await using var r = await cmd.ExecuteReaderAsync();
                var list = new List<IHolon>(); while (await r.ReadAsync()) { var h = Des<Holon>(r.GetString(0)); if (h != null) list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"LitestreamOASIS: Loaded {list.Count} child holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) { if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"LitestreamOASIS: Invalid GUID '{providerKey}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = await OpenAsync(); await using var cmd = conn.CreateCommand();
                cmd.CommandText = softDelete ? "UPDATE oasis_holons SET is_deleted=1 WHERE id=@id" : "DELETE FROM oasis_holons WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", id.ToString()); await cmd.ExecuteNonQueryAsync();
                result.Result = true; result.IsError = false; result.Message = $"LitestreamOASIS: Holon '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"LitestreamOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true) => DeleteHolonAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteHolonAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"LitestreamOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<bool> DeleteHolon(string pk, bool softDelete = true) => DeleteHolonAsync(pk, softDelete).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>(); var sr = new SearchResults();
            var avatarResult = await LoadAllAvatarsAsync(); if (!avatarResult.IsError && avatarResult.Result != null) sr.Avatars = new List<IAvatar>(avatarResult.Result);
            var holonResult = await LoadAllHolonsAsync(); if (!holonResult.IsError && holonResult.Result != null) sr.Holons = new List<IHolon>(holonResult.Result);
            result.Result = sr; result.IsError = false; return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        public override Task<OASISResult<IAvatar>> SearchAvatarsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("LitestreamOASIS: Use SearchAsync.");
        public override OASISResult<IAvatar> SearchAvatars(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();
        public override Task<OASISResult<IHolon>> SearchHolonsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("LitestreamOASIS: Use SearchAsync.");
        public override OASISResult<IHolon> SearchHolons(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();

        public override string GetProviderVersion() => "1.0.0";
        public override Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");
    }
}
