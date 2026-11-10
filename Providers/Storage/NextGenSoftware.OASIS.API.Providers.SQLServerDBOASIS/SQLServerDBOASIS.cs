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

        private const string CreateAvatarHistoryTables = @"
IF OBJECT_ID(N'OASISAvatarHistory', N'U') IS NULL
CREATE TABLE OASISAvatarHistory (
    Id UNIQUEIDENTIFIER NOT NULL,
    Version INT NOT NULL CHECK (Version > 0),
    Username NVARCHAR(256) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    IsDeleted BIT NOT NULL,
    DataJson NVARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_OASISAvatarHistory PRIMARY KEY (Id, Version),
    CONSTRAINT FK_OASISAvatarHistory_Avatar FOREIGN KEY (Id)
        REFERENCES OASISAvatars(Id) ON DELETE CASCADE
);
IF OBJECT_ID(N'OASISAvatarDetailHistory', N'U') IS NULL
CREATE TABLE OASISAvatarDetailHistory (
    Id UNIQUEIDENTIFIER NOT NULL,
    Version INT NOT NULL CHECK (Version > 0),
    Username NVARCHAR(256) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    DataJson NVARCHAR(MAX) NOT NULL,
    CONSTRAINT PK_OASISAvatarDetailHistory PRIMARY KEY (Id, Version),
    CONSTRAINT FK_OASISAvatarDetailHistory_AvatarDetail FOREIGN KEY (Id)
        REFERENCES OASISAvatarDetails(Id) ON DELETE CASCADE
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
                foreach (string ddl in new[] { CreateAvatarsTable, CreateAvatarDetailsTable, CreateHolonsTable, CreateHolonHistoryTable, CreateAvatarHistoryTables })
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
                ArgumentOutOfRangeException.ThrowIfNegative(avatar.Version);
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null)
                    avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.SQLServerDBOASIS] = avatar.Id.ToString();

                const string sql = @"
INSERT INTO OASISAvatarHistory (Id, Version, Username, Email,IsDeleted, DataJson)
SELECT c.Id, v.Version, c.Username, c.Email,c.IsDeleted, c.DataJson
FROM OASISAvatars c WITH (UPDLOCK, HOLDLOCK)
CROSS APPLY (VALUES (TRY_CONVERT(int, JSON_VALUE(c.DataJson, '$.version')))) v(Version)
WHERE c.Id=@Id AND v.Version > 0 AND v.Version < @Version
AND NOT EXISTS (SELECT 1 FROM OASISAvatarHistory h WHERE h.Id=c.Id AND h.Version=v.Version);
MERGE OASISAvatars AS target
USING (SELECT @Id AS Id) AS source ON target.Id = source.Id
WHEN MATCHED AND COALESCE(TRY_CONVERT(int, JSON_VALUE(target.DataJson, '$.version')), 0) <= @Version THEN
    UPDATE SET Username=@Username, Email=@Email, IsDeleted=@IsDeleted, ModifiedDate=SYSUTCDATETIME(), DataJson=@DataJson
WHEN NOT MATCHED THEN
    INSERT (Id,Username,Email,IsDeleted,DataJson) VALUES (@Id,@Username,@Email,@IsDeleted,@DataJson); SELECT @@ROWCOUNT;";

                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();
                await using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@Id", avatar.Id);
                cmd.Parameters.AddWithValue("@Version", avatar.Version);
                cmd.Parameters.AddWithValue("@Username", (object?)avatar.Username ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object?)avatar.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@IsDeleted", avatar.IsDeleted);
                cmd.Parameters.AddWithValue("@DataJson", Serialize(avatar));
                if ((int)(await cmd.ExecuteScalarAsync())! != 1)
                    throw new InvalidOperationException("SQL Server did not acknowledge exactly one avatar row.");

                await transaction.CommitAsync();
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

        private const string AvatarSnapshotRows = @"
SELECT c.Id, c.Username, c.Email, c.DataJson FROM OASISAvatars c
WHERE c.IsDeleted=0 AND (@Version=0 OR TRY_CONVERT(int, JSON_VALUE(c.DataJson, '$.version'))=@Version)
UNION ALL
SELECT h.Id, h.Username, h.Email, h.DataJson FROM OASISAvatarHistory h
JOIN OASISAvatars c ON c.Id=h.Id
WHERE h.Version=@Version AND h.IsDeleted=0 AND c.IsDeleted=0";

        private const string AvatarDetailSnapshotRows = @"
