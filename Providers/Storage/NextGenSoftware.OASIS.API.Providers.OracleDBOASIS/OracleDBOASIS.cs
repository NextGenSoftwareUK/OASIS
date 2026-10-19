using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
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

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS
{
    /// <summary>
    /// OASIS provider for Oracle Database (12c+ / Autonomous Database).
    ///
    /// Uses Oracle.ManagedDataAccess.Core (ADO.NET) with three tables:
    ///   OASIS_AVATARS        — ID (RAW 16), USERNAME, EMAIL, IS_DELETED, DATA_JSON (CLOB)
    ///   OASIS_AVATAR_DETAILS — ID (RAW 16), USERNAME, EMAIL, DATA_JSON (CLOB)
    ///   OASIS_HOLONS         — ID (RAW 16), PARENT_HOLON_ID, HOLON_TYPE, IS_DELETED, DATA_JSON (CLOB)
    ///
    /// The DATA_JSON CLOB column holds the full JSON-serialised OASIS object so every
    /// interface method round-trips the complete object without a generated ORM model.
    /// Indexed columns (ID, USERNAME, EMAIL, PARENT_HOLON_ID) serve fast key lookups.
    ///
    /// Pass a standard Oracle connection string, e.g.:
    ///   "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=myhost)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=myservice)));User Id=myuser;Password=mypassword;"
    /// For Oracle Autonomous Database use the wallet-based connection string from the OCI console.
    /// </summary>
    public partial class OracleDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _connectionString;

        public OracleDBOASIS(string connectionString)
        {
            _connectionString = connectionString;
            ProviderName = "OracleDBOASIS";
            ProviderDescription = "Oracle Database provider (ADO.NET, JSON blob storage per row via CLOB)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.OracleDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();

                // Create tables only if they do not exist (PL/SQL anonymous block)
                string[] ddlBlocks = new[]
                {
                    @"BEGIN
  EXECUTE IMMEDIATE 'CREATE TABLE OASIS_AVATARS (
    ID           RAW(16)        DEFAULT SYS_GUID() NOT NULL PRIMARY KEY,
    USERNAME     VARCHAR2(256)  DEFAULT '''' NOT NULL,
    EMAIL        VARCHAR2(256)  DEFAULT '''' NOT NULL,
    IS_DELETED   NUMBER(1)      DEFAULT 0 NOT NULL,
    CREATED_DATE TIMESTAMP      DEFAULT SYSTIMESTAMP NOT NULL,
    MODIFIED_DATE TIMESTAMP     DEFAULT SYSTIMESTAMP NOT NULL,
    DATA_JSON    CLOB           NOT NULL
  )';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",

                    @"BEGIN
  EXECUTE IMMEDIATE 'CREATE INDEX IDX_OASIS_AVT_USERNAME ON OASIS_AVATARS(USERNAME)';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",

                    @"BEGIN
  EXECUTE IMMEDIATE 'CREATE INDEX IDX_OASIS_AVT_EMAIL ON OASIS_AVATARS(EMAIL)';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",

                    @"BEGIN
  EXECUTE IMMEDIATE 'CREATE TABLE OASIS_AVATAR_DETAILS (
    ID        RAW(16)       DEFAULT SYS_GUID() NOT NULL PRIMARY KEY,
    USERNAME  VARCHAR2(256) DEFAULT '''' NOT NULL,
    EMAIL     VARCHAR2(256) DEFAULT '''' NOT NULL,
    DATA_JSON CLOB          NOT NULL
  )';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",

                    @"BEGIN
  EXECUTE IMMEDIATE 'CREATE TABLE OASIS_HOLONS (
    ID              RAW(16)       DEFAULT SYS_GUID() NOT NULL PRIMARY KEY,
    PARENT_HOLON_ID RAW(16)       NULL,
    HOLON_TYPE      NUMBER(10)    DEFAULT 0 NOT NULL,
    IS_DELETED      NUMBER(1)     DEFAULT 0 NOT NULL,
    CREATED_DATE    TIMESTAMP     DEFAULT SYSTIMESTAMP NOT NULL,
    MODIFIED_DATE   TIMESTAMP     DEFAULT SYSTIMESTAMP NOT NULL,
    DATA_JSON       CLOB          NOT NULL
  )';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",

                    @"BEGIN
  EXECUTE IMMEDIATE 'CREATE INDEX IDX_OASIS_HLN_PARENT ON OASIS_HOLONS(PARENT_HOLON_ID)';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;",

                    @"BEGIN
  EXECUTE IMMEDIATE 'CREATE INDEX IDX_OASIS_HLN_TYPE ON OASIS_HOLONS(HOLON_TYPE)';
