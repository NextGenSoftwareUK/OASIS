using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using ClickHouse.Client.ADO;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS;

public partial class ClickHouseOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.ClickHouseOASIS;
    public CosmicQueryCapabilities QueryCapabilities => ClickHouseCqlCompiler.Capabilities;

    protected sealed class NativeCqlDocument
    {
        public NativeCqlDocument() { }
        public string Key { get; set; } = "";
        public string Id { get; set; } = "";
        public bool IsDeleted { get; set; }
        public string DataJson { get; set; } = "";
    }

    protected virtual async IAsyncEnumerable<NativeCqlDocument> ReadNativeCqlAsync(
        IReadOnlyList<Guid> ids, [EnumeratorCancellation] CancellationToken cancellation)
    {
        using var connection = new ClickHouseConnection(_connectionString);
        await connection.OpenAsync(cancellation);
        using var command = connection.CreateCommand();
        var names = new List<string>();
        for (var i = 0; i < ids.Count; i++)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = "id"+i;
            parameter.Value = ids[i];
            command.Parameters.Add(parameter);
            names.Add("{"+parameter.ParameterName+":UUID}");
        }
        // FINAL resolves native ReplacingMergeTree versions before materialization.
        // It is not an atomic compare-and-swap or a cross-provider snapshot.
        command.CommandText = "SELECT id, is_deleted, data_json FROM oasis.holons FINAL WHERE id IN ("+
            string.Join(",", names)+") SETTINGS max_execution_time=15, max_result_rows=101, result_overflow_mode='throw', max_result_bytes=8388608";
        command.CommandTimeout = 20;
        using var reader = await command.ExecuteReaderAsync(cancellation);
        while (await reader.ReadAsync(cancellation))
        {
            var id = reader.GetGuid(0).ToString("D");
            yield return new NativeCqlDocument { Key = id, Id = id,
                IsDeleted = reader.GetByte(1) != 0, DataJson = reader.GetString(2) };
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
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit ClickHouse target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = ClickHouseCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "ClickHouse history is not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate configured ClickHouse collections before querying.");
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
        { return Error("CQL_QUERY_CANCELLED", "ClickHouse query cancelled; no partial rows published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native ClickHouse query failed; no partial rows published.", cause); }
    }
}
