using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SurrealDb.Net.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS;

public partial class SurrealDBOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.SurrealDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities => SurrealDbCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit SurrealDB target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = SurrealDbCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate configured SurrealDB before querying.");

            await using var client = await GetClientAsync(cancellationToken);
            var rows = new List<IHolon>();
            long decodedBytes = 0;
            foreach (var id in selected.Result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // A typed native record key cannot become SurrealQL or a table scan.
                var key = RecordId.From("oasis_holons", id.ToString("D"));
                var record = await client.Select<HolonRecord>(key, cancellationToken);
                if (record == null) continue;
                if (record.Id is null || record.Id.ToString() != key.ToString() || record.DataJson == null)
                    throw new InvalidDataException("Native SurrealDB record differs from selected key.");
                decodedBytes = checked(decodedBytes + Encoding.UTF8.GetByteCount(record.DataJson));
                if (decodedBytes > 4 * 1024 * 1024)
                    return Error("CQL_QUERY_LIMIT", "Complete decoded payload exceeds 4 MiB.");
                var row = Des<Holon>(record.DataJson);
                if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Native holon differs from read authority.");
                if (record.IsDeleted || row.IsDeleted) continue;
                row.Children = Array.Empty<IHolon>();
                rows.Add(row);
            }

            // Independent record reads: no database or federated snapshot guarantee.
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "SurrealDB query cancelled; no partial rows published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native SurrealDB query failed; no partial rows published.", cause); }
    }
}
