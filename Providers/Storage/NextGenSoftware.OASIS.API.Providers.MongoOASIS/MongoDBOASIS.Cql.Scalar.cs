using System;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICosmicQueryScalarStorageProvider
{
    private static class ScalarContract
    {
        internal static readonly CosmicScalarCapabilities Capabilities = new(
            new[] { CosmicScalarOperation.Count, CosmicScalarOperation.LongCount, CosmicScalarOperation.Any },
            new[]
            {
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Min, CqlProjectionCapabilities),
                new CosmicScalarSelectionCapability(CosmicScalarOperation.Max, CqlProjectionCapabilities)
            });
    }
    public static CosmicScalarCapabilities CqlScalarCapabilities => ScalarContract.Capabilities;
    public CosmicScalarCapabilities ScalarCapabilities => CqlScalarCapabilities;

    public async Task<OASISResult<TResult>> ExecuteCosmicScalarAsync<T, TResult>(CosmicScalarPlan<T, TResult> plan,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var admission = CosmicScalarPlanner.Admit(plan, Core.Enums.ProviderType.MongoDBOASIS, CqlCapabilities, CqlScalarCapabilities);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, TResult>(admission);
            if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return ScalarCqlFailure<TResult>("CQL_PROVIDER_NOT_READY", "Activate MongoDB before native scalar execution.");
            var translator = new MongoCqlTranslator();
            if (plan.Query.Selector != null)
            {
                var selected = await repository.ExecuteNativeCqlExtremumAsync(translator.CompileExtremum(plan.Query),
                    plan.Query.Selector.ValueType, cancellationToken).ConfigureAwait(false);
                return CosmicSelectedScalarResults.FromValue(plan.Query, selected.Value, selected.HasValue);
            }
            var command = translator.CompileCount(plan.Query.Source);
            var count = await repository.ExecuteNativeCqlCountAsync(command, cancellationToken).ConfigureAwait(false);
            return CosmicScalarResults.FromCount(plan.Query, count);
        }
        catch (System.IO.InvalidDataException cause) { return ScalarCqlFailure<TResult>("CQL_DATA_INVALID", cause.Message, cause); }
        catch (MongoCqlLimitException cause) { return ScalarCqlFailure<TResult>("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (MongoCqlGraphTimeoutException cause) { return ScalarCqlFailure<TResult>("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (NotSupportedException cause) { return ScalarCqlFailure<TResult>("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return ScalarCqlFailure<TResult>("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (MongoCommandException cause) when (cause.Code == 146 || cause.Code == 292)
        { return ScalarCqlFailure<TResult>("CQL_QUERY_LIMIT", "MongoDB scalar query exceeded its native in-memory budget.", cause); }
        catch (MongoExecutionTimeoutException cause) { return ScalarCqlFailure<TResult>("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (Exception cause) { return ScalarCqlFailure<TResult>("CQL_QUERY_FAILED", "MongoDB scalar execution failed.", cause); }
    }

    private static OASISResult<T> ScalarCqlFailure<T>(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
