using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Documents.Session;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.RavenDBOASIS;

public partial class RavenDBOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.RavenDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities => RavenDbCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit RavenDB target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = RavenDbCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured RavenDB database before querying.");

            var rows = new List<IHolon>();
            long decodedBytes = 0;
            if (selected.Result.Count != 0)
            {
                // A single bounded native multi-load, with no index/table scan or local predicate.
                var keys = selected.Result.Select(id => $"OasisHolons/{id:D}").ToArray();
                using var session = GetStore().OpenAsyncSession(new SessionOptions { NoTracking = true, NoCaching = true });
                var records = await session.LoadAsync<HolonDoc>(keys, cancellationToken);
                if (records.Keys.Any(key => !keys.Contains(key, StringComparer.Ordinal)))
                    throw new InvalidDataException("RavenDB returned an identity outside the selected native keys.");
                for (var index = 0; index < keys.Length; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!records.TryGetValue(keys[index], out var record) || record == null) continue;
                    if (record.Id != keys[index] || record.DataJson == null)
                        throw new InvalidDataException("Native document differs from the selected identity.");
                    decodedBytes = checked(decodedBytes + Encoding.UTF8.GetByteCount(record.DataJson));
                    if (decodedBytes > 4 * 1024 * 1024)
                        return Error("CQL_QUERY_LIMIT", "Complete decoded payload exceeds 4 MiB.");
                    var row = Des<Holon>(record.DataJson);
                    if (row == null || row.Id != selected.Result[index] || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Native holon differs from read authority.");
                    if (record.IsDeleted || row.IsDeleted) continue;
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(row);
                }
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "RavenDB query cancelled; no partial rows published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native RavenDB query failed; no partial rows published.", cause); }
    }
}
