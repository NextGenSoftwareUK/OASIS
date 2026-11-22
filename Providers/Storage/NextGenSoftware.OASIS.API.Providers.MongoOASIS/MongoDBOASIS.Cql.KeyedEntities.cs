using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS:ICosmicQueryKeyedEntityStorageProvider
{
    public CosmicProjectionCapabilities KeyedEntityCapabilities=>CqlProjectionCapabilities;

    public async Task<OASISResult<CosmicKeyedEntityBatch>> ExecuteCosmicKeyedEntitiesAsync<T>(
        CosmicQueryPlan<T> plan,IReadOnlyList<CosmicQueryPath> keys,int maximumRows=1000,
        int maximumPayloadBytes=CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var admitted=CosmicKeyedEntitySource.Admit(plan,keys,Core.Enums.ProviderType.MongoDBOASIS,
                CqlCapabilities,CqlProjectionCapabilities,maximumRows,maximumPayloadBytes);
            if(admitted.IsError)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,CosmicKeyedEntityBatch>(admitted);
            var command=new MongoCqlTranslator().CompileKeyedSource(plan.Query,keys,Array.Empty<CosmicQueryPath>(),maximumRows);
            if(!IsProviderActivated || _holonRepository is not HolonRepository repository)
                return KeyedCqlFailure("CQL_PROVIDER_NOT_READY","Activate MongoDB before native keyed entity execution.");
            var batch=await repository.ExecuteNativeCqlKeyedEntitiesAsync(command,plan.Query,keys,
                maximumRows,maximumPayloadBytes,cancellationToken).ConfigureAwait(false);
            return new OASISResult<CosmicKeyedEntityBatch>(batch){IsLoaded=true};
        }
        catch(System.IO.InvalidDataException cause){return KeyedCqlFailure("CQL_DATA_INVALID",cause.Message,cause);}
        catch(OverflowException cause){return KeyedCqlFailure("CQL_DATA_INVALID",cause.Message,cause);}
        catch(MongoCqlLimitException cause){return KeyedCqlFailure("CQL_QUERY_LIMIT",cause.Message,cause);}
        catch(MongoCqlGraphTimeoutException cause){return KeyedCqlFailure("CQL_QUERY_TIMEOUT",cause.Message,cause);}
        catch(NotSupportedException cause){return KeyedCqlFailure("CQL_CAPABILITY_UNSUPPORTED",cause.Message,cause);}
        catch(OperationCanceledException cause){return KeyedCqlFailure("CQL_QUERY_CANCELLED",cause.Message,cause);}
        catch(MongoCommandException cause) when(cause.Code==146 || cause.Code==292)
        {return KeyedCqlFailure("CQL_QUERY_LIMIT","Keyed entity source exceeded its native in-memory budget.",cause);}
        catch(MongoExecutionTimeoutException cause){return KeyedCqlFailure("CQL_QUERY_TIMEOUT",cause.Message,cause);}
        catch(Exception cause){return KeyedCqlFailure("CQL_QUERY_FAILED","MongoDB native keyed entity read did not complete.",cause);}
    }

    private static OASISResult<CosmicKeyedEntityBatch> KeyedCqlFailure(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCode=code,Message=message,Exception=cause};
}
