using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
public partial class HolonRepository
{
    internal Task<CosmicProjectedSetPacket> ExecuteNativeProjectedSetAsync<T,TResult>(
        CosmicProjectedSetPlan<T,TResult> plan,CosmicProjectedSetExecutionLimits limits,
        CancellationToken cancellationToken) where T:IHolon
        =>ExecuteSelectedSnapshotOwnerAsync(async(session,Options,token)=>
        {
            var command=new MongoCqlTranslator().CompileProjectedSet(plan,
                MongoCqlTranslator.ProjectedSetInputLimits(plan.LeftInputOptions),
                MongoCqlTranslator.ProjectedSetInputLimits(plan.RightInputOptions),
                _dbContext.Holon.CollectionNamespace.CollectionName);
            async Task<CosmicProjectedSetPacket> ReadNode(CosmicProjectedSetPlan<T,TResult> node,
                MongoCqlProjectedSetCommand native,CosmicProjectedSetExecutionLimits nodeLimits)
            {
                async Task<(CosmicProjectedSetInputPacket Proof,IReadOnlyList<CosmicProjectionRow> Rows)> Input(
                    CosmicProjectionCompositionPlan<T,TResult> leaf,CosmicProjectedSetPlan<T,TResult> child,
                    MongoCqlCompositionCommand leafCommand,MongoCqlProjectedSetCommand childCommand,int maximum)
                {
                    if(child!=null)
                    {
                        var packet=await ReadNode(child,childCommand,
                            new CosmicProjectedSetExecutionLimits(child.Options,maximum,nodeLimits.MaximumPayloadBytes)).ConfigureAwait(false);
                        return(new(null,packet),packet.Rows);
                    }
                    var read=await ReadNativeSelectedPacketAsync(leafCommand,leaf.Query,
                        MongoCqlTranslator.ProjectedSetInputLimits(leaf.Options),session,Options,token).ConfigureAwait(false);
                    return(new(read.Proof,null),read.Proof.Rows.Select(row=>row.Row).ToArray());
                }
                var left=await Input(node.Left,node.LeftSet,native.Left,native.LeftSet,node.Options.Bounds.MaximumLeftInputRows).ConfigureAwait(false);
                var right=await Input(node.Right,node.RightSet,native.Right,native.RightSet,node.Options.Bounds.MaximumRightInputRows).ConfigureAwait(false);
            long proofBytes=0;
            async Task<List<CosmicProjectedSetOccurrence>> Read(BsonDocument[] pipeline)
            {
                var output=new List<CosmicProjectedSetOccurrence>();var seen=new HashSet<CosmicProjectedSetOccurrence>();
                long count=0,priorRank=0;
                using var cursor=await _dbContext.Holon.AggregateAsync<BsonDocument>(session,pipeline,
                    Options(),token).ConfigureAwait(false);
                while(await cursor.MoveNextAsync(token).ConfigureAwait(false))
                    foreach(var data in cursor.Current)
                    {
                        token.ThrowIfCancellationRequested();
                        if(data.ElementCount!=4)throw new InvalidDataException("Native selected-set occurrence proof shape is invalid.");
                        var side=CompositionCount(data,"side",allowZero:true);
                        var index=CompositionCount(data,"index",allowZero:true);
                        var rank=CompositionCount(data,"rank");
                        var reported=CompositionCount(data,"stageCount");
                        if(side>1 || index>=(side==0?left.Rows.Count:right.Rows.Count) || rank<=priorRank ||
                            output.Count>0 && reported!=count)
                            throw new InvalidDataException("Native selected-set occurrence, rank or count is invalid.");
                        var occurrence=new CosmicProjectedSetOccurrence(side==1,checked((int)index));
                        if(!seen.Add(occurrence))throw new InvalidDataException("Native selected-set occurrence repeats within one phase.");
                        proofBytes=checked(proofBytes+5);
                        if(proofBytes>node.Options.MaximumProofPayloadBytes || output.Count>=20000)
                            throw new MongoCqlLimitException();
                        count=reported;priorRank=rank;output.Add(occurrence);
                    }
                if(count!=output.Count)throw new InvalidDataException("Native selected-set phase stream is incomplete.");
                return output;
            }
            var members=await Read(native.Members).ConfigureAwait(false);
            var stages=new List<CosmicProjectedSetStageProof>();var current=members;
            long work=checked((long)left.Rows.Count+right.Rows.Count);
            foreach(var pipeline in native.Stages)
            {
                work=checked(work+current.Count);
                if(work>node.Options.MaximumStageRows)throw new MongoCqlLimitException();
                current=await Read(pipeline).ConfigureAwait(false);
                stages.Add(new(current));
            }
            if(work>node.Options.MaximumStageRows || current.Count>nodeLimits.MaximumResults)throw new MongoCqlLimitException();
            if(nodeLimits.CountOnly)
            {
                bool acknowledged=false;long nativeCount=0;
                using var countCursor=await _dbContext.Holon.AggregateAsync<BsonDocument>(session,
                    MongoCqlTranslator.CompileProjectedSetCount(native.Rows),Options(),token).ConfigureAwait(false);
                while(await countCursor.MoveNextAsync(token).ConfigureAwait(false))
                    foreach(var header in countCursor.Current)
                    {
                        token.ThrowIfCancellationRequested();
                        if(acknowledged||header.ElementCount!=1)throw new InvalidDataException("Selected-set count requires one native header.");
                        nativeCount=CompositionCount(header,"nativeCount",allowZero:true);acknowledged=true;
                    }
                if(!acknowledged)throw new InvalidDataException("Selected-set native count header is missing.");
                if(nativeCount>nodeLimits.MaximumResults)throw new MongoCqlLimitException();
                if(nodeLimits.NumericReduction is {} numericIntent)
                {
                    if(checked(work+current.Count)>node.Options.MaximumStageRows
                        ||checked(proofBytes+9L+8L*MongoCqlTranslator.SelectedNumericWidth(numericIntent.InputType))>node.Options.MaximumProofPayloadBytes)
                        throw new MongoCqlLimitException();
                    CosmicProjectedNumericPacket numeric=null;
                    using var numericCursor=await _dbContext.Holon.AggregateAsync<BsonDocument>(session,
                        new MongoCqlTranslator().CompileProjectedSetNumeric(native.Rows,numericIntent),Options(),token).ConfigureAwait(false);
                    while(await numericCursor.MoveNextAsync(token).ConfigureAwait(false))
                        foreach(var header in numericCursor.Current)
                        {
                            token.ThrowIfCancellationRequested();
                            if(numeric!=null)throw new InvalidDataException("Native selected-set numeric header is duplicated.");
                            numeric=ReadNativeCqlSelectedNumeric(header,numericIntent);
                        }
                    if(numeric==null)throw new InvalidDataException("Native selected-set numeric header is missing.");
                    return CosmicProjectedSetPacket.FromNumeric(left.Proof,right.Proof,new(members),stages,nativeCount,numeric,work);
                }
                return CosmicProjectedSetPacket.FromCount(left.Proof,right.Proof,new(members),stages,nativeCount,work);
            }
            var rows=new List<CosmicProjectionRow>();long bytes=0;
            foreach(var occurrence in current)
            {
                token.ThrowIfCancellationRequested();
                var source=occurrence.IsRight?right.Rows[occurrence.Index]:left.Rows[occurrence.Index];
                var indices=occurrence.IsRight?node.Query.RightInput.Anchor.OutputColumnIndices:node.Query.LeftInput.Anchor.OutputColumnIndices;
                var values=indices.Select(index=>source.Values[index]).ToArray();bytes=checked(bytes+16);
                foreach(var value in values)ReserveJoinCell(value,ref bytes,nodeLimits.MaximumPayloadBytes);
                rows.Add(new(values));
            }
            token.ThrowIfCancellationRequested();
            return CosmicProjectedSetPacket.FromTree(left.Proof,right.Proof,new(members),stages,rows,work);
            }
            return await ReadNode(plan,command,limits).ConfigureAwait(false);
        },cancellationToken);
}
