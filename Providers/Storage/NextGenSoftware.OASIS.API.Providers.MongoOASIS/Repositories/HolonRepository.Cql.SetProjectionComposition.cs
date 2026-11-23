using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Queries;
namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
public partial class HolonRepository
{
    internal async Task<CosmicSetProjectionCompositionPacket> ReadNativeSetProjectionCompositionAsync(
        MongoCqlSetProjectionCompositionCommand command,CosmicProjectionCompositionExecutionLimits limits,
        IClientSessionHandle session,Func<AggregateOptions> options,
        IReadOnlyList<CosmicProjectionRow> input,CancellationToken token)
    {
        if(session==null)throw new InvalidDataException("Selected set stages require the complete input snapshot owner.");
        long proofBytes=checked(4L*input.Count);
        async Task<List<int>> Read(BsonDocument[] pipeline)
        {
            var rows=new List<int>();var seen=new HashSet<int>();long count=-1,rank=0;
            using var cursor=await _dbContext.Holon.AggregateAsync<BsonDocument>(session,pipeline,options(),token).ConfigureAwait(false);
            while(await cursor.MoveNextAsync(token).ConfigureAwait(false))
                foreach(var document in cursor.Current)
                {
                    token.ThrowIfCancellationRequested();
                    if(document.ElementCount!=4 || !document.TryGetValue("side",out var side)
                        || !document.TryGetValue("index",out var index) || !document.TryGetValue("rank",out var position)
                        || !document.TryGetValue("stageCount",out var stageCount)
                        || !(side.IsInt32 || side.IsInt64) || side.ToInt64()!=0
                        || !(index.IsInt32 || index.IsInt64) || !(position.IsInt32 || position.IsInt64)
                        || !(stageCount.IsInt32 || stageCount.IsInt64))
                        throw new InvalidDataException("Selected set native occurrence shape is invalid.");
                    var ordinal=index.ToInt64();var next=position.ToInt64();var total=stageCount.ToInt64();
                    if(ordinal<0 || ordinal>=input.Count || next<=rank || total<1 || total>input.Count
                        || count>=0 && total!=count || !seen.Add((int)ordinal))
                        throw new InvalidDataException("Selected set native occurrence or stage count is invalid.");
                    count=total;rank=next;rows.Add((int)ordinal);
                    proofBytes=checked(proofBytes+4);
                    if(proofBytes>limits.MaximumProofPayloadBytes || rows.Count>limits.MaximumInputRows)
                        throw new MongoCqlLimitException();
                }
            if(count>=0 && count!=rows.Count)throw new InvalidDataException("Selected set native stage is incomplete.");
            return rows;
        }
        var current=await Read(command.Input).ConfigureAwait(false);
        if(!current.SequenceEqual(Enumerable.Range(0,input.Count)))
            throw new InvalidDataException("Selected set native input occurrence order contradicts its complete cell read.");
        var stages=new List<CosmicSetProjectionSelectedStageProof>();long work=0;
        foreach(var pipeline in command.Stages)
        {
            work=checked(work+current.Count);
            if(work>limits.MaximumStageRows)throw new MongoCqlLimitException();
            current=await Read(pipeline).ConfigureAwait(false);
            stages.Add(new(current));
        }
        if(current.Count>limits.MaximumResults)throw new MongoCqlLimitException();
        token.ThrowIfCancellationRequested();
        return new CosmicSetProjectionCompositionPacket(input,stages,
            current.Select(index=>input[index]).ToArray(),work);
    }
}
