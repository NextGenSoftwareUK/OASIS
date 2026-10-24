using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICosmicQueryGroupedStorageProvider
{
    private static class GroupContract
    {
        internal static readonly CosmicGroupedCapabilities Value = new(CqlProjectionCapabilities, CqlScalarCapabilities);
    }
    public static CosmicGroupedCapabilities CqlGroupedCapabilities => GroupContract.Value;
    public CosmicGroupedCapabilities GroupedCapabilities => CqlGroupedCapabilities;

    public async Task<OASISResult<IReadOnlyList<CosmicGroupedRow>>> ExecuteCosmicGroupedAsync<T, TKey, TResult>(
        CosmicGroupedPlan<T, TKey, TResult> plan, int maximumGroups = 1000,
        int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var admission = CosmicGroupedPlanner.Admit(plan, Core.Enums.ProviderType.MongoDBOASIS, CqlCapabilities, CqlGroupedCapabilities);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, IReadOnlyList<CosmicGroupedRow>>(admission);
            if (maximumGroups < 0 || maximumGroups > plan.Query.Grouping.Options.MaximumGroups
                || maximumPayloadBytes < 0 || maximumPayloadBytes > plan.Query.Grouping.Options.MaximumPayloadBytes)
                return GroupedCqlFailure("CQL_QUERY_LIMIT", "Grouped output bounds are invalid.");
            var command = new MongoCqlTranslator().CompileGrouped(plan.Query, maximumGroups);
            if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return GroupedCqlFailure("CQL_PROVIDER_NOT_READY", "Activate MongoDB before native grouping execution.");
            var rows = await repository.ExecuteNativeCqlGroupedAsync(command, plan.Query, maximumGroups, maximumPayloadBytes, cancellationToken).ConfigureAwait(false);
            return new OASISResult<IReadOnlyList<CosmicGroupedRow>>(rows) { IsLoaded = true };
        }
        catch (MongoCqlGroupedException cause) { return GroupedCqlFailure(cause.Code, cause.Message, cause); }
        catch (System.IO.InvalidDataException cause) { return GroupedCqlFailure("CQL_DATA_INVALID", cause.Message, cause); }
        catch (OverflowException cause) { return GroupedCqlFailure("CQL_DATA_INVALID", "Native grouping exceeds its declared CLR domain.", cause); }
        catch (MongoCqlLimitException cause) { return GroupedCqlFailure("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (MongoCqlGraphTimeoutException cause) { return GroupedCqlFailure("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (NotSupportedException cause) { return GroupedCqlFailure("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return GroupedCqlFailure("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (MongoCommandException cause) when (cause.Code == 146 || cause.Code == 292)
        { return GroupedCqlFailure("CQL_QUERY_LIMIT", "Projection exceeded its native in-memory budget.", cause); }
        catch (MongoExecutionTimeoutException cause) { return GroupedCqlFailure("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (Exception cause) { return GroupedCqlFailure("CQL_QUERY_FAILED", "MongoDB grouping did not complete.", cause); }
    }

    private static OASISResult<IReadOnlyList<CosmicGroupedRow>> GroupedCqlFailure(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
