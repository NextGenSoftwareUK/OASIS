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
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
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
    public partial class SQLServerDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
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

        private const string CreateHolonHistoryTable = @"
IF OBJECT_ID(N'OASISHolonHistory', N'U') IS NULL
CREATE TABLE OASISHolonHistory (
    Id UNIQUEIDENTIFIER NOT NULL,
    Version INT NOT NULL CHECK (Version > 0),
    ParentHolonId UNIQUEIDENTIFIER NULL,
    HolonType INT NOT NULL,
    IsDeleted BIT NOT NULL,
    DataJson NVARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_OASISHolonHistory PRIMARY KEY (Id, Version),
    CONSTRAINT FK_OASISHolonHistory_Holon FOREIGN KEY (Id)
        REFERENCES OASISHolons(Id) ON DELETE CASCADE
);";

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
                foreach (string ddl in new[] { CreateAvatarsTable, CreateAvatarDetailsTable, CreateHolonsTable, CreateHolonHistoryTable })
                {
                    await using var cmd = new SqlCommand(ddl, conn);
                    await cmd.ExecuteNonQueryAsync();
                }
                result.Result = true;
                result.IsError = false;
                result.Message = "SQLServerDBOASIS activated — tables created/verified.";
                IsProviderActivated = true;
            }
            catch (Exception ex)
            {
                IsProviderActivated = false;
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error activating provider — {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "SQLServerDBOASIS deactivated." });
        }

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
                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException("SQL Server did not acknowledge exactly one avatar row.");

                result.Result = avatar;
                result.IsSaved = true;
                result.SavedCount = 1;
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
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar found with ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                result.Result = avatar; result.IsLoaded = true; result.LoadedCount = 1; result.Message = $"SQLServerDBOASIS: Avatar loaded for ID '{id}'.";
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
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar found with username '{username}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                result.Result = avatar; result.IsLoaded = true; result.LoadedCount = 1; result.Message = $"SQLServerDBOASIS: Avatar loaded for username '{username}'.";
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
                result.Result = avatars; result.IsLoaded = true; result.LoadedCount = avatars.Count; result.Message = $"SQLServerDBOASIS: Loaded {avatars.Count} avatar(s).";
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
                    : @"SET XACT_ABORT ON;
BEGIN TRANSACTION;
DELETE FROM OASISAvatars WHERE Id=@Id;
DECLARE @Deleted int = @@ROWCOUNT;
IF @Deleted = 1 DELETE FROM OASISAvatarDetails WHERE Id=@Id;
COMMIT TRANSACTION;
SELECT @Deleted;";
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                int rows = softDelete ? await cmd.ExecuteNonQueryAsync() : (int)(await cmd.ExecuteScalarAsync())!;
                result.Result = rows == 1; result.IsError = !result.Result;
                result.IsDeleted = result.Result; result.DeletedCount = rows;
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
                cmd.Parameters.AddWithValue("@DataJson", StoredRecordJson.Serialize(StoredRecordJson.Project<AvatarDetail>(avatarDetail)));
                if (await cmd.ExecuteNonQueryAsync() != 1)
                    throw new InvalidOperationException("SQL Server did not acknowledge exactly one avatar row.");
                result.Result = avatarDetail; result.IsSaved = true; result.SavedCount = 1; result.Message = "SQLServerDBOASIS: AvatarDetail saved.";
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
                if (scalar == null || scalar == DBNull.Value) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar detail found for ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                result.Result = StoredRecordJson.Deserialize<AvatarDetail>(scalar.ToString()!); result.IsLoaded = true; result.LoadedCount = 1; result.Message = $"SQLServerDBOASIS: AvatarDetail loaded for ID '{id}'.";
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
                if (scalar == null || scalar == DBNull.Value) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar detail found for username '{username}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                result.Result = StoredRecordJson.Deserialize<AvatarDetail>(scalar.ToString()!); result.IsLoaded = true; result.LoadedCount = 1; result.Message = $"SQLServerDBOASIS: AvatarDetail loaded for username '{username}'.";
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
                if (scalar == null || scalar == DBNull.Value) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar detail found for email '{email}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                result.Result = StoredRecordJson.Deserialize<AvatarDetail>(scalar.ToString()!); result.IsLoaded = true; result.LoadedCount = 1; result.Message = $"SQLServerDBOASIS: AvatarDetail loaded for email '{email}'.";
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
                    var detail = StoredRecordJson.Deserialize<AvatarDetail>(reader.GetString(0));
                    if (detail != null) details.Add(detail);
                }
                result.Result = details; result.IsLoaded = true; result.LoadedCount = details.Count; result.Message = $"SQLServerDBOASIS: Loaded {details.Count} avatar detail(s).";
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
                var graph = HolonGraphWritePlan.CreateForSave(new[] { holon }, saveChildren, recursive, maxChildDepth, 0);

                const string sql = @"
