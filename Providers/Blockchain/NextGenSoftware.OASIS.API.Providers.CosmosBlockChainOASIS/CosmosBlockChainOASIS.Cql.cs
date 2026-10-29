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

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;

public partial class CosmosBlockChainOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.CosmosBlockChainOASIS;
    public CosmicQueryCapabilities QueryCapabilities => CosmosCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Cosmos blockchain target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = CosmosCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Cosmos provider before querying.");

            var rows = new List<IHolon>();
            var payloadBytes = 0;
            foreach (var id in selection.Result)
            {
                // Backend is the shipping private-storage wrapper over native
                // CosmJS contract gets. It decrypts/authenticates the exact key;
                // never list the store or substitute a plaintext/local source.
                cancellationToken.ThrowIfCancellationRequested();
                var json = await Backend.GetAsync(KeyPrefix + "holon/" + id.ToString("D"), cancellationToken).ConfigureAwait(false);
                if (json == null) continue;
                payloadBytes = checked(payloadBytes + Encoding.UTF8.GetByteCount(json));
                if (payloadBytes > 4 * 1024 * 1024)
                    return Error("CQL_QUERY_LIMIT", "The complete canonical response exceeds 4 MiB.");
                var row = OasisJson.DeserializeStored<Holon>(json);
                if (row == null || row.Id != id || !plan.Query.ReadAccess.CanRead(row))
                    throw new InvalidDataException("Native Cosmos record differs from the authorized identity.");
                if (row.IsDeleted) continue;
                row.Children = Array.Empty<IHolon>();
                rows.Add(row);
            }

            // Key reads may observe different blocks. No chain-wide or
            // cross-provider snapshot is claimed by this bounded profile.
            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause)
        { return Error("CQL_QUERY_CANCELLED", "Cosmos query cancelled; no partial result published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Cosmos key query failed; no partial result published.", cause); }
    }
}
