using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PineconeOASIS;

public partial class PineconeOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.PineconeOASIS;
    public CosmicQueryCapabilities QueryCapabilities => PineconeCqlCompiler.Capabilities;

    /// <summary>Reads the admitted holon identities through the provider's own record store (no vector similarity or
    /// collection scan) and publishes only complete, readable rows.</summary>
    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception? cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Pinecone target required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = PineconeCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            if (IsVersionControlEnabled) return Error("CQL_CAPABILITY_UNSUPPORTED", "Pinecone CQL reads current records only.");
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Pinecone index before querying.");

            var rows = new List<IHolon>();
            foreach (var id in selected.Result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var loaded = await LoadHolonAsync(id, loadChildren: false);
                if (loaded.IsError)
                {
                    if (loaded.ErrorCode == OASISResultErrorCodes.RecordNotFound && loaded.Exception == null) continue;
                    return Error("CQL_QUERY_FAILED", "Native Pinecone query failed; no partial rows published.",
                        loaded.Exception ?? new IOException(loaded.Message));
                }
                var row = loaded.Result;
                if (row == null || row.Id != id)
                    return Error("CQL_QUERY_FAILED", "Native holon identity differs from the admitted catalogue.");
                if (row.IsDeleted) continue;
                if (!plan.Query.ReadAccess.CanRead(row))
                    return Error("CQL_QUERY_FAILED", "Native holon differs from read authority.");
                row.Children = Array.Empty<IHolon>();
                rows.Add(row);
            }
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Pinecone query cancelled; no partial rows published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Pinecone query failed; no partial rows published.", cause); }
    }
}
