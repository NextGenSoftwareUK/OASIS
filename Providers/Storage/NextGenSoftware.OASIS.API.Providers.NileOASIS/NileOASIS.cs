using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.NileOASIS
{
    /// <summary>Nile — serverless Postgres with multi-tenant isolation; each tenant gets its own virtual database.</summary>
    public class NileOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider, IOASISBlockchainStorageProvider
    {
        private readonly string _connectionString;
        private bool _isActivated;

        public NileOASIS(string connectionString)
        {
            _connectionString = connectionString;
            ProviderName = "NileOASIS";
            ProviderDescription = "Nile Serverless Postgres Multi-Tenant SaaS Provider";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.NileOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage);
            ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Network));
        }

        private NpgsqlConnection OpenConnection() => new NpgsqlConnection(_connectionString);

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try
            {
                if (_isActivated) { r.Result = true; r.Message = "NileOASIS already activated"; return r; }
                await using var conn = OpenConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS oasis_avatars (
                        id UUID PRIMARY KEY,
                        data JSONB NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS oasis_holons (
                        id UUID PRIMARY KEY,
                        data JSONB NOT NULL
                    );";
                await cmd.ExecuteNonQueryAsync();
                _isActivated = true;
                r.Result = true;
                r.Message = "NileOASIS activated successfully";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS activation failed: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var r = new OASISResult<bool>();
            try { _isActivated = false; r.Result = true; r.Message = "NileOASIS deactivated"; }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS deactivation failed: {ex.Message}", ex); }
            return r;
        }

        // ── Avatars ─────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                await using var conn = OpenConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT data FROM oasis_avatars WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                var val = await cmd.ExecuteScalarAsync();
                if (val == null || val == DBNull.Value) { OASISErrorHandling.HandleError(ref r, $"NileOASIS: avatar {id} not found"); return r; }
                r.Result = JsonConvert.DeserializeObject<Avatar>(val.ToString());
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS LoadAvatarAsync: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string u, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                await using var conn = OpenConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT data FROM oasis_avatars WHERE data->>'Username' = @u LIMIT 1";
                cmd.Parameters.AddWithValue("@u", u);
                var val = await cmd.ExecuteScalarAsync();
                if (val == null || val == DBNull.Value) { OASISErrorHandling.HandleError(ref r, $"NileOASIS: avatar '{u}' not found"); return r; }
                r.Result = JsonConvert.DeserializeObject<Avatar>(val.ToString());
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS LoadAvatarByUsernameAsync: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar a)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                if (a.Id == Guid.Empty) a.Id = Guid.NewGuid();
                var json = JsonConvert.SerializeObject(a);
                await using var conn = OpenConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO oasis_avatars(id, data) VALUES(@id, @data::jsonb)
                    ON CONFLICT(id) DO UPDATE SET data = EXCLUDED.data";
                cmd.Parameters.AddWithValue("@id", a.Id);
                cmd.Parameters.AddWithValue("@data", json);
                await cmd.ExecuteNonQueryAsync();
                r.Result = a;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS SaveAvatarAsync: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool soft = true)
        {
            var r = new OASISResult<bool>();
            try
            {
                if (soft)
                {
                    var load = await LoadAvatarAsync(id);
                    if (!load.IsError && load.Result != null)
                    {
                        load.Result.DeletedDate = DateTime.UtcNow;
                        await SaveAvatarAsync(load.Result);
                    }
                }
                else
                {
                    await using var conn = OpenConnection();
                    await conn.OpenAsync();
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM oasis_avatars WHERE id = @id";
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
                r.Result = true;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS DeleteAvatarAsync: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int v = 0)
        {
            var r = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var list = new List<IAvatar>();
                await using var conn = OpenConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT data FROM oasis_avatars";
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var avatar = JsonConvert.DeserializeObject<Avatar>(reader.GetString(0));
                    if (avatar != null) list.Add(avatar);
                }
                r.Result = list;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS LoadAllAvatarsAsync: {ex.Message}", ex); }
            return r;
        }

        // ── Holons ──────────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                await using var conn = OpenConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT data FROM oasis_holons WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                var val = await cmd.ExecuteScalarAsync();
                if (val == null || val == DBNull.Value) { OASISErrorHandling.HandleError(ref r, $"NileOASIS: holon {id} not found"); return r; }
                r.Result = JsonConvert.DeserializeObject<Holon>(val.ToString());
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS LoadHolonAsync: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                if (h.Id == Guid.Empty) h.Id = Guid.NewGuid();
                var json = JsonConvert.SerializeObject(h);
                await using var conn = OpenConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO oasis_holons(id, data) VALUES(@id, @data::jsonb)
                    ON CONFLICT(id) DO UPDATE SET data = EXCLUDED.data";
                cmd.Parameters.AddWithValue("@id", h.Id);
                cmd.Parameters.AddWithValue("@data", json);
                await cmd.ExecuteNonQueryAsync();
                r.Result = h;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS SaveHolonAsync: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType ht = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var list = new List<IHolon>();
                await using var conn = OpenConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = ht == HolonType.All
                    ? "SELECT data FROM oasis_holons"
                    : "SELECT data FROM oasis_holons WHERE data->>'HolonType' = @ht";
                if (ht != HolonType.All) cmd.Parameters.AddWithValue("@ht", ht.ToString());
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var holon = JsonConvert.DeserializeObject<Holon>(reader.GetString(0));
                    if (holon != null) list.Add(holon);
                }
                r.Result = list;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS LoadAllHolonsAsync: {ex.Message}", ex); }
            return r;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false)
        {
            var r = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>();
            foreach (var h in holons) { var sr = await SaveHolonAsync(h, sc, rec, md, coe, scop); if (!sr.IsError && sr.Result != null) saved.Add(sr.Result); }
            r.Result = saved;
            return r;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var r = new OASISResult<IHolon>();
            try
            {
                await using var conn = OpenConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM oasis_holons WHERE id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
                r.Result = new Holon { Id = id };
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS DeleteHolonAsync: {ex.Message}", ex); }
            return r;
        }

        // ── Remaining boilerplate ────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0) { var r = new OASISResult<ISearchResults>(); r.Result = new SearchResults(); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "NileOASIS does not support avatar detail storage"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail ad) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "NileOASIS does not support avatar detail storage"); return r; }
        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int v = 0) { var r = new OASISResult<IEnumerable<IAvatarDetail>>(); r.Result = new List<IAvatarDetail>(); return r; }
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string k, int v = 0) => await LoadAvatarByUsernameAsync(k, v);
        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string e, int v = 0)
        {
            var r = new OASISResult<IAvatar>();
            try
            {
                await using var conn = OpenConnection();
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT data FROM oasis_avatars WHERE data->>'Email' = @e LIMIT 1";
                cmd.Parameters.AddWithValue("@e", e);
                var val = await cmd.ExecuteScalarAsync();
                if (val == null || val == DBNull.Value) { OASISErrorHandling.HandleError(ref r, $"NileOASIS: avatar with email '{e}' not found"); return r; }
                r.Result = JsonConvert.DeserializeObject<Avatar>(val.ToString());
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref r, $"NileOASIS LoadAvatarByEmailAsync: {ex.Message}", ex); }
            return r;
        }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IAvatarDetail>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string k, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool s = true) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string k, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string k) { var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> h) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid id, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int v = 0) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int v = 0) => LoadAvatarAsync(id, v).Result;
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string k, int v = 0) => LoadAvatarByProviderKeyAsync(k, v).Result;
        public override OASISResult<IAvatar> LoadAvatarByUsername(string u, int v = 0) => LoadAvatarByUsernameAsync(u, v).Result;
        public override OASISResult<IAvatar> LoadAvatarByEmail(string e, int v = 0) => LoadAvatarByEmailAsync(e, v).Result;
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int v = 0) => LoadAllAvatarsAsync(v).Result;
        public override OASISResult<IAvatar> SaveAvatar(IAvatar a) => SaveAvatarAsync(a).Result;
        public override OASISResult<bool> DeleteAvatar(Guid id, bool s = true) => DeleteAvatarAsync(id, s).Result;
        public override OASISResult<bool> DeleteAvatar(string k, bool s = true) => DeleteAvatarAsync(k, s).Result;
        public override OASISResult<bool> DeleteAvatarByEmail(string e, bool s = true) => DeleteAvatarByEmailAsync(e, s).Result;
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool s = true) => DeleteAvatarByUsernameAsync(u, s).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int v = 0) => LoadAvatarDetailAsync(id, v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int v = 0) => LoadAvatarDetailByEmailAsync(e, v).Result;
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int v = 0) => LoadAvatarDetailByUsernameAsync(u, v).Result;
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int v = 0) => LoadAllAvatarDetailsAsync(v).Result;
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail ad) => SaveAvatarDetailAsync(ad).Result;
        public override OASISResult<ISearchResults> Search(ISearchParams sp, bool lc = true, bool rec = true, int md = 0, bool coe = true, int v = 0) => SearchAsync(sp, lc, rec, md, coe, v).Result;
        public override OASISResult<IHolon> LoadHolon(Guid id, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonAsync(id, lc, rec, md, coe, lcfp, v).Result;
        public override OASISResult<IHolon> LoadHolon(string k, bool lc = true, bool rec = true, int md = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonAsync(k, lc, rec, md, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsForParentAsync(id, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string k, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsForParentAsync(k, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string mk, string mv, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsByMetaDataAsync(mk, mv, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> m, MetaKeyValuePairMatchMode mm, HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadHolonsByMetaDataAsync(m, mm, t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType t = HolonType.All, bool lc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool lcfp = false, int v = 0) => LoadAllHolonsAsync(t, lc, rec, md, cd, coe, lcfp, v).Result;
        public override OASISResult<IHolon> SaveHolon(IHolon h, bool sc = true, bool rec = true, int md = 0, bool coe = true, bool scop = false) => SaveHolonAsync(h, sc, rec, md, coe, scop).Result;
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool sc = true, bool rec = true, int md = 0, int cd = 0, bool coe = true, bool scop = false) => SaveHolonsAsync(holons, sc, rec, md, cd, coe, scop).Result;
        public override OASISResult<IHolon> DeleteHolon(Guid id) { var r = DeleteHolonAsync(id).Result; return new OASISResult<IHolon> { IsError = r.IsError, Message = r.Message, Result = r.Result }; }
        public override OASISResult<IHolon> DeleteHolon(string k) { var r = DeleteHolonAsync(k).Result; return new OASISResult<IHolon> { IsError = r.IsError, Message = r.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> h) => ImportAsync(h).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid id, int v = 0) => ExportAllDataForAvatarByIdAsync(id, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int v = 0) => ExportAllDataForAvatarByUsernameAsync(u, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int v = 0) => ExportAllDataForAvatarByEmailAsync(e, v).Result;
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int v = 0) => ExportAllAsync(v).Result;

        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long lat, long lon, int radius) { var r = new OASISResult<IEnumerable<IAvatar>>(); r.Result = new List<IAvatar>(); return r; }
        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long lat, long lon, int radius, HolonType t) { var r = new OASISResult<IEnumerable<IHolon>>(); r.Result = new List<IHolon>(); return r; }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest req) { var r = new OASISResult<ITransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest req) { var r = new OASISResult<double>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest req) { var r = new OASISResult<double>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest req) { var r = new OASISResult<IList<IWalletTransaction>>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest req) { var r = new OASISResult<IList<IWalletTransaction>>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public OASISResult<IKeyPairAndWallet> GenerateKeyPair() { var r = new OASISResult<IKeyPairAndWallet>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync() { var r = new OASISResult<IKeyPairAndWallet>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(System.Threading.CancellationToken token = default) { var r = new OASISResult<(string, string, string)>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seed, System.Threading.CancellationToken token = default) { var r = new OASISResult<(string, string)>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amt, string addr, string key) { var r = new OASISResult<BridgeTransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amt, string addr) { var r = new OASISResult<BridgeTransactionResponse>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string hash, System.Threading.CancellationToken token = default) { var r = new OASISResult<BridgeTransactionStatus>(); OASISErrorHandling.HandleError(ref r, "Not supported"); return r; }
    }
}
