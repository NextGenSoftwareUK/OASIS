using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MySqlConnector;
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

namespace NextGenSoftware.OASIS.API.Providers.PlanetScaleOASIS
{
    /// <summary>
    /// OASIS provider for PlanetScale (serverless MySQL) using MySqlConnector ADO.NET.
    ///
    /// PlanetScale is a serverless MySQL-compatible database.
    /// This provider is also compatible with any MySQL 8+ instance (AWS Aurora MySQL,
    /// Google Cloud SQL MySQL, Azure Database for MySQL, self-hosted MySQL).
    ///
    /// Tables are auto-created on ActivateProvider.
    /// Schema:
    ///   oasis_avatars       — id VARCHAR(36) PK, username VARCHAR(255), email VARCHAR(255), is_deleted TINYINT(1), data_json LONGTEXT
    ///   oasis_avatar_details — id VARCHAR(36) PK, username VARCHAR(255), email VARCHAR(255), data_json LONGTEXT
    ///   oasis_holons        — id VARCHAR(36) PK, parent_holon_id VARCHAR(36), holon_type INT, is_deleted TINYINT(1), data_json LONGTEXT
    ///
    /// Constructor parameters:
    ///   connectionString — MySQL connection string, e.g.:
    ///     "Server=aws.connect.psdb.cloud;Database=mydb;User=user;Password=pass;SslMode=Required"
    /// </summary>
    public class PlanetScaleOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _connectionString;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public PlanetScaleOASIS(string connectionString)
        {
            _connectionString = connectionString;
            ProviderName = "PlanetScaleOASIS";
            ProviderDescription = "PlanetScale provider (serverless MySQL via MySqlConnector ADO.NET)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.PlanetScaleOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ─── Connection helpers ───────────────────────────────────────────────────

        private MySqlConnection Open() { var conn = new MySqlConnection(_connectionString); conn.Open(); return conn; }
        private async Task<MySqlConnection> OpenAsync() { var conn = new MySqlConnection(_connectionString); await conn.OpenAsync(); return conn; }
        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        // ─── Schema bootstrap ─────────────────────────────────────────────────────

