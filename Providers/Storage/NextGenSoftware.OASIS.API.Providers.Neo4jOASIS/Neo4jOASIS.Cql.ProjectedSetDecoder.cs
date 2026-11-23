using System;
using System.Collections.Generic;
using System.Linq;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
public partial class Neo4jOASIS
{
    private sealed class NeoCqlProjectedSetDecoder<T,TResult> : NeoCqlDetachedSourceDecoder where T:IHolon
    {
        private readonly CosmicProjectedSetPlan<T,TResult> plan;
        private readonly CosmicProjectedSetExecutionLimits limits;
        private CosmicProjectedSetPacket packet;
        internal NeoCqlProjectedSetDecoder(CosmicProjectedSetPlan<T,TResult> plan,
            CosmicProjectedSetExecutionLimits limits,Action check):base(check)
        {this.plan=plan;this.limits=limits;}
        internal void Add(IReadOnlyDictionary<string,object> record)
        {
            check();
            if(packet!=null || record.Count!=5)throw Invalid("Native selected-set snapshot requires one exact envelope.");
            var left=Input("leftProof",plan.Query.Left,plan.Left.Options);
            var right=Input("rightProof",plan.Query.Right,plan.Right.Options);
            long proofBytes=0;
            CosmicProjectedSetStageProof Stage(object raw)
            {
                var references=new List<CosmicProjectedSetOccurrence>();var seen=new HashSet<CosmicProjectedSetOccurrence>();
                foreach(var value in raw.As<IReadOnlyList<object>>())
                {
                    check();var row=value.As<IReadOnlyDictionary<string,object>>();
                    if(row.Count!=2)throw Invalid("Native selected-set occurrence width is invalid.");
                    var side=Number(row,"side");var index=Number(row,"index");
                    if(side is not (0 or 1) || index<0 || index>=(side==0?left.Rows.Count:right.Rows.Count))
                        throw Invalid("Native selected-set occurrence is outside its complete input.");
                    var occurrence=new CosmicProjectedSetOccurrence(side==1,checked((int)index));
                    if(!seen.Add(occurrence))throw Invalid("Native selected-set occurrence repeats within one phase.");
                    proofBytes=checked(proofBytes+5);
                    if(proofBytes>plan.Options.MaximumProofPayloadBytes || references.Count>=20000)
                        throw new NeoCqlLimitException("Selected-set occurrence proof exceeds its bounded size.");
                    references.Add(occurrence);
                }
                return new(references);
            }
            var members=Stage(record["members"]);
            var stages=record["stages"].As<IReadOnlyList<object>>().Select(Stage).ToArray();
            if(stages.Length!=plan.Query.Output.ResultStages.Count)
                throw Invalid("Native selected-set output stages are incomplete.");
            var work=Number(record,"work");
            if(work<0 || work>plan.Options.MaximumStageRows)
                throw new NeoCqlLimitException("Selected-set native work exceeds its admitted bound.");
            var final=stages.Length==0?members.Occurrences:stages[stages.Length-1].Occurrences;
            if(final.Count>limits.MaximumResults)throw new NeoCqlLimitException("Selected-set output exceeds its admitted row bound.");
            var rows=new List<CosmicProjectionRow>();long outputBytes=0;
            foreach(var occurrence in final)
            {
                check();var source=occurrence.IsRight?right.Rows[occurrence.Index]:left.Rows[occurrence.Index];
                var indices=occurrence.IsRight?plan.Query.Right.OutputColumnIndices:plan.Query.Left.OutputColumnIndices;
                var values=indices.Select(index=>source.Row.Values[index]).ToArray();outputBytes=checked(outputBytes+16);
                foreach(var value in values)Reserve(value,ref outputBytes,limits.MaximumPayloadBytes);
                rows.Add(new(values));
            }
            check();packet=new(left,right,members,stages,rows,work);
            CosmicProjectionCompositionPacket Input(string key,CosmicProjectionQuery<T,TResult> query,
                CosmicProjectionCompositionOptions options)
            {
                var decoder=new NeoCqlCompositionDecoder<T,TResult>(query,ProjectedSetInputLimits(options),check,selectedSetInput:true);
                foreach(var row in record[key].As<IReadOnlyList<object>>())
                {
                    check();decoder.Add(row.As<IReadOnlyDictionary<string,object>>());
                }
                return decoder.Complete();
            }
        }
        internal CosmicProjectedSetPacket Complete()
        {
            check();return packet??throw Invalid("Native selected-set snapshot did not acknowledge its complete envelope.");
        }
    }
}
