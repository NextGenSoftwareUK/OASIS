using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS;

public partial class ArweaveOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.ArweaveOASIS;
    public CosmicQueryCapabilities QueryCapabilities => ArweaveCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Arweave target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admission = QueryCapabilities.AdmitPlan(plan);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message);
            var selected = ArweaveCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selected.IsError) return Error(selected.ErrorCode, selected.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Arweave provider first.");
            var rows = new List<IHolon>();
            foreach (var id in selected.Result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Reads go through the owner-pinned, encrypted record store: a public transaction with a
                // copied GUID or tag set cannot impersonate the configured wallet's record.
                var loaded = await LoadHolonAsync(id, loadChildren: false, recursive: false);
                cancellationToken.ThrowIfCancellationRequested();
                if (loaded.IsError)
                {
                    if (loaded.ErrorCode == OASISResultErrorCodes.RecordNotFound && loaded.Exception == null) continue;
                    return Error("CQL_QUERY_FAILED", "Native Arweave query failed; no partial result published.",
                        loaded.Exception ?? new InvalidOperationException(loaded.Message));
                }
                var row = loaded.Result;
                if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Native Arweave record differs from the authorized identity.");
                if (row.IsDeleted) continue;
                row.Children = Array.Empty<IHolon>();
                rows.Add(row);
            }
            // Moving native reads; no gateway-wide or cross-provider snapshot claimed.
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults) return Error("CQL_QUERY_LIMIT", "Complete page exceeds result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "Arweave query cancelled; no partial result published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Arweave query failed; no partial result published.", cause); }
    }
}
