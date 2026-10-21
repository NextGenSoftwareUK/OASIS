using System;
using System.Collections.Generic;
using System.Linq;
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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

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
    public partial class PostgreSQLOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _connectionString;

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
                IsProviderActivated=true;
            }
            catch (Exception ex)
            {
                IsProviderActivated=false;
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error activating provider — {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated=false;
            return Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "PostgreSQLOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private static string Serialize(object obj) => Core.Serialization.StoredRecordJson.Serialize(obj);
        private static T? Deserialize<T>(string json) where T:class,IHolonBase => ReadStoredRecordAsync<T>(json,0).GetAwaiter().GetResult();

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var source=avatar;
                var publication=StoredRecordPublicationSnapshot<IAvatar>.Capture(source);
                avatar=publication.Proposal;
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null)
                    avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[ProviderType.Value] = avatar.Id.ToString();

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
                publication.ValidateUnchanged();
                avatar=await ExecuteRecordWriteAsync(cmd,"oasis_avatars",avatar,publication.ValidateUnchanged);
                PublishAcknowledgedIdentity(source,avatar,publication);
                result.IsSaved=true;result.SavedCount=1;
                result.Result = source;
                result.IsError = false;
                result.Message = $"PostgreSQLOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex)
            {
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error saving avatar '{avatar?.Username}': {ex.Message}");
                if(IsLifecycleConflict(ex))result.ErrorCode=StorageLifecycleConflictException.ErrorCode;
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private async Task<Avatar?> LoadAvatarByIdAsync(Guid id,int version=0)
        {
            if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
            if(id==Guid.Empty)throw new ArgumentException("A nonempty avatar identity is required.",nameof(id));
            const string sql = "SELECT data_json FROM oasis_avatars WHERE id=@val AND is_deleted=FALSE LIMIT 1";
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@val", id);
            var scalar = await cmd.ExecuteScalarAsync();
            if (scalar == null) return null;
            return await ReadVisibleAvatarRecordAsync<Avatar>(id,scalar.ToString()!,version);
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await LoadAvatarByIdAsync(id,version);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: No avatar found with ID '{id}'.");result.ErrorCode=OASISResultErrorCodes.RecordNotFound;return result; }
                result.Result = avatar; result.IsLoaded=true;result.LoadedCount=1;result.IsError = false; result.Message = $"PostgreSQLOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading avatar by ID '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
            => await StoredRecordIdentityLookup.SelectAsync(username,version,LoadAllAvatarsAsync,row=>row.Username);

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
            => await StoredRecordIdentityLookup.SelectAsync(avatarEmail,version,LoadAllAvatarsAsync,row=>row.Email);

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
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                var avatars = new List<IAvatar>();
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT id,data_json FROM oasis_avatars WHERE is_deleted=FALSE", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var a = await ReadVisibleAvatarRecordAsync<Avatar>(reader.GetGuid(0),reader.GetString(1),version);
                    if (a != null) avatars.Add(a);
                }
                result.Result = avatars; result.IsLoaded=true;result.LoadedCount=avatars.Count;result.IsError = false; result.Message = $"PostgreSQLOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            =>await DeleteAvatarRecordAsync(id,null,null,softDelete);

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
            =>await DeleteAvatarRecordAsync(Guid.Empty,username,false,softDelete);

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
            =>await DeleteAvatarRecordAsync(Guid.Empty,email,true,softDelete);

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) => DeleteAvatarByEmailAsync(email, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            if (Guid.TryParse(providerKey, out Guid id)&&id!=Guid.Empty) return await DeleteAvatarAsync(id, softDelete);
            var result=new OASISResult<bool>(false);var cause=new ArgumentException("A nonempty PostgreSQL avatar GUID key is required.",nameof(providerKey));
            OASISErrorHandling.HandleError(ref result,cause.Message,cause);result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var source=avatarDetail;
                var publication=StoredRecordPublicationSnapshot<IAvatarDetail>.Capture(source);
                avatarDetail=publication.Proposal;
                if (avatarDetail.Id == Guid.Empty) avatarDetail.Id = Guid.NewGuid();
                avatarDetail.ProviderUniqueStorageKey??=new();
                avatarDetail.ProviderUniqueStorageKey[ProviderType.Value]=avatarDetail.Id.ToString();
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
                publication.ValidateUnchanged();
                avatarDetail=await ExecuteRecordWriteAsync(cmd,"oasis_avatar_details",avatarDetail,publication.ValidateUnchanged);
                PublishAcknowledgedIdentity(source,avatarDetail,publication);
                result.Result = source; result.IsSaved=true;result.SavedCount=1;result.IsError = false; result.Message = "PostgreSQLOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error saving avatar detail: {ex.Message}");if(IsLifecycleConflict(ex))result.ErrorCode=StorageLifecycleConflictException.ErrorCode; }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_avatar_details WHERE id=@id LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@id", id);
                var scalar = await cmd.ExecuteScalarAsync();
                if (scalar == null) { OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: No avatar detail found for ID '{id}'.");result.ErrorCode=OASISResultErrorCodes.RecordNotFound;return result; }
                result.Result = await ReadVisibleAvatarRecordAsync<AvatarDetail>(id,scalar.ToString()!,version);
                if(result.Result==null){result.IsError=true;result.ErrorCode=OASISResultErrorCodes.RecordNotFound;result.Message="Requested avatar-detail version was not found.";return result;}result.IsLoaded=true;
                result.IsError = false;
                result.Message = $"PostgreSQLOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading avatar detail for '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
            => await StoredRecordIdentityLookup.SelectAsync(username,version,LoadAllAvatarDetailsAsync,row=>row.Username);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
            => await StoredRecordIdentityLookup.SelectAsync(email,version,LoadAllAvatarDetailsAsync,row=>row.Email);

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                var details = new List<IAvatarDetail>();
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand("SELECT id,data_json FROM oasis_avatar_details", conn);
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var d = await ReadVisibleAvatarRecordAsync<AvatarDetail>(reader.GetGuid(0),reader.GetString(1),version);
                    if (d != null) details.Add(d);
                }
                result.Result = details; result.IsLoaded=true;result.LoadedCount=details.Count;result.IsError = false; result.Message = $"PostgreSQLOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: Error loading all avatar details: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var graph=await WriteSelectedGraphAsync(new[]{holon},saveChildren,recursive,maxChildDepth,0,continueOnError);
            var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,IHolon>(graph);
            if(!graph.IsError)result.Result=holon;
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            =>await WriteSelectedGraphAsync(holons,saveChildren,recursive,maxChildDepth,curentChildDepth,continueOnError);

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        private async Task<Holon?> LoadHolonByIdAsync(Guid id,int version=0)
        {
            if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_holons WHERE id=@id AND is_deleted=FALSE LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@id", id);
            var scalar = await cmd.ExecuteScalarAsync();
            if (scalar == null) return null;
            return await ReadKeyedStoredRecordAsync<Holon>(id,scalar.ToString()!,version);
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if(maxChildDepth<0)throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                var holon = await LoadHolonByIdAsync(id,version);
                if (holon == null) { OASISErrorHandling.HandleError(ref result, $"PostgreSQLOASIS: No holon found with ID '{id}'.");result.ErrorCode=OASISResultErrorCodes.RecordNotFound;return result; }
                var pool=loadChildren&&loadChildrenFromProvider?await ReadHolonRowsAsync(0):new List<IHolon>{holon};
                var project=HolonGraphProjection.Create(pool,loadChildren,recursive,maxChildDepth,0,loadChildrenFromProvider,
                    row=>Core.Serialization.StoredRecordJson.DeserializeStored<Holon>(Core.Serialization.StoredRecordJson.Serialize(row)));
                result.Result = project(holon);result.IsLoaded=true;result.LoadedCount=1;result.IsError = false; result.Message = $"PostgreSQLOASIS: Holon loaded for ID '{id}'.";
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
            => await QueryStoredHolonsAsync(type,null,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await QueryStoredHolonsAsync(type,row=>row.ParentHolonId==id,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version,rootsAreChildren:true);

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
            =>await DeleteHolonRecordAsync(id);

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (Guid.TryParse(providerKey, out Guid id)&&id!=Guid.Empty) return await DeleteHolonAsync(id);
            var result = new OASISResult<IHolon>();var cause=new ArgumentException("A nonempty PostgreSQL holon GUID key is required.",nameof(providerKey));
            OASISErrorHandling.HandleError(ref result,cause.Message,cause);result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => await SearchStoredRecordsAsync(searchParams,loadChildren,recursive,maxChildDepth,version);

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Metadata queries ─────────────────────────────────────────────────────

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await QueryStoredMetadataAsync(metaKey,metaValue,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => await QueryStoredMetadataAsync(metaKeyValuePairs,metaKeyValuePairMatchMode,type,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider,version);

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var saved = await SaveHolonsAsync(holons,continueOnError:false);
            var result=OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IEnumerable<IHolon>,bool>(saved);
            result.Result=!saved.IsError&&saved.IsSaved;return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => await LoadAllHolonsAsync(loadChildrenFromProvider:true,version:version);

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
            => await QueryStoredHolonsAsync(HolonType.All,row=>row.CreatedByAvatarId==avatarId||row.ParentHolonId==avatarId,true,true,0,0,true,version);

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername,version);
            if(avatarResult.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(avatarResult);
            if (avatarResult.Result == null)
            {
                var r = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref r,"PostgreSQL identity selector returned no successful avatar payload.",new System.IO.InvalidDataException("Missing successful identity payload."));
                return r;
            }
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress,version);
            if(avatarResult.IsError)return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatar,IEnumerable<IHolon>>(avatarResult);
            if (avatarResult.Result == null)
            {
                var r = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref r,"PostgreSQL identity selector returned no successful avatar payload.",new System.IO.InvalidDataException("Missing successful identity payload."));
                return r;
            }
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0) => ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
