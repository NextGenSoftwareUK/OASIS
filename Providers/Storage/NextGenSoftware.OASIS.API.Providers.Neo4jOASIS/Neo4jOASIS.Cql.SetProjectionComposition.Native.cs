using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS
{
    private static NeoCqlCompositionCommand CompileCqlSetProjectionComposition<T,TResult>(
        CosmicSetProjectionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
        IReadOnlyList<object> nativeInput) where T:IHolon
    {
        if(plan==null || limits==null || !plan.RequiresSelectedComposition
            || limits.Admit(plan.SelectedOptions).IsError || nativeInput==null || nativeInput.Count>limits.MaximumInputRows)
            throw new ArgumentException("Selected entity-set stages require complete captured native cells and admitted limits.");
        var parameters=new Dictionary<string,object>(StringComparer.Ordinal){{"setInput",nativeInput}};
        var binder=new NeoCqlTranslator("h","p",parameters,null);
        var maximumWork=binder.Bind(limits.MaximumStageRows);var maximumProof=binder.Bind(limits.MaximumProofPayloadBytes);
        // Reuse the exact native cells already validated under this read owner.
        // No second MATCH, managed predicate/sort, or synthetic entity Id.
        var text=new StringBuilder("WITH $setInput AS input,range(0,size($setInput)-1) AS s0,0 AS work,0 AS proofBytes ");
        var carried=AppendSelectedStages(text,plan.Query.Selection,binder,"input,s0",maximumWork,maximumProof,referenceBytes:4);
        var import=carried+",work,proofBytes";
        text.Append("CALL { WITH ").Append(import)
            .Append(" RETURN -1 AS phase,-1 AS position,-1 AS sourcePosition,size(input) AS nativeCount,work AS stageWork ");
        for(var index=0;index<plan.Query.Selection.ResultStages.Count;index++)
        {
            var list="s"+(index+1);
            text.Append("UNION ALL WITH ").Append(import).Append(" RETURN ").Append(index)
                .Append(" AS phase,-1 AS position,-1 AS sourcePosition,size(").Append(list).Append(") AS nativeCount,work AS stageWork ")
                .Append("UNION ALL WITH ").Append(import).Append(" UNWIND range(0,size(").Append(list).Append(")-1) AS pos RETURN ")
                .Append(index).Append(" AS phase,pos AS position,").Append(list)
                .Append("[pos] AS sourcePosition,size(").Append(list).Append(") AS nativeCount,work AS stageWork ");
        }
        text.Append("} RETURN phase,position,sourcePosition,nativeCount,stageWork ORDER BY phase ASC,position ASC");
        if(text.Length>1024*1024)throw new NeoCqlLimitException("Selected set stage command exceeds its statement budget.");
        return new NeoCqlCompositionCommand(text.ToString(),parameters);
    }

    private static async Task<CosmicSetProjectionCompositionPacket> ReadCqlSetProjectionOccurrencesAsync<T,TResult>(
        NeoCqlCompositionCommand command,CosmicSetProjectionPlan<T,TResult> plan,CosmicProjectionCompositionExecutionLimits limits,
        IAsyncTransaction transaction,NeoCqlReadBudget budget,IReadOnlyList<CosmicProjectionRow> input,CancellationToken token) where T:IHolon
    {
        void Check(){token.ThrowIfCancellationRequested();budget.Check();}
        var stages=Enumerable.Range(0,plan.Query.Selection.ResultStages.Count).Select(_=>new List<int>()).ToArray();
        var seen=stages.Select(_=>new HashSet<int>()).ToArray();
        var counts=Enumerable.Repeat(-1L,stages.Length).ToArray();
        bool header=false;long work=0,proofBytes=checked(4L*input.Count);
        Check();var cursor=await transaction.RunAsync(command.Text,command.Parameters).ConfigureAwait(false);
        while(await cursor.FetchAsync().ConfigureAwait(false))
        {
            Check();var record=cursor.Current;var phase=record["phase"].As<long>();
            var position=record["position"].As<long>();var sourcePosition=record["sourcePosition"].As<long>();
            var count=record["nativeCount"].As<long>();var stageWork=record["stageWork"].As<long>();
            if(phase==-1)
            {
                if(header || position!=-1 || sourcePosition!=-1 || count!=input.Count)
                    throw new InvalidDataException("Selected set native source header contradicts its captured cells.");
                header=true;work=stageWork;
                if(work<0 || work>limits.MaximumStageRows)
                    throw new NeoCqlLimitException("Selected set cumulative stage work exceeds its limit.");
                continue;
            }
            if(!header || phase<0 || phase>=stages.Length || stageWork!=work)
                throw new InvalidDataException("Selected set native stage phase or work header is invalid.");
            var index=(int)phase;
            if(position==-1)
            {
                if(counts[index]!=-1 || sourcePosition!=-1 || count<0 || count>input.Count)
                    throw new InvalidDataException("Selected set native stage header is invalid.");
                counts[index]=count;continue;
            }
            if(counts[index]!=count || position!=stages[index].Count || sourcePosition<0
                || sourcePosition>=input.Count || !seen[index].Add((int)sourcePosition))
                throw new InvalidDataException("Selected set native occurrence order is incomplete or duplicated.");
            proofBytes=checked(proofBytes+4);
            if(proofBytes>limits.MaximumProofPayloadBytes)
                throw new NeoCqlLimitException("Selected set native occurrence proof exceeds its limit.");
            stages[index].Add((int)sourcePosition);
        }
        Check();
        if(!header || counts.Where((count,index)=>count!=stages[index].Count).Any())
            throw new InvalidDataException("Selected set native stage proof is incomplete.");
        var final=stages[stages.Length-1];
        if(final.Count>limits.MaximumResults)throw new NeoCqlLimitException("Selected set output exceeds its limit.");
        return new CosmicSetProjectionCompositionPacket(input,
            stages.Select(stage=>new CosmicSetProjectionSelectedStageProof(stage)).ToArray(),
            final.Select(index=>input[index]).ToArray(),work);
    }
}
