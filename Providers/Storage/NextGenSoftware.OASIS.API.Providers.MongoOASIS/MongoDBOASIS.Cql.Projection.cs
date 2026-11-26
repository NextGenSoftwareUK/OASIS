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

public partial class MongoDBOASIS : ICosmicQueryEntityProjectionStorageProvider,ICosmicQuerySetProjectionStorageProvider
{
    public static CosmicProjectionCapabilities CqlProjectionCapabilities { get; } = new CosmicProjectionCapabilities(
        CqlCapabilities.CanonicalFields.Select(field => new CosmicProjectionFieldCapability(field.Name, field.ValueType)).ToArray(),
        CqlCapabilities.MetadataTypes.Select(type => type.ValueType).ToArray()).WithFunctions(CosmicProjectionFunction.Utf16Length,CosmicProjectionFunction.TextCoalesce,CosmicProjectionFunction.BooleanTextCase,CosmicProjectionFunction.Int32Coalesce,CosmicProjectionFunction.UtcYear,CosmicProjectionFunction.UtcMonth,CosmicProjectionFunction.UtcDay,CosmicProjectionFunction.UtcDayOfYear,CosmicProjectionFunction.UtcHour,CosmicProjectionFunction.UtcMinute,CosmicProjectionFunction.UtcSecond,CosmicProjectionFunction.TextValueCase,CosmicProjectionFunction.Int32Affine,CosmicProjectionFunction.TextToInt32Case,CosmicProjectionFunction.DoubleAffine,CosmicProjectionFunction.Int64Affine,CosmicProjectionFunction.Int32ToTextCase);
    public CosmicProjectionCapabilities ProjectionCapabilities => CqlProjectionCapabilities;
    public bool SupportsEntityProjection => true;
    public bool SupportsSetProjection => true;

    public Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicSetProjectionAsync<T,TResult>(
        CosmicSetProjectionPlan<T,TResult> plan,int maximumResults=100,
        int maximumPayloadBytes=CosmicProjectionResults.MaximumPayloadLimit,CancellationToken cancellationToken=default) where T:IHolon
        =>plan==null?Task.FromResult(ProjectionCqlFailure("CQL_PLAN_INVALID","An immutable late set projection is required."))
            :ExecuteProjectionReadAsync(plan.Selection,maximumResults,maximumPayloadBytes,cancellationToken,setProjection:plan);


    public Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicEntityProjectionAsync<T,TResult>(
        CosmicEntityProjectionPlan<T,TResult> plan,int maximumResults=100,
        int maximumPayloadBytes=CosmicProjectionResults.MaximumPayloadLimit,CancellationToken cancellationToken=default) where T:IHolon
        => plan==null ? Task.FromResult(ProjectionCqlFailure("CQL_PLAN_INVALID","An immutable entity projection plan is required."))
            : ExecuteProjectionReadAsync(plan.Selection,maximumResults,maximumPayloadBytes,cancellationToken,plan);

    public Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteCosmicProjectionAsync<T, TResult>(
        CosmicProjectionPlan<T, TResult> plan, int maximumResults = 100,
        int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken = default) where T : IHolon
        => ExecuteProjectionReadAsync(plan,maximumResults,maximumPayloadBytes,cancellationToken);

    private async Task<OASISResult<IReadOnlyList<CosmicProjectionRow>>> ExecuteProjectionReadAsync<T,TResult>(
        CosmicProjectionPlan<T,TResult> plan,int maximumResults,int maximumPayloadBytes,CancellationToken cancellationToken,
        CosmicEntityProjectionPlan<T,TResult> entityProjection=null,
        CosmicSetProjectionPlan<T,TResult> setProjection=null,
        Func<IClientSessionHandle,Func<AggregateOptions>,IReadOnlyList<CosmicProjectionRow>,CancellationToken,Task> selectedContinuation=null) where T:IHolon
    {
        try
        {
            if(entityProjection!=null && setProjection!=null)
                return ProjectionCqlFailure("CQL_PLAN_INVALID","Select one native projection source kind.");
            var composition=entityProjection?.Source;
            cancellationToken.ThrowIfCancellationRequested();
            var admission = CosmicProjectionPlanner.Admit(plan, Core.Enums.ProviderType.MongoDBOASIS, CqlCapabilities, CqlProjectionCapabilities);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, IReadOnlyList<CosmicProjectionRow>>(admission);
            if (maximumResults < 0 || maximumResults > CosmicProjectionResults.MaximumResultLimit
                || maximumPayloadBytes < 0 || maximumPayloadBytes > CosmicProjectionResults.MaximumPayloadLimit)
                return ProjectionCqlFailure("CQL_QUERY_LIMIT", "Projection output bounds are invalid.");
            if(composition!=null)
            {
                var source=CqlCapabilities.AdmitPlan(composition.Source);
                if(source.IsError) return ProjectionCqlFailure(source.ErrorCode,source.Message,source.Exception);
                var stages=EntityCompositionCapabilities.Admit(composition.Query);
                if(stages.IsError) return ProjectionCqlFailure(stages.ErrorCode,stages.Message,stages.Exception);
            }
            if(setProjection!=null)
            {
                var actual=CosmicSetProjectionPlanner.Plan(setProjection.Query,Core.Enums.ProviderType.MongoDBOASIS,
                    new CosmicProjectionTarget(Core.Enums.ProviderType.MongoDBOASIS,CqlCapabilities,CqlProjectionCapabilities),
                    SetQueryCapabilities,setProjection.Source.Options);
                if(actual.IsError)return ProjectionCqlFailure(actual.ErrorCode,actual.Message,actual.Exception);
                setProjection=actual.Result;
                maximumResults=Math.Min(maximumResults,setProjection.Source.Options.MaximumResultRows);
            }
            var translator=new MongoCqlTranslator();
            var command=setProjection!=null?translator.CompileSetProjection(setProjection,maximumResults)
                :composition!=null?translator.CompileEntityProjection(entityProjection,maximumResults)
                :translator.CompileProjection(plan.Query,maximumResults);
            if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return ProjectionCqlFailure("CQL_PROVIDER_NOT_READY", "Activate MongoDB before native projection execution.");
            var rows = await repository.ExecuteNativeCqlProjectionAsync(command, plan.Query.Columns, maximumResults, maximumPayloadBytes, cancellationToken,selectedContinuation).ConfigureAwait(false);
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
