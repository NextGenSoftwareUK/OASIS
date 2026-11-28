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
        string Build(CosmicProjectedSetPlan<T,TResult> node,bool exportRows)
        {
        var text=new StringBuilder();
        void Input(string name,CosmicProjectionCompositionPlan<T,TResult> leaf,CosmicProjectedSetPlan<T,TResult> child,
            CosmicProjectionQuery<T,TResult> selection)
        {
            if(child!=null)
            {
                text.Append("CALL { CALL { ").Append(Build(child,true))
                    .Append(" } RETURN {kind:'set',proof:{leftProof:leftProof,rightProof:rightProof,members:members,stages:stages,work:work}} AS ")
                    .Append(name).Append("Proof,rows AS ").Append(name).Append("Values } ");
                return;
            }
            var command=CompileCqlSelected(leaf.Query,ProjectedSetInputLimits(leaf.Options),sharedParameters:parameters);
            text.Append("CALL { CALL { ").Append(command.Text)
                .Append(" } WITH phase,position,sourcePosition,proof,nativeCount,stageWork ORDER BY phase,position ")
                .Append("WITH collect({phase:phase,position:position,sourcePosition:sourcePosition,proof:proof,nativeCount:nativeCount,stageWork:stageWork}) AS captured ")
                .Append("WITH captured,[row IN captured WHERE row.phase=-1 | row.proof] AS input,");
            var positions=selection.ResultStages.Count==0
                ? "[row IN captured WHERE row.phase=-1 | row.sourcePosition]"
                : "[row IN captured WHERE row.phase="+(selection.ResultStages.Count-1)+" AND row.position>=0 | row.sourcePosition]";
            text.Append(positions).Append(" AS positions RETURN {kind:'source',proof:captured} AS ").Append(name)
                .Append("Proof,[index IN range(0,size(positions)-1) | {values:[")
                .Append(string.Join(",",selection.OutputColumnIndices.Select(column=>"input[positions[index]].values["+column+"]")))
                .Append("]}] AS ").Append(name).Append("Values } ");
        }
        Input("left",node.Left,node.LeftSet,node.Query.LeftInput.Anchor);
        Input("right",node.Right,node.RightSet,node.Query.RightInput.Anchor);
        string Occurrences(string name,int side)=>"[index IN range(0,size("+name+"Values)-1) | {side:"+side+",index:index,values:"+name+"Values[index].values}]";
        text.Append("WITH leftProof,rightProof,").Append(Occurrences("left",0)).Append("+")
            .Append(Occurrences("right",1)).Append(" AS input ");
        if(node.Query.Kind==CosmicSetQueryKind.Concat)
            text.Append("WITH leftProof,rightProof,input,range(0,size(input)-1) AS s0 ");
        else
        {
            text.Append("CALL { WITH input UNWIND range(0,size(input)-1) AS position ")
                .Append("WITH input,position,[cell IN input[position].values | CASE WHEN cell.v IS NULL THEN {state:0} ELSE {state:1,value:cell.v} END] AS valueKey ORDER BY position ")
                .Append("WITH valueKey,collect(position) AS occurrences,max(CASE WHEN input[position].side=0 THEN 1 ELSE 0 END) AS leftPresent,max(CASE WHEN input[position].side=1 THEN 1 ELSE 0 END) AS rightPresent ");
            if(node.Query.Kind==CosmicSetQueryKind.Intersect || node.Query.Kind==CosmicSetQueryKind.Except)
                text.Append("WHERE leftPresent=1 AND rightPresent=")
                    .Append(node.Query.Kind==CosmicSetQueryKind.Except?"0 ":"1 ");
            else if(node.Query.Kind!=CosmicSetQueryKind.Union)throw new NotSupportedException("Unknown selected-set operation.");
            text.Append("WITH head(occurrences) AS position ORDER BY position RETURN collect(position) AS s0 } ");
        }
        text.Append("WITH leftProof,rightProof,input,s0,size(input) AS work,5*size(s0) AS proofBytes ");
        var binder=new NeoCqlTranslator("h","p",parameters,null);
        var carried=AppendSelectedStages(text,node.Query.Output,binder,"leftProof,rightProof,input,s0",
            binder.Bind(node.Options.MaximumStageRows),binder.Bind(node.Options.MaximumProofPayloadBytes),referenceBytes:5);
        var members="[position IN s0 | {side:input[position].side,index:input[position].index}]";
        var stages=Enumerable.Range(1,node.Query.Output.ResultStages.Count)
            .Select(index=>"[position IN s"+index+" | {side:input[position].side,index:input[position].index}]");
        text.Append("RETURN leftProof,rightProof,").Append(members).Append(" AS members,[")
            .Append(string.Join(",",stages)).Append("] AS stages,work ");
        if(!exportRows&&limits.CountOnly)text.Append(",size(s"+node.Query.Output.ResultStages.Count+") AS nativeCount ");
        if(exportRows)text.Append(",[position IN s"+node.Query.Output.ResultStages.Count+" | {values:input[position].values}] AS rows ");
        if(text.Length>1024*1024)throw new NeoCqlLimitException("Native selected-set statement exceeds its bounded size.");
        return text.ToString();
        }
        return new(Build(plan,false),parameters);
    }
}
