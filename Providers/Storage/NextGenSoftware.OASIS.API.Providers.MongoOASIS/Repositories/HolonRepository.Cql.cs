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
        => await ReadNativeCqlRowsAsync(command,maximumResults,64*1024*1024,null,
            () => new AggregateOptions{Collation=Collation.Simple,MaxTime=TimeSpan.FromSeconds(30),BatchSize=16,AllowDiskUse=false},
            cancellationToken).ConfigureAwait(false);

    private async Task<IReadOnlyList<IHolon>> ReadNativeCqlRowsAsync(MongoCqlCommand command,int maximumResults,
        int maximumBytes,IClientSessionHandle session,Func<AggregateOptions> options,CancellationToken cancellationToken,
        Action<int> reserveNativeBytes=null)
    {
        if(command.FetchCount==0)return Array.Empty<IHolon>();
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
