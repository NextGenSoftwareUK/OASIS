using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cassandra;
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

namespace NextGenSoftware.OASIS.API.Providers.CassandraOASIS
{
    /// <summary>
    /// OASIS provider for Apache Cassandra / ScyllaDB via the DataStax CassandraCSharpDriver.
    ///
    /// Schema (keyspace: oasis):
    ///   oasis_avatars        (id UUID PRIMARY KEY, username TEXT, email TEXT, is_deleted BOOLEAN, data_json TEXT)
    ///   oasis_avatar_details (id UUID PRIMARY KEY, username TEXT, email TEXT, data_json TEXT)
    ///   oasis_holons         (id UUID PRIMARY KEY, parent_holon_id UUID, holon_type INT, is_deleted BOOLEAN, data_json TEXT)
    ///
    /// Secondary-index tables for username/email lookups:
    ///   oasis_avatar_by_username (username TEXT PRIMARY KEY, id UUID)
    ///   oasis_avatar_by_email    (email    TEXT PRIMARY KEY, id UUID)
    ///
    /// Constructor parameters:
    ///   contactPoints — Cassandra/Scylla host addresses (e.g. ["localhost"])
    ///   port          — CQL port (default 9042)
    ///   keyspace      — keyspace name (default "oasis")
    ///   username      — optional CQL credentials
    ///   password      — optional CQL credentials
    ///   replication   — replication factor for keyspace creation (default 1)
    /// </summary>
    public class CassandraOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string[] _contactPoints;
        private readonly int _port;
        private readonly string _keyspace;
        private readonly string? _username;
        private readonly string? _password;
        private readonly int _replication;

