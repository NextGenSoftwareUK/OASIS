using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS:ICosmicQueryStorageProvider
{
    public static CosmicQueryCapabilities CqlCapabilities=>MongoCqlContract.Capabilities;
    Core.Enums.ProviderType ICosmicQueryStorageProvider.QueryProviderType=>Core.Enums.ProviderType.MongoDBOASIS;
    CosmicQueryCapabilities ICosmicQueryStorageProvider.QueryCapabilities=>CqlCapabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(plan==null||plan.Targets.Count!=1||plan.Targets[0].Provider!=Core.Enums.ProviderType.MongoDBOASIS)
                return CqlFailure("CQL_PLAN_INVALID","MongoDB requires an exclusively MongoDB query plan.");
            if(maximumResults<1||maximumResults>10000)return CqlFailure("CQL_QUERY_LIMIT","Result budget must be between one and 10,000.");
            // Re-admit against this adapter's actual contract, not a caller-supplied capability profile.
            var command=new MongoCqlTranslator().CompileExecution(plan.Query,maximumResults);
            if(!IsProviderActivated||_holonRepository is not HolonRepository repository)
                return CqlFailure("CQL_PROVIDER_NOT_READY","Activate MongoDB before native CQL execution.");
            if(plan.Query.GraphOptions!=null)
            {
                var graph=await repository.ExecuteNativeCqlGraphAsync(command,maximumResults,plan.Query.GraphOptions,cancellationToken,plan.Query.ReadAccess).ConfigureAwait(false);
                graph.IsLoaded=!graph.IsError;
                return graph;
            }
            var rows=await repository.ExecuteNativeCqlAsync(command,maximumResults,cancellationToken).ConfigureAwait(false);
            return new OASISResult<IReadOnlyList<IHolon>>(rows){IsLoaded=true};
        }
        catch(MongoCqlLimitException cause){return CqlFailure("CQL_QUERY_LIMIT",cause.Message,cause);}
        catch(Repositories.MongoCqlGraphTimeoutException cause){return CqlFailure("CQL_QUERY_TIMEOUT",cause.Message,cause);}
        catch(NotSupportedException cause){return CqlFailure("CQL_CAPABILITY_UNSUPPORTED",cause.Message,cause);}
        catch(OperationCanceledException cause){return CqlFailure("CQL_QUERY_CANCELLED","MongoDB CQL was cancelled.",cause);}
        catch(MongoCommandException cause) when(cause.Code==292){return CqlFailure("CQL_QUERY_LIMIT","MongoDB exceeded its native in-memory query budget.",cause);}
        catch(MongoExecutionTimeoutException cause){return CqlFailure("CQL_QUERY_TIMEOUT","MongoDB exceeded the native query deadline.",cause);}
        catch(Exception cause){return CqlFailure("CQL_QUERY_FAILED","MongoDB CQL execution failed.",cause);}
    }
    private static OASISResult<IReadOnlyList<IHolon>> CqlFailure(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
}
