using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => ProviderType.Value;
    private static readonly CosmicQueryCapabilities MissingChainQueryCapabilities = new(
        CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());

    // The chain identity is part of the authenticated private-record context.
    // An unconfigured facade cannot advertise a query it can never decrypt.
    public CosmicQueryCapabilities QueryCapabilities => _expectedChainId.HasValue && _expectedChainId.Value > 0
        ? BoundedIdentityCqlCompiler.Capabilities : MissingChainQueryCapabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code,
                Message = message, Exception = cause };
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Web3Core V2 authority is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Result budget must be between 1 and 10,000.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = BoundedIdentityCqlCompiler.Compile(plan.Query, QueryProviderType, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured V2 provider before querying.");

            var rows = new List<IHolon>();
            long bytes = 0;
            foreach (var id in selection.Result)
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Reuse the one canonical V2 read/decryption/identity path. The
                // existing RPC has no cancellation parameter: WaitAsync cancels
                // publication, but does not claim to abort an in-flight RPC.
                var loaded = await LoadHolonAsync(id, loadChildren: false,
                    recursive: false, loadChildrenFromProvider: false, version: 0)
                    .WaitAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (loaded.IsError)
                {
                    if (loaded.ErrorCode == OASISResultErrorCodes.RecordNotFound) continue;
                    return Error(loaded.ErrorCode ?? "CQL_QUERY_FAILED", loaded.Message, loaded.Exception);
                }
                var row = loaded.Result;
                if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Canonical V2 record differs from the authorized identity.");
                if (row.IsDeleted) continue;
                bytes = checked(bytes + Encoding.UTF8.GetByteCount(OasisJson.Serialize(row)));
                if (bytes > 4 * 1024 * 1024)
                    return Error("CQL_QUERY_LIMIT", "Complete decoded records exceed 4 MiB.");
                rows.Add(row);
            }
            // Each key is an independent native observation, not a pinned-block
            // or cross-provider snapshot. Missing/deleted records do not consume pages.
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "Complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause) when (cancellationToken.IsCancellationRequested)
        { return Error("CQL_QUERY_CANCELLED", "V2 query cancelled; no partial result published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "V2 canonical key query failed; no partial result published.", cause); }
    }
}
