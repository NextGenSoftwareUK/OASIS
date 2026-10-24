using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NativeHolon=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;
using Repository=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories.HolonRepository;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS
{
    private async Task<List<IHolon>> MatchHistoricalMetadataAsync(List<IHolon> candidates,Dictionary<string,string> metadata,MetaKeyValuePairMatchMode mode,bool synchronous)
    {
        if(metadata==null || candidates.Count==0)return candidates;
        var filter=Repository.BuildMetaDataMultiFilter(metadata,mode).Render(new RenderArgs<NativeHolon>(Database.Holon.DocumentSerializer,Database.Holon.Settings.SerializerRegistry));
        var selected=new HashSet<Guid>();var batch=new BsonArray();var bytes=0;
        async Task Flush()
        {
            if(batch.Count==0)return;
            // Literal input prevents strings/keys resembling operators becoming expressions.
            // The unchanged native filter owns scalar/null/compound conversion semantics.
            PipelineDefinition<NoPipelineInput,BsonDocument> pipeline=new[]{
                new BsonDocument("$documents",new BsonDocument("$literal",batch)),
                new BsonDocument("$match",filter),
                new BsonDocument("$project",new BsonDocument{{"_id",0},{"historyIdentity",1}})};
            using var cursor=synchronous?Database.MongoDB.Aggregate(pipeline):await Database.MongoDB.AggregateAsync(pipeline).ConfigureAwait(false);
            var rows=synchronous?cursor.ToList():await cursor.ToListAsync().ConfigureAwait(false);
            var expected=batch.Select(value=>Guid.Parse(value["historyIdentity"].AsString)).ToHashSet();
            foreach(var row in rows)
            {
                if(!row.TryGetValue("historyIdentity",out var identity) || !identity.IsString || !Guid.TryParse(identity.AsString,out var id)
                    || !expected.Contains(id) || !selected.Add(id))
                    throw new InvalidDataException("Historical native metadata result identity is inconsistent.");
            }
            batch=new BsonArray();bytes=0;
        }
        foreach(var candidate in candidates)
        {
            var native=Helpers.DataHelper.ConvertOASISHolonToMongoEntity(candidate,false);
            var body=native.ToBsonDocument(Database.Holon.DocumentSerializer);
            var projection=new BsonDocument{{"historyIdentity",candidate.Id.ToString("D")},{"MetaData",body.GetValue("MetaData",BsonNull.Value)}};
            var size=projection.ToBson().Length;
            // Batching bounds ordinary command payloads, not the total result count.
            // An oversized single item remains an explicit native error, never a dropped row.
            if(batch.Count>0 && (batch.Count>=256 || (long)bytes+size>1024*1024))await Flush().ConfigureAwait(false);
            batch.Add(projection);bytes=checked(bytes+size);
        }
        await Flush().ConfigureAwait(false);
        return candidates.Where(candidate=>selected.Contains(candidate.Id)).ToList();
    }
}
