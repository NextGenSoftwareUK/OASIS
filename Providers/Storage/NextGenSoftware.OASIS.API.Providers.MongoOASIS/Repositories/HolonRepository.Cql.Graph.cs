using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteNativeCqlGraphAsync(
        MongoCqlCommand command,int maximumResults,CosmicQueryGraphOptions graph,CancellationToken cancellationToken,
        CosmicQueryReadAccess readAccess)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if(command.FetchCount==0)return new OASISResult<IReadOnlyList<IHolon>>(Array.Empty<IHolon>()){IsLoaded=true};
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var elapsed=Stopwatch.StartNew();
        long nativeBytes=0;
        void Reserve(int count)
        {
            nativeBytes+=count;
            if(nativeBytes>graph.MaximumPayloadBytes)throw new MongoCqlLimitException();
        }
        void Check()
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(deadline.IsCancellationRequested || elapsed.Elapsed>=TimeSpan.FromSeconds(30))throw new MongoCqlGraphTimeoutException();
        }
        AggregateOptions Options()
        {
            Check();
            var remaining=TimeSpan.FromSeconds(30)-elapsed.Elapsed;
            if(remaining<=TimeSpan.Zero)throw new MongoCqlGraphTimeoutException();
            // MongoDB interprets maxTimeMS:0 as unlimited, so a submillisecond
            // remainder must round up rather than disabling the native deadline.
            return new AggregateOptions{Collation=Collation.Simple,MaxTime=TimeSpan.FromMilliseconds(Math.Ceiling(remaining.TotalMilliseconds)),
                BatchSize=16,AllowDiskUse=false};
        }
        try
        {
            // Snapshot sessions pin atClusterTime on the first read and carry it
            // to every descendant command. No transaction commit or write occurs.
            using var session=await _dbContext.MongoClient.StartSessionAsync(
                new ClientSessionOptions{Snapshot=true,CausalConsistency=false},deadline.Token).ConfigureAwait(false);
            var roots=await ReadNativeCqlRowsAsync(command,Math.Min(maximumResults,graph.MaximumNodes),
                graph.MaximumPayloadBytes,session,Options,deadline.Token,Reserve).ConfigureAwait(false);
            Task<IReadOnlyList<IHolon>> ReadChildren(CosmicQueryChildSelection selection,CancellationToken token)
                => ReadCqlGraphChildrenAsync(selection,session,Options,Reserve,token);
            var result=readAccess==null
                ? await HolonGraphQueryExpansion.ExpandAsync(roots,graph,ReadChildren,deadline.Token,Check).ConfigureAwait(false)
                : await HolonGraphQueryExpansion.ExpandAsync(roots,graph,readAccess,ReadChildren,deadline.Token,Check).ConfigureAwait(false);
            Check();
            return result;
        }
        catch(OperationCanceledException) when(!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        { throw new MongoCqlGraphTimeoutException(); }
    }

    private async Task<IReadOnlyList<IHolon>> ReadCqlGraphChildrenAsync(CosmicQueryChildSelection selection,
        IClientSessionHandle session,Func<AggregateOptions> options,Action<int> reserveNativeBytes,CancellationToken token)
    {
        var rows=new List<IHolon>();
        var parent=selection.ParentId;
        var childType=selection.ChildType;
        var members=selection.MemberIds;
        var batches=members==null?1:(members.Count+CosmicQueryExpression.MaximumMembershipValues-1)/CosmicQueryExpression.MaximumMembershipValues;
        long bytes=0;
        for(var batch=0;batch<batches;batch++)
        {
            token.ThrowIfCancellationRequested();
            var query=CosmicQuery<Core.Holons.Holon>.Create().Where(child=>child.ParentHolonId==parent);
            if(selection.ReadAccess!=null)query=query.WithReadAccess(selection.ReadAccess);
            if(childType!=HolonType.All)query=query.Where(child=>child.HolonType==childType);
            if(members!=null)
            {
                var selected=members.Skip(batch*CosmicQueryExpression.MaximumMembershipValues)
                    .Take(CosmicQueryExpression.MaximumMembershipValues).ToArray();
                query=query.Where(child=>selected.Contains(child.Id));
            }
            if(query.IsError)throw new InvalidOperationException(query.Message);
            var remaining=selection.MaximumNodes-rows.Count;
            query=query.Take(Math.Max(1,remaining+1));
            if(query.IsError)throw new InvalidOperationException(query.Message);
            var command=new MongoCqlTranslator().CompileExecution(query.Result,Math.Max(1,remaining));
            var chunk=await ReadNativeCqlRowsAsync(command,remaining,(int)Math.Max(0,selection.MaximumPayloadBytes-bytes),
                session,options,token,reserveNativeBytes).ConfigureAwait(false);
            // The shared assembler charges canonical UTF-8 records at discovery;
            // native BSON is also bounded before decoding across every batch.
            foreach(var row in chunk)
                bytes+=System.Text.Encoding.UTF8.GetByteCount(Core.Serialization.StoredRecordJson.Serialize(row));
            if(bytes>selection.MaximumPayloadBytes)throw new MongoCqlLimitException();
            rows.AddRange(chunk);
        }
        return rows.AsReadOnly();
    }
}

internal sealed class MongoCqlGraphTimeoutException:Exception
{
    internal MongoCqlGraphTimeoutException():base("MongoDB CQL graph exceeded its cumulative native deadline.") { }
}
