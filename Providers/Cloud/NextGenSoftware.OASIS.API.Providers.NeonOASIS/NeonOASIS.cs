using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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

namespace NextGenSoftware.OASIS.API.Providers.NeonOASIS
{
    /// <summary>
    /// OASIS provider for Neon — serverless PostgreSQL via Npgsql ADO.NET.
    ///
    /// Neon is a serverless, autoscaling PostgreSQL with branching support.
    /// It is PostgreSQL-wire-compatible; this provider is a named, pre-configured
    /// wrapper that enforces Neon's required SSL mode and connection pooler port.
    ///
    /// Tables are auto-created on ActivateProvider.
    ///
    /// Constructor parameters:
    ///   connectionString — Npgsql connection string pointing to Neon, e.g.:
    ///     "Host=ep-xxx.us-east-2.aws.neon.tech;Database=neondb;Username=user;Password=pass;SslMode=Require;Trust Server Certificate=true"
    ///   usePooler — set true to append the Neon connection pooler port (5432→6543).
    ///               Neon recommends the pooler for serverless workloads.
    /// </summary>
    public class NeonOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _connectionString;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public NeonOASIS(string connectionString, bool usePooler = true)
        {
            // Neon's pooler listens on port 6543 by default; swap the port if requested
            if (usePooler && connectionString.Contains("neon.tech") && !connectionString.Contains("Port=6543") && !connectionString.Contains(":6543"))
                _connectionString = connectionString.Replace("Port=5432", "Port=6543");
            else
                _connectionString = connectionString;

            ProviderName = "NeonOASIS";
            ProviderDescription = "Neon provider (serverless PostgreSQL via Npgsql ADO.NET)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.NeonOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocalAndNetwork);
        }

