using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public partial class AzureCosmosDBOASIS : ICosmicQueryStorageProvider
{
    ProviderType ICosmicQueryStorageProvider.QueryProviderType => Core.Enums.ProviderType.AzureCosmosDBOASIS;
    CosmicQueryCapabilities ICosmicQueryStorageProvider.QueryCapabilities => CqlCapabilities;

    /// <summary>Native canonical/string/Boolean metadata rows; graph/projection remain unsupported.</summary>
    public static CosmicQueryCapabilities CqlCapabilities => CosmosCqlRowCommand.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults = 100, CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        var acquired = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != Core.Enums.ProviderType.AzureCosmosDBOASIS)
                return Error("CQL_PLAN_INVALID", "Cosmos CQL requires an exclusively Cosmos DB plan.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "Cosmos CQL result limit must be between one and 10,000.");
            // Complete translation before acquiring a live client. Querying never
            // activates storage or creates a database/container as a side effect.
            var command = CosmosCqlRowCommand.Compile(plan, maximumResults);
            await lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;
            if (!IsProviderActivated || holonRepository is not HolonRepository repository)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the Cosmos provider before executing CQL.");
            var rows = await repository.ExecuteCqlCommandRowsAsync(command, plan.Query.ReadAccess,
                maximumResults, cancellationToken).ConfigureAwait(false);
            return new OASISResult<IReadOnlyList<IHolon>>(rows) { IsLoaded = true, LoadedCount = rows.Count };
        }
        catch (OperationCanceledException cause) { return Error("CQL_CANCELLED", "Cosmos CQL execution was cancelled.", cause); }
        catch (NotSupportedException cause) { return Error("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (Exception cause) { return Error("CQL_PROVIDER_ERROR", "Cosmos CQL native execution failed.", cause); }
        finally { if (acquired) lifecycleGate.Release(); }
    }
}
