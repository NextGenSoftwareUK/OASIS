using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS;

public partial class SQLServerDBOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => ProviderType.Value;
    private static readonly CosmicQueryCapabilities UnsupportedAliasCapabilities = new(CosmicQueryFeatures.None,
        Array.Empty<CosmicQueryFieldCapability>());
    public CosmicQueryCapabilities QueryCapabilities => QueryProviderType == Core.Enums.ProviderType.SQLServerDBOASIS
        ? SqlServerCqlCompiler.Capabilities : UnsupportedAliasCapabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit SQL Server target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = SqlServerCqlCompiler.CompileNative(plan.Query, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            await using var command = selection.Result.Command;
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured SQL Server provider before querying.");

            // SQL Server FETCH requires a positive count. A logical empty page
            // is returned after activation/selection admission without database I/O.
            if (plan.Query.Limit == 0 || selection.Result.SourceCount == 0)
                return new(Array.AsReadOnly(Array.Empty<IHolon>())) { IsLoaded = true, LoadedCount = 0 };

            // One parameterized native SELECT applies selection/paging before
            // canonical JSON materialization. No managed predicate/source scan.
            await using var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            command.Connection = connection;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var rows = new List<IHolon>();
            var payloadBytes = 0;
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var id = reader.GetGuid(0);
                var json = reader.GetString(1);
                payloadBytes = checked(payloadBytes + Encoding.UTF8.GetByteCount(json));
                if (payloadBytes > 4 * 1024 * 1024)
                    return Error("CQL_QUERY_LIMIT", "The complete decoded response exceeds 4 MiB.");
                var row = Deserialize<Holon>(json);
                if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row) || row.IsDeleted != reader.GetBoolean(2))
                    throw new InvalidDataException("Native SQL Server columns differ from the authorized canonical record.");
                row.Children = Array.Empty<IHolon>();
                row.ProviderUniqueStorageKey[QueryProviderType] = id.ToString("D");
                rows.Add(row);
                if (rows.Count > maximumResults)
                    return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            }
            // Native isolation follows the configured SQL Server database. Federation
            // reads providers separately; no distributed snapshot is claimed.
            var page = rows.ToArray();
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "SQL Server query cancelled; no partial result published.", new OperationCanceledException(cancellationToken)); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native SQL Server native-key query failed; no partial result published.", cause); }
    }
}
