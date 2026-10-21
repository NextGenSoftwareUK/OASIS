using System;
using System.Collections.Generic;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed class MongoCqlCommand
{
    internal BsonDocument[] Pipeline { get; }
    internal int FetchCount { get; }
    internal MongoCqlCommand(BsonDocument[] pipeline,int fetchCount){Pipeline=pipeline;FetchCount=fetchCount;}
}

internal sealed partial class MongoCqlTranslator
{
    private static BsonValue OrderingKey(CosmicQueryPath path,string prefix)
    {
        BsonValue Key(BsonValue raw)
        {
            var type=Nullable.GetUnderlyingType(path.ValueType)??path.ValueType;
            var value=path.IsMetadata && type==typeof(DateTime)?MetadataDate(raw,path.Segments):raw;
            var valid=Domain(value,path.ValueType,path.IsMetadata);
            var rank=Choose(Op("$eq",Type(raw),Literal("missing")),Literal(0),
                Choose(Op("$eq",Type(raw),Literal("null")),Literal(1),Choose(valid,Literal(2),Literal(3))));
            var scalar=Choose(valid,type==typeof(Guid)?ComparableGuid(value):value,Literal(BsonNull.Value));
            return new BsonDocument{{"rank",rank},{"value",scalar}};
        }
        return path.IsMetadata?Metadata(path.Segments,Key,prefix):Key(Canonical(path));
    }

    internal MongoCqlCommand CompileExecution<T>(CosmicQuery<T> query,int maximumResults) where T:IHolon
    {
        var filter=Compile(query);
        var pipeline=new List<BsonDocument>{new("$match",filter),new("$project",new BsonDocument("Children",0))};
        if(query.Orderings.Count!=0)
        {
            // Wrap the complete row so computed sort keys cannot overwrite any
            // persisted property, including documents with hostile field names.
            var projection=new BsonDocument{{"_id",0},{"row","$$ROOT"}};
            var sort=new BsonDocument();
            for(var i=0;i<query.Orderings.Count;i++)
            {
                var ordering=query.Orderings[i];var key="s"+i;var direction=ordering.Descending?-1:1;
                projection.Add(key,OrderingKey(ordering.Path,"cqlsort"+i));
                sort.Add(key+".rank",direction);sort.Add(key+".value",direction);
            }
            sort.Add("row.HolonId",1);
            pipeline.Add(new("$project",projection));pipeline.Add(new("$sort",sort));
        }
        else pipeline.Add(new("$sort",new BsonDocument("HolonId",1)));
        if(query.Offset!=0)pipeline.Add(new("$skip",query.Offset));
        var fetch=query.Limit.HasValue?Math.Min(query.Limit.Value,maximumResults+1):maximumResults+1;
        pipeline.Add(new("$limit",Math.Max(1,fetch)));
        if(query.Orderings.Count!=0)pipeline.Add(new("$replaceRoot",new BsonDocument("newRoot","$row")));
        var envelope=new BsonDocument("pipeline",new BsonArray(pipeline));
        var pending=new Stack<(BsonValue Value,int Depth)>();pending.Push((envelope,0));
        while(pending.Count!=0)
        {
            var entry=pending.Pop();if(entry.Depth>90)throw new MongoCqlLimitException();
            if(entry.Value is BsonDocument document)foreach(var element in document)pending.Push((element.Value,entry.Depth+1));
            else if(entry.Value is BsonArray array)foreach(var value in array)pending.Push((value,entry.Depth+1));
        }
        if(envelope.ToBson().Length>1024*1024)throw new MongoCqlLimitException();
        return new MongoCqlCommand(pipeline.ToArray(),fetch);
    }
}
