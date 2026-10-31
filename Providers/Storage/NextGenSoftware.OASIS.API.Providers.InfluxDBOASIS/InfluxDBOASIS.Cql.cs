using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using InfluxDB.Client.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS;

public partial class InfluxDBOASIS : ICosmicQueryStorageProvider
{
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
                    if (!record.IsComplete) throw new InvalidDataException("Native latest record lacks required fields.");
                    if (!Guid.TryParseExact(record.Key, "D", out var id) || record.Key != id.ToString("D") ||
                        record.Id != record.Key || !selected.Result.Contains(id) || !seen.Add(id))
                        throw new InvalidDataException("Native key/identity differs from admitted catalogue.");
                    bytes = checked(bytes + Encoding.UTF8.GetByteCount(record.DataJson));
                    if (bytes > 4 * 1024 * 1024)
                        return Error("CQL_QUERY_LIMIT", "Complete decoded payload exceeds 4 MiB.");
                    var row = Des<Holon>(record.DataJson);
                    if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Native holon differs from read authority.");
                    if (record.IsDeleted || row.IsDeleted) continue;
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