        private ICluster? _cluster;
        private ISession? _session;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public CassandraOASIS(string[] contactPoints, int port = 9042, string keyspace = "oasis", string? username = null, string? password = null, int replication = 1)
        {
            _contactPoints = contactPoints;
            _port = port;
            _keyspace = keyspace;
            _username = username;
            _password = password;
            _replication = replication;
            ProviderName = "CassandraOASIS";
            ProviderDescription = "Apache Cassandra / ScyllaDB provider via DataStax CassandraCSharpDriver";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.CassandraOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private static string Ser(object o) => JsonSerializer.Serialize(o, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private ISession GetSession()
        {
            if (_session != null) return _session;
            var builder = Cluster.Builder().AddContactPoints(_contactPoints).WithPort(_port);
            if (!string.IsNullOrEmpty(_username)) builder = builder.WithCredentials(_username, _password);
            _cluster = builder.Build();
            // Create keyspace if absent, then connect to it
            using (var sysSession = _cluster.Connect())
            {
                sysSession.Execute($"CREATE KEYSPACE IF NOT EXISTS {_keyspace} WITH replication = {{'class': 'SimpleStrategy', 'replication_factor': {_replication}}}");
            }
            _session = _cluster.Connect(_keyspace);
            CreateSchema(_session);
            return _session;
        }

        private void CreateSchema(ISession s)
        {
            s.Execute($"CREATE TABLE IF NOT EXISTS {_keyspace}.oasis_avatars (id UUID PRIMARY KEY, username TEXT, email TEXT, is_deleted BOOLEAN, data_json TEXT)");
            s.Execute($"CREATE TABLE IF NOT EXISTS {_keyspace}.oasis_avatar_details (id UUID PRIMARY KEY, username TEXT, email TEXT, data_json TEXT)");
            s.Execute($"CREATE TABLE IF NOT EXISTS {_keyspace}.oasis_holons (id UUID PRIMARY KEY, parent_holon_id UUID, holon_type INT, is_deleted BOOLEAN, data_json TEXT)");
            // Lookup tables — Cassandra doesn't allow secondary index queries over all rows efficiently
            s.Execute($"CREATE TABLE IF NOT EXISTS {_keyspace}.oasis_avatar_by_username (username TEXT PRIMARY KEY, id UUID)");
            s.Execute($"CREATE TABLE IF NOT EXISTS {_keyspace}.oasis_avatar_by_email (email TEXT PRIMARY KEY, id UUID)");
        }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try { GetSession(); result.Result = true; result.IsError = false; result.Message = "CassandraOASIS activated."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _session?.Dispose(); _session = null;
            _cluster?.Dispose(); _cluster = null;
            return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "CassandraOASIS deactivated." });
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
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.CassandraOASIS] = avatar.Id.ToString();
                var s = GetSession();
                var ps = s.Prepare($"INSERT INTO {_keyspace}.oasis_avatars (id, username, email, is_deleted, data_json) VALUES (?, ?, ?, ?, ?)");
                s.Execute(ps.Bind(avatar.Id, avatar.Username, avatar.Email, avatar.IsDeleted, Ser(avatar)));
                // Update lookup tables
                if (!string.IsNullOrEmpty(avatar.Username)) s.Execute(s.Prepare($"INSERT INTO {_keyspace}.oasis_avatar_by_username (username, id) VALUES (?, ?)").Bind(avatar.Username, avatar.Id));
                if (!string.IsNullOrEmpty(avatar.Email)) s.Execute(s.Prepare($"INSERT INTO {_keyspace}.oasis_avatar_by_email (email, id) VALUES (?, ?)").Bind(avatar.Email, avatar.Id));
                result.Result = avatar; result.IsError = false; result.Message = $"CassandraOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var s = GetSession();
                var row = s.Execute(s.Prepare($"SELECT data_json, is_deleted FROM {_keyspace}.oasis_avatars WHERE id = ?").Bind(id)).FirstOrDefault();
                if (row == null || row.GetValue<bool>("is_deleted")) { OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: No avatar for ID '{id}'."); return await Task.FromResult(result); }
                var avatar = Des<Avatar>(row.GetValue<string>("data_json")); if (avatar == null) { OASISErrorHandling.HandleError(ref result, "CassandraOASIS: Deserialise failed."); return await Task.FromResult(result); }
                result.Result = avatar; result.IsError = false; result.Message = "CassandraOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var s = GetSession();
                var idRow = s.Execute(s.Prepare($"SELECT id FROM {_keyspace}.oasis_avatar_by_username WHERE username = ?").Bind(username)).FirstOrDefault();
                if (idRow == null) { OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: No avatar for username '{username}'."); return await Task.FromResult(result); }
                return await LoadAvatarAsync(idRow.GetValue<Guid>("id"), version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var s = GetSession();
                var idRow = s.Execute(s.Prepare($"SELECT id FROM {_keyspace}.oasis_avatar_by_email WHERE email = ?").Bind(email)).FirstOrDefault();
                if (idRow == null) { OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: No avatar for email '{email}'."); return await Task.FromResult(result); }
                return await LoadAvatarAsync(idRow.GetValue<Guid>("id"), version);
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0)
        { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"CassandraOASIS: Invalid GUID '{pk}'."); return r; }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var s = GetSession();
                var rows = s.Execute($"SELECT data_json, is_deleted FROM {_keyspace}.oasis_avatars");
                var avatars = rows.Where(r => !r.GetValue<bool>("is_deleted")).Select(r => Des<Avatar>(r.GetValue<string>("data_json"))).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"CassandraOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var loaded = await LoadAvatarAsync(id);
                if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: Avatar '{id}' not found."); return result; }
                var s = GetSession();
                if (softDelete)
                {
                    var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                    s.Execute(s.Prepare($"UPDATE {_keyspace}.oasis_avatars SET is_deleted = true, data_json = ? WHERE id = ?").Bind(Ser(av), id));
                }
                else { s.Execute(s.Prepare($"DELETE FROM {_keyspace}.oasis_avatars WHERE id = ?").Bind(id)); }
                result.Result = true; result.IsError = false; result.Message = $"CassandraOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"CassandraOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "CassandraOASIS: Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                var s = GetSession();
                s.Execute(s.Prepare($"INSERT INTO {_keyspace}.oasis_avatar_details (id, username, email, data_json) VALUES (?, ?, ?, ?)").Bind(d.Id, d.Username, d.Email, Ser(d)));
                result.Result = d; result.IsError = false; result.Message = "CassandraOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                var s = GetSession();
                var row = s.Execute(s.Prepare($"SELECT data_json FROM {_keyspace}.oasis_avatar_details WHERE id = ?").Bind(id)).FirstOrDefault();
                if (row == null) { OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: No detail for ID '{id}'."); return await Task.FromResult(result); }
                var detail = Des<AvatarDetail>(row.GetValue<string>("data_json")); if (detail == null) { OASISErrorHandling.HandleError(ref result, "CassandraOASIS: Deserialise failed."); return await Task.FromResult(result); }
                result.Result = detail; result.IsError = false; result.Message = "CassandraOASIS: AvatarDetail loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        private async Task<AvatarDetail?> ScanDetailsAsync(Func<Row, bool> predicate)
        {
            var s = GetSession();
            var rows = s.Execute($"SELECT data_json, username, email FROM {_keyspace}.oasis_avatar_details");
            var row = rows.FirstOrDefault(predicate);
            return row == null ? null : Des<AvatarDetail>(row.GetValue<string>("data_json"));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await ScanDetailsAsync(r => r.GetValue<string>("username") == u); if (d == null) { OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "CassandraOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await ScanDetailsAsync(r => r.GetValue<string>("email") == e); if (d == null) { OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "CassandraOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var s = GetSession();
                var rows = s.Execute($"SELECT data_json FROM {_keyspace}.oasis_avatar_details");
                var details = rows.Select(r => Des<AvatarDetail>(r.GetValue<string>("data_json"))).Where(d => d != null).Cast<IAvatarDetail>().ToList();
                result.Result = details; result.IsError = false; result.Message = $"CassandraOASIS: Loaded {details.Count} detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.CassandraOASIS] = holon.Id.ToString();
                var s = GetSession();
                s.Execute(s.Prepare($"INSERT INTO {_keyspace}.oasis_holons (id, parent_holon_id, holon_type, is_deleted, data_json) VALUES (?, ?, ?, ?, ?)").Bind(holon.Id, holon.ParentHolonId, (int)holon.HolonType, holon.IsDeleted, Ser(holon)));
                result.Result = holon; result.IsError = false; result.Message = $"CassandraOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"CassandraOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var s = GetSession();
                var row = s.Execute(s.Prepare($"SELECT data_json, is_deleted FROM {_keyspace}.oasis_holons WHERE id = ?").Bind(id)).FirstOrDefault();
                if (row == null || row.GetValue<bool>("is_deleted")) { OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: No holon for ID '{id}'."); return await Task.FromResult(result); }
                var holon = Des<Holon>(row.GetValue<string>("data_json")); if (holon == null) { OASISErrorHandling.HandleError(ref result, "CassandraOASIS: Deserialise failed."); return await Task.FromResult(result); }
                result.Result = holon; result.IsError = false; result.Message = "CassandraOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"CassandraOASIS: Invalid GUID '{pk}'."); return r; }

        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var s = GetSession();
                var rows = s.Execute($"SELECT data_json, is_deleted, holon_type FROM {_keyspace}.oasis_holons");
                var holons = rows.Where(r => !r.GetValue<bool>("is_deleted") && (type == HolonType.All || r.GetValue<int>("holon_type") == (int)type)).Select(r => Des<Holon>(r.GetValue<string>("data_json"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"CassandraOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // Full scan — Cassandra requires ALLOW FILTERING for non-primary-key predicates
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var s = GetSession();
                var rows = s.Execute($"SELECT data_json, is_deleted, holon_type, parent_holon_id FROM {_keyspace}.oasis_holons");
                var holons = rows.Where(r => !r.GetValue<bool>("is_deleted") && r.GetValue<Guid>("parent_holon_id") == id && (type == HolonType.All || r.GetValue<int>("holon_type") == (int)type)).Select(r => Des<Holon>(r.GetValue<string>("data_json"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"CassandraOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return await Task.FromResult(result);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"CassandraOASIS: Invalid GUID '{pk}'."); return r; }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: Holon '{id}' not found."); return result; }
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                var s = GetSession(); s.Execute(s.Prepare($"UPDATE {_keyspace}.oasis_holons SET is_deleted = true, data_json = ? WHERE id = ?").Bind(Ser(holon), id));
                result.Result = holon; result.IsError = false; result.Message = $"CassandraOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"CassandraOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"CassandraOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        // ─── Search + Metadata ────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"CassandraOASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"CassandraOASIS: {holons.Count} holon(s)." }; }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"CassandraOASIS: {holons.Count} holon(s)." }; }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(); var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = h, IsError = false, Message = $"CassandraOASIS: {h.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
