using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

public partial class AzureCosmosDBOASIS : ICosmicQueryScalarStorageProvider
{
    public static CosmicScalarCapabilities CqlScalarCapabilities { get; } = new(
        CosmicScalarOperation.Count, CosmicScalarOperation.LongCount, CosmicScalarOperation.Any);
    public CosmicScalarCapabilities ScalarCapabilities => CqlScalarCapabilities;

    public async Task<OASISResult<TResult>> ExecuteCosmicScalarAsync<T, TResult>(CosmicScalarPlan<T, TResult> plan,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<TResult> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
        var acquired = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var admitted = CosmicScalarPlanner.Admit(plan, Core.Enums.ProviderType.AzureCosmosDBOASIS,
                CqlCapabilities, CqlScalarCapabilities);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message, admitted.Exception);
            var command = CosmosCqlCount.Compile(plan);
            await lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            acquired = true;
            if (!IsProviderActivated || dbClientFactory == null)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the Cosmos provider before executing CQL.");
            if (dbClientFactory.GetClient("holonItems") is not CosmosDbClient native)
                return Error("CQL_CAPABILITY_UNSUPPORTED", "Cosmos scalars require the native SDK query owner.");
            var rows = await native.ReadBoundedQueryAsync(command, 1, 128, 1024 * 1024, cancellationToken).ConfigureAwait(false);
            var count = CosmosCqlCount.Decode(rows);
            count = CosmosCqlCount.ApplyWindow(count, plan.Query.Source.Offset, plan.Query.Source.Limit);
            cancellationToken.ThrowIfCancellationRequested();
            return CosmicScalarResults.FromCount(plan.Query, count);
        }
        catch (OperationCanceledException cause) { return Error("CQL_CANCELLED", "Cosmos scalar execution was cancelled.", cause); }
        catch (NotSupportedException cause) { return Error("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (Exception cause) { return Error("CQL_PROVIDER_ERROR", "Cosmos native scalar execution failed.", cause); }
        finally { if (acquired) lifecycleGate.Release(); }
    }
}
