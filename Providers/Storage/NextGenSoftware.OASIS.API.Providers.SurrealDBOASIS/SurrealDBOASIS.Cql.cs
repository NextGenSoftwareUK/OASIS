using System;
using System.Collections.Generic;
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
using SurrealDb.Net.Models;

namespace NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS;

public partial class SurrealDBOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.SurrealDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities => BoundedIdentityCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string? code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

        if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
            return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit SurrealDB target required.");
        if (maximumResults < 1 || maximumResults > 10000)
            return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");

        var admission = QueryCapabilities.AdmitPlan(plan);
        if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
        var selected = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
        if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
        if (IsVersionControlEnabled)
            return Error("CQL_CAPABILITY_UNSUPPORTED", "SurrealDB history is unavailable.");
        if (!IsProviderActivated)
            return Error("CQL_PROVIDER_INACTIVE", "Activate the configured SurrealDB namespace and database before querying.");

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            deadline.Token.ThrowIfCancellationRequested();
            var rows = new List<IHolon>();
            var admittedIds = selected.Result.Where(id => plan.Query.ReadAccess.CanRead(id, false, Guid.Empty)).ToArray();
            if (admittedIds.Length != 0)
            {
                await using var client = await GetClientAsync(deadline.Token);
                var payloadBytes = 0;
                foreach (var id in admittedIds)
                {
                    var record = await client.Select<HolonRecord>(new StringRecordId($"oasis_holons:{id:D}"), deadline.Token);
                    if (record == null || record.IsDeleted) continue;
                    if (record.DataJson == null)
                        return Error("CQL_QUERY_FAILED", "Native holon payload is missing.");
                    var bytes = Encoding.UTF8.GetByteCount(record.DataJson);
                    if (bytes > 1024 * 1024 || payloadBytes + bytes > 4 * 1024 * 1024)
                        return Error("CQL_QUERY_LIMIT", "Native payload exceeds the admitted byte budget.");
                    payloadBytes += bytes;

                    var row = Des<Holon>(record.DataJson);
                    if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                        return Error("CQL_QUERY_FAILED", "Native holon differs from admitted identity or read authority.");
                    if (row.IsDeleted) continue;
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(row);
                }
            }

            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            deadline.Token.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        {
            return Error("CQL_QUERY_CANCELLED", "SurrealDB query cancelled. No partial rows published.", cause);
        }
        catch (Exception cause)
        {
            return Error("CQL_QUERY_FAILED", "Native SurrealDB query failed. No partial rows published.", cause);
        }
    }
}