SELECT c.Id, c.Username, c.Email, c.DataJson FROM OASISAvatarDetails c
WHERE (@Version=0 OR TRY_CONVERT(int, JSON_VALUE(c.DataJson, '$.Version'))=@Version)
AND NOT EXISTS (SELECT 1 FROM OASISAvatars a WHERE a.Id=c.Id AND a.IsDeleted=1)
UNION ALL
SELECT h.Id, h.Username, h.Email, h.DataJson FROM OASISAvatarDetailHistory h
WHERE h.Version=@Version
AND NOT EXISTS (SELECT 1 FROM OASISAvatars a WHERE a.Id=h.Id AND a.IsDeleted=1)";

        private async Task<Avatar?> LoadAvatarByColumnAsync(string column, object value, int version)
        {
            string sql = $"SELECT TOP (2) Id, DataJson FROM ({AvatarSnapshotRows}) snapshot WHERE {column}=@val";
            ArgumentOutOfRangeException.ThrowIfNegative(version);
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@val", value);
            cmd.Parameters.AddWithValue("@Version", version);
            await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
            if (!await reader.ReadAsync().ConfigureAwait(false))
                return null;

            var id = reader.GetGuid(0);
            var json = reader.GetString(1);
            if (await reader.ReadAsync().ConfigureAwait(false))
                throw new StorageIdentityAmbiguousException();

            var avatar = Deserialize<Avatar>(json);
            if (avatar == null || avatar.Id != id)
                throw new System.IO.InvalidDataException("SQL Server avatar payload does not match its row identity.");

            return avatar;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await LoadAvatarByColumnAsync("Id", id, version);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar found with ID '{id}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                result.Result = avatar; result.IsLoaded = true; result.LoadedCount = 1; result.Message = $"SQLServerDBOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading avatar by ID '{id}': {ex.Message}"); if (ex is StorageIdentityAmbiguousException) result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity; }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await LoadAvatarByColumnAsync("Username", username, version);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar found with username '{username}'."); result.ErrorCode = OASISResultErrorCodes.RecordNotFound; return result; }
                result.Result = avatar; result.IsLoaded = true; result.LoadedCount = 1; result.Message = $"SQLServerDBOASIS: Avatar loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading avatar by username '{username}': {ex.Message}"); if (ex is StorageIdentityAmbiguousException) result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity; }
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
                ArgumentOutOfRangeException.ThrowIfNegative(version);
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand($"SELECT Id, DataJson FROM ({AvatarSnapshotRows}) snapshot", conn);
                cmd.Parameters.AddWithValue("@Version", version);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var avatar = Deserialize<Avatar>(reader.GetString(1));
                    if (avatar == null || avatar.Id != reader.GetGuid(0))
                        throw new System.IO.InvalidDataException("SQL Server avatar payload does not match its row identity.");
                    avatars.Add(avatar);
                }
                result.Result = avatars; result.IsLoaded = true; result.LoadedCount = avatars.Count; result.Message = $"SQLServerDBOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => DeleteAvatarMatchingAsync("Id=@Value", id, softDelete);

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
            => DeleteAvatarMatchingAsync("Username=@Value", username, softDelete);

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
            => DeleteAvatarMatchingAsync("Email=@Value", email, softDelete);

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) => DeleteAvatarByEmailAsync(email, softDelete).Result;

        private async Task<OASISResult<bool>> DeleteAvatarMatchingAsync(string predicate, object value, bool softDelete)
        {
            var result = new OASISResult<bool>();
            try
            {
                string mutation = softDelete
                    ? "UPDATE OASISAvatars SET IsDeleted=1, ModifiedDate=SYSUTCDATETIME() WHERE Id=(SELECT Id FROM @Matches);"
                    : @"DELETE FROM OASISAvatars WHERE Id=(SELECT Id FROM @Matches);
DELETE FROM OASISAvatarDetails WHERE Id=(SELECT Id FROM @Matches);";
                string activeFilter = softDelete ? "AND IsDeleted=0" : "";
                string sql = $@"SET XACT_ABORT ON;
BEGIN TRANSACTION;
DECLARE @Matches TABLE (Id uniqueidentifier);
INSERT INTO @Matches SELECT TOP (2) Id FROM OASISAvatars WITH (UPDLOCK, HOLDLOCK)
WHERE ({predicate}) {activeFilter};
DECLARE @Count int = @@ROWCOUNT;
IF @Count = 1
BEGIN
    {mutation}
END;
COMMIT TRANSACTION;
SELECT @Count;";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Value", value);
                int matches = (int)(await cmd.ExecuteScalarAsync())!;
                if (matches > 1)
                    throw new StorageIdentityAmbiguousException();

                result.Result = matches == 1;
                result.IsDeleted = result.Result;
                result.DeletedCount = matches;
                result.IsError = !result.Result;
                result.Message = result.Result
                    ? $"SQLServerDBOASIS: Avatar '{value}' {(softDelete ? "soft" : "hard")}-deleted."
                    : $"SQLServerDBOASIS: No avatar found matching '{value}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error deleting avatar '{value}': {ex.Message}");
                if (ex is StorageIdentityAmbiguousException)
                    result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity;
            }
            return result;
        }

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                ArgumentOutOfRangeException.ThrowIfNegative(avatarDetail.Version);
                if (avatarDetail.Id == Guid.Empty) avatarDetail.Id = Guid.NewGuid();
                const string sql = @"
