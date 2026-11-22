using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS
{
    private static CosmicProjectionCompositionExecutionLimits ProjectedSetInputLimits(CosmicProjectionCompositionOptions options)
        =>new(options.MaximumInputRows,options.MaximumInputPayloadBytes,options.MaximumStageRows,
            options.MaximumProofPayloadBytes,10000,options.MaximumInputPayloadBytes);
    private static NeoCqlCompositionCommand CompileCqlProjectedSet<T,TResult>(
        CosmicProjectedSetPlan<T,TResult> plan,CosmicProjectedSetExecutionLimits limits) where T:IHolon
    {
        var parameters=new Dictionary<string,object>(StringComparer.Ordinal);
        var left=CompileCqlSelected(plan.Query.Left,ProjectedSetInputLimits(plan.Left.Options),sharedParameters:parameters);
        var right=CompileCqlSelected(plan.Query.Right,ProjectedSetInputLimits(plan.Right.Options),sharedParameters:parameters);
        var text=new StringBuilder();
        void Input(string name,NeoCqlCompositionCommand command)
        {
            text.Append("CALL { CALL { ").Append(command.Text)
                .Append(" } WITH phase,position,sourcePosition,proof,nativeCount,stageWork ORDER BY phase,position ")
                .Append("RETURN collect({phase:phase,position:position,sourcePosition:sourcePosition,proof:proof,nativeCount:nativeCount,stageWork:stageWork}) AS ")
                .Append(name).Append(" } ");
        }
        Input("leftProof",left);Input("rightProof",right);
        string Positions(string name,int stageCount)=>stageCount==0
            ? "[row IN "+name+" WHERE row.phase=-1 | row.sourcePosition]"
            : "[row IN "+name+" WHERE row.phase="+(stageCount-1)+" AND row.position>=0 | row.sourcePosition]";
        text.Append("WITH leftProof,rightProof,[row IN leftProof WHERE row.phase=-1 | row.proof] AS leftInput,")
            .Append("[row IN rightProof WHERE row.phase=-1 | row.proof] AS rightInput,")
            .Append(Positions("leftProof",plan.Query.Left.ResultStages.Count)).Append(" AS leftPositions,")
            .Append(Positions("rightProof",plan.Query.Right.ResultStages.Count)).Append(" AS rightPositions ");
        string Occurrences(string name,CosmicProjectionQuery<T,TResult> query,int side)=>
            "[index IN range(0,size("+name+"Positions)-1) | {side:"+side+",index:index,values:["+
            string.Join(",",query.OutputColumnIndices.Select(column=>name+"Input["+name+"Positions[index]].values["+column+"]"))+"]}]";
        text.Append("WITH leftProof,rightProof,").Append(Occurrences("left",plan.Query.Left,0)).Append("+")
            .Append(Occurrences("right",plan.Query.Right,1)).Append(" AS input ");
        if(plan.Query.Kind==CosmicSetQueryKind.Concat)
            text.Append("WITH leftProof,rightProof,input,range(0,size(input)-1) AS s0 ");
        else
        {
            text.Append("CALL { WITH input UNWIND range(0,size(input)-1) AS position ")
                .Append("WITH input,position,[cell IN input[position].values | CASE WHEN cell.v IS NULL THEN {state:0} ELSE {state:1,value:cell.v} END] AS valueKey ORDER BY position ")
                .Append("WITH valueKey,collect(position) AS occurrences,max(CASE WHEN input[position].side=0 THEN 1 ELSE 0 END) AS leftPresent,max(CASE WHEN input[position].side=1 THEN 1 ELSE 0 END) AS rightPresent ");
            if(plan.Query.Kind==CosmicSetQueryKind.Intersect || plan.Query.Kind==CosmicSetQueryKind.Except)
                text.Append("WHERE leftPresent=1 AND rightPresent=")
                    .Append(plan.Query.Kind==CosmicSetQueryKind.Except?"0 ":"1 ");
            else if(plan.Query.Kind!=CosmicSetQueryKind.Union)throw new NotSupportedException("Unknown selected-set operation.");
            text.Append("WITH head(occurrences) AS position ORDER BY position RETURN collect(position) AS s0 } ");
        }
        text.Append("WITH leftProof,rightProof,input,s0,size(input) AS work,5*size(s0) AS proofBytes ");
        var binder=new NeoCqlTranslator("h","p",parameters,null);
        var carried=AppendSelectedStages(text,plan.Query.Output,binder,"leftProof,rightProof,input,s0",
            binder.Bind(plan.Options.MaximumStageRows),binder.Bind(plan.Options.MaximumProofPayloadBytes),referenceBytes:5);
        var members="[position IN s0 | {side:input[position].side,index:input[position].index}]";
        var stages=Enumerable.Range(1,plan.Query.Output.ResultStages.Count)
            .Select(index=>"[position IN s"+index+" | {side:input[position].side,index:input[position].index}]");
        text.Append("RETURN leftProof,rightProof,").Append(members).Append(" AS members,[")
            .Append(string.Join(",",stages)).Append("] AS stages,work ");
        if(text.Length>1024*1024)throw new NeoCqlLimitException("Native selected-set statement exceeds its bounded size.");
        return new(text.ToString(),parameters);
    }
}
