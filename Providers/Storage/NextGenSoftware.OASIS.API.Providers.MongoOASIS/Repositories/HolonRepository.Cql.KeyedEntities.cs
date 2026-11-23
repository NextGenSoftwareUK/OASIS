using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task<CosmicKeyedEntityBatch> ExecuteNativeCqlKeyedEntitiesAsync<T>(
        MongoCqlProjectionCommand command,CosmicQuery<T> query,IReadOnlyList<CosmicQueryPath> keys,
        int maximumRows,int maximumBytes,CancellationToken cancellationToken) where T:IHolon
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var elapsed=System.Diagnostics.Stopwatch.StartNew();
        AggregateOptions Options()
        {
            deadline.Token.ThrowIfCancellationRequested();
            var remaining=TimeSpan.FromSeconds(30)-elapsed.Elapsed;
            if(remaining<=TimeSpan.Zero)throw new MongoCqlGraphTimeoutException();
            return new AggregateOptions{Collation=Collation.Simple,AllowDiskUse=false,BatchSize=1,
                MaxTime=TimeSpan.FromMilliseconds(Math.Ceiling(remaining.TotalMilliseconds))};
        }
        try
        {
            using var session=await _dbContext.MongoClient.StartSessionAsync(
                new ClientSessionOptions{Snapshot=true,CausalConsistency=false},deadline.Token).ConfigureAwait(false);
            if(command.RequiresMembership)
                await ValidateCqlMembershipAsync(session,Options,deadline.Token).ConfigureAwait(false);
            var input=await ReadNativeCqlKeyedSourceAsync(command,query,keys,Array.Empty<CosmicQueryPath>(),
                maximumRows,maximumBytes,session,Options,deadline.Token).ConfigureAwait(false);
            long bytes=input.Bytes;
            var entities=await ReadNativeCqlIdentitySnapshotsAsync(input.Rows.Keys.ToArray(),query.Scope,query.ReadAccess,
                checked((int)(maximumBytes-bytes)),session,Options,deadline.Token,
                payloadBytes=>bytes=checked(bytes+payloadBytes)).ConfigureAwait(false);
            deadline.Token.ThrowIfCancellationRequested();
            var rows=input.Rows.Values.Select(proof=>new CosmicKeyedEntityRow(proof,entities[proof.Id])).ToArray();
            return new CosmicKeyedEntityBatch(input.Count,rows);
        }
        catch(OperationCanceledException) when(!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        {throw new MongoCqlGraphTimeoutException();}
    }

    private async Task<Dictionary<Guid,IHolon>> ReadNativeCqlIdentitySnapshotsAsync(
        IReadOnlyList<Guid> identities,CosmicQueryScope scope,CosmicQueryReadAccess access,
        int maximumBytes,IClientSessionHandle session,Func<AggregateOptions> options,
        CancellationToken cancellationToken,Action<int> reserveBytes)
    {
        var byId=new Dictionary<Guid,IHolon>(identities.Count);
        long measured=0;
        foreach(var batch in identities.Chunk(CosmicQueryExpression.MaximumMembershipValues))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var selection=CosmicQuery<Core.Holons.Holon>.Create(scope).Where(child=>batch.Contains(child.Id));
            if(access!=null)selection=selection.WithReadAccess(access);
            if(selection.IsError)throw new InvalidDataException(selection.Message);
            var command=new MongoCqlTranslator().CompileExecution(selection.Result,batch.Length);
            var matched=await ReadNativeCqlRowsAsync(command,batch.Length,checked((int)(maximumBytes-measured)),
                session,options,cancellationToken,payloadBytes=>
                {
                    measured=checked(measured+payloadBytes);
                    if(measured>maximumBytes)throw new MongoCqlLimitException();
                    reserveBytes(payloadBytes);
                }).ConfigureAwait(false);
            foreach(var entity in matched)byId.Add(entity.Id,entity);
        }
        cancellationToken.ThrowIfCancellationRequested();
        if(byId.Count!=identities.Count || identities.Any(id=>!byId.ContainsKey(id)))
            throw new InvalidDataException("Native entity snapshots do not cover the complete selected identity set.");
        return byId;
    }
}