INSERT INTO OASISAvatarDetailHistory (Id, Version, Username, Email, DataJson)
SELECT c.Id, v.Version, c.Username, c.Email, c.DataJson
FROM OASISAvatarDetails c WITH (UPDLOCK, HOLDLOCK)
CROSS APPLY (VALUES (TRY_CONVERT(int, JSON_VALUE(c.DataJson, '$.Version')))) v(Version)
WHERE c.Id=@Id AND v.Version > 0 AND v.Version < @Version
AND NOT EXISTS (SELECT 1 FROM OASISAvatarDetailHistory h WHERE h.Id=c.Id AND h.Version=v.Version);
MERGE OASISAvatarDetails AS target
USING (SELECT @Id AS Id) AS source ON target.Id = source.Id
WHEN MATCHED AND COALESCE(TRY_CONVERT(int, JSON_VALUE(target.DataJson, '$.Version')), 0) <= @Version THEN UPDATE SET Username=@Username, Email=@Email, DataJson=@DataJson
WHEN NOT MATCHED THEN INSERT (Id,Username,Email,DataJson) VALUES (@Id,@Username,@Email,@DataJson); SELECT @@ROWCOUNT;";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var transaction = (SqlTransaction)await conn.BeginTransactionAsync();
                await using var cmd = new SqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@Id", avatarDetail.Id);
                cmd.Parameters.AddWithValue("@Version", avatarDetail.Version);
                cmd.Parameters.AddWithValue("@Username", (object?)avatarDetail.Username ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Email", (object?)avatarDetail.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DataJson", StoredRecordJson.Serialize(StoredRecordJson.Project<AvatarDetail>(avatarDetail)));
                if ((int)(await cmd.ExecuteScalarAsync())! != 1)
                    throw new InvalidOperationException("SQL Server did not acknowledge exactly one avatar row.");
                await transaction.CommitAsync();
                result.Result = avatarDetail; result.IsSaved = true; result.SavedCount = 1; result.Message = "SQLServerDBOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
            => LoadAvatarDetailMatchingAsync("Id", id, version);

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
            => LoadAvatarDetailMatchingAsync("Username", username, version);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
            => LoadAvatarDetailMatchingAsync("Email", email, version);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        private async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailMatchingAsync(string column, object value, int version)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                ArgumentOutOfRangeException.ThrowIfNegative(version);
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand($"SELECT TOP (2) Id, DataJson FROM ({AvatarDetailSnapshotRows}) snapshot WHERE {column}=@Value", conn);
                cmd.Parameters.AddWithValue("@Value", value);
                cmd.Parameters.AddWithValue("@Version", version);
                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: No avatar detail found for {column} '{value}'.");
                    result.ErrorCode = OASISResultErrorCodes.RecordNotFound;
                    return result;
                }

                var id = reader.GetGuid(0);
                var json = reader.GetString(1);
                if (await reader.ReadAsync())
                    throw new StorageIdentityAmbiguousException();

                var detail = StoredRecordJson.Deserialize<AvatarDetail>(json);
                if (detail == null || detail.Id != id)
                    throw new InvalidOperationException("Stored avatar detail does not match its SQL Server row identity.");

                result.Result = detail;
                result.IsLoaded = true;
                result.LoadedCount = 1;
                result.Message = $"SQLServerDBOASIS: Avatar detail loaded for {column} '{value}'.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: Error loading avatar detail for {column} '{value}': {ex.Message}");
                if (ex is StorageIdentityAmbiguousException)
                    result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity;
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var details = new List<IAvatarDetail>();
                ArgumentOutOfRangeException.ThrowIfNegative(version);
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand($"SELECT Id, DataJson FROM ({AvatarDetailSnapshotRows}) snapshot", conn);
                cmd.Parameters.AddWithValue("@Version", version);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var detail = StoredRecordJson.Deserialize<AvatarDetail>(reader.GetString(1));
                    if (detail == null || detail.Id != reader.GetGuid(0))
                        throw new System.IO.InvalidDataException("SQL Server avatar detail payload does not match its row identity.");
                    details.Add(detail);
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
WHEN MATCHED AND COALESCE(TRY_CONVERT(int, JSON_VALUE(target.DataJson, '$.version')), 0) <= @Version THEN
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
                    cmd.Parameters.AddWithValue("@Version", row.Version);
                    cmd.Parameters.AddWithValue("@ParentHolonId", row.ParentHolonId == Guid.Empty ? (object)DBNull.Value : row.ParentHolonId);
                    cmd.Parameters.AddWithValue("@HolonType", (int)row.HolonType);
                    cmd.Parameters.AddWithValue("@IsDeleted", row.IsDeleted);
                    cmd.Parameters.AddWithValue("@DataJson", Serialize(row));
                    if (await cmd.ExecuteNonQueryAsync() != 1)
                        throw new InvalidOperationException("SQL Server holon upsert did not acknowledge exactly one row. The current version may be newer than the proposed version.");
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

        private const string VersionedHolonRows = @"
