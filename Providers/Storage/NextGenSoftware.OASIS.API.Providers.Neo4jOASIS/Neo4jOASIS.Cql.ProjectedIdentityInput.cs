using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS
{
    private static void AppendCqlProjectedIdentityInput<T,TResult>(StringBuilder text,string name,
        CosmicSetProjectionPlan<T,TResult> plan,Dictionary<string,object> parameters) where T:IHolon
    {
        var limits=ProjectedSetInputLimits(plan.SelectedOptions);
        var source=CompileCqlSource(plan.Query.Source.Anchor,parameters);
        var query=CompileCqlSet(source,plan.Source,limits.MaximumInputRows,projection:plan.Selection.Query.Columns,
            maximumProjectionBytes:limits.MaximumInputPayloadBytes);
        text.Append("CALL { CALL { ").Append(query).Append(" } ")
            .Append("WITH id,revision,valid,snapshot,traversalCycle,traversalLimit,entityInput,entityPosition,payloadLimit,cells ORDER BY entityPosition ")
            .Append("WITH collect({id:id,revision:revision,valid:valid,snapshot:snapshot,traversalCycle:traversalCycle,traversalLimit:traversalLimit,entityInput:entityInput,entityPosition:entityPosition,payloadLimit:payloadLimit,cells:cells}) AS source ")
            .Append("WITH source,[row IN source WHERE NOT row.entityInput AND row.entityPosition>=0 | {values:row.cells}] AS input ")
            .Append("WITH source,input,range(0,size(input)-1) AS s0,0 AS work,0 AS proofBytes ");
        var binder=new NeoCqlTranslator("h","p",parameters,null);
        AppendSelectedStages(text,plan.Query.Selection,binder,"source,input,s0",binder.Bind(limits.MaximumStageRows),
            binder.Bind(limits.MaximumProofPayloadBytes),referenceBytes:4);
        var stages=Enumerable.Range(1,plan.Query.Selection.ResultStages.Count).Select(index=>"s"+index);
        text.Append("RETURN {kind:'identity',source:source,stages:[").Append(string.Join(",",stages))
            .Append("],work:work} AS ").Append(name).Append("Proof,[position IN s")
            .Append(plan.Query.Selection.ResultStages.Count).Append(" | {values:[")
            .Append(string.Join(",",plan.Query.Selection.OutputColumnIndices.Select(column=>"input[position].values["+column+"]")))
            .Append("]}] AS ").Append(name).Append("Values } ");
    }
}
