using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
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
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS
{
    /// <summary>
    /// OASIS provider for InfluxDB — time-series database accessed via the official InfluxDB.Client SDK.
    ///
    /// OASIS objects are stored as single-timestamp points (UTC now at write time) in measurements:
    ///   oasis_avatars, oasis_avatar_details, oasis_holons
    ///
    /// Each point has:
    ///   - tag  "id"              — OASIS object UUID (used for fast tag-based lookup)
    ///   - tag  "username" / "email" / "parent_holon_id" / "holon_type"
    ///   - field "is_deleted"     — boolean flag
    ///   - field "data_json"      — full serialised object
    ///
    /// Because InfluxDB is append-only, "update" writes a new point with a newer timestamp.
    /// Queries use the Flux language, reading only the latest point per ID.
    ///
    /// Constructor parameters:
    ///   url    — InfluxDB URL, e.g. "http://localhost:8086" or "https://us-east-1-1.aws.cloud2.influxdata.com"
    ///   token  — InfluxDB API token
    ///   org    — organisation name or ID
    ///   bucket — bucket name (will be created if it does not exist)
    /// </summary>
    public class InfluxDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISDBStorageProvider
    {
        private readonly string _url;
        private readonly string _token;
        private readonly string _org;
        private readonly string _bucket;
        private InfluxDBClient? _client;

        private static readonly JsonSerializerOptions _jsonOpts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public InfluxDBOASIS(string url, string token, string org, string bucket)
        {
            _url = url; _token = token; _org = org; _bucket = bucket;
            ProviderName = "InfluxDBOASIS";
            ProviderDescription = "InfluxDB time-series database provider (InfluxDB.Client SDK — OASIS data as tagged measurement points)";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.InfluxDBOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocal);
        }

        private static string Ser(object o) => JsonSerializer.Serialize(o, _jsonOpts);
        private static T? Des<T>(string? json) => json == null ? default : JsonSerializer.Deserialize<T>(json, _jsonOpts);

        private InfluxDBClient GetClient() => _client ??= InfluxDBClientFactory.Create(_url, _token.ToCharArray());

        // ─── Flux helpers ─────────────────────────────────────────────────────────

        private async Task WritePointAsync(PointData point)
        {
            using var write = GetClient().GetWriteApiAsync();
            await write.WritePointAsync(point, _bucket, _org);
        }

        private async Task<List<FluxRecord>> QueryAsync(string flux)
        {
            var query = GetClient().GetQueryApi();
            var tables = await query.QueryAsync(flux, _org);
            return tables.SelectMany(t => t.Records).ToList();
        }

        // Build a Flux query that returns the latest record per tag "id" for a measurement
        private static string LatestByIdFlux(string bucket, string measurement, string? tagFilter = null)
        {
            var filter = tagFilter != null ? $"|> filter(fn: (r) => {tagFilter})" : "";
            return $@"
from(bucket: ""{bucket}"")
  |> range(start: -100y)
  |> filter(fn: (r) => r._measurement == ""{measurement}"")
  {filter}
  |> pivot(rowKey: [""_time""], columnKey: [""_field""], valueColumn: ""_value"")
  |> group(columns: [""id""])
  |> last(column: ""_time"")
";
        }

        private static string? Field(FluxRecord rec, string name) { if (rec.Values.TryGetValue(name, out var v)) return v?.ToString(); return null; }
        private static bool FieldBool(FluxRecord rec, string name) { if (rec.Values.TryGetValue(name, out var v)) return v is bool b && b; return false; }

        // ─── Activation ───────────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                // Ensure bucket exists; create if absent
                var buckets = GetClient().GetBucketsApi();
                var existing = await buckets.FindBucketByNameAsync(_bucket);
                if (existing == null)
                {
                    var orgs = GetClient().GetOrganizationsApi();
                    var org = (await orgs.FindOrganizationsAsync(org: _org)).FirstOrDefault();
                    if (org == null) throw new Exception($"InfluxDB: Organisation '{_org}' not found.");
                    await buckets.CreateBucketAsync(new BucketRetentionRules(), _bucket, org);
                }
                result.Result = true; result.IsError = false; result.Message = $"InfluxDBOASIS activated — bucket '{_bucket}' ready.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().Result;
        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        { _client?.Dispose(); _client = null; return await Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "InfluxDBOASIS deactivated." }); }
        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().Result;

        // ─── Avatar saving ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (avatar.Id == Guid.Empty) avatar.Id = Guid.NewGuid();
                if (avatar.ProviderUniqueStorageKey == null) avatar.ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string>();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.InfluxDBOASIS] = avatar.Id.ToString();
                var point = PointData.Measurement("oasis_avatars")
                    .Tag("id", avatar.Id.ToString())
                    .Tag("username", avatar.Username ?? "")
                    .Tag("email", avatar.Email ?? "")
                    .Field("is_deleted", avatar.IsDeleted)
                    .Field("data_json", Ser(avatar))
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);
                await WritePointAsync(point);
                result.Result = avatar; result.IsError = false; result.Message = $"InfluxDBOASIS: Avatar '{avatar.Username}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar) => SaveAvatarAsync(avatar).Result;

        // ─── Avatar loading ───────────────────────────────────────────────────────

        private async Task<Avatar?> LoadAvatarByTagAsync(string tagName, string tagValue)
        {
            var flux = LatestByIdFlux(_bucket, "oasis_avatars", $"r.{tagName} == \"{EscapeFlux(tagValue)}\"");
            var records = await QueryAsync(flux);
            var rec = records.FirstOrDefault(r => !FieldBool(r, "is_deleted"));
            if (rec == null) return null;
            return Des<Avatar>(Field(rec, "data_json"));
        }

        private static string EscapeFlux(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await LoadAvatarByTagAsync("id", id.ToString()); if (a == null) { OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: No avatar for ID '{id}'."); return result; } result.Result = a; result.IsError = false; result.Message = "InfluxDBOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0) => LoadAvatarAsync(id, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await LoadAvatarByTagAsync("username", username); if (a == null) { OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: No avatar for username '{username}'."); return result; } result.Result = a; result.IsError = false; result.Message = "InfluxDBOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0) => LoadAvatarByUsernameAsync(username, version).Result;

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try { var a = await LoadAvatarByTagAsync("email", email); if (a == null) { OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: No avatar for email '{email}'."); return result; } result.Result = a; result.IsError = false; result.Message = "InfluxDBOASIS: Avatar loaded."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0) => LoadAvatarByEmailAsync(email, version).Result;
        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string pk, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadAvatarAsync(id, version); var r = new OASISResult<IAvatar>(); OASISErrorHandling.HandleError(ref r, $"InfluxDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string pk, int version = 0) => LoadAvatarByProviderKeyAsync(pk, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var records = await QueryAsync(LatestByIdFlux(_bucket, "oasis_avatars"));
                var avatars = records.Where(r => !FieldBool(r, "is_deleted")).Select(r => Des<Avatar>(Field(r, "data_json"))).Where(a => a != null).Cast<IAvatar>().ToList();
                result.Result = avatars; result.IsError = false; result.Message = $"InfluxDBOASIS: Loaded {avatars.Count} avatar(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0) => LoadAllAvatarsAsync(version).Result;

        // ─── Avatar deletion ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var loaded = await LoadAvatarAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: Avatar '{id}' not found."); return result; }
                var av = (Avatar)loaded.Result; av.DeletedDate = DateTime.UtcNow;
                // Soft-delete: write new point with is_deleted=true (InfluxDB is append-only, newest wins)
                var point = PointData.Measurement("oasis_avatars")
                    .Tag("id", id.ToString()).Tag("username", av.Username ?? "").Tag("email", av.Email ?? "")
                    .Field("is_deleted", true).Field("data_json", Ser(av))
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);
                await WritePointAsync(point);
                result.Result = true; result.IsError = false; result.Message = $"InfluxDBOASIS: Avatar '{id}' deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true) => DeleteAvatarAsync(id, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string u, bool softDelete = true) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
        public override OASISResult<bool> DeleteAvatarByUsername(string u, bool softDelete = true) => DeleteAvatarByUsernameAsync(u, softDelete).Result;
        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string e, bool softDelete = true) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<bool>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await DeleteAvatarAsync(a.Result.Id, softDelete); }
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
                var point = PointData.Measurement("oasis_avatar_details")
                    .Tag("id", d.Id.ToString()).Tag("username", d.Username ?? "").Tag("email", d.Email ?? "")
                    .Field("data_json", Ser(d)).Timestamp(DateTime.UtcNow, WritePrecision.Ns);
                await WritePointAsync(point);
                result.Result = d; result.IsError = false; result.Message = "InfluxDBOASIS: AvatarDetail saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail d) => SaveAvatarDetailAsync(d).Result;

        private async Task<AvatarDetail?> LoadDetailByTagAsync(string tag, string val)
        {
            var flux = LatestByIdFlux(_bucket, "oasis_avatar_details", $"r.{tag} == \"{EscapeFlux(val)}\"");
            var records = await QueryAsync(flux);
            var rec = records.FirstOrDefault();
            return rec == null ? null : Des<AvatarDetail>(Field(rec, "data_json"));
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await LoadDetailByTagAsync("id", id.ToString()); if (d == null) { OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: No detail for ID '{id}'."); return result; } result.Result = d; result.IsError = false; result.Message = "InfluxDBOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0) => LoadAvatarDetailAsync(id, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string u, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await LoadDetailByTagAsync("username", u); if (d == null) { OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: No detail for username '{u}'."); return result; } result.Result = d; result.IsError = false; result.Message = "InfluxDBOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string u, int version = 0) => LoadAvatarDetailByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string e, int version = 0) { var result = new OASISResult<IAvatarDetail>(); try { var d = await LoadDetailByTagAsync("email", e); if (d == null) { OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: No detail for email '{e}'."); return result; } result.Result = d; result.IsError = false; result.Message = "InfluxDBOASIS: AvatarDetail loaded."; } catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); } return result; }
        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string e, int version = 0) => LoadAvatarDetailByEmailAsync(e, version).Result;

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try { var records = await QueryAsync(LatestByIdFlux(_bucket, "oasis_avatar_details")); var details = records.Select(r => Des<AvatarDetail>(Field(r, "data_json"))).Where(d => d != null).Cast<IAvatarDetail>().ToList(); result.Result = details; result.IsError = false; result.Message = $"InfluxDBOASIS: Loaded {details.Count} detail(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
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
                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.InfluxDBOASIS] = holon.Id.ToString();
                var point = PointData.Measurement("oasis_holons")
                    .Tag("id", holon.Id.ToString())
                    .Tag("parent_holon_id", holon.ParentHolonId.ToString())
                    .Tag("holon_type", ((int)holon.HolonType).ToString())
                    .Field("is_deleted", holon.IsDeleted)
                    .Field("data_json", Ser(holon))
                    .Timestamp(DateTime.UtcNow, WritePrecision.Ns);
                await WritePointAsync(point);
                result.Result = holon; result.IsError = false; result.Message = $"InfluxDBOASIS: Holon '{holon.Name}' saved.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            var saved = new List<IHolon>(); var errors = new List<string>();
            foreach (var h in holons) { var r = await SaveHolonAsync(h, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider); if (r.IsError) errors.Add(r.Message ?? ""); else if (r.Result != null) saved.Add(r.Result); }
            result.Result = saved; result.IsError = errors.Count > 0; result.Message = errors.Count > 0 ? string.Join("; ", errors) : $"InfluxDBOASIS: {saved.Count} holon(s) saved.";
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false) => SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;

        // ─── Holon loading ────────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var records = await QueryAsync(LatestByIdFlux(_bucket, "oasis_holons", $"r.id == \"{EscapeFlux(id.ToString())}\""));
                var rec = records.FirstOrDefault(r => !FieldBool(r, "is_deleted")); if (rec == null) { OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: No holon for ID '{id}'."); return result; }
                var holon = Des<Holon>(Field(rec, "data_json")); if (holon == null) { OASISErrorHandling.HandleError(ref result, "InfluxDBOASIS: Deserialise failed."); return result; }
                result.Result = holon; result.IsError = false; result.Message = "InfluxDBOASIS: Holon loaded.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"InfluxDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> LoadHolon(string pk, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonAsync(pk, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var records = await QueryAsync(LatestByIdFlux(_bucket, "oasis_holons"));
                var holons = records.Where(r => !FieldBool(r, "is_deleted") && (type == HolonType.All || r.Values.TryGetValue("holon_type", out var ht) && ht?.ToString() == ((int)type).ToString())).Select(r => Des<Holon>(Field(r, "data_json"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"InfluxDBOASIS: Loaded {holons.Count} holon(s).";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var idStr = id.ToString();
                var records = await QueryAsync(LatestByIdFlux(_bucket, "oasis_holons", $"r.parent_holon_id == \"{EscapeFlux(idStr)}\""));
                var holons = records.Where(r => !FieldBool(r, "is_deleted") && (type == HolonType.All || r.Values.TryGetValue("holon_type", out var ht) && ht?.ToString() == ((int)type).ToString())).Select(r => Des<Holon>(Field(r, "data_json"))).Where(h => h != null).Cast<IHolon>().ToList();
                result.Result = holons; result.IsError = false; result.Message = $"InfluxDBOASIS: Loaded {holons.Count} holon(s) for parent '{id}'.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) { if (Guid.TryParse(pk, out Guid id)) return await LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version); var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, $"InfluxDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string pk, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsForParentAsync(pk, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Holon deletion ───────────────────────────────────────────────────────

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var loaded = await LoadHolonAsync(id); if (loaded.IsError || loaded.Result == null) { OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: Holon '{id}' not found."); return result; }
                var holon = (Holon)loaded.Result; holon.DeletedDate = DateTime.UtcNow;
                var point = PointData.Measurement("oasis_holons").Tag("id", id.ToString()).Tag("parent_holon_id", holon.ParentHolonId.ToString()).Tag("holon_type", ((int)holon.HolonType).ToString()).Field("is_deleted", true).Field("data_json", Ser(holon)).Timestamp(DateTime.UtcNow, WritePrecision.Ns);
                await WritePointAsync(point);
                result.Result = holon; result.IsError = false; result.Message = $"InfluxDBOASIS: Holon '{id}' soft-deleted.";
            }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, $"InfluxDBOASIS: {ex.Message}"); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id) => DeleteHolonAsync(id).Result;
        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string pk) { if (Guid.TryParse(pk, out Guid id)) return await DeleteHolonAsync(id); var r = new OASISResult<IHolon>(); OASISErrorHandling.HandleError(ref r, $"InfluxDBOASIS: Invalid GUID '{pk}'."); return r; }
        public override OASISResult<IHolon> DeleteHolon(string pk) => DeleteHolonAsync(pk).Result;

        // ─── Search ───────────────────────────────────────────────────────────────

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            try { string? q = searchParams.SearchGroups?.OfType<SearchTextGroup>().FirstOrDefault()?.SearchQuery?.ToLower(); var all = await LoadAllHolonsAsync(); var holons = all.Result?.ToList() ?? new List<IHolon>(); if (!string.IsNullOrEmpty(q)) holons = holons.Where(h => h.Name?.ToLower().Contains(q) == true || h.Description?.ToLower().Contains(q) == true).ToList(); result.Result = new SearchResults { SearchResultHolons = holons, NumberOfResults = holons.Count }; result.IsError = false; result.Message = $"InfluxDBOASIS: Found {holons.Count} result(s)."; }
            catch (Exception ex) { result.Exception = ex; OASISErrorHandling.HandleError(ref result, ex.Message); }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0) => SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); var holons = all.Result?.Where(h => h.MetaData != null && h.MetaData.TryGetValue(metaKey, out var v) && v?.ToString() == metaValue).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"InfluxDBOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        { var all = await LoadAllHolonsAsync(type); bool IsMatch(IHolon h) { if (h.MetaData == null) return false; var checks = metaKvp.Select(kvp => h.MetaData.TryGetValue(kvp.Key, out var v) && v?.ToString() == kvp.Value); return mode == MetaKeyValuePairMatchMode.Any ? checks.Any(c => c) : checks.All(c => c); } var holons = all.Result?.Where(IsMatch).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = holons, IsError = false, Message = $"InfluxDBOASIS: {holons.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKvp, MetaKeyValuePairMatchMode mode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0) => LoadHolonsByMetaDataAsync(metaKvp, mode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;

        // ─── Import / Export ──────────────────────────────────────────────────────

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons) { var s = await SaveHolonsAsync(holons); return new OASISResult<bool> { Result = !s.IsError, IsError = s.IsError, Message = s.Message }; }
        public override OASISResult<bool> Import(IEnumerable<IHolon> holons) => ImportAsync(holons).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0) => await LoadAllHolonsAsync();
        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0) => ExportAllAsync(version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0) { var all = await LoadAllHolonsAsync(); var h = all.Result?.Where(x => x.CreatedByAvatarId == avatarId).ToList() ?? new List<IHolon>(); return new OASISResult<IEnumerable<IHolon>> { Result = h, IsError = false, Message = $"InfluxDBOASIS: {h.Count} holon(s)." }; }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0) => ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string u, int version = 0) { var a = await LoadAvatarByUsernameAsync(u); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string u, int version = 0) => ExportAllDataForAvatarByUsernameAsync(u, version).Result;
        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string e, int version = 0) { var a = await LoadAvatarByEmailAsync(e); if (a.IsError || a.Result == null) { var r = new OASISResult<IEnumerable<IHolon>>(); OASISErrorHandling.HandleError(ref r, "Not found."); return r; } return await ExportAllDataForAvatarByIdAsync(a.Result.Id, version); }
        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string e, int version = 0) => ExportAllDataForAvatarByEmailAsync(e, version).Result;

        public bool IsVersionControlEnabled { get; set; } = false;
    }
}