SELECT h.Id, snapshot.DataJson
FROM OASISHolons h
LEFT JOIN OASISHolonHistory a ON a.Id = h.Id AND a.Version = @Version AND a.IsDeleted = 0
CROSS APPLY (VALUES (
    CASE WHEN @Version = 0 OR TRY_CONVERT(int, JSON_VALUE(h.DataJson, '$.version')) = @Version
         THEN h.DataJson ELSE a.DataJson END,
    CASE WHEN @Version = 0 OR TRY_CONVERT(int, JSON_VALUE(h.DataJson, '$.version')) = @Version
         THEN h.HolonType ELSE a.HolonType END,
    CASE WHEN @Version = 0 OR TRY_CONVERT(int, JSON_VALUE(h.DataJson, '$.version')) = @Version
         THEN h.ParentHolonId ELSE a.ParentHolonId END
)) snapshot(DataJson, HolonType, ParentHolonId)
WHERE h.IsDeleted = 0 AND snapshot.DataJson IS NOT NULL";

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

            return ReadHolonSnapshot(id, json, version);
        }

        private static Holon ReadHolonSnapshot(Guid id, string json, int version)
        {
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

        public Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsyncLegacy(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

        public OASISResult<IEnumerable<IHolon>> LoadAllHolonsLegacy(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadAllHolonsAsyncLegacy(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsyncLegacy(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);

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
                    : "DELETE FROM OASISHolons WHERE Id=@Id; SELECT @@ROWCOUNT;";
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                int rows = softDelete ? await cmd.ExecuteNonQueryAsync() : (int)(await cmd.ExecuteScalarAsync())!;
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
                ArgumentOutOfRangeException.ThrowIfNegative(version);
                var holons = new List<IHolon>();
                string? query = searchParams.SearchGroups?
                    .OfType<NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup>()
                    .FirstOrDefault()?.SearchQuery;
                string sql = string.IsNullOrEmpty(query)
                    ? VersionedHolonRows
                    : VersionedHolonRows + " AND (CHARINDEX(@Q, JSON_VALUE(snapshot.DataJson,'$.name')) > 0 OR CHARINDEX(@Q, JSON_VALUE(snapshot.DataJson,'$.description')) > 0)";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Version", version);
                if (!string.IsNullOrEmpty(query)) cmd.Parameters.AddWithValue("@Q", query);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    holons.Add(ReadHolonSnapshot(reader.GetGuid(0), reader.GetString(1), version));
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

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            if (Guid.TryParse(providerKey, out var id))
                return DeleteAvatarAsync(id, softDelete);

            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, $"SQLServerDBOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return Task.FromResult(result);
        }
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Load avatar by email to get its Id, then export all holons for that Id
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
                if (avatarResult.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar, IEnumerable<IHolon>>(avatarResult);
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
                var avatar = await LoadAvatarByColumnAsync("Email", avatarEmail, version);
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
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); if (ex is StorageIdentityAmbiguousException) result.ErrorCode = OASISResultErrorCodes.AmbiguousIdentity; }
            return result;
        }
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            if (metaKeyValuePairs == null || metaKeyValuePairs.Count == 0) { OASISErrorHandling.HandleError(ref result, "SQLServerDBOASIS: No metadata filters provided."); return result; }
            try
            {
                ArgumentOutOfRangeException.ThrowIfNegative(version);

                var filters = metaKeyValuePairs.ToArray();
                var conditions = filters.Select((_, i) =>
                    $"JSON_VALUE(snapshot.DataJson,@path{i}) = @v{i}").ToList();
                string join = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? " OR " : " AND ";
                string sql = $"{VersionedHolonRows} AND ({string.Join(join, conditions)})";
                if (type != HolonType.All) sql += " AND snapshot.HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Version", version);
                for (var i = 0; i < filters.Length; i++)
                {
                    cmd.Parameters.AddWithValue($"@path{i}", "$.metaData." + System.Text.Json.JsonSerializer.Serialize(filters[i].Key));
                    cmd.Parameters.AddWithValue($"@v{i}", filters[i].Value);
                }
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    holons.Add(ReadHolonSnapshot(reader.GetGuid(0), reader.GetString(1), version));
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
                    ? VersionedHolonRows + " AND snapshot.ParentHolonId=@ParentHolonId"
                    : VersionedHolonRows + " AND snapshot.ParentHolonId=@ParentHolonId AND snapshot.HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Version", version);
                cmd.Parameters.AddWithValue("@ParentHolonId", id);
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)type);
                ArgumentOutOfRangeException.ThrowIfNegative(version);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    holons.Add(ReadHolonSnapshot(reader.GetGuid(0), reader.GetString(1), version));
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
                    ? VersionedHolonRows
                    : VersionedHolonRows + " AND snapshot.HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Version", version);
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)type);
                ArgumentOutOfRangeException.ThrowIfNegative(version);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    holons.Add(ReadHolonSnapshot(reader.GetGuid(0), reader.GetString(1), version));
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
                ArgumentOutOfRangeException.ThrowIfNegative(version);

                string sql = $"{VersionedHolonRows} AND JSON_VALUE(snapshot.DataJson,@MetaPath)=@MetaValue";
                if (type != HolonType.All) sql += " AND snapshot.HolonType=@HolonType";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Version", version);
                cmd.Parameters.AddWithValue("@MetaPath", "$.metaData." + System.Text.Json.JsonSerializer.Serialize(metaKey));
                cmd.Parameters.AddWithValue("@MetaValue", metaValue);
                if (type != HolonType.All) cmd.Parameters.AddWithValue("@HolonType", (int)type);
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    holons.Add(ReadHolonSnapshot(reader.GetGuid(0), reader.GetString(1), version));
                result.Result = holons; result.IsLoaded = true; result.LoadedCount = holons.Count;
                result.Message = $"SQLServerDBOASIS: Loaded {holons.Count} holon(s) where {metaKey}={metaValue}.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => LoadAllHolonsAsync(version: version);
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                ArgumentOutOfRangeException.ThrowIfNegative(version);
                const string sql = VersionedHolonRows + " AND TRY_CONVERT(uniqueidentifier, JSON_VALUE(snapshot.DataJson,'$.createdByAvatarId'))=@AvatarId";
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Version", version);
                cmd.Parameters.Add("@AvatarId", System.Data.SqlDbType.UniqueIdentifier).Value = avatarId;
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    holons.Add(ReadHolonSnapshot(reader.GetGuid(0), reader.GetString(1), version));
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
                var avatar = await LoadAvatarByUsernameAsync(avatarUsername).ConfigureAwait(false);
                if (avatar.IsError)
                    return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar, IEnumerable<IHolon>>(avatar);
                return await ExportAllDataForAvatarByIdAsync(avatar.Result.Id, version).ConfigureAwait(false);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
    }
}
