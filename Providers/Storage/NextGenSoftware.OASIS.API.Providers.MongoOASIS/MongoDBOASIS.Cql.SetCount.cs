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

public partial class MongoDBOASIS : ICosmicSetCountQueryStorageProvider
{
    public bool SupportsSetCount => true;

    public async Task<OASISResult<long>> ExecuteCosmicSetCountAsync<T>(CosmicSetQueryPlan<T> plan,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (plan == null || plan.Left.Targets.Count != 1 || plan.Left.Targets[0].Provider != Core.Enums.ProviderType.MongoDBOASIS)
                return ScalarCqlFailure<long>("CQL_PLAN_INVALID","One exclusively Mongo set-count plan is required.");
            var source=CqlCapabilities.AdmitPlan(plan.Left);
            if (source.IsError) return ScalarCqlFailure<long>(source.ErrorCode,source.Message,source.Exception);
            var stages=CosmicSetQueryPlanner.Plan(plan.Query,Core.Enums.ProviderType.MongoDBOASIS,
                new CosmicQueryTarget(Core.Enums.ProviderType.MongoDBOASIS,CqlCapabilities),SetQueryCapabilities,plan.Options);
            if (stages.IsError) return ScalarCqlFailure<long>(stages.ErrorCode,stages.Message,stages.Exception);
            var command=new MongoCqlTranslator().CompileSetCount(plan);
            if (!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return ScalarCqlFailure<long>("CQL_PROVIDER_NOT_READY","Activate MongoDB before native set count.");
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

}