        private async Task<NpgsqlConnection> OpenAsync() { var conn = new NpgsqlConnection(_connectionString); await conn.OpenAsync(); return conn; }
        private static string Ser(object obj) => JsonSerializer.Serialize(obj, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private async Task EnsureTablesAsync()
        {
            await using var conn = await OpenAsync();
            async Task Exec(string sql) { await using var cmd = new NpgsqlCommand(sql, conn); await cmd.ExecuteNonQueryAsync(); }
            await Exec("CREATE TABLE IF NOT EXISTS oasis_avatars (id UUID PRIMARY KEY, username TEXT, email TEXT, is_deleted BOOL NOT NULL DEFAULT FALSE, data_json TEXT)");
            await Exec("CREATE INDEX IF NOT EXISTS idx_avatars_username ON oasis_avatars(username)");
            await Exec("CREATE INDEX IF NOT EXISTS idx_avatars_email ON oasis_avatars(email)");
            await Exec("CREATE TABLE IF NOT EXISTS oasis_avatar_details (id UUID PRIMARY KEY, username TEXT, email TEXT, data_json TEXT)");
            await Exec("CREATE TABLE IF NOT EXISTS oasis_holons (id UUID PRIMARY KEY, parent_holon_id UUID, holon_type INT DEFAULT 0, is_deleted BOOL NOT NULL DEFAULT FALSE, data_json TEXT)");
            await Exec("CREATE INDEX IF NOT EXISTS idx_holons_parent ON oasis_holons(parent_holon_id)");
            await Exec("CREATE INDEX IF NOT EXISTS idx_holons_type ON oasis_holons(holon_type)");
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try { await EnsureTablesAsync(); result.Result = true; result.IsError = false; result.Message = "NeonOASIS activated — tables ready on Neon serverless PostgreSQL."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: Error activating — {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync() => await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "NeonOASIS deactivated." });
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        private async Task<Avatar?> QueryAvatarAsync(string sql, Action<NpgsqlCommand> bind)
        {
            await using var conn = await OpenAsync(); await using var cmd = new NpgsqlCommand(sql, conn); bind(cmd);
            await using var r = await cmd.ExecuteReaderAsync(); if (!await r.ReadAsync()) return null; return Des<Avatar>(r.GetString(0));
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.NeonOASIS] = avatar.Id.ToString();
                await using var conn = await OpenAsync();
                await using var cmd = new NpgsqlCommand("INSERT INTO oasis_avatars(id,username,email,is_deleted,data_json) VALUES(@id,@u,@e,@del,@data) ON CONFLICT(id) DO UPDATE SET username=EXCLUDED.username,email=EXCLUDED.email,is_deleted=EXCLUDED.is_deleted,data_json=EXCLUDED.data_json", conn);
                cmd.Parameters.AddWithValue("id", avatar.Id); cmd.Parameters.AddWithValue("u", (object?)avatar.Username ?? DBNull.Value); cmd.Parameters.AddWithValue("e", (object?)avatar.Email ?? DBNull.Value); cmd.Parameters.AddWithValue("del", avatar.IsDeleted); cmd.Parameters.AddWithValue("data", Ser(avatar));
                await cmd.ExecuteNonQueryAsync();
                result.Result = avatar; result.IsError = false; result.Message = $"NeonOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE id=@id AND is_deleted=FALSE LIMIT 1", c => c.Parameters.AddWithValue("id", id)); if (a == null) { OASISErrorHandling.HandleError(ref result, $"NeonOASIS: No avatar for ID '{id}'."); return result; } result.Result = a; result.IsError = false; result.Message = "NeonOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE username=@u AND is_deleted=FALSE LIMIT 1", c => c.Parameters.AddWithValue("u", username)); if (a == null) { OASISErrorHandling.HandleError(ref result, $"NeonOASIS: No avatar for username '{username}'."); return result; } result.Result = a; result.IsError = false; result.Message = "NeonOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await QueryAvatarAsync("SELECT data_json FROM oasis_avatars WHERE email=@e AND is_deleted=FALSE LIMIT 1", c => c.Parameters.AddWithValue("e", email)); if (a == null) { OASISErrorHandling.HandleError(ref result, $"NeonOASIS: No avatar for email '{email}'."); return result; } result.Result = a; result.IsError = false; result.Message = "NeonOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0)
        { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"NeonOASIS: Invalid GUID '{pk}'."); return r; }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                await using var conn = await OpenAsync(); await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_avatars WHERE is_deleted=FALSE", conn); await using var r = await cmd.ExecuteReaderAsync();
                var list = new List<IAvatar>(); while (await r.ReadAsync()) { var a = Des<Avatar>(r.GetString(0)); if (a != null) list.Add(a); }
                result.Result = list; result.IsError = false; result.Message = $"NeonOASIS: Loaded {list.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                await using var conn = await OpenAsync(); string sql = softDelete ? "UPDATE oasis_avatars SET is_deleted=TRUE WHERE id=@id" : "DELETE FROM oasis_avatars WHERE id=@id";
                await using var cmd = new NpgsqlCommand(sql, conn); cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync();
                result.Result = true; result.IsError = false; result.Message = $"NeonOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, $"NeonOASIS: Avatar '{u}' not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "NeonOASIS: Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                await using var conn = await OpenAsync();
                await using var cmd = new NpgsqlCommand("INSERT INTO oasis_avatar_details(id,username,email,data_json) VALUES(@id,@u,@e,@data) ON CONFLICT(id) DO UPDATE SET username=EXCLUDED.username,email=EXCLUDED.email,data_json=EXCLUDED.data_json", conn);
                cmd.Parameters.AddWithValue("id", d.Id); cmd.Parameters.AddWithValue("u", (object?)d.Username ?? DBNull.Value); cmd.Parameters.AddWithValue("e", (object?)d.Email ?? DBNull.Value); cmd.Parameters.AddWithValue("data", Ser(d));
                await cmd.ExecuteNonQueryAsync();
                result.Result = d; result.IsError = false; result.Message = "NeonOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        private async Task<AvatarDetail?> QueryDetailAsync(string sql, Action<NpgsqlCommand> bind)
        {
            await using var conn = await OpenAsync(); await using var cmd = new NpgsqlCommand(sql, conn); bind(cmd);
            await using var r = await cmd.ExecuteReaderAsync(); if (!await r.ReadAsync()) return null; return Des<AvatarDetail>(r.GetString(0));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await QueryDetailAsync("SELECT data_json FROM oasis_avatar_details WHERE id=@id LIMIT 1", c => c.Parameters.AddWithValue("id", id)); if (d == null) { OASISErrorHandling.HandleError(ref result, $"NeonOASIS: No detail for ID '{id}'."); return result; } result.Result = d; result.IsError = false; result.Message = "NeonOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await QueryDetailAsync("SELECT data_json FROM oasis_avatar_details WHERE username=@u LIMIT 1", c => c.Parameters.AddWithValue("u", u)); if (d == null) { OASISErrorHandling.HandleError(ref result, $"NeonOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "NeonOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try { var d = await QueryDetailAsync("SELECT data_json FROM oasis_avatar_details WHERE email=@e LIMIT 1", c => c.Parameters.AddWithValue("e", e)); if (d == null) { OASISErrorHandling.HandleError(ref result, $"NeonOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "NeonOASIS: AvatarDetail loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                await using var conn = await OpenAsync(); await using var cmd = new NpgsqlCommand("SELECT data_json FROM oasis_avatar_details", conn); await using var r = await cmd.ExecuteReaderAsync();
                var list = new List<IAvatarDetail>(); while (await r.ReadAsync()) { var d = Des<AvatarDetail>(r.GetString(0)); if (d != null) list.Add(d); }
                result.Result = list; result.IsError = false; result.Message = $"NeonOASIS: Loaded {list.Count} detail(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0) => LoadAllAvatarDetailsAsync(version).Result;

        // ─── Holons ───────────────────────────────────────────────────────────────

        private async Task<List<IHolon>> QueryHolonsAsync(string sql, Action<NpgsqlCommand>? bind = null)
        {
            await using var conn = await OpenAsync(); await using var cmd = new NpgsqlCommand(sql, conn); bind?.Invoke(cmd);
            await using var r = await cmd.ExecuteReaderAsync();
            var list = new List<IHolon>(); while (await r.ReadAsync()) { var h = Des<Holon>(r.GetString(0)); if (h != null) list.Add(h); } return list;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (holon.Id == Guid.Empty) holon.Id = Guid.NewGuid();
                if (holon.ProviderUniqueStorageKey == null) holon.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.NeonOASIS] = holon.Id.ToString();
                await using var conn = await OpenAsync();
                await using var cmd = new NpgsqlCommand("INSERT INTO oasis_holons(id,parent_holon_id,holon_type,is_deleted,data_json) VALUES(@id,@parent,@type,@del,@data) ON CONFLICT(id) DO UPDATE SET parent_holon_id=EXCLUDED.parent_holon_id,holon_type=EXCLUDED.holon_type,is_deleted=EXCLUDED.is_deleted,data_json=EXCLUDED.data_json", conn);
                cmd.Parameters.AddWithValue("id", holon.Id); cmd.Parameters.AddWithValue("parent", holon.ParentHolonId == Guid.Empty ? (object)DBNull.Value : holon.ParentHolonId); cmd.Parameters.AddWithValue("type", (int)holon.HolonType); cmd.Parameters.AddWithValue("del", holon.IsDeleted); cmd.Parameters.AddWithValue("data", Ser(holon));
                await cmd.ExecuteNonQueryAsync();
                result.Result = holon; result.IsError = false; result.Message = $"NeonOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"NeonOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try { var list = await QueryHolonsAsync("SELECT data_json FROM oasis_holons WHERE id=@id AND is_deleted=FALSE LIMIT 1", c => c.Parameters.AddWithValue("id", id)); if (list.Count == 0) { OASISErrorHandling.HandleError(ref result, $"NeonOASIS: No holon for ID '{id}'."); return result; } result.Result = list[0]; result.IsError = false; result.Message = "NeonOASIS: Holon loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"NeonOASIS: Invalid GUID '{pk}'."); return r; }

        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = type == HolonType.All ? "SELECT data_json FROM oasis_holons WHERE is_deleted=FALSE" : "SELECT data_json FROM oasis_holons WHERE is_deleted=FALSE AND holon_type=@type";
                var list = await QueryHolonsAsync(sql, type == HolonType.All ? null : (Action<NpgsqlCommand>)(c => c.Parameters.AddWithValue("type", (int)type)));
                result.Result = list; result.IsError = false; result.Message = $"NeonOASIS: Loaded {list.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                string sql = type == HolonType.All ? "SELECT data_json FROM oasis_holons WHERE parent_holon_id=@parent AND is_deleted=FALSE" : "SELECT data_json FROM oasis_holons WHERE parent_holon_id=@parent AND is_deleted=FALSE AND holon_type=@type";
                var list = await QueryHolonsAsync(sql, c => { c.Parameters.AddWithValue("parent", id); if (type != HolonType.All) c.Parameters.AddWithValue("type", (int)type); });
                result.Result = list; result.IsError = false; result.Message = $"NeonOASIS: Loaded {list.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"NeonOASIS: Invalid GUID '{pk}'."); return r; }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"NeonOASIS: Holon '{id}' not found."); return result; }
                await using var conn = await OpenAsync(); await using var cmd = new NpgsqlCommand("UPDATE oasis_holons SET is_deleted=TRUE WHERE id=@id", conn); cmd.Parameters.AddWithValue("id", id); await cmd.ExecuteNonQueryAsync();
                result.Result = loaded.Result; result.IsError = false; result.Message = $"NeonOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"NeonOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"NeonOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"NeonOASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"NeonOASIS: {holons.Count} holon(s)." }; }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"NeonOASIS: {holons.Count} holon(s)." }; }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(); var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = h, IsError = false, Message = $"NeonOASIS: {h.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
