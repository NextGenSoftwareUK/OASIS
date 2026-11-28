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
            if(record.Count!=(nodeLimits.NumericReduction!=null?7:nodeLimits.CountOnly?6:5))throw Invalid("Native selected-set snapshot requires one exact envelope.");
            var left=Input("leftProof",node.Left,node.LeftSet,node.LeftIdentity,node.Options.Bounds.MaximumLeftInputRows);
            var right=Input("rightProof",node.Right,node.RightSet,node.RightIdentity,node.Options.Bounds.MaximumRightInputRows);
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
                if(nodeLimits.NumericReduction is {} numericIntent)
                {
                    if(checked(work+final.Count)>node.Options.MaximumStageRows
                        ||checked(proofBytes+9L+8L*SelectedNumericWidth(numericIntent.InputType))>node.Options.MaximumProofPayloadBytes)
                        throw new NeoCqlLimitException("Native numeric work/proof exceeds its selected-set bound.");
                    var header=record["numeric"].As<IReadOnlyDictionary<string,object>>();
                    if(header.Count!=2)throw Invalid("Native selected-set numeric header is malformed.");
                    var numeric=ReadSelectedIntegerReduction(header["proof"],Number(header,"count"),numericIntent);
                    return CosmicProjectedSetPacket.FromNumeric(left.Proof,right.Proof,members,stages,nativeCount,numeric,work);
                }
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
                CosmicProjectionCompositionPlan<T,TResult> leaf,CosmicProjectedSetPlan<T,TResult> child,CosmicSetProjectionPlan<T,TResult> identity,int maximum)
            {
                var envelope=record[key].As<IReadOnlyDictionary<string,object>>();
                if(identity!=null)return IdentityInput(identity,envelope);
                if(envelope.Count!=2 || envelope["kind"].As<string>()!=(child==null?"source":"set"))
                    throw Invalid("The native selected input envelope does not match its captured source or subtree.");
                if(child!=null)
                {
                    var childPacket=ReadNode(child,new CosmicProjectedSetExecutionLimits(child.Options,maximum,nodeLimits.MaximumPayloadBytes),
                        envelope["proof"].As<IReadOnlyDictionary<string,object>>());
                    return(new(null,childPacket),childPacket.Rows);
                }
                var decoder=new NeoCqlCompositionDecoder<T,TResult>(leaf.Query,ProjectedSetInputLimits(leaf.Options),check,selectedSetInput:true);
                var entityProof=leaf.EntitySource==null?null:new NeoCqlEntitySelectedProof(leaf.EntitySource.Options);
                foreach(var row in envelope["proof"].As<IReadOnlyList<object>>())
                {
                    check();var captured=row.As<IReadOnlyDictionary<string,object>>();
                    if(entityProof?.Consume(captured)!=true)decoder.Add(captured);
                }
                entityProof?.Complete();
                var sourcePacket=decoder.Complete();
                return(new(sourcePacket,null),sourcePacket.Rows.Select(row=>row.Row).ToArray());
            }
        }
        private (CosmicProjectedSetInputPacket Proof,IReadOnlyList<CosmicProjectionRow> Rows) IdentityInput(
            CosmicSetProjectionPlan<T,TResult> identity,IReadOnlyDictionary<string,object> envelope)
        {
            if(envelope.Count!=4||envelope["kind"].As<string>()!="identity")
                throw Invalid("The native late identity envelope differs from its captured source.");
            var inputLimits=ProjectedSetInputLimits(identity.SelectedOptions);
            var sourceGuard=new NeoCqlEntityInputProof(new CosmicEntityCompositionOptions(
                Math.Max(identity.Source.Options.MaximumLeftInputRows,identity.Source.Options.MaximumRightInputRows),
                identity.Source.Options.MaximumInputPayloadBytes),checked((int)identity.Source.MaximumLeafRows));
            var input=new List<CosmicProjectionRow>();long inputBytes=0;
            foreach(var value in envelope["source"].As<IReadOnlyList<object>>())
            {
                check();var row=value.As<IReadOnlyDictionary<string,object>>();
                if(row.Count!=10)throw Invalid("The native identity source envelope has the wrong shape.");
                if(row["traversalCycle"].As<bool>())throw Invalid("Identity source traversal encountered a cycle.");
                if(row["traversalLimit"].As<bool>())throw new NeoCqlLimitException("Identity source exceeds its native traversal/work/payload allowance.");
                if(!row["valid"].As<bool>())throw Invalid("Identity source ownership or membership is invalid.");
                if(row["entityInput"].As<bool>()){sourceGuard.Observe(row);continue;}
                if(row["payloadLimit"].As<bool>())throw new NeoCqlLimitException("Identity selected row exceeds its native payload allowance.");
                if(input.Count>=inputLimits.MaximumInputRows)throw new NeoCqlLimitException("Identity selected input exceeds its result bound.");
                var selected=ReadNativeProjectionRow(row["cells"].As<IReadOnlyList<object>>(),identity.Selection.Query.Columns);
                inputBytes=checked(inputBytes+16);foreach(var cell in selected.Values)Reserve(cell,ref inputBytes,inputLimits.MaximumInputPayloadBytes);
                input.Add(selected);
            }
            var nativeStages=envelope["stages"].As<IReadOnlyList<object>>();
            if(nativeStages.Count!=identity.Query.Selection.ResultStages.Count)throw Invalid("Identity selected stage proof is incomplete.");
            var stages=new List<CosmicSetProjectionSelectedStageProof>();long proofBytes=checked(4L*input.Count);
            foreach(var native in nativeStages)
            {
                var positions=new List<int>();var seen=new HashSet<int>();
                foreach(var value in native.As<IReadOnlyList<object>>())
                {
                    check();if(value is not long ordinal||ordinal<0||ordinal>=input.Count||!seen.Add((int)ordinal))
                        throw Invalid("Identity selected stage contains an invalid or repeated occurrence.");
                    positions.Add((int)ordinal);proofBytes=checked(proofBytes+4);
                    if(proofBytes>inputLimits.MaximumProofPayloadBytes)throw new NeoCqlLimitException("Identity selected proof exceeds its byte allowance.");
                }
                stages.Add(new(positions));
            }
            if(proofBytes>inputLimits.MaximumProofPayloadBytes)throw new NeoCqlLimitException("Identity input proof exceeds its byte allowance.");
            var final=stages.Count==0?Enumerable.Range(0,input.Count).ToArray():stages[stages.Count-1].Occurrences.ToArray();
            var work=Number(envelope,"work");
            if(work<0||work>inputLimits.MaximumStageRows||final.Length>inputLimits.MaximumResults)
                throw new NeoCqlLimitException("Identity selected stages exceed their work/result allowance.");
            var packet=new CosmicSetProjectionCompositionPacket(input,stages,final.Select(index=>input[index]).ToArray(),work);
            return(CosmicProjectedSetInputPacket.FromIdentitySelection(packet,identity.Query.Source.TreeDepth,identity.Query.Source.LeafCount),packet.Rows);
        }
        internal CosmicProjectedSetPacket Complete()
        {
            check();return packet??throw Invalid("Native selected-set snapshot did not acknowledge its complete envelope.");
        }
    }
}
