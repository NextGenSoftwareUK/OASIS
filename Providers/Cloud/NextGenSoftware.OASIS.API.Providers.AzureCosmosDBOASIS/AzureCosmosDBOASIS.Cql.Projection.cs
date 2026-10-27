using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public partial class AzureCosmosDBOASIS : ICosmicQueryProjectionStorageProvider
{
    public static CosmicProjectionCapabilities CqlProjectionCapabilities { get; } = new(
        CqlCapabilities.CanonicalFields.Select(field => new CosmicProjectionFieldCapability(field.Name, field.ValueType)).ToArray(),
        new[] { typeof(string), typeof(bool), typeof(bool?) });
    public CosmicProjectionCapabilities ProjectionCapabilities => CqlProjectionCapabilities;

    public async Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicProjectionAsync<T, TResult>(
        CosmicProjectionPlan<T, TResult> plan, int maximumResults = 100,
        int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<CosmicProjectionRow>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        var acquired = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (maximumResults < 1 || maximumResults > CosmicProjectionResults.MaximumResultLimit ||
                maximumPayloadBytes < 0 || maximumPayloadBytes > CosmicProjectionResults.MaximumPayloadLimit)
                return Error("CQL_QUERY_LIMIT", "Cosmos projection bounds are invalid.");
            var admission = CosmicProjectionPlanner.Admit(plan, Core.Enums.ProviderType.AzureCosmosDBOASIS,
                CqlCapabilities, CqlProjectionCapabilities);
            if (admission.IsError) return Error(admission.ErrorCode, admission.Message, admission.Exception);
            var command = CosmosCqlProjection.Compile(plan, maximumResults);
            await lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;
            if (!IsProviderActivated || dbClientFactory == null)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the Cosmos provider before executing CQL.");
            if (dbClientFactory.GetClient("holonItems") is not CosmosDbClient native)
                return Error("CQL_CAPABILITY_UNSUPPORTED", "Cosmos projections require the native SDK query owner.");
            var documents = await native.ReadBoundedQueryAsync(command, maximumResults, 128,
                CosmicProjectionResults.MaximumPayloadLimit, cancellationToken).ConfigureAwait(false);
            var rows = CosmosCqlProjection.Decode(plan, documents, maximumResults, maximumPayloadBytes, cancellationToken);
            return new OASISResult<IReadOnlyList<CosmicProjectionRow>>(rows) { IsLoaded = true, LoadedCount = rows.Count };
        }
        catch (OperationCanceledException cause) { return Error("CQL_CANCELLED", "Cosmos projection was cancelled.", cause); }
        catch (NotSupportedException cause) { return Error("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (Exception cause) { return Error("CQL_PROVIDER_ERROR", "Cosmos native projection failed.", cause); }
        finally { if (acquired) lifecycleGate.Release(); }
    }
}