MERGE OASISHolons AS target
USING (SELECT @Id AS Id) AS source ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET ParentHolonId=@ParentHolonId, HolonType=@HolonType, IsDeleted=@IsDeleted, ModifiedDate=SYSUTCDATETIME(), DataJson=@DataJson
WHEN NOT MATCHED THEN
    INSERT (Id,ParentHolonId,HolonType,IsDeleted,DataJson) VALUES (@Id,@ParentHolonId,@HolonType,@IsDeleted,@DataJson);";

                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();
                foreach (var node in graph.Nodes)
                {
                    var row = node.Snapshot;
                    row.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType, string>();
                    row.ProviderUniqueStorageKey[Core.Enums.ProviderType.SQLServerDBOASIS] = row.Id.ToString("D");
                    const string archiveSql = @"
INSERT INTO OASISHolonHistory (Id, Version, ParentHolonId, HolonType, IsDeleted, DataJson)
SELECT h.Id, v.Version, h.ParentHolonId, h.HolonType, h.IsDeleted, h.DataJson
FROM OASISHolons h WITH (UPDLOCK, HOLDLOCK)
CROSS APPLY (VALUES (TRY_CONVERT(int, JSON_VALUE(h.DataJson, '$.version')))) v(Version)
WHERE h.Id = @Id AND v.Version > 0 AND v.Version < @Version
AND NOT EXISTS (SELECT 1 FROM OASISHolonHistory a WHERE a.Id = h.Id AND a.Version = v.Version);";
                    await using var archive = new SqlCommand(archiveSql, conn, transaction);
                    archive.Parameters.AddWithValue("@Id", row.Id);
                    archive.Parameters.AddWithValue("@Version", row.Version);
                    await archive.ExecuteNonQueryAsync();

                    await using var cmd = new SqlCommand(sql, conn, transaction);
                    cmd.Parameters.AddWithValue("@Id", row.Id);
                    cmd.Parameters.AddWithValue("@ParentHolonId", row.ParentHolonId == Guid.Empty ? (object)DBNull.Value : row.ParentHolonId);
                    cmd.Parameters.AddWithValue("@HolonType", (int)row.HolonType);
                    cmd.Parameters.AddWithValue("@IsDeleted", row.IsDeleted);
                    cmd.Parameters.AddWithValue("@DataJson", Serialize(row));
                    if (await cmd.ExecuteNonQueryAsync() != 1)
                        throw new InvalidOperationException("SQL Server holon upsert must acknowledge exactly one row.");
                }
                await transaction.CommitAsync();

                foreach (var node in graph.Nodes)
                {
                    node.Source.Id = node.Snapshot.Id;
                    node.Source.ParentHolonId = node.Snapshot.ParentHolonId;
                    node.Source.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType, string>();
                    node.Source.ProviderUniqueStorageKey[Core.Enums.ProviderType.SQLServerDBOASIS] = node.Snapshot.Id.ToString("D");
                }
                result.Result = holon;
                result.IsSaved = true;
                result.SavedCount = graph.Nodes.Count;
                result.Message = $"SQLServerDBOASIS: {graph.Nodes.Count} holon(s) saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error saving holon '{holon.Name}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, 0, continueOnError, saveChildrenOnProvider);

        public OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private async Task<Holon?> LoadHolonVersionAsync(Guid id, int version)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(version);
            const string sql = @"
SELECT CASE WHEN @Version = 0
              OR TRY_CONVERT(int, JSON_VALUE(h.DataJson, '$.version')) = @Version
            THEN h.DataJson ELSE a.DataJson END
