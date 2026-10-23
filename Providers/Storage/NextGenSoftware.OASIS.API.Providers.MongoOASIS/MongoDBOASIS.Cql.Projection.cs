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

public partial class MongoDBOASIS : ICosmicQueryProjectionStorageProvider
{
    public static CosmicProjectionCapabilities CqlProjectionCapabilities { get; } = new(
        CqlCapabilities.CanonicalFields.Select(field => new CosmicProjectionFieldCapability(field.Name, field.ValueType)).ToArray(),
        CqlCapabilities.MetadataTypes.Select(type => type.ValueType).ToArray());
    public CosmicProjectionCapabilities ProjectionCapabilities => CqlProjectionCapabilities;

    public async Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicProjectionAsync<T, TResult>(
        CosmicProjectionPlan<T, TResult> plan, int maximumResults = 100,
        int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var admission = CosmicProjectionPlanner.Admit(plan, Core.Enums.ProviderType.MongoDBOASIS, CqlCapabilities, CqlProjectionCapabilities);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, IReadOnlyList<CosmicProjectionRow>>(admission);
            if (maximumResults < 0 || maximumResults > CosmicProjectionResults.MaximumResultLimit
                || maximumPayloadBytes < 0 || maximumPayloadBytes > CosmicProjectionResults.MaximumPayloadLimit)
                return ProjectionCqlFailure("CQL_QUERY_LIMIT", "Projection output bounds are invalid.");
            var command = new MongoCqlTranslator().CompileProjection(plan.Query, maximumResults);
            if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return ProjectionCqlFailure("CQL_PROVIDER_NOT_READY", "Activate MongoDB before native projection execution.");
            var rows = await repository.ExecuteNativeCqlProjectionAsync(command, plan.Query.Columns, maximumResults, maximumPayloadBytes, cancellationToken).ConfigureAwait(false);
            return new OASISResult<IReadOnlyList<CosmicProjectionRow>>(rows) { IsLoaded = true };
        }
        catch (System.IO.InvalidDataException cause) { return ProjectionCqlFailure("CQL_DATA_INVALID", cause.Message, cause); }
        catch (OverflowException cause) { return ProjectionCqlFailure("CQL_DATA_INVALID", "Native projection exceeds its declared CLR domain.", cause); }
        catch (MongoCqlLimitException cause) { return ProjectionCqlFailure("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (MongoCqlGraphTimeoutException cause) { return ProjectionCqlFailure("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (NotSupportedException cause) { return ProjectionCqlFailure("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return ProjectionCqlFailure("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (MongoCommandException cause) when (cause.Code == 146 || cause.Code == 292)
        { return ProjectionCqlFailure("CQL_QUERY_LIMIT", "Projection exceeded its native in-memory budget.", cause); }
        catch (MongoExecutionTimeoutException cause) { return ProjectionCqlFailure("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (Exception cause) { return ProjectionCqlFailure("CQL_QUERY_FAILED", "MongoDB projection did not complete.", cause); }
    }

    private static OASISResult<IReadOnlyList<CosmicProjectionRow>> ProjectionCqlFailure(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
