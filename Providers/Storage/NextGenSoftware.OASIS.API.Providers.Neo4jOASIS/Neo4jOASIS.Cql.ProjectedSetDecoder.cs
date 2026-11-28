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
            if(packet!=null)throw Invalid("Native selected-set snapshot repeats its envelope.");
            packet=ReadNode(plan,limits,record);
        }
        private CosmicProjectedSetPacket ReadNode(CosmicProjectedSetPlan<T,TResult> node,
            CosmicProjectedSetExecutionLimits nodeLimits,IReadOnlyDictionary<string,object> record)
        {
            check();
            if(record.Count!=(nodeLimits.CountOnly?6:5))throw Invalid("Native selected-set snapshot requires one exact envelope.");
            var left=Input("leftProof",node.Left,node.LeftSet,node.Options.Bounds.MaximumLeftInputRows);
            var right=Input("rightProof",node.Right,node.RightSet,node.Options.Bounds.MaximumRightInputRows);
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
                    if(proofBytes>node.Options.MaximumProofPayloadBytes || references.Count>=20000)
                        throw new NeoCqlLimitException("Selected-set occurrence proof exceeds its bounded size.");
                    references.Add(occurrence);
                }
                return new(references);
            }
            var members=Stage(record["members"]);
            var stages=record["stages"].As<IReadOnlyList<object>>().Select(Stage).ToArray();
            if(stages.Length!=node.Query.Output.ResultStages.Count)
                throw Invalid("Native selected-set output stages are incomplete.");
            var work=Number(record,"work");
            if(work<0 || work>node.Options.MaximumStageRows)
                throw new NeoCqlLimitException("Selected-set native work exceeds its admitted bound.");
            var final=stages.Length==0?members.Occurrences:stages[stages.Length-1].Occurrences;
            if(final.Count>nodeLimits.MaximumResults)throw new NeoCqlLimitException("Selected-set output exceeds its admitted row bound.");
            if(nodeLimits.CountOnly)
            {
                var nativeCount=Number(record,"nativeCount");
                if(nativeCount<0||nativeCount>nodeLimits.MaximumResults)throw new NeoCqlLimitException("Native selected-set count exceeds its admitted bound.");
                return CosmicProjectedSetPacket.FromCount(left.Proof,right.Proof,members,stages,nativeCount,work);
            }
            var rows=new List<CosmicProjectionRow>();long outputBytes=0;
            foreach(var occurrence in final)
            {
                check();var source=occurrence.IsRight?right.Rows[occurrence.Index]:left.Rows[occurrence.Index];
                var indices=occurrence.IsRight?node.Query.RightInput.Anchor.OutputColumnIndices:node.Query.LeftInput.Anchor.OutputColumnIndices;
                var values=indices.Select(index=>source.Values[index]).ToArray();outputBytes=checked(outputBytes+16);
                foreach(var value in values)Reserve(value,ref outputBytes,nodeLimits.MaximumPayloadBytes);
                rows.Add(new(values));
            }
            check();return CosmicProjectedSetPacket.FromTree(left.Proof,right.Proof,members,stages,rows,work);
            (CosmicProjectedSetInputPacket Proof,IReadOnlyList<CosmicProjectionRow> Rows) Input(string key,
                CosmicProjectionCompositionPlan<T,TResult> leaf,CosmicProjectedSetPlan<T,TResult> child,int maximum)
            {
                var envelope=record[key].As<IReadOnlyDictionary<string,object>>();
                if(envelope.Count!=2 || envelope["kind"].As<string>()!=(child==null?"source":"set"))
                    throw Invalid("The native selected input envelope does not match its captured source or subtree.");
                if(child!=null)
                {
                    var childPacket=ReadNode(child,new CosmicProjectedSetExecutionLimits(child.Options,maximum,nodeLimits.MaximumPayloadBytes),
                        envelope["proof"].As<IReadOnlyDictionary<string,object>>());
                    return(new(null,childPacket),childPacket.Rows);
                }
                var decoder=new NeoCqlCompositionDecoder<T,TResult>(leaf.Query,ProjectedSetInputLimits(leaf.Options),check,selectedSetInput:true);
                foreach(var row in envelope["proof"].As<IReadOnlyList<object>>())
                {
                    check();decoder.Add(row.As<IReadOnlyDictionary<string,object>>());
                }
                var sourcePacket=decoder.Complete();
                return(new(sourcePacket,null),sourcePacket.Rows.Select(row=>row.Row).ToArray());
            }
        }
        internal CosmicProjectedSetPacket Complete()
        {
            check();return packet??throw Invalid("Native selected-set snapshot did not acknowledge its complete envelope.");
        }
    }
}
