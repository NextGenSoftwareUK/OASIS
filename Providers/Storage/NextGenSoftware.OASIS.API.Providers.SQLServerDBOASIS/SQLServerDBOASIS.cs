using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
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

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS
{
    /// <summary>
    /// OASIS provider for Microsoft SQL Server (and Azure SQL).
    ///
    /// Uses ADO.NET (Microsoft.Data.SqlClient) with two tables:
    ///   OASISAvatars  — Id, Username, Email, IsDeleted, DataJson (NVARCHAR(MAX))
    ///   OASISHolons   — Id, ParentHolonId, HolonType, IsDeleted, DataJson (NVARCHAR(MAX))
    ///
    /// The DataJson column holds the full JSON-serialised OASIS object, so every
    /// interface method can round-trip the complete object without a generated ORM model.
    /// Indexed columns (Id, Username, Email, ParentHolonId) serve fast key lookups.
    ///
    /// Compatible with SQL Server 2016+ and Azure SQL Database / Azure SQL Managed Instance.
    /// Pass a standard SQL Server connection string (e.g. from appsettings.json or environment).
    /// </summary>
    public class SQLServerDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _connectionString;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private const string CreateAvatarsTable = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OASISAvatars' AND xtype='U')
CREATE TABLE OASISAvatars (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Username    NVARCHAR(256)    NOT NULL DEFAULT '',
    Email       NVARCHAR(256)    NOT NULL DEFAULT '',
    IsDeleted   BIT              NOT NULL DEFAULT 0,
    CreatedDate DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    ModifiedDate DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
    DataJson    NVARCHAR(MAX)    NOT NULL
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_OASISAvatars_Username')
    CREATE INDEX IX_OASISAvatars_Username ON OASISAvatars(Username);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_OASISAvatars_Email')
    CREATE INDEX IX_OASISAvatars_Email ON OASISAvatars(Email);";

        private const string CreateAvatarDetailsTable = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OASISAvatarDetails' AND xtype='U')
CREATE TABLE OASISAvatarDetails (
    Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    Username    NVARCHAR(256)    NOT NULL DEFAULT '',
    Email       NVARCHAR(256)    NOT NULL DEFAULT '',
    DataJson    NVARCHAR(MAX)    NOT NULL
);";

        private const string CreateHolonsTable = @"
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='OASISHolons' AND xtype='U')
CREATE TABLE OASISHolons (
    Id             UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    ParentHolonId  UNIQUEIDENTIFIER NULL,
    HolonType      INT              NOT NULL DEFAULT 0,
    IsDeleted      BIT              NOT NULL DEFAULT 0,
    CreatedDate    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    ModifiedDate   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    DataJson       NVARCHAR(MAX)    NOT NULL
);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_OASISHolons_ParentHolonId')
    CREATE INDEX IX_OASISHolons_ParentHolonId ON OASISHolons(ParentHolonId);
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_OASISHolons_HolonType')
    CREATE INDEX IX_OASISHolons_HolonType ON OASISHolons(HolonType);";

        public SQLServerDBOASIS(string connectionString)
        {
            _connectionString = connectionString;
            ProviderName = "SQLServerDBOASIS";
            ProviderDescription = "Microsoft SQL Server provider (ADO.NET, JSON blob storage per row)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SQLServerDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                foreach (string ddl in new[] { CreateAvatarsTable, CreateAvatarDetailsTable, CreateHolonsTable })
                {
                    await using var cmd = new SqlCommand(ddl, conn);
                    await cmd.ExecuteNonQueryAsync();
                }
                result.Result = true;
                result.IsError = false;
                result.Message = "SQLServerDBOASIS activated — tables created/verified.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error activating provider — {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
            => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "SQLServerDBOASIS deactivated." });

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── ADO helpers ──────────────────────────────────────────────────────────

        private SqlConnection OpenConnection() => new SqlConnection(_connectionString);

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
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SQLServerDBOASIS] = avatar.Id.ToString();

                const string sql = @"
