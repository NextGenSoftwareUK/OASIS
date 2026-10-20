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
    internal async Task<IReadOnlyList<IHolon>> ExecuteNativeCqlAsync(BsonDocument filter,int maximumResults,CancellationToken cancellationToken)
    {
        // Every predicate is evaluated by MongoDB; fetch one extra row only to detect budget overflow.
        using var cursor=await _dbContext.Holon.Find(filter,new FindOptions{Collation=Collation.Simple,MaxTime=TimeSpan.FromSeconds(30),BatchSize=16})
            .Sort(new BsonDocument("HolonId",1)).Limit(maximumResults+1)
            .Project<BsonDocument>(new BsonDocument("Children",0)).ToCursorAsync(cancellationToken).ConfigureAwait(false);
        var rows=new List<IHolon>();long bytes=0;
        while(await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
            foreach(var document in cursor.Current)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if(rows.Count>=maximumResults)throw new MongoCqlLimitException();
                var payload=document.ToBson();bytes+=payload.Length;
                if(bytes>64L*1024*1024)throw new MongoCqlLimitException();
                var native=BsonSerializer.Deserialize<Entities.Holon>(payload);
                rows.Add(DataHelper.ConvertMongoEntityToOASISHolon(native,mapChildren:false));
            }
        cancellationToken.ThrowIfCancellationRequested();return rows.AsReadOnly();
    }
}
