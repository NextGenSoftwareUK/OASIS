using System;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICosmicQueryEntityCountStorageProvider
{
    public bool SupportsEntityCount => true;

    public async Task<OASISResult<long>> ExecuteCosmicEntityCountAsync<T>(CosmicEntityCompositionPlan<T> plan,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (plan == null || plan.Source.Targets.Count != 1 || plan.Source.Targets[0].Provider != Core.Enums.ProviderType.MongoDBOASIS)
                return ScalarCqlFailure<long>("CQL_PLAN_INVALID","One exclusively Mongo entity-count plan is required.");
            var source=CqlCapabilities.AdmitPlan(plan.Source);
            if (source.IsError) return ScalarCqlFailure<long>(source.ErrorCode,source.Message,source.Exception);
            var stages=EntityCompositionCapabilities.Admit(plan.Query);
            if (stages.IsError) return ScalarCqlFailure<long>(stages.ErrorCode,stages.Message,stages.Exception);
            var command=new MongoCqlTranslator().CompileEntityCount(plan);
            if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return ScalarCqlFailure<long>("CQL_PROVIDER_NOT_READY","Activate MongoDB before native entity count.");
            var count=await repository.ExecuteNativeCqlCountAsync(command,cancellationToken).ConfigureAwait(false);
            return new OASISResult<long>(count){IsLoaded=true};
        }
        catch(System.IO.InvalidDataException cause){return ScalarCqlFailure<long>("CQL_DATA_INVALID",cause.Message,cause);}
        catch(MongoCqlLimitException cause){return ScalarCqlFailure<long>("CQL_QUERY_LIMIT",cause.Message,cause);}
        catch(MongoCqlGraphTimeoutException cause){return ScalarCqlFailure<long>("CQL_QUERY_TIMEOUT",cause.Message,cause);}
        catch(NotSupportedException cause){return ScalarCqlFailure<long>("CQL_CAPABILITY_UNSUPPORTED",cause.Message,cause);}
        catch(OperationCanceledException cause){return ScalarCqlFailure<long>("CQL_QUERY_CANCELLED",cause.Message,cause);}
        catch(MongoCommandException cause) when(cause.Code==146 || cause.Code==292){return ScalarCqlFailure<long>("CQL_QUERY_LIMIT",cause.Message,cause);}
        catch(MongoExecutionTimeoutException cause){return ScalarCqlFailure<long>("CQL_QUERY_TIMEOUT",cause.Message,cause);}
        catch(Exception cause){return ScalarCqlFailure<long>("CQL_QUERY_FAILED",cause.Message,cause);}
    }

    public CosmicEntityCompositionCapabilities EntityCompositionCapabilities => new(
        CosmicEntityStageFeatures.Filter | CosmicEntityStageFeatures.Ordering | CosmicEntityStageFeatures.Paging | CosmicEntityStageFeatures.Reverse,
        CqlCapabilities);

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicEntityCompositionAsync<T>(CosmicEntityCompositionPlan<T> plan,
        int maximumResults=100, CancellationToken cancellationToken=default) where T:IHolon
        => ExecuteCqlRowsAsync(plan?.Source, maximumResults, cancellationToken, plan);
}
