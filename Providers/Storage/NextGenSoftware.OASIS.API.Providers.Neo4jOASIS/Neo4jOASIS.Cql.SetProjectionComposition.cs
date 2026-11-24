using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS:ICosmicQuerySetProjectionCompositionStorageProvider
{
    public bool SupportsSetProjectionComposition=>true;
    public async Task<OASISResult<CosmicSetProjectionCompositionPacket>> ExecuteCosmicSetProjectionCompositionAsync<T,TResult>(
        CosmicSetProjectionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken token=default) where T:IHolon
    {
        OASISResult<CosmicSetProjectionCompositionPacket> Error(string code,string message,Exception cause=null)
            =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
        if(plan==null || limits==null || !plan.RequiresSelectedComposition)
            return Error("CQL_PLAN_INVALID","A late entity-set selected-stage plan and limits are required.");
        var actual=CosmicSetProjectionPlanner.Plan(plan.Query,Core.Enums.ProviderType.Neo4jOASIS,
            new CosmicProjectionTarget(Core.Enums.ProviderType.Neo4jOASIS,CqlCapabilities,CqlProjectionCapabilities),
            SetQueryCapabilities,plan.Source.Options,CompositionCapabilities);
        if(actual.IsError)return Error(actual.ErrorCode,actual.Message,actual.Exception);
        var admitted=limits.Admit(actual.Result.SelectedOptions);
        if(admitted.IsError)return Error(admitted.ErrorCode,admitted.Message,admitted.Exception);
        var basePlan=CosmicSetProjectionPlanner.PlanBase(actual.Result,
            new CosmicProjectionTarget(Core.Enums.ProviderType.Neo4jOASIS,CqlCapabilities,CqlProjectionCapabilities),SetQueryCapabilities);
        if(basePlan.IsError)return Error(basePlan.ErrorCode,basePlan.Message,basePlan.Exception);
        CosmicSetProjectionCompositionPacket packet=null;
        var read=await ExecuteProjectionReadAsync(basePlan.Result.Selection,limits.MaximumInputRows,
            limits.MaximumInputPayloadBytes,token,setProjection:basePlan.Result,
            selectedContinuation:async(transaction,budget,input,nativeInput)=>{
                var command=CompileCqlSetProjectionComposition(actual.Result,limits,nativeInput);
                packet=await ReadCqlSetProjectionOccurrencesAsync(command,actual.Result,limits,transaction,budget,input,token)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
        if(read.IsError)return Error(read.ErrorCode,read.Message,read.Exception);
        if(!read.IsLoaded || packet==null)
            return Error("CQL_PROVIDER_RESULT_INVALID","Neo4j selected set read owner returned no complete packet.");
        return new OASISResult<CosmicSetProjectionCompositionPacket>(packet){IsLoaded=true,LoadedCount=packet.Rows.Count};
    }
}