MERGE OASISAvatars AS target
USING (SELECT @Id AS Id) AS source ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET Username=@Username, Email=@Email, IsDeleted=@IsDeleted, ModifiedDate=SYSUTCDATETIME(), DataJson=@DataJson
WHEN NOT MATCHED THEN
    INSERT (Id,Username,Email,IsDeleted,DataJson) VALUES (@Id,@Username,@Email,@IsDeleted,@DataJson);";

                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", avatar.Id);
                cmd.Parameters.AddWithValue("@Username", (object?)avatar.Username ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object?)avatar.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsDeleted", avatar.IsDeleted);
                cmd.Parameters.AddWithValue("@DataJson", Serialize(avatar));
                await cmd.ExecuteNonQueryAsync();

                result.Result = avatar;
                result.IsError = false;
                result.Message = $"SQLServerDBOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error saving avatar '{avatar.Username}': {ex.Message}");
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private async Task<Avatar?> LoadAvatarByColumnAsync(string column, object value)
        {
            string sql = $"SELECT DataJson FROM OASISAvatars WHERE {column}=@val AND IsDeleted=0";
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@val", value);
            var scalar = await cmd.ExecuteScalarAsync();
            if (scalar == null || scalar == DBNull.Value) return null;
            return Deserialize<Avatar>(scalar.ToString()!);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await LoadAvatarByColumnAsync("Id", id);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar found with ID '{id}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"SQLServerDBOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading avatar by ID '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await LoadAvatarByColumnAsync("Username", username);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar found with username '{username}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"SQLServerDBOASIS: Avatar loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var avatars = new List<IAvatar>();
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("SELECT DataJson FROM OASISAvatars WHERE IsDeleted=0", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var avatar = Deserialize<Avatar>(reader.GetString(0));
                    if (avatar != null) avatars.Add(avatar);
                }
                result.Result = avatars; result.IsError = false; result.Message = $"SQLServerDBOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                string sql = softDelete
                    ? "UPDATE OASISAvatars SET IsDeleted=1, ModifiedDate=SYSUTCDATETIME() WHERE Id=@Id"
                    : "DELETE FROM OASISAvatars WHERE Id=@Id";
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                result.Result = rows > 0; result.IsError = !result.Result;
                result.Message = result.Result
                    ? $"SQLServerDBOASIS: Avatar '{id}' {(softDelete ? "soft" : "hard")}-deleted."
                    : $"SQLServerDBOASIS: No avatar found with ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error deleting avatar '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                string sql = softDelete
                    ? "UPDATE OASISAvatars SET IsDeleted=1, ModifiedDate=SYSUTCDATETIME() WHERE Username=@Username"
                    : "DELETE FROM OASISAvatars WHERE Username=@Username";
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                int rows = await cmd.ExecuteNonQueryAsync();
                result.Result = rows > 0; result.IsError = !result.Result;
                result.Message = result.Result
                    ? $"SQLServerDBOASIS: Avatar '{username}' {(softDelete ? "soft" : "hard")}-deleted."
                    : $"SQLServerDBOASIS: No avatar found with username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error deleting avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                string sql = softDelete
                    ? "UPDATE OASISAvatars SET IsDeleted=1, ModifiedDate=SYSUTCDATETIME() WHERE Email=@Email"
                    : "DELETE FROM OASISAvatars WHERE Email=@Email";
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Email", email);
                int rows = await cmd.ExecuteNonQueryAsync();
                result.Result = rows > 0; result.IsError = !result.Result;
                result.Message = result.Result
                    ? $"SQLServerDBOASIS: Avatar with email '{email}' {(softDelete ? "soft" : "hard")}-deleted."
                    : $"SQLServerDBOASIS: No avatar found with email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error deleting avatar by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) => DeleteAvatarByEmailAsync(email, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (avatarDetail.Id == Guid.Empty) avatarDetail.Id = Guid.NewGuid();
                const string sql = @"
MERGE OASISAvatarDetails AS target
USING (SELECT @Id AS Id) AS source ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET Username=@Username, Email=@Email, DataJson=@DataJson
WHEN NOT MATCHED THEN INSERT (Id,Username,Email,DataJson) VALUES (@Id,@Username,@Email,@DataJson);";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", avatarDetail.Id);
                cmd.Parameters.AddWithValue("@Username", (object?)avatarDetail.Username ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object?)avatarDetail.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DataJson", Serialize(avatarDetail));
                await cmd.ExecuteNonQueryAsync();
                result.Result = avatarDetail; result.IsError = false; result.Message = "SQLServerDBOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("SELECT DataJson FROM OASISAvatarDetails WHERE Id=@Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                var scalar = await cmd.ExecuteScalarAsync();
                if (scalar == null || scalar == DBNull.Value) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar detail found for ID '{id}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(scalar.ToString()!); result.IsError = false; result.Message = $"SQLServerDBOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading avatar detail for '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("SELECT DataJson FROM OASISAvatarDetails WHERE Username=@Username", conn);
                cmd.Parameters.AddWithValue("@Username", username);
                var scalar = await cmd.ExecuteScalarAsync();
                if (scalar == null || scalar == DBNull.Value) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar detail found for username '{username}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(scalar.ToString()!); result.IsError = false; result.Message = $"SQLServerDBOASIS: AvatarDetail loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading avatar detail by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("SELECT DataJson FROM OASISAvatarDetails WHERE Email=@Email", conn);
                cmd.Parameters.AddWithValue("@Email", email);
                var scalar = await cmd.ExecuteScalarAsync();
                if (scalar == null || scalar == DBNull.Value) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar detail found for email '{email}'."); return result; }
                result.Result = Deserialize<AvatarDetail>(scalar.ToString()!); result.IsError = false; result.Message = $"SQLServerDBOASIS: AvatarDetail loaded for email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading avatar detail by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var details = new List<IAvatarDetail>();
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("SELECT DataJson FROM OASISAvatarDetails", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var detail = Deserialize<AvatarDetail>(reader.GetString(0));
                    if (detail != null) details.Add(detail);
                }
                result.Result = details; result.IsError = false; result.Message = $"SQLServerDBOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading all avatar details: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SQLServerDBOASIS] = holon.Id.ToString();

                const string sql = @"