        private async Task EnsureTablesAsync()
        {
            await using var conn = await OpenAsync();
            async Task Exec(string sql) { await using var cmd = new MySqlCommand(sql, conn); await cmd.ExecuteNonQueryAsync(); }
            await Exec(@"CREATE TABLE IF NOT EXISTS oasis_avatars (
                id VARCHAR(36) NOT NULL PRIMARY KEY,
                username VARCHAR(255), email VARCHAR(255),
                is_deleted TINYINT(1) NOT NULL DEFAULT 0,
                data_json LONGTEXT,
                INDEX idx_username(username), INDEX idx_email(email))");
            await Exec(@"CREATE TABLE IF NOT EXISTS oasis_avatar_details (
                id VARCHAR(36) NOT NULL PRIMARY KEY,
                username VARCHAR(255), email VARCHAR(255), data_json LONGTEXT,
                INDEX idx_username(username), INDEX idx_email(email))");
            await Exec(@"CREATE TABLE IF NOT EXISTS oasis_holons (
                id VARCHAR(36) NOT NULL PRIMARY KEY,
                parent_holon_id VARCHAR(36), holon_type INT DEFAULT 0,
                is_deleted TINYINT(1) NOT NULL DEFAULT 0,
                data_json LONGTEXT,
                INDEX idx_parent(parent_holon_id), INDEX idx_type(holon_type))");
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await EnsureTablesAsync();
                result.Result = true; result.IsError = false;
                result.Message = "PlanetScaleOASIS activated — tables ready in MySQL/PlanetScale.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error activating provider — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "PlanetScaleOASIS deactivated." });
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.PlanetScaleOASIS] = avatar.Id.ToString();
                await using var conn = await OpenAsync();
                await using var cmd = new MySqlCommand(
                    "INSERT INTO oasis_avatars(id,username,email,is_deleted,data_json) VALUES(@id,@username,@email,@is_deleted,@data_json) ON DUPLICATE KEY UPDATE username=VALUES(username),email=VALUES(email),is_deleted=VALUES(is_deleted),data_json=VALUES(data_json)", conn);
                cmd.Parameters.AddWithValue("@id", avatar.Id.ToString());
                cmd.Parameters.AddWithValue("@username", avatar.Username ?? "");
                cmd.Parameters.AddWithValue("@email", avatar.Email ?? "");
                cmd.Parameters.AddWithValue("@is_deleted", avatar.IsDeleted ? 1 : 0);
                cmd.Parameters.AddWithValue("@data_json", Ser(avatar));
                await cmd.ExecuteNonQueryAsync();
                result.Result = avatar; result.IsError = false; result.Message = $"PlanetScaleOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private async Task<Avatar?> QueryAvatarAsync(string sql, Action<MySqlCommand> bind)
        {
            await using var conn = await OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            bind(cmd);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return Des<Avatar>(reader.GetString("data_json"));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE id=@id AND is_deleted=0 LIMIT 1", c => c.Parameters.AddWithValue("@id", id.ToString()));
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: No avatar found for ID '{id}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"PlanetScaleOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error loading avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE username=@u AND is_deleted=0 LIMIT 1", c => c.Parameters.AddWithValue("@u", username));
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: No avatar found for username '{username}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"PlanetScaleOASIS: Avatar loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error loading avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE email=@e AND is_deleted=0 LIMIT 1", c => c.Parameters.AddWithValue("@e", avatarEmail));
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: No avatar found for email '{avatarEmail}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"PlanetScaleOASIS: Avatar loaded for email '{avatarEmail}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error loading avatar by email '{avatarEmail}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"PlanetScaleOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                await using var conn = await OpenAsync();
                await using var cmd = new MySqlCommand("SELECT data_json FROM oasis_avatars WHERE is_deleted=0", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                var avatars = new List<IAvatar>();
                while (await reader.ReadAsync()) { var a = Des<Avatar>(reader.GetString("data_json")); if (a != null) avatars.Add(a); }
                result.Result = avatars; result.IsError = false; result.Message = $"PlanetScaleOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = await OpenAsync();
                string sql = softDelete ? "UPDATE oasis_avatars SET is_deleted=1 WHERE id=@id" : "DELETE FROM oasis_avatars WHERE id=@id";
                await using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id.ToString());
                await cmd.ExecuteNonQueryAsync();
                result.Result = true; result.IsError = false; result.Message = $"PlanetScaleOASIS: Avatar '{id}' {(softDelete ? "soft" : "hard")}-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error deleting avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var a = await LoadAvatarByUsernameAsync(username);
            if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"PlanetScaleOASIS: Avatar '{username}' not found."); return r; }
            return await DeleteAvatarAsync(a.Result.Id, softDelete);
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var a = await LoadAvatarByEmailAsync(email);
            if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"PlanetScaleOASIS: Avatar with email '{email}' not found."); return r; }
            return await DeleteAvatarAsync(a.Result.Id, softDelete);
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
                await using var conn = await OpenAsync();
                await using var cmd = new MySqlCommand("INSERT INTO oasis_avatar_details(id,username,email,data_json) VALUES(@id,@username,@email,@data_json) ON DUPLICATE KEY UPDATE username=VALUES(username),email=VALUES(email),data_json=VALUES(data_json)", conn);
                cmd.Parameters.AddWithValue("@id", avatarDetail.Id.ToString());
                cmd.Parameters.AddWithValue("@username", avatarDetail.Username ?? "");
                cmd.Parameters.AddWithValue("@email", avatarDetail.Email ?? "");
                cmd.Parameters.AddWithValue("@data_json", Ser(avatarDetail));
                await cmd.ExecuteNonQueryAsync();
                result.Result = avatarDetail; result.IsError = false; result.Message = "PlanetScaleOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        private async Task<AvatarDetail?> QueryDetailAsync(string sql, Action<MySqlCommand> bind)
        {
            await using var conn = await OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            bind(cmd);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            return Des<AvatarDetail>(reader.GetString("data_json"));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var d = await QueryDetailAsync("SELECT data_json FROM oasis_avatar_details WHERE id=@id LIMIT 1", c => c.Parameters.AddWithValue("@id", id.ToString()));
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: No avatar detail found for ID '{id}'."); return result; }
                result.Result = d; result.IsError = false; result.Message = $"PlanetScaleOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error loading avatar detail '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var d = await QueryDetailAsync("SELECT data_json FROM oasis_avatar_details WHERE username=@u LIMIT 1", c => c.Parameters.AddWithValue("@u", username));
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: No avatar detail found for username '{username}'."); return result; }
                result.Result = d; result.IsError = false; result.Message = $"PlanetScaleOASIS: AvatarDetail loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error loading avatar detail by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var d = await QueryDetailAsync("SELECT data_json FROM oasis_avatar_details WHERE email=@e LIMIT 1", c => c.Parameters.AddWithValue("@e", email));
                if (d == null) { OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: No avatar detail found for email '{email}'."); return result; }
                result.Result = d; result.IsError = false; result.Message = $"PlanetScaleOASIS: AvatarDetail loaded for email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error loading avatar detail by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                await using var conn = await OpenAsync();
                await using var cmd = new MySqlCommand("SELECT data_json FROM oasis_avatar_details", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                var details = new List<IAvatarDetail>();
                while (await reader.ReadAsync()) { var d = Des<AvatarDetail>(reader.GetString("data_json")); if (d != null) details.Add(d); }
                result.Result = details; result.IsError = false; result.Message = $"PlanetScaleOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error loading all avatar details: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.PlanetScaleOASIS] = holon.Id.ToString();
                await using var conn = await OpenAsync();
                await using var cmd = new MySqlCommand("INSERT INTO oasis_holons(id,parent_holon_id,holon_type,is_deleted,data_json) VALUES(@id,@parent,@type,@del,@data) ON DUPLICATE KEY UPDATE parent_holon_id=VALUES(parent_holon_id),holon_type=VALUES(holon_type),is_deleted=VALUES(is_deleted),data_json=VALUES(data_json)", conn);
                cmd.Parameters.AddWithValue("@id", holon.Id.ToString());
                cmd.Parameters.AddWithValue("@parent", holon.ParentHolonId == Guid.Empty ? (object)DBNull.Value : holon.ParentHolonId.ToString());
                cmd.Parameters.AddWithValue("@type", (int)holon.HolonType);
                cmd.Parameters.AddWithValue("@del", holon.IsDeleted ? 1 : 0);
                cmd.Parameters.AddWithValue("@data", Ser(holon));
                await cmd.ExecuteNonQueryAsync();
                result.Result = holon; result.IsError = false; result.Message = $"PlanetScaleOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error saving holon '{holon.Name}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var holon in holons) { var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"PlanetScaleOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private async Task<List<IHolon>> QueryHolonsAsync(string sql, Action<MySqlCommand>? bind = null)
        {
            await using var conn = await OpenAsync();
            await using var cmd = new MySqlCommand(sql, conn);
            bind?.Invoke(cmd);
            await using var reader = await cmd.ExecuteReaderAsync();
            var holons = new List<IHolon>();
            while (await reader.ReadAsync()) { var h = Des<Holon>(reader.GetString("data_json")); if (h != null) holons.Add(h); }
            return holons;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holons = await QueryHolonsAsync("SELECT data_json FROM oasis_holons WHERE id=@id AND is_deleted=0 LIMIT 1", c => c.Parameters.AddWithValue("@id", id.ToString()));
                if (holons.Count == 0) { OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: No holon found for ID '{id}'."); return result; }
                result.Result = holons[0]; result.IsError = false; result.Message = $"PlanetScaleOASIS: Holon loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error loading holon '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"PlanetScaleOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = type == HolonType.All ? "SELECT data_json FROM oasis_holons WHERE is_deleted=0" : "SELECT data_json FROM oasis_holons WHERE is_deleted=0 AND holon_type=@type";
                var holons = await QueryHolonsAsync(sql, type == HolonType.All ? null : (Action<MySqlCommand>)(c => c.Parameters.AddWithValue("@type", (int)type)));
                result.Result = holons; result.IsError = false; result.Message = $"PlanetScaleOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error loading all holons: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = type == HolonType.All ? "SELECT data_json FROM oasis_holons WHERE parent_holon_id=@parent AND is_deleted=0" : "SELECT data_json FROM oasis_holons WHERE parent_holon_id=@parent AND is_deleted=0 AND holon_type=@type";
                var holons = await QueryHolonsAsync(sql, c => { c.Parameters.AddWithValue("@parent", id.ToString()); if (type != HolonType.All) c.Parameters.AddWithValue("@type", (int)type); });
                result.Result = holons; result.IsError = false; result.Message = $"PlanetScaleOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: Error loading holons for parent '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"PlanetScaleOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id);
                if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"PlanetScaleOASIS: No holon found with ID '{id}'."); return result; }
                await using var conn = await OpenAsync();
                await using var cmd = new MySqlCommand("UPDATE oasis_holons SET is_deleted=1 WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id.ToString());
                await cmd.ExecuteNonQueryAsync();
                result.Result = loaded.Result; result.IsError = false; result.Message = $"PlanetScaleOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonAsync(id);
            var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"PlanetScaleOASIS: providerKey '{providerKey}' is not a valid GUID."); return r;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search + Metadata ────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower();
                var all = await LoadAllHolonsAsync();
                var holons = all.Result?.ToList() ?? new List<IHolon>();
                if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList();
                result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count };
                result.IsError = false; result.Message = $"PlanetScaleOASIS: Found {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var all = await LoadAllHolonsAsync(type);
            var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>();
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"PlanetScaleOASIS: Loaded {holons.Count} holon(s) where {metaKey}={metaValue}." };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var all = await LoadAllHolonsAsync(type);
            bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKeyValuePairs.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); }
            var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>();
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"PlanetScaleOASIS: Loaded {holons.Count} holon(s) matching metadata filter." };
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var all = await LoadAllHolonsAsync();
            var holons = all.Result?.Where(h => h.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>();
            return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"PlanetScaleOASIS: Exported {holons.Count} holon(s) for avatar '{avatarId}'." };
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"PlanetScaleOASIS: Avatar '{u}' not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"PlanetScaleOASIS: Avatar with email '{e}' not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
