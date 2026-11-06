using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using InfluxDB.Client;
using InfluxDB.Client.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS;

public partial class InfluxDBOASIS : ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation
{
    private readonly Guid? _queryProbeId;

    /// <summary>Explicit public profile with an HTTP transport supplied by its caller.</summary>
    public InfluxDBOASIS(string url, string token, string org, string bucket, HttpClient transport, Guid queryProbeId)
        : this(url, token, org, bucket)
    {
        ArgumentNullException.ThrowIfNull(transport);
        if (queryProbeId == Guid.Empty)
            throw new ArgumentException("An existing public teaching probe is required.", nameof(queryProbeId));
        _queryProbeId = queryProbeId;
        _createClient = () => InfluxDBClientFactory.Create(new InfluxDBClientOptions.Builder()
            .Url(url).AuthenticateToken(token.ToCharArray()).SetHttpClient(transport).Build());
    }

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        IsProviderActivated = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_queryProbeId is not Guid probe)
                throw new InvalidOperationException("An explicit teaching probe is required.");
            var verified = false;
            await foreach (var record in ReadNativeCqlAsync(new[] { probe }, cancellationToken).WithCancellation(cancellationToken))
            {
                if (verified || record.Key != probe.ToString("D") || record.Id != record.Key)
                    throw new InvalidDataException("InfluxDB did not return exactly the configured probe.");
                var row = ReadQueryRecord(record, probe);
                if (record.IsDeleted || row.IsDeleted || !row.IsPublic)
                    throw new InvalidDataException("The teaching probe must be an existing public record.");
                verified = true;
            }
            if (!verified) throw new InvalidDataException("The teaching probe was missing.");
            cancellationToken.ThrowIfCancellationRequested();
            IsProviderActivated = true;
            return new(true);
        }
        catch (Exception cause)
        {
            return new()
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = cause is OperationCanceledException ? "CQL_QUERY_CANCELLED" : "CQL_QUERY_SOURCE_UNAVAILABLE",
                Message = "The native InfluxDB query source did not verify its teaching probe.",
                Exception = cause
            };
        }
    }

    private static Holon ReadQueryRecord(NativeCqlDocument record, Guid id)
    {
        if (!record.IsComplete)
            throw new InvalidDataException("Native latest record lacks required fields.");
        var row = Des<Holon>(record.DataJson);
        if (row == null || row.Id != id)
            throw new InvalidDataException("Native InfluxDB record differs from the requested identity.");
        return row;
    }

    public ProviderType QueryProviderType => Core.Enums.ProviderType.InfluxDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities => InfluxDBCqlCompiler.Capabilities;

    protected sealed class NativeCqlDocument
    {
        public NativeCqlDocument() { }
        [Column("complete")]
        public bool IsComplete { get; set; }
        [Column("id")]
        public string Key { get; set; } = "";
        [Column("id")]
        public string Id { get; set; } = "";
        [Column("is_deleted")]
        public bool IsDeleted { get; set; }
        [Column("data_json")]
        public string DataJson { get; set; } = "";
    }

    protected virtual async IAsyncEnumerable<NativeCqlDocument> ReadNativeCqlAsync(
        IReadOnlyList<Guid> ids, [EnumeratorCancellation] CancellationToken cancellation)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        var flux = InfluxDBCqlCompiler.BuildNativeFlux(_bucket, ids);
        await foreach (var row in GetClient().GetQueryApi()
            .QueryAsyncEnumerable<NativeCqlDocument>(flux, _org, deadline.Token).WithCancellation(deadline.Token))
        {
            yield return row;
        }
    }

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit InfluxDB target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = InfluxDBCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "InfluxDB history is not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate configured InfluxDB collections before querying.");
            var rows = new Dictionary<Guid, IHolon>();
            var seen = new HashSet<Guid>();
            long bytes = 0;
            if (selected.Result.Count != 0)
            {
                await foreach (var record in ReadNativeCqlAsync(selected.Result, cancellationToken).WithCancellation(cancellationToken))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Guid.TryParseExact(record.Key, "D", out var id) || record.Key != id.ToString("D") ||
                        record.Id != record.Key || !selected.Result.Contains(id) || !seen.Add(id))
                        throw new InvalidDataException("Native key/identity differs from admitted catalogue.");
                    bytes = checked(bytes + Encoding.UTF8.GetByteCount(record.DataJson));
                    if (bytes > 4 * 1024 * 1024)
                        return Error("CQL_QUERY_LIMIT", "Complete decoded payload exceeds 4 MiB.");
                    var row = ReadQueryRecord(record, id);
                    if (!plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Native holon differs from read authority.");
                    if (record.IsDeleted || row.IsDeleted) continue;
                    if (_queryProbeId.HasValue && !row.IsPublic)
                        throw new InvalidDataException("Native teaching record is no longer public.");
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(id, row);
                }
            }
            var page = selected.Result.Where(rows.ContainsKey).Select(id => rows[id])
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "InfluxDB query cancelled; no partial rows published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native InfluxDB query failed; no partial rows published.", cause); }
    }
}
