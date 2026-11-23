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
            CosmicProjectionCompositionExecutionLimits InputLimits(CosmicProjectionCompositionOptions options)
                =>new(options.MaximumInputRows,options.MaximumInputPayloadBytes,options.MaximumStageRows,
                    options.MaximumProofPayloadBytes,10000,options.MaximumInputPayloadBytes);
            var leftLimits=InputLimits(plan.Left.Options);var rightLimits=InputLimits(plan.Right.Options);
            var command=new MongoCqlTranslator().CompileProjectedSet(plan,leftLimits,rightLimits,
                _dbContext.Holon.CollectionNamespace.CollectionName);
            var left=(await ReadNativeSelectedPacketAsync(command.Left,plan.Query.Left,leftLimits,
                session,Options,token).ConfigureAwait(false)).Proof;
            var right=(await ReadNativeSelectedPacketAsync(command.Right,plan.Query.Right,rightLimits,
                session,Options,token).ConfigureAwait(false)).Proof;
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
                        if(proofBytes>plan.Options.MaximumProofPayloadBytes || output.Count>=20000)
                            throw new MongoCqlLimitException();
                        count=reported;priorRank=rank;output.Add(occurrence);
                    }
                if(count!=output.Count)throw new InvalidDataException("Native selected-set phase stream is incomplete.");
                return output;
            }
            var members=await Read(command.Members).ConfigureAwait(false);
            var stages=new List<CosmicProjectedSetStageProof>();var current=members;
            long work=checked((long)left.Rows.Count+right.Rows.Count);
            foreach(var pipeline in command.Stages)
            {
                work=checked(work+current.Count);
                if(work>plan.Options.MaximumStageRows)throw new MongoCqlLimitException();
                current=await Read(pipeline).ConfigureAwait(false);
                stages.Add(new(current));
            }
            if(work>plan.Options.MaximumStageRows || current.Count>limits.MaximumResults)throw new MongoCqlLimitException();
            var rows=new List<CosmicProjectionRow>();long bytes=0;
            foreach(var occurrence in current)
            {
                token.ThrowIfCancellationRequested();
                var source=occurrence.IsRight?right.Rows[occurrence.Index]:left.Rows[occurrence.Index];
                var indices=occurrence.IsRight?plan.Query.Right.OutputColumnIndices:plan.Query.Left.OutputColumnIndices;
                var values=indices.Select(index=>source.Row.Values[index]).ToArray();bytes=checked(bytes+16);
                foreach(var value in values)ReserveJoinCell(value,ref bytes,limits.MaximumPayloadBytes);
                rows.Add(new(values));
            }
            token.ThrowIfCancellationRequested();
            return new CosmicProjectedSetPacket(left,right,new(members),stages,rows,work);
        },cancellationToken);
}
