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

public partial class MongoDBOASIS:ICosmicQueryStorageProvider,ICosmicJoinFetchStorageProvider
{
    public static CosmicQueryCapabilities CqlCapabilities=>MongoCqlContract.Capabilities;
    Core.Enums.ProviderType ICosmicQueryStorageProvider.QueryProviderType=>Core.Enums.ProviderType.MongoDBOASIS;
    CosmicQueryCapabilities ICosmicQueryStorageProvider.QueryCapabilities=>CqlCapabilities;

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
        => ExecuteCqlRowsAsync(plan, maximumResults, cancellationToken);

    private async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCqlRowsAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults, CancellationToken cancellationToken, CosmicEntityCompositionPlan<T> composition=null,CosmicSetQueryPlan<T> set=null,
        Action<MongoCqlCommand> explain=null,MongoCqlJoinFetch joinFetch=null) where T:IHolon
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(plan==null||plan.Targets.Count!=1||plan.Targets[0].Provider!=Core.Enums.ProviderType.MongoDBOASIS)
                return CqlFailure("CQL_PLAN_INVALID","MongoDB requires an exclusively MongoDB query plan.");
            if(maximumResults<1||maximumResults>10000)return CqlFailure("CQL_QUERY_LIMIT","Result budget must be between one and 10,000.");
            // Re-admit against this adapter's actual contract, not a caller-supplied capability profile.
            var admission = CqlCapabilities.AdmitPlan(plan);
            if (admission.IsError) return CqlFailure(admission.ErrorCode, admission.Message);
            if (composition != null)
            {
                var stages = EntityCompositionCapabilities.Admit(composition.Query);
                if (stages.IsError) return CqlFailure(stages.ErrorCode, stages.Message);
            }
            if(set!=null)
            {
                var admitted=CosmicSetQueryPlanner.Plan(set.Query,Core.Enums.ProviderType.MongoDBOASIS,
                    new CosmicQueryTarget(Core.Enums.ProviderType.MongoDBOASIS,CqlCapabilities),SetQueryCapabilities,set.Options);
                if(admitted.IsError)return CqlFailure(admitted.ErrorCode,admitted.Message);
            }
            var translator = new MongoCqlTranslator();
            var command = set != null ? translator.CompileSet(set,maximumResults)
                : composition != null ? translator.CompileEntityComposition(composition, maximumResults)
                : plan.PostWindowPredicate != null ? translator.CompilePostWindowElement(plan, maximumResults)
                : translator.CompileExecution(plan.Query,maximumResults,plan.TailIndex);
            if(joinFetch!=null)
            {
                // Join fetch extends this one root compiler with one correlated $lookup stage.
                if(set!=null || composition!=null || plan.PostWindowPredicate!=null || plan.TailIndex.HasValue || plan.Query.GraphOptions!=null)
                    throw new NotSupportedException("MongoDB join fetch requires a plain root window.");
                command=joinFetch.Compile(translator,command);
                joinFetch.Command=MongoDB.Bson.BsonExtensionMethods.ToJson(new MongoDB.Bson.BsonArray(command.Pipeline));
            }
            if(explain!=null){explain(command);return new OASISResult<IReadOnlyList<IHolon>>(Array.Empty<IHolon>()){IsLoaded=true};}
            if(!IsProviderActivated||_holonRepository is not HolonRepository repository)
                return CqlFailure("CQL_PROVIDER_NOT_READY","Activate MongoDB before native CQL execution.");
            if(plan.Query.GraphOptions!=null)
            {
                var graph=await repository.ExecuteNativeCqlGraphAsync(command,maximumResults,plan.Query.GraphOptions,cancellationToken,plan.Query.ReadAccess).ConfigureAwait(false);
                graph.IsLoaded=!graph.IsError;
                return graph;
            }
            var rows=await repository.ExecuteNativeCqlAsync(command,joinFetch==null?maximumResults:maximumResults+1,cancellationToken,joinFetch).ConfigureAwait(false);
            return new OASISResult<IReadOnlyList<IHolon>>(rows){IsLoaded=true};
        }
        catch(System.IO.InvalidDataException cause){return CqlFailure("CQL_DATA_INVALID",cause.Message,cause);}
        catch(MongoCqlLimitException cause){return CqlFailure("CQL_QUERY_LIMIT",cause.Message,cause);}
        catch(Repositories.MongoCqlGraphTimeoutException cause){return CqlFailure("CQL_QUERY_TIMEOUT",cause.Message,cause);}
        catch(NotSupportedException cause){return CqlFailure("CQL_CAPABILITY_UNSUPPORTED",cause.Message,cause);}
        catch(OperationCanceledException cause){return CqlFailure("CQL_QUERY_CANCELLED","MongoDB CQL was cancelled.",cause);}
        catch(MongoCommandException cause) when(cause.Code==146 || cause.Code==292){return CqlFailure("CQL_QUERY_LIMIT","MongoDB exceeded its native in-memory query budget.",cause);}
        catch(MongoExecutionTimeoutException cause){return CqlFailure("CQL_QUERY_TIMEOUT","MongoDB exceeded the native query deadline.",cause);}
        catch(Exception cause){return CqlFailure("CQL_QUERY_FAILED","MongoDB CQL execution failed.",cause);}
    }
    /// <summary>A13 join fetch: the admitted root pipeline plus one correlated $lookup, executed as ONE aggregate command.</summary>
    public async Task<OASISResult<CosmicJoinFetchPacket>> ExecuteCosmicJoinFetchAsync<TRoot,TChild>(CosmicJoinFetchPlan<TRoot,TChild> plan,
        CancellationToken cancellationToken=default) where TRoot:IHolon where TChild:IHolon
    {
        if(plan?.Roots==null || plan.Children==null)
            return new OASISResult<CosmicJoinFetchPacket>{IsError=true,ErrorCount=1,ErrorCode="CQL_PLAN_INVALID",Message="An admitted join-fetch plan is required."};
        var request=new MongoCqlJoinFetch((translator,roots)=>translator.CompileJoinFetch(roots,plan.Children,plan.MaximumChildren));
        var rows=await ExecuteCqlRowsAsync(plan.Roots,plan.MaximumRoots,cancellationToken,joinFetch:request).ConfigureAwait(false);
        var result=Core.Helpers.OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(rows,new OASISResult<CosmicJoinFetchPacket>());
        if(!rows.IsError)result.Result=new CosmicJoinFetchPacket(rows.Result,request.Children,request.NativeCommands,"MongoDB aggregate",request.Command);
        return result;
    }

    private static OASISResult<IReadOnlyList<IHolon>> CqlFailure(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
}

/// <summary>The join-fetch extension of one root command and the sink its single native read fills.</summary>
internal sealed class MongoCqlJoinFetch
{
    internal readonly Func<MongoCqlTranslator,MongoCqlCommand,MongoCqlCommand> Compile;
    internal readonly List<IHolon> Children=new();
    internal string Command;
    internal int NativeCommands;
    internal MongoCqlJoinFetch(Func<MongoCqlTranslator,MongoCqlCommand,MongoCqlCommand> compile){Compile=compile;}
}
