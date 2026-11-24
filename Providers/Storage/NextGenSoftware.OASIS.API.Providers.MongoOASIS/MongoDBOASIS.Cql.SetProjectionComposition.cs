using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using NextGenSoftware.OASIS.Common;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
public partial class MongoDBOASIS:ICosmicQuerySetProjectionCompositionStorageProvider
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
        var actual=CosmicSetProjectionPlanner.Plan(plan.Query,Core.Enums.ProviderType.MongoDBOASIS,
            new CosmicProjectionTarget(Core.Enums.ProviderType.MongoDBOASIS,CqlCapabilities,CqlProjectionCapabilities),
            SetQueryCapabilities,plan.Source.Options,CompositionCapabilities);
        if(actual.IsError)return Error(actual.ErrorCode,actual.Message,actual.Exception);
        var admitted=limits.Admit(actual.Result.SelectedOptions);
        if(admitted.IsError)return Error(admitted.ErrorCode,admitted.Message,admitted.Exception);
        if(!IsProviderActivated || _holonRepository is not HolonRepository repository)
            return Error("CQL_PROVIDER_NOT_READY","Activate MongoDB before selected stages after entity sets.");
        var basePlan=CosmicSetProjectionPlanner.PlanBase(actual.Result,
            new CosmicProjectionTarget(Core.Enums.ProviderType.MongoDBOASIS,CqlCapabilities,CqlProjectionCapabilities),SetQueryCapabilities);
        if(basePlan.IsError)return Error(basePlan.ErrorCode,basePlan.Message,basePlan.Exception);
        CosmicSetProjectionCompositionPacket packet=null;
        var read=await ExecuteProjectionReadAsync(basePlan.Result.Selection,limits.MaximumInputRows,
            limits.MaximumInputPayloadBytes,token,setProjection:basePlan.Result,
            selectedContinuation:async(session,options,input,deadline)=>{
                if(!ReferenceEquals(_holonRepository,repository))
                    throw new System.IO.InvalidDataException("Selected set repository identity changed within its read owner.");
                var command=new MongoCqlTranslator().CompileSetProjectionComposition(actual.Result,limits);
                packet=await repository.ReadNativeSetProjectionCompositionAsync(command,limits,session,options,input,deadline)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
        if(read.IsError)return Error(read.ErrorCode,read.Message,read.Exception);
        if(!read.IsLoaded || packet==null)
            return Error("CQL_PROVIDER_RESULT_INVALID","MongoDB selected set snapshot returned no complete packet.");
        return new OASISResult<CosmicSetProjectionCompositionPacket>(packet){IsLoaded=true,LoadedCount=packet.Rows.Count};
    }
}