EXCEPTION WHEN OTHERS THEN IF SQLCODE != -955 THEN RAISE; END IF; END;"
                };

                foreach (string block in ddlBlocks)
                {
                    await using var cmd = CreateCommand(block, conn);
                    await cmd.ExecuteNonQueryAsync();
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "OracleDBOASIS activated — tables created/verified.";
                IsProviderActivated = true;
            }
            catch (Exception ex)
            {
                IsProviderActivated = false;
                result.Exception = ex;
                OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error activating provider — {ex.Message}");
            }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "OracleDBOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── ADO helpers ──────────────────────────────────────────────────────────

        private static OracleCommand CreateCommand(string sql, OracleConnection connection)
            => new OracleCommand(sql, connection) { BindByName = true };

        private static void PublishSavedIdentity(IHolonBase source,IHolonBase stored)
        {
            source.Id=stored.Id;
            source.ProviderUniqueStorageKey ??=new Dictionary<Core.Enums.ProviderType,string>();
            source.ProviderUniqueStorageKey[Core.Enums.ProviderType.OracleDBOASIS]=stored.Id.ToString();
            source.Version=stored.Version;source.VersionId=stored.VersionId;source.PreviousVersionId=stored.PreviousVersionId;
        }

        private static string Serialize(object obj) => Core.Serialization.StoredRecordJson.Serialize(obj);
        private static string MetadataPath(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            // JSON member quoting and SQL literal quoting are separate boundaries.
            var member = Newtonsoft.Json.JsonConvert.SerializeObject(key);
            return "'$.MetaData." + member.Replace("'", "''") + "'";
        }
        private static T Deserialize<T>(string json)
        {
            var record = Core.Serialization.StoredRecordJson.DeserializeStored<T>(json);
            if (record is null)
                throw new System.IO.InvalidDataException("Oracle stored record JSON must contain a record, not null.");
            return record;
        }

        // Oracle stores GUIDs as RAW(16); convert to/from byte arrays.
        private static byte[] GuidToRaw(Guid id) => id.ToByteArray();
        private static Guid RawToGuid(byte[] raw) => new Guid(raw);

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                ArgumentNullException.ThrowIfNull(avatar);
                var source=avatar;
                avatar=Core.Serialization.StoredRecordJson.Project<Avatar>(source);
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null)
                    avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.OracleDBOASIS] = avatar.Id.ToString();

                var stored=await WriteCanonicalRecordAsync((Avatar)avatar,"OASIS_AVATARS");
                PublishSavedIdentity(source,stored);
                result.Result = source; result.IsSaved=true; result.IsError = false;
                result.Message = $"OracleDBOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private Task<Avatar?> LoadAvatarByColumnAsync(string column, OracleDbType type, object value,int version)
            => LoadIdentityByColumnAsync<Avatar>("OASIS_AVATARS",column,type,value,true,version);

        private async Task<T?> LoadIdentityByColumnAsync<T>(string table,string column,OracleDbType type,object value,bool excludeDeleted,int version) where T:class,IHolonBase
        {
            if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
            // Table/column are internal constants, never caller-supplied SQL identifiers.
            var historicalIdentity=version>0&&column!="ID";
            string sql = $"SELECT ID,DATA_JSON FROM {table}" + (historicalIdentity?"":$" WHERE {column}=:val" + (excludeDeleted&&version==0?" AND IS_DELETED=0":""));
            await using var conn = new OracleConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = CreateCommand(sql, conn);
            if(!historicalIdentity)cmd.Parameters.Add("val", type).Value = value;
            await using var reader=await cmd.ExecuteReaderAsync();
            T? selected=null;
            while(await reader.ReadAsync()){
                var id=new Guid((byte[])reader.GetValue(0));var record=await ReadRecordVersionAsync<T>(id,reader.GetString(1),version);
                if(record==null||record.IsDeleted)continue;
                if(historicalIdentity){
                    var identity=record is IAvatar avatar?(column=="USERNAME"?avatar.Username:avatar.Email):record is IAvatarDetail detail?(column=="USERNAME"?detail.Username:detail.Email):null;
                    if(!string.Equals(identity,(string)value,StringComparison.Ordinal))continue;
                }
                if(selected!=null)throw new System.IO.InvalidDataException("Oracle identity selector matched multiple records.");
                selected=record;
            }
            return selected;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await LoadAvatarByColumnAsync("ID", OracleDbType.Raw, GuidToRaw(id),version);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: No avatar found with ID '{id}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"OracleDBOASIS: Avatar loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error loading avatar by ID '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var avatar = await LoadAvatarByColumnAsync("USERNAME", OracleDbType.Varchar2, username,version);
                if (avatar == null) { OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: No avatar found with username '{username}'."); return result; }
                result.Result = avatar; result.IsError = false; result.Message = $"OracleDBOASIS: Avatar loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error loading avatar by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var result = new OASISResult<IAvatar>();
            OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var avatars = await ReadRecordCollectionAsync<Avatar>("OASIS_AVATARS",version);
                result.Result = avatars; result.IsError = false; result.Message = $"OracleDBOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error loading all avatars: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        private async Task<OASISResult<bool>> DeleteAvatarByColumnAsync(string column, OracleDbType type, object value, bool softDelete, string label)
        {
            return await DeleteAvatarPairAsync(column,type,value,softDelete);
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
            => await DeleteAvatarByColumnAsync("ID", OracleDbType.Raw, GuidToRaw(id), softDelete, id.ToString());
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
            => await DeleteAvatarByColumnAsync("USERNAME", OracleDbType.Varchar2, username, softDelete, username);
        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true) => DeleteAvatarByUsernameAsync(username, softDelete).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
            => await DeleteAvatarByColumnAsync("EMAIL", OracleDbType.Varchar2, email, softDelete, email);
        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true) => DeleteAvatarByEmailAsync(email, softDelete).Result;

        // ─── AvatarDetail ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                ArgumentNullException.ThrowIfNull(avatarDetail);
                var source=avatarDetail;
                avatarDetail=Core.Serialization.StoredRecordJson.Project<AvatarDetail>(source);
                if (avatarDetail.Id == Guid.Empty) avatarDetail.Id = Guid.NewGuid();
                avatarDetail.ProviderUniqueStorageKey ??=new Dictionary<Core.Enums.ProviderType,string>();
                avatarDetail.ProviderUniqueStorageKey[Core.Enums.ProviderType.OracleDBOASIS]=avatarDetail.Id.ToString();
                var stored=await WriteCanonicalRecordAsync((AvatarDetail)avatarDetail,"OASIS_AVATAR_DETAILS");
                PublishSavedIdentity(source,stored);
                result.Result = source; result.IsSaved=true; result.IsError = false; result.Message = "OracleDBOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error saving avatar detail: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail) => SaveAvatarDetailAsync(avatarDetail).Result;

        private Task<AvatarDetail?> LoadAvatarDetailByColumnAsync(string column, OracleDbType type, object value,int version)
            => LoadIdentityByColumnAsync<AvatarDetail>("OASIS_AVATAR_DETAILS",column,type,value,false,version);

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var detail = await LoadAvatarDetailByColumnAsync("ID", OracleDbType.Raw, GuidToRaw(id),version);
                if (detail == null) { OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: No avatar detail found for ID '{id}'."); return result; }
                result.Result = detail; result.IsError = false; result.Message = $"OracleDBOASIS: AvatarDetail loaded for ID '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error loading avatar detail for '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var detail = await LoadAvatarDetailByColumnAsync("USERNAME", OracleDbType.Varchar2, username,version);
                if (detail == null) { OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: No avatar detail found for username '{username}'."); return result; }
                result.Result = detail; result.IsError = false; result.Message = $"OracleDBOASIS: AvatarDetail loaded for username '{username}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error loading avatar detail by username '{username}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0) => LoadAvatarDetailByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var detail = await LoadAvatarDetailByColumnAsync("EMAIL", OracleDbType.Varchar2, email,version);
                if (detail == null) { OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: No avatar detail found for email '{email}'."); return result; }
                result.Result = detail; result.IsError = false; result.Message = $"OracleDBOASIS: AvatarDetail loaded for email '{email}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error loading avatar detail by email '{email}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0) => LoadAvatarDetailByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var details = await ReadRecordCollectionAsync<AvatarDetail>("OASIS_AVATAR_DETAILS",version);
                result.Result = details; result.IsError = false; result.Message = $"OracleDBOASIS: Loaded {details.Count} avatar detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error loading all avatar details: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holon saving ─────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                ArgumentNullException.ThrowIfNull(holon);
                var source=holon;
                holon=Core.Serialization.StoredRecordJson.Project<Holon>(source);
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                if (holon.ProviderUniqueStorageKey == null)
                    holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.OracleDBOASIS] = holon.Id.ToString();

                var stored=await WriteCanonicalRecordAsync((Holon)holon,"OASIS_HOLONS");
                PublishSavedIdentity(source,stored);
                result.Result = source; result.IsSaved=true; result.IsError = false; result.Message = $"OracleDBOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error saving holon: {ex.Message}"); }
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
            result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"OracleDBOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> SaveHolonsLegacy(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                ValidateGraphRead(maxChildDepth,0,version);
                var stored=await LoadIdentityByColumnAsync<Holon>("OASIS_HOLONS","ID",OracleDbType.Raw,GuidToRaw(id),true,version);
                if(stored==null){OASISErrorHandling.HandleError(ref result,$"OracleDBOASIS: No holon found with ID '{id}'.");return result;}
                result.Result=(await ProjectHolonRootsAsync(new[]{stored},loadChildren,recursive,maxChildDepth,0,loadChildrenFromProvider)).Single();result.IsError=false;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error loading holon '{id}': {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var result = new OASISResult<IHolon>();
            OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: providerKey '{providerKey}' is not a valid GUID.");
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
                    ? "SELECT DATA_JSON FROM OASIS_HOLONS WHERE IS_DELETED=0"
                    : "SELECT DATA_JSON FROM OASIS_HOLONS WHERE IS_DELETED=0 AND HOLON_TYPE=:HolonType";
                await using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = CreateCommand(sql, conn);
                if (holonType != HolonType.All) cmd.Parameters.Add("HolonType", OracleDbType.Int32).Value = (int)holonType;
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var holon = Deserialize<Holon>(reader.GetString(0));
                    if (holon != null) holons.Add(holon);
                }
                result.Result = holons; result.IsError = false; result.Message = $"OracleDBOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error loading all holons: {ex.Message}"); }
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
                    ? "SELECT DATA_JSON FROM OASIS_HOLONS WHERE PARENT_HOLON_ID=:ParentId AND IS_DELETED=0"
                    : "SELECT DATA_JSON FROM OASIS_HOLONS WHERE PARENT_HOLON_ID=:ParentId AND IS_DELETED=0 AND HOLON_TYPE=:HolonType";
                await using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = CreateCommand(sql, conn);
                cmd.Parameters.Add("ParentId", OracleDbType.Raw).Value = GuidToRaw(id);
                if (holonType != HolonType.All) cmd.Parameters.Add("HolonType", OracleDbType.Int32).Value = (int)holonType;
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var holon = Deserialize<Holon>(reader.GetString(0));
                    if (holon != null) holons.Add(holon);
                }
                result.Result = holons; result.IsError = false; result.Message = $"OracleDBOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Error loading holons for parent '{id}': {ex.Message}"); }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentLegacy(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadHolonsForParentAsyncLegacy(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsyncLegacy(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsyncLegacy(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider);
            var result = new OASISResult<IEnumerable<IHolon>>();
            OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> LoadHolonsForParentLegacy(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
            => LoadHolonsForParentAsyncLegacy(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public async Task<OASISResult<bool>> DeleteHolonWithSoftDeleteAsync(Guid id, bool softDelete = true)
        {
            var deleted=await DeleteHolonRecordAsync(id,softDelete);
            return new OASISResult<bool>{Result=!deleted.IsError&&deleted.Result!=null,IsError=deleted.IsError,Exception=deleted.Exception,Message=deleted.Message,ErrorCode=deleted.ErrorCode};
        }

        public OASISResult<bool> DeleteHolonWithSoftDelete(Guid id, bool softDelete = true) => DeleteHolonWithSoftDeleteAsync(id, softDelete).Result;

        public async Task<OASISResult<bool>> DeleteHolonWithSoftDeleteAsync(string providerKey, bool softDelete = true)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await DeleteHolonWithSoftDeleteAsync(id, softDelete);
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: providerKey '{providerKey}' is not a valid GUID.");
            return result;
        }

        public OASISResult<bool> DeleteHolonWithSoftDelete(string providerKey, bool softDelete = true) => DeleteHolonWithSoftDeleteAsync(providerKey, softDelete).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try
            {
                NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Validate(searchParams,version,maxChildDepth,supportsHistory:true);
                var groups=searchParams.SearchGroups??new List<ISearchGroupBase>();
                var wantsAvatars=groups.Count==0||groups.Any(group=>group.SearchAvatars||!group.SearchHolons);
                var wantsHolons=groups.Count==0||groups.Any(group=>group.SearchHolons||!group.SearchAvatars);
                var holons=wantsHolons||searchParams.ParentId!=Guid.Empty?(await ReadRecordCollectionAsync<Holon>("OASIS_HOLONS",version)).Cast<IHolon>().ToList():new List<IHolon>();
                var found=new SearchResults();
                if(wantsAvatars){
                    var avatars=await ReadRecordCollectionAsync<Avatar>("OASIS_AVATARS",version);
                    found.SearchResultAvatars.AddRange(avatars.Where(row=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Matches(row,true,searchParams,holons)));
                }
                if(wantsHolons)found.SearchResultHolons.AddRange(await ProjectHolonRootsAsync(holons.Where(row=>NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.Matches(row,false,searchParams,holons)),loadChildren,recursive,maxChildDepth,0,true));
                found.NumberOfResults=found.SearchResultAvatars.Count+found.SearchResultHolons.Count;result.Result=found;result.IsLoaded=true;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        // ─── Members added to base after initial implementation ───────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            if(!Guid.TryParse(providerKey,out var id))
            {
                var result=new OASISResult<bool>();
                var error=new FormatException("Oracle avatar storage key must be a GUID.");
                result.Exception=error;OASISErrorHandling.HandleError(ref result,error.Message);return result;
            }
            return await DeleteAvatarAsync(id,softDelete);
        }
        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true) => DeleteAvatarAsync(providerKey, softDelete).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
                if (avatarResult.IsError || avatarResult.Result == null) { OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Avatar with email '{avatarEmailAddress}' not found."); return result; }
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
                var avatar = await LoadAvatarByColumnAsync("EMAIL", OracleDbType.Varchar2, avatarEmail,version);
                if (avatar != null) result.Result = avatar;
                else OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Avatar with email '{avatarEmail}' not found.");
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                ValidateGraphRead(maxChildDepth,curentChildDepth,version,type);
                if(version>0){
                    var selected=await ReadRecordCollectionAsync<Holon>("OASIS_HOLONS",version);
                    result.Result=await ProjectHolonRootsAsync(selected.Where(row=>(type==HolonType.All||row.HolonType==type)&&NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.MatchesMetadata(row.MetaData,metaKeyValuePairs,metaKeyValuePairMatchMode)),loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider);return result;
                }
                string joiner = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.Any ? " OR " : " AND ";
                var conditions = metaKeyValuePairs.Keys.Select((k, i) => $"JSON_VALUE(DATA_JSON,{MetadataPath(k)})=:v{i}").ToList();
                string sql = $"SELECT ID,DATA_JSON FROM OASIS_HOLONS WHERE IS_DELETED=0 AND ({string.Join(joiner, conditions)})";
                if (type != HolonType.All) sql += " AND HOLON_TYPE=:HType";
                await using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = CreateCommand(sql, conn);
                int idx = 0;
                foreach (var v in metaKeyValuePairs.Values) cmd.Parameters.Add($":v{idx++}", OracleDbType.Varchar2).Value = v;
                if (type != HolonType.All) cmd.Parameters.Add(":HType", OracleDbType.Int32).Value = (int)type;
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = await ReadRecordVersionAsync<Holon>(new Guid((byte[])reader.GetValue(0)),reader.GetString(1),0); if (h != null&&!h.IsDeleted) holons.Add(h); }
                result.Result = await ProjectHolonRootsAsync(holons,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider); result.IsError = false;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try { var r = await SaveHolonsAsync(holons); result.IsError = r.IsError; result.Result = !r.IsError; result.Message = r.Message; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (!Guid.TryParse(providerKey, out var id)) { var r2 = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r2, $"OracleDBOASIS: Invalid Guid providerKey '{providerKey}'."); return r2; }
            return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                ValidateGraphRead(maxChildDepth,curentChildDepth,version,type);
                if(version>0){
                    var selected=await ReadRecordCollectionAsync<Holon>("OASIS_HOLONS",version);
                    result.Result=await ProjectHolonRootsAsync(selected.Where(row=>row.ParentHolonId==id&&(type==HolonType.All||row.HolonType==type)),loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider);return result;
                }
                string sql = "SELECT ID,DATA_JSON FROM OASIS_HOLONS WHERE PARENT_HOLON_ID=:ParentId AND IS_DELETED=0";
                if (type != HolonType.All) sql += " AND HOLON_TYPE=:HType";
                await using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = CreateCommand(sql, conn);
                cmd.Parameters.Add(":ParentId", OracleDbType.Raw).Value = GuidToRaw(id);
                if (type != HolonType.All) cmd.Parameters.Add(":HType", OracleDbType.Int32).Value = (int)type;
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = await ReadRecordVersionAsync<Holon>(new Guid((byte[])reader.GetValue(0)),reader.GetString(1),0); if (h != null&&!h.IsDeleted) holons.Add(h); }
                result.Result = await ProjectHolonRootsAsync(holons,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider); result.IsError = false;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                ValidateGraphRead(maxChildDepth,curentChildDepth,version,type);
                if(version>0){
                    var selected=await ReadRecordCollectionAsync<Holon>("OASIS_HOLONS",version);
                    result.Result=await ProjectHolonRootsAsync(selected.Where(row=>type==HolonType.All||row.HolonType==type),loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider);return result;
                }
                string sql = "SELECT ID,DATA_JSON FROM OASIS_HOLONS WHERE IS_DELETED=0";
                if (type != HolonType.All) sql += " AND HOLON_TYPE=:HType";
                await using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = CreateCommand(sql, conn);
                if (type != HolonType.All) cmd.Parameters.Add(":HType", OracleDbType.Int32).Value = (int)type;
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = await ReadRecordVersionAsync<Holon>(new Guid((byte[])reader.GetValue(0)),reader.GetString(1),0); if (h != null&&!h.IsDeleted) holons.Add(h); }
                result.Result = await ProjectHolonRootsAsync(holons,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider); result.IsError = false;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            return await DeleteHolonRecordAsync(id,true);
        }
        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            if (!Guid.TryParse(providerKey, out var id)) { var r2 = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r2, $"OracleDBOASIS: Invalid Guid providerKey '{providerKey}'."); return r2; }
            return await DeleteHolonAsync(id);
        }
        public override OASISResult<IHolon> DeleteHolon(string providerKey) => DeleteHolonAsync(providerKey).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            try
            {
                foreach (var holon in holons)
                {
                    var r = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (!r.IsError && r.Result != null) saved.Add(r.Result);
                    else if (!continueOnError) { result.IsError = true; result.Message = r.Message; return result; }
                }
                result.Result = saved; result.IsError = false;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                ValidateGraphRead(maxChildDepth,curentChildDepth,version,type);
                if(version>0){
                    var selected=await ReadRecordCollectionAsync<Holon>("OASIS_HOLONS",version);
                    result.Result=await ProjectHolonRootsAsync(selected.Where(row=>(type==HolonType.All||row.HolonType==type)&&NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.StoredRecordSearch.MatchesMetadata(row.MetaData,new Dictionary<string,string>{{metaKey,metaValue}},MetaKeyValuePairMatchMode.All)),loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider);return result;
                }
                string sql = $"SELECT ID,DATA_JSON FROM OASIS_HOLONS WHERE IS_DELETED=0 AND JSON_VALUE(DATA_JSON,{MetadataPath(metaKey)})=:MetaValue";
                if (type != HolonType.All) sql += " AND HOLON_TYPE=:HType";
                await using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = CreateCommand(sql, conn);
                cmd.Parameters.Add(":MetaValue", OracleDbType.Varchar2).Value = metaValue;
                if (type != HolonType.All) cmd.Parameters.Add(":HType", OracleDbType.Int32).Value = (int)type;
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = await ReadRecordVersionAsync<Holon>(new Guid((byte[])reader.GetValue(0)),reader.GetString(1),0); if (h != null&&!h.IsDeleted) holons.Add(h); }
                result.Result = await ProjectHolonRootsAsync(holons,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider); result.IsError = false;
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync(loadChildrenFromProvider:true,version:version);
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                if(version>0){
                    var selected=await ReadRecordCollectionAsync<Holon>("OASIS_HOLONS",version);
                    result.Result=await ProjectHolonRootsAsync(selected.Where(row=>row.CreatedByAvatarId==avatarId),true,true,0,0,true);return result;
                }
                const string sql = "SELECT ID,DATA_JSON FROM OASIS_HOLONS WHERE IS_DELETED=0 AND JSON_VALUE(DATA_JSON,'$.CreatedByAvatarId')=:AvatarId";
                await using var conn = new OracleConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = CreateCommand(sql, conn);
                cmd.Parameters.Add(":AvatarId", OracleDbType.Varchar2).Value = avatarId.ToString();
                var holons = new List<IHolon>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync()) { var h = await ReadRecordVersionAsync<Holon>(new Guid((byte[])reader.GetValue(0)),reader.GetString(1),0); if (h != null&&!h.IsDeleted) holons.Add(h); }
                result.Result = await ProjectHolonRootsAsync(holons,true,true,0,0,true); result.IsError = false;
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
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername,version);
                if (avatarResult.IsError || avatarResult.Result == null) { OASISErrorHandling.HandleError(ref result, $"OracleDBOASIS: Avatar '{avatarUsername}' not found."); return result; }
                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0) => ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
    }
}