MERGE OASISHolons AS target
USING (SELECT @Id AS Id) AS source ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET ParentHolonId=@ParentHolonId, HolonType=@HolonType, IsDeleted=@IsDeleted, ModifiedDate=SYSUTCDATETIME(), DataJson=@DataJson
WHEN NOT MATCHED THEN
    INSERT (Id,ParentHolonId,HolonType,IsDeleted,DataJson) VALUES (@Id,@ParentHolonId,@HolonType,@IsDeleted,@DataJson);";

                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", holon.Id);
                cmd.Parameters.AddWithValue("@ParentHolonId", holon.ParentHolonId == Guid.Empty ? (object)DBNull.Value : holon.ParentHolonId);
                cmd.Parameters.AddWithValue("@HolonType", (int)holon.HolonType);
                cmd.Parameters.AddWithValue("@IsDeleted", holon.IsDeleted);
                cmd.Parameters.AddWithValue("@DataJson", Serialize(holon));
                await cmd.ExecuteNonQueryAsync();

                result.Result = holon; result.IsError = false; result.Message = $"SQLServerDBOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error saving holon '{holon.Name}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var holon in holons)
            {
                var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                if (r.IsError) errors.Add(r.Message); else saved.Add(r.Result!);
            }
            result.Result = saved; result.IsError = errors.Count > 0;
            result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"SQLServerDBOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private async Task<Holon?> LoadHolonByColumnAsync(string column, object value)
        {
            string sql = $"SELECT DataJson FROM OASISHolons WHERE {column}=@val AND IsDeleted=0";
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@val", value);
            var scalar = await cmd.ExecuteScalarAsync();
            if (scalar == null || scalar == DBNull.Value) return null;
            return Deserialize<Holon>(scalar.ToString()!);
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holon = await LoadHolonByColumnAsync("Id", id);
                if (holon == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No holon found with ID '{id}'."); return result; }
                result.Result = holon; result.IsError = false; result.Message = $"SQLServerDBOASIS: Holon loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading holon '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsyncLegacy(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                string sql = holonType == HolonType.All
                    ? "SELECT DataJson FROM OASISHolons WHERE IsDeleted=0"
                    : "SELECT DataJson FROM OASISHolons WHERE IsDeleted=0 AND HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                if (holonType != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)holonType);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var holon = Deserialize<Holon>(reader.GetString(0));
                    if (holon != null) holons.Add(holon);
                }
                result.Result = holons; result.IsError = false; result.Message = $"SQLServerDBOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading all holons: {ex.Message}"); }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadAllHolonsLegacy(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadAllHolonsAsyncLegacy(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsyncLegacy(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var holons = new List<IHolon>();
                string sql = holonType == HolonType.All
                    ? "SELECT DataJson FROM OASISHolons WHERE ParentHolonId=@ParentHolonId AND IsDeleted=0"
                    : "SELECT DataJson FROM OASISHolons WHERE ParentHolonId=@ParentHolonId AND IsDeleted=0 AND HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ParentHolonId", id);
                if (holonType != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)holonType);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var holon = Deserialize<Holon>(reader.GetString(0));
                    if (holon != null) holons.Add(holon);
                }
                result.Result = holons; result.IsError = false; result.Message = $"SQLServerDBOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading holons for parent '{id}': {ex.Message}"); }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentLegacy(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadHolonsForParentAsyncLegacy(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsyncLegacy(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsyncLegacy(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider);
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentLegacy(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadHolonsForParentAsyncLegacy(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public async Task<OASISResult<bool>> DeleteHolonWithSoftDeleteAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                string sql = softDelete
                    ? "UPDATE OASISHolons SET IsDeleted=1, ModifiedDate=SYSUTCDATETIME() WHERE Id=@Id"
                    : "DELETE FROM OASISHolons WHERE Id=@Id";
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                result.Result = rows > 0; result.IsError = !result.Result;
                result.Message = result.Result ? $"SQLServerDBOASIS: Holon '{id}' {(softDelete ? "soft" : "hard")}-deleted." : $"SQLServerDBOASIS: No holon found with ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error deleting holon '{id}': {ex.Message}"); }
            return result;
        }

        public OASISResult<bool> DeleteHolonWithSoftDelete(Guid id, bool softDelete = true) => DeleteHolonWithSoftDeleteAsync(id, softDelete).Result;

        public async Task<OASISResult<bool>> DeleteHolonWithSoftDeleteAsync(string providerKey, bool softDelete = true)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonWithSoftDeleteAsync(id, softDelete);
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public OASISResult<bool> DeleteHolonWithSoftDelete(string providerKey, bool softDelete = true) => DeleteHolonWithSoftDeleteAsync(providerKey, softDelete).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                var holons = new List<IHolon>();
                string? query = searchParams.SearchGroups?
                    .OfType<NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup>()
                    .FirstOrDefault()?.SearchQuery;
                string sql = string.IsNullOrEmpty(query)
                    ? "SELECT DataJson FROM OASISHolons WHERE IsDeleted=0"
                    : "SELECT DataJson FROM OASISHolons WHERE IsDeleted=0 AND (JSON_VALUE(DataJson,'$.name') LIKE @Q OR JSON_VALUE(DataJson,'$.description') LIKE @Q)";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(query)) cmd.Parameters.AddWithValue("@Q", $"%{query}%");
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                var sr = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count };
                result.Result = sr; result.IsError = false;
                result.Message = $"SQLServerDBOASIS: Found {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Members added to base after initial implementation ───────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                string sql = softDelete
                    ? "UPDATE OASISAvatars SET IsDeleted=1 WHERE Username=@Key OR Email=@Key"
                    : "DELETE FROM OASISAvatars WHERE Username=@Key OR Email=@Key";
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Key", providerKey);
                int rows = await cmd.ExecuteNonQueryAsync();
                result.Result = rows > 0;
                result.Message = result.Result
                    ? $"SQLServerDBOASIS: Avatar '{providerKey}' {(softDelete ? "soft" : "hard")}-deleted."
                    : $"SQLServerDBOASIS: No avatar found matching '{providerKey}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error deleting avatar '{providerKey}': {ex.Message}"); }
            return result;
        }
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Load avatar by email to get its Id, then export all holons for that Id
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
                if (avatarResult.IsError || avatarResult.Result == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Avatar with email '{avatarEmailAddress}' not found."); return result; }
                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                const string sql = "SELECT DataJson FROM OASISAvatars WHERE Email=@Email AND IsDeleted=0";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Email", avatarEmail);
                var json = (string?)await cmd.ExecuteScalarAsync();
                if (json != null) result.Result = Deserialize<Avatar>(json);
                else OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Avatar with email '{avatarEmail}' not found.");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0) { OASISErrorHandling.HandleError(ref result, "SQLServerDBOASIS: No metadata filters provided."); return result; }
            try
            {
                var conditions = metaKeyValuePairs.Select((kvp, i) =>
                    $"JSON_VALUE(DataJson,'$.metaData.{kvp.Key}') = @v{i}").ToList();
                string join = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? " OR " : " AND ";
                string sql = $"SELECT DataJson FROM OASISHolons WHERE IsDeleted=0 AND ({string.Join(join, conditions)})";
                if (type != HolonType.All) sql += " AND HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                int idx = 0;
                foreach (var kvp in metaKeyValuePairs) cmd.Parameters.AddWithValue($"@v{idx++}", kvp.Value);
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsError = false;
                result.Message = $"SQLServerDBOASIS: Loaded {holons.Count} holon(s) matching metadata filter.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                var saved = await SaveHolonsAsync(holons);
                result.Result = !saved.IsError;
                result.IsError = saved.IsError;
                result.Message = saved.Message;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = type == HolonType.All
                    ? "SELECT DataJson FROM OASISHolons WHERE ParentHolonId=@ParentHolonId AND IsDeleted=0"
                    : "SELECT DataJson FROM OASISHolons WHERE ParentHolonId=@ParentHolonId AND IsDeleted=0 AND HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ParentHolonId", id);
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsError = false;
                result.Message = $"SQLServerDBOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = type == HolonType.All
                    ? "SELECT DataJson FROM OASISHolons WHERE IsDeleted=0"
                    : "SELECT DataJson FROM OASISHolons WHERE IsDeleted=0 AND HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsError = false;
                result.Message = $"SQLServerDBOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id);
                if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No holon found with ID '{id}'."); return result; }
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand("UPDATE OASISHolons SET IsDeleted=1, ModifiedDate=SYSUTCDATETIME() WHERE Id=@Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);
                await cmd.ExecuteNonQueryAsync();
                result.Result = loaded.Result; result.IsError = false;
                result.Message = $"SQLServerDBOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonAsync(id);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

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
            result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"SQLServerDBOASIS: {saved.Count} holon(s) saved.";
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = $"SELECT DataJson FROM OASISHolons WHERE IsDeleted=0 AND JSON_VALUE(DataJson,'$.metaData.{metaKey}')=@MetaValue";
                if (type != HolonType.All) sql += " AND HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@MetaValue", metaValue);
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsError = false;
                result.Message = $"SQLServerDBOASIS: Loaded {holons.Count} holon(s) where {metaKey}={metaValue}.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                const string sql = "SELECT DataJson FROM OASISHolons WHERE IsDeleted=0 AND JSON_VALUE(DataJson,'$.avatarId')=@AvatarId";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@AvatarId", avatarId.ToString());
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsError = false;
                result.Message = $"SQLServerDBOASIS: Exported {holons.Count} holon(s) for avatar '{avatarId}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                const string avatarSql = "SELECT DataJson FROM OASISAvatars WHERE Username=@Username AND IsDeleted=0";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var avatarCmd = new SqlCommand(avatarSql, conn);
                avatarCmd.Parameters.AddWithValue("@Username", avatarUsername);
                var json = (string?)await avatarCmd.ExecuteScalarAsync();
                if (json == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Avatar '{avatarUsername}' not found."); return result; }
                var avatar = Deserialize<Avatar>(json);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Could not deserialise avatar '{avatarUsername}'."); return result; }
                return await ExportAllDataForAvatarByIdAsync(avatar.Id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
    }
}
