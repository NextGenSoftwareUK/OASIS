using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS;

public partial class ScyllaDBOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.ScyllaDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities => ScyllaDbCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit ScyllaDB target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = ScyllaDbCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated || _session == null)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured ScyllaDB database before querying.");

            var rows = new List<IHolon>();
            long decodedBytes = 0;
            if (selected.Result.Count != 0)
            {
                // Partition-key IN is bounded by the admitted catalogue; never ALLOW FILTERING.
                // Cassandra does not promise IN ordering, so restore catalogue order before paging.
                var statement = await _session!.PrepareAsync(
                    $"SELECT id, is_deleted, data_json FROM {_keyspace}.oasis_holons WHERE id IN ?");
                cancellationToken.ThrowIfCancellationRequested();
                var records = await _session.ExecuteAsync(statement.Bind(selected.Result.ToArray()));
                var materialized = new Dictionary<Guid, IHolon>();
                var seen = new HashSet<Guid>();
                foreach (var record in records)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var id = record.GetValue<Guid>("id");
                    if (!selected.Result.Contains(id) || !seen.Add(id))
                        throw new InvalidDataException("Native identity outside admitted catalogue or duplicated.");
                    var json = record.GetValue<string>("data_json");
                    if (json == null) throw new InvalidDataException("Native payload missing.");
                    decodedBytes = checked(decodedBytes + Encoding.UTF8.GetByteCount(json));
                    if (decodedBytes > 4 * 1024 * 1024)
                        return Error("CQL_QUERY_LIMIT", "Complete decoded payload exceeds 4 MiB.");
                    var row = Des<Holon>(json);
                    if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Native holon differs from read authority.");
                    if (record.GetValue<bool>("is_deleted") || row.IsDeleted) continue;
                    row.Children = Array.Empty<IHolon>();
                    materialized.Add(id, row);
                }
                foreach (var id in selected.Result)
                    if (materialized.TryGetValue(id, out var row)) rows.Add(row);
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "ScyllaDB query cancelled; no partial rows published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native ScyllaDB query failed; no partial rows published.", cause); }
    }
}