FROM OASISHolons h
LEFT JOIN OASISHolonHistory a ON a.Id = h.Id AND a.Version = @Version AND a.IsDeleted = 0
WHERE h.Id = @Id AND h.IsDeleted = 0;";
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync().ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Version", version);
            var scalar = await cmd.ExecuteScalarAsync().ConfigureAwait(false);
            if (scalar is not string json)
                return null;

            var holon = Deserialize<Holon>(json);
            if (holon == null || holon.Id != id || (version != 0 && holon.Version != version))
                throw new System.IO.InvalidDataException("SQL Server returned an inconsistent holon snapshot.");

            return holon;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holon = await LoadHolonVersionAsync(id, version).ConfigureAwait(false);
                if (holon == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No holon found with ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                result.Result = holon; result.IsLoaded = true; result.LoadedCount = 1; result.Message = $"SQLServerDBOASIS: Holon loaded for ID '{id}'.";
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
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count; result.Message = $"SQLServerDBOASIS: Loaded {holons.Count} holon(s).";
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
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count; result.Message = $"SQLServerDBOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
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
                    ? "UPDATE OASISHolons SET IsDeleted=1, ModifiedDate=SYSUTCDATETIME() WHERE Id=@Id AND IsDeleted=0"
                    : "DELETE FROM OASISHolons WHERE Id=@Id";
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                int rows = await cmd.ExecuteNonQueryAsync();
                result.Result = rows == 1; result.IsError = !result.Result;
                result.IsDeleted = result.Result; result.DeletedCount = rows;
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
                    : "SELECT DataJson FROM OASISHolons WHERE IsDeleted=0 AND (CHARINDEX(@Q, JSON_VALUE(DataJson,'$.name')) > 0 OR CHARINDEX(@Q, JSON_VALUE(DataJson,'$.description')) > 0)";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                if (!string.IsNullOrEmpty(query)) cmd.Parameters.AddWithValue("@Q", query);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                var sr = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count };
                result.Result = sr; result.IsLoaded = true; result.LoadedCount = holons.Count;
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
                var avatar = await LoadAvatarByColumnAsync("Email", avatarEmail);
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Avatar with email '{avatarEmail}' not found.");
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    return result;
                }

                result.Result = avatar;
                result.IsLoaded = true;
                result.LoadedCount = 1;
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
                if (version != 0)
                    throw new NotSupportedException("SQL Server metadata queries currently support only the current record version.");

                var filters = metaKeyValuePairs.ToArray();
                var conditions = filters.Select((_, i) =>
                    $"JSON_VALUE(DataJson,@path{i}) = @v{i}").ToList();
                string join = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? " OR " : " AND ";
                string sql = $"SELECT DataJson FROM OASISHolons WHERE IsDeleted=0 AND ({string.Join(join, conditions)})";
                if (type != HolonType.All) sql += " AND HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                for (var i = 0; i < filters.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@path{i}", "$.metaData." + System.Text.Json.JsonSerializer.Serialize(filters[i].Key));
                    cmd.Parameters.AddWithValue($"@v{i}", filters[i].Value);
                }
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count;
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
                result.IsSaved = saved.IsSaved;
                result.SavedCount = saved.SavedCount;
                result.Exception = saved.Exception;
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
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count;
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
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count;
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
                if (loaded.IsError || loaded.Result == null) return loaded;
                var deleted = await DeleteHolonWithSoftDeleteAsync(id);
                result.IsError = deleted.IsError;
                result.Exception = deleted.Exception;
                result.IsDeleted = deleted.IsDeleted;
                result.DeletedCount = deleted.DeletedCount;
                result.Result = deleted.IsDeleted ? loaded.Result : null;
                result.Message = deleted.Message;
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
                if (r.IsError || !r.IsSaved || r.Result == null)
                {
                    errors.Add(r.Message ?? "SQL Server did not acknowledge the holon save.");
                    result.Exception ??= r.Exception;
                    if (!continueOnError) break;
                }
                else
                {
                    saved.Add(r.Result);
                    result.SavedCount += r.SavedCount;
                }
            }
            result.Result = saved; result.IsError = errors.Count > 0;
            result.IsSaved = !result.IsError && result.SavedCount > 0;
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
                if (version != 0)
                    throw new NotSupportedException("SQL Server metadata queries currently support only the current record version.");

                string sql = $"SELECT DataJson FROM OASISHolons WHERE IsDeleted=0 AND JSON_VALUE(DataJson,@MetaPath)=@MetaValue";
                if (type != HolonType.All) sql += " AND HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@MetaPath", "$.metaData." + System.Text.Json.JsonSerializer.Serialize(metaKey));
                cmd.Parameters.AddWithValue("@MetaValue", metaValue);
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count;
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
                const string sql = "SELECT DataJson FROM OASISHolons WHERE IsDeleted=0 AND TRY_CONVERT(uniqueidentifier, JSON_VALUE(DataJson,'$.createdByAvatarId'))=@AvatarId";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.Add("@AvatarId", System.Data.SqlDbType.UniqueIdentifier).Value = avatarId;
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = Deserialize<Holon>(reader.GetString(0)); if (h != null) holons.Add(h); }
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count;
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
