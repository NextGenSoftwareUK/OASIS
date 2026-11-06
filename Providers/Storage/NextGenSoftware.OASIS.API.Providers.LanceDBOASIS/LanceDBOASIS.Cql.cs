using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.LanceDBOASIS;

public partial class LanceDBOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.LanceDBOASIS;
    public CosmicQueryCapabilities QueryCapabilities => LanceDBCqlCompiler.Capabilities;
    protected virtual bool RequiresPublicQueryRecords => false;

    // Only captured canonical UUIDs enter SQL.
    private static string IdentityPredicate(IEnumerable<Guid> ids) =>
        "id IN (" + string.Join(",", ids.Select(id => "'" + id.ToString("D") + "'")) + ")";

    protected async Task VerifyPublicQueryProbeAsync(Guid id, CancellationToken cancellationToken)
    {
        var records = await QueryArrowRowsAsync(CollHolons, IdentityPredicate(new[] { id }), 1, cancellationToken)
            .ConfigureAwait(false);
        if (records.Count != 1 || records[0].Id != id ||
            JsonSerializer.Deserialize<Holon>(records[0].Json) is not { IsPublic: true, IsDeleted: false } row || row.Id != id)
            throw new InvalidDataException("The LanceDB probe must be an existing public holon.");
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
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit LanceDB target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between1and10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = LanceDBCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "LanceDB history is not implemented.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated) return Error("CQL_PROVIDER_INACTIVE", "Activate the explicit LanceDB tables before querying.");

            var rows = new Dictionary<Guid, IHolon>();
            if (selected.Result.Count != 0)
            {
                // Only captured canonical GUIDs enter SQL; never interpolate caller expressions.
                var predicate = IdentityPredicate(selected.Result);
                var records = await QueryArrowRowsAsync(CollHolons, predicate, selected.Result.Count, cancellationToken);
                foreach (var record in records)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!selected.Result.Contains(record.Id)) throw new InvalidDataException("LanceDB returned a foreign identity.");
                    var row = JsonSerializer.Deserialize<Holon>(record.Json);
                    if (row == null || row.Id != record.Id || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("LanceDB payload differs from admitted read authority.");
                    if (row.IsDeleted) continue;
                    if (RequiresPublicQueryRecords && !row.IsPublic)
                        throw new InvalidDataException("Native LanceDB teaching record is no longer public.");
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(record.Id, row);
                }
            }
            var page = selected.Result.Where(rows.ContainsKey).Select(id => rows[id])
                .Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "LanceDB query cancelled; no partial rows published.", cause); }
        catch (NativePayloadLimitException cause)
        { return Error("CQL_QUERY_LIMIT", "LanceDB native result exceeds the admitted byte/row budget.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native LanceDB query failed; no partial rows published.", cause); }
    }
}
