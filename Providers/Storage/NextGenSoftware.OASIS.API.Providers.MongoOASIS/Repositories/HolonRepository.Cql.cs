using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task<IReadOnlyList<IHolon>> ExecuteNativeCqlAsync(MongoCqlCommand command,int maximumResults,CancellationToken cancellationToken)
    {
        using var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var elapsed=System.Diagnostics.Stopwatch.StartNew();
        AggregateOptions Options()
        {
            deadline.Token.ThrowIfCancellationRequested();
            var remaining=TimeSpan.FromSeconds(30)-elapsed.Elapsed;
            if(remaining<=TimeSpan.Zero)throw new MongoCqlGraphTimeoutException();
            return new AggregateOptions{Collation=Collation.Simple,MaxTime=TimeSpan.FromMilliseconds(Math.Ceiling(remaining.TotalMilliseconds)),BatchSize=16,AllowDiskUse=false};
        }
        try
        {
            using var session=command.RequiresMembership && command.FetchCount!=0
                ? await _dbContext.MongoClient.StartSessionAsync(new ClientSessionOptions{Snapshot=true,CausalConsistency=false},deadline.Token).ConfigureAwait(false)
                : null;
            return await ReadNativeCqlRowsAsync(command,maximumResults,64*1024*1024,session,Options,deadline.Token).ConfigureAwait(false);
        }
        catch(OperationCanceledException) when(!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        {throw new MongoCqlGraphTimeoutException();}
    }

    private async Task<IReadOnlyList<IHolon>> ReadNativeCqlRowsAsync(MongoCqlCommand command,int maximumResults,
        int maximumBytes,IClientSessionHandle session,Func<AggregateOptions> options,CancellationToken cancellationToken,
        Action<int> reserveNativeBytes=null)
    {
        if(command.FetchCount==0)return Array.Empty<IHolon>();
        if(command.RequiresMembership)
        {
            if(session==null)throw new InvalidOperationException("Mongo relationship validation requires its native snapshot owner.");
            await ValidateCqlMembershipAsync(session,options,cancellationToken).ConfigureAwait(false);
        }
        // The native pipeline owns filtering, exact sort keys, offset and page
        // boundaries. Disk spilling is explicitly disabled to bound resources.
        using var cursor=session==null
            ? await _dbContext.Holon.AggregateAsync<BsonDocument>(command.Pipeline,options(),cancellationToken).ConfigureAwait(false)
            : await _dbContext.Holon.AggregateAsync<BsonDocument>(session,command.Pipeline,options(),cancellationToken).ConfigureAwait(false);
        var rows=new List<IHolon>();long bytes=0;
        while(await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            foreach(var document in cursor.Current)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if(rows.Count>=maximumResults)throw new MongoCqlLimitException();
                var payload=document.ToBson();bytes+=payload.Length;
                if(bytes>maximumBytes)throw new MongoCqlLimitException();
                reserveNativeBytes?.Invoke(payload.Length);
                var native=BsonSerializer.Deserialize<Entities.Holon>(payload);
                rows.Add(DataHelper.ConvertMongoEntityToOASISHolon(native,mapChildren:false));
            }
        cancellationToken.ThrowIfCancellationRequested();return rows.AsReadOnly();
    }
}
