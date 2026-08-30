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

namespace NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS
{
    /// <summary>
    /// OASIS provider for ScyllaDB — a high-performance Cassandra-compatible database.
    /// Uses the CassandraCSharpDriver which is fully wire-compatible with ScyllaDB.
    ///
    /// Schema (keyspace: oasis, replication: SimpleStrategy):
    ///   oasis_avatars       — id uuid PK, username text, email text, is_deleted boolean, data_json text
    ///   oasis_avatar_details — id uuid PK, username text, email text, data_json text
    ///   oasis_holons        — id uuid PK, parent_holon_id uuid, holon_type int, is_deleted boolean, data_json text
    ///
    /// Constructor parameters:
    ///   contactPoints — comma-separated ScyllaDB/Cassandra hosts, e.g. "localhost" or "node1,node2"
    ///   port          — CQL native transport port (default 9042)
    ///   username      — optional credentials
    ///   password      — optional credentials
    ///   keyspace      — keyspace to use/create (default "oasis")
    ///   replication   — replication factor for keyspace creation (default 1)
    /// </summary>
    public class ScyllaDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string[] _contactPoints;
        private readonly int _port;
        private readonly string? _username;
        private readonly string? _password;
        private readonly string _keyspace;
        private readonly int _replication;

        private ICluster? _cluster;
        private ISession? _session;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ScyllaDBOASIS(string contactPoints, int port = 9042, string? username = null, string? password = null, string keyspace = "oasis", int replication = 1)
        {
            _contactPoints = contactPoints.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            _port = port;
            _username = username;
            _password = password;
            _keyspace = keyspace;
            _replication = replication;
            ProviderName = "ScyllaDBOASIS";
            ProviderDescription = "ScyllaDB provider (Cassandra-compatible high-performance NoSQL via CassandraCSharpDriver)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ScyllaDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private ISession GetSession()
        {
            if (_session != null) return _session;
            var builder = Cluster.Builder().AddContactPoints(_contactPoints).WithPort(_port);
            if (_username != null && _password != null)
                builder = builder.WithCredentials(_username, _password);
            _cluster = builder.Build();
            _session = _cluster.Connect();
            return _session;
        }

        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private void EnsureSchema()
        {
            var s = GetSession();
            s.Execute($"CREATE KEYSPACE IF NOT EXISTS {_keyspace} WITH replication = {{'class':'SimpleStrategy','replication_factor':{_replication}}}");
            s.Execute($"USE {_keyspace}");
            s.Execute($"CREATE TABLE IF NOT EXISTS {_keyspace}.oasis_avatars (id uuid PRIMARY KEY, username text, email text, is_deleted boolean, data_json text)");
            s.Execute($"CREATE INDEX IF NOT EXISTS ON {_keyspace}.oasis_avatars (username)");
            s.Execute($"CREATE INDEX IF NOT EXISTS ON {_keyspace}.oasis_avatars (email)");
            s.Execute($"CREATE TABLE IF NOT EXISTS {_keyspace}.oasis_avatar_details (id uuid PRIMARY KEY, username text, email text, data_json text)");
            s.Execute($"CREATE INDEX IF NOT EXISTS ON {_keyspace}.oasis_avatar_details (username)");
            s.Execute($"CREATE INDEX IF NOT EXISTS ON {_keyspace}.oasis_avatar_details (email)");
            s.Execute($"CREATE TABLE IF NOT EXISTS {_keyspace}.oasis_holons (id uuid PRIMARY KEY, parent_holon_id uuid, holon_type int, is_deleted boolean, data_json text)");
            s.Execute($"CREATE INDEX IF NOT EXISTS ON {_keyspace}.oasis_holons (parent_holon_id)");
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try { await Task.Run(EnsureSchema); result.Result = true; IsProviderActivated = true; result.IsError = false; result.Message = "ScyllaDBOASIS activated."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _session?.Dispose(); _session = null;
            _cluster?.Dispose(); _cluster = null;
            IsProviderActivated = false;
            return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "ScyllaDBOASIS deactivated." });
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
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.ScyllaDBOASIS] = avatar.Id.ToString();
                var s = GetSession();
                var ps = s.Prepare($"INSERT INTO {_keyspace}.oasis_avatars (id, username, email, is_deleted, data_json) VALUES (?, ?, ?, ?, ?)");
                await Task.Run(() => s.Execute(ps.Bind(avatar.Id, avatar.Username, avatar.Email, avatar.IsDeleted, Ser(avatar))));
                result.Result = avatar; result.IsError = false; result.Message = $"ScyllaDBOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: Error saving avatar: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        private async Task<Avatar?> QueryAvatarAsync(string cql, params object[] values)
        {
            var s = GetSession();
            var ps = s.Prepare(cql);
            var row = await Task.Run(() => s.Execute(ps.Bind(values)).FirstOrDefault());
            if (row == null) return null;
            return Des<Avatar>(row.GetValue<string>("data_json"));
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var a = await QueryAvatarAsync($"SELECT data_json FROM {_keyspace}.oasis_avatars WHERE id=? LIMIT 1", id);
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: No avatar for ID '{id}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "ScyllaDBOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var a = await QueryAvatarAsync($"SELECT data_json FROM {_keyspace}.oasis_avatars WHERE username=? ALLOW FILTERING LIMIT 1", username);
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: No avatar for username '{username}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "ScyllaDBOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var a = await QueryAvatarAsync($"SELECT data_json FROM {_keyspace}.oasis_avatars WHERE email=? ALLOW FILTERING LIMIT 1", avatarEmail);
                if (a == null || a.IsDeleted) { OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: No avatar for email '{avatarEmail}'."); return result; }
                result.Result = a; result.IsError = false; result.Message = "ScyllaDBOASIS: Avatar loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0) => LoadAvatarByEmailAsync(avatarEmail, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadAvatarAsync(id, version);
            var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"ScyllaDBOASIS: Invalid GUID '{providerKey}'."); return r;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0) => LoadAvatarByProviderKeyAsync(providerKey, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var s = GetSession();
                var rows = await Task.Run(() => s.Execute($"SELECT data_json, is_deleted FROM {_keyspace}.oasis_avatars"));
                var list = new List<IAvatar>();
                foreach (var row in rows)
                {
                    if (row.GetValue<bool>("is_deleted")) continue;
                    var a = Des<Avatar>(row.GetValue<string>("data_json")); if (a != null) list.Add(a);
                }
                result.Result = list; result.IsError = false; result.Message = $"ScyllaDBOASIS: Loaded {list.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var s = GetSession();
                if (softDelete)
                {
                    var ps = s.Prepare($"UPDATE {_keyspace}.oasis_avatars SET is_deleted=? WHERE id=?");
                    await Task.Run(() => s.Execute(ps.Bind(true, id)));
                }
                else
                {
                    var ps = s.Prepare($"DELETE FROM {_keyspace}.oasis_avatars WHERE id=?");
                    await Task.Run(() => s.Execute(ps.Bind(id)));
                }
                result.Result = true; result.IsError = false; result.Message = $"ScyllaDBOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"ScyllaDBOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"ScyllaDBOASIS: Avatar not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                var ps = s.Prepare($"INSERT INTO {_keyspace}.oasis_avatar_details (id, username, email, data_json) VALUES (?, ?, ?, ?)");
                await Task.Run(() => s.Execute(ps.Bind(d.Id, d.Username, d.Email, Ser(d))));
                result.Result = d; result.IsError = false; result.Message = "ScyllaDBOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        private async Task<AvatarDetail?> QueryDetailAsync(string cql, params object[] values)
        {
            var s = GetSession(); var ps = s.Prepare(cql);
            var row = await Task.Run(() => s.Execute(ps.Bind(values)).FirstOrDefault());
            if (row == null) return null; return Des<AvatarDetail>(row.GetValue<string>("data_json"));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await QueryDetailAsync($"SELECT data_json FROM {_keyspace}.oasis_avatar_details WHERE id=? LIMIT 1", id); if (d == null) { OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: No detail for ID '{id}'."); return result; } result.Result = d; result.IsError = false; result.Message = "ScyllaDBOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await QueryDetailAsync($"SELECT data_json FROM {_keyspace}.oasis_avatar_details WHERE username=? ALLOW FILTERING LIMIT 1", u); if (d == null) { OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "ScyllaDBOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await QueryDetailAsync($"SELECT data_json FROM {_keyspace}.oasis_avatar_details WHERE email=? ALLOW FILTERING LIMIT 1", e); if (d == null) { OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "ScyllaDBOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                var s = GetSession(); var rows = await Task.Run(() => s.Execute($"SELECT data_json FROM {_keyspace}.oasis_avatar_details"));
                var list = new List<IAvatarDetail>(); foreach (var row in rows) { var d = Des<AvatarDetail>(row.GetValue<string>("data_json")); if (d != null) list.Add(d); }
                result.Result = list; result.IsError = false; result.Message = $"ScyllaDBOASIS: Loaded {list.Count} detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
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
                var s = GetSession();
                var ps = s.Prepare($"INSERT INTO {_keyspace}.oasis_holons (id, parent_holon_id, holon_type, is_deleted, data_json) VALUES (?, ?, ?, ?, ?)");
                int holonType = holon.HolonType.HasValue ? (int)holon.HolonType.Value : 0;
                await Task.Run(() => s.Execute(ps.Bind(holon.Id, holon.ParentHolonId, holonType, holon.IsDeleted, Ser(holon))));
                result.Result = holon; result.IsError = false; result.Message = $"ScyllaDBOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: Error saving holon: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version); if (!r.IsError && r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = false; result.Message = $"ScyllaDBOASIS: Saved {saved.Count} holons.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false, int version = 0) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var s = GetSession(); var ps = s.Prepare($"SELECT data_json, is_deleted FROM {_keyspace}.oasis_holons WHERE id=? LIMIT 1");
                var row = await Task.Run(() => s.Execute(ps.Bind(id)).FirstOrDefault());
                if (row == null || row.GetValue<bool>("is_deleted")) { OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: No holon for ID '{id}'."); return result; }
                var h = Des<Holon>(row.GetValue<string>("data_json")); if (h == null) { OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: Failed to deserialise holon '{id}'."); return result; }
                result.Result = h; result.IsError = false; result.Message = "ScyllaDBOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
            var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"ScyllaDBOASIS: Invalid GUID '{providerKey}'."); return r;
        }

        public override OASISResult<IHolon> LoadHolonByProviderKey(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var s = GetSession(); RowSet rows;
                if (holonType == HolonType.All)
                    rows = await Task.Run(() => s.Execute($"SELECT data_json, is_deleted FROM {_keyspace}.oasis_holons"));
                else
                {
                    var ps = s.Prepare($"SELECT data_json, is_deleted FROM {_keyspace}.oasis_holons WHERE holon_type=? ALLOW FILTERING");
                    rows = await Task.Run(() => s.Execute(ps.Bind((int)holonType)));
                }
                var list = new List<IHolon>();
                foreach (var row in rows) { if (row.GetValue<bool>("is_deleted")) continue; var h = Des<Holon>(row.GetValue<string>("data_json")); if (h != null) list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"ScyllaDBOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadAllHolonsAsync(holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var s = GetSession(); RowSet rows;
                if (holonType == HolonType.All)
                {
                    var ps = s.Prepare($"SELECT data_json, is_deleted FROM {_keyspace}.oasis_holons WHERE parent_holon_id=? ALLOW FILTERING");
                    rows = await Task.Run(() => s.Execute(ps.Bind(id)));
                }
                else
                {
                    var ps = s.Prepare($"SELECT data_json, is_deleted FROM {_keyspace}.oasis_holons WHERE parent_holon_id=? AND holon_type=? ALLOW FILTERING");
                    rows = await Task.Run(() => s.Execute(ps.Bind(id, (int)holonType)));
                }
                var list = new List<IHolon>();
                foreach (var row in rows) { if (row.GetValue<bool>("is_deleted")) continue; var h = Des<Holon>(row.GetValue<string>("data_json")); if (h != null) list.Add(h); }
                result.Result = list; result.IsError = false; result.Message = $"ScyllaDBOASIS: Loaded {list.Count} child holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false)
        {
            if (Guid.TryParse(providerKey, out Guid id)) return await LoadHolonsForParentAsync(id, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider);
            var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"ScyllaDBOASIS: Invalid GUID '{providerKey}'."); return r;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType holonType = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int version = 0, bool continueOnError = true, bool loadChildrenFromProvider = false) => LoadHolonsForParentAsync(providerKey, holonType, loadChildren, recursive, maxChildDepth, version, continueOnError, loadChildrenFromProvider).Result;

        public override async Task<OASISResult<bool>> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var s = GetSession();
                if (softDelete)
                {
                    var ps = s.Prepare($"UPDATE {_keyspace}.oasis_holons SET is_deleted=? WHERE id=?");
                    await Task.Run(() => s.Execute(ps.Bind(true, id)));
                }
                else
                {
                    var ps = s.Prepare($"DELETE FROM {_keyspace}.oasis_holons WHERE id=?");
                    await Task.Run(() => s.Execute(ps.Bind(id)));
                }
                result.Result = true; result.IsError = false; result.Message = $"ScyllaDBOASIS: Holon '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"ScyllaDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteHolon(Guid id, bool softDelete = true) => DeleteHolonAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteHolonAsync(string pk, bool softDelete = true) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id, softDelete); var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"ScyllaDBOASIS: Invalid GUID '{pk}'."); return r; }
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

        public override Task<OASISResult<IAvatar>> SearchAvatarsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("ScyllaDBOASIS: SearchAvatarsAsync not implemented. Use SearchAsync.");
        public override OASISResult<IAvatar> SearchAvatars(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();
        public override Task<OASISResult<IHolon>> SearchHolonsAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException("ScyllaDBOASIS: SearchHolonsAsync not implemented. Use SearchAsync.");
        public override OASISResult<IHolon> SearchHolons(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => throw new NotImplementedException();

        public override string GetProviderVersion() => "1.0.0";
        public override Task<string> GetProviderVersionAsync() => Task.FromResult("1.0.0");
    }
}
