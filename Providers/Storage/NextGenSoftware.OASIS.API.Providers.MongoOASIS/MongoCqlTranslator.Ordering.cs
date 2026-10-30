using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed class MongoCqlCommand
{
    internal BsonDocument[] Pipeline { get; }
    internal int FetchCount { get; }
    internal bool RequiresMembership { get; }
    internal bool HasTraversalEnvelope { get; }
    internal MongoCqlEntityCompositionCommand EntityComposition { get; }
    internal MongoCqlCommand(BsonDocument[] pipeline,int fetchCount,bool requiresMembership=false,bool hasTraversalEnvelope=false,
        MongoCqlEntityCompositionCommand entityComposition=null)
    {Pipeline=pipeline;FetchCount=fetchCount;RequiresMembership=requiresMembership;HasTraversalEnvelope=hasTraversalEnvelope;EntityComposition=entityComposition;}
}

internal sealed partial class MongoCqlTranslator
{
    private static BsonValue OrderingKey(CosmicQueryPath path,string prefix)
    {
        BsonValue Key(BsonValue raw) => WithMetadataOperand(raw, path, value =>
        {
            var type=Nullable.GetUnderlyingType(path.ValueType)??path.ValueType;
            var valid=Domain(value,path.ValueType,path.IsMetadata);
            var rank=Choose(Op("$eq",Type(raw),Literal("missing")),Literal(0),
                Choose(Op("$eq",Type(raw),Literal("null")),Literal(1),Choose(valid,Literal(2),Literal(3))));
            var scalar=Choose(valid,type==typeof(Guid)?ComparableGuid(value):value,Literal(BsonNull.Value));
            return new BsonDocument{{"rank",rank},{"value",scalar}};
        }, prefix);
        return path.IsMetadata?Metadata(path.Segments,Key,prefix):Key(Canonical(path));
    }

    internal MongoCqlCommand CompileExecution<T>(CosmicQuery<T> query,int maximumResults,int? tailIndex = null) where T:IHolon
    {
        var source = CompileSource(query, out var traverses);
        var resultBudget = query.GraphOptions == null ? maximumResults : Math.Min(maximumResults, query.GraphOptions.MaximumNodes);
        var fetch = tailIndex.HasValue ? (query.Limit == 0 ? 0 : 1)
            : query.Limit.HasValue ? Math.Min(query.Limit.Value, resultBudget + 1) : resultBudget + 1;
        // Share the selected-window owner: Take(0) suppresses rows while the
        // outer source still produces protected traversal error envelopes.
        var pipeline = tailIndex.HasValue ? SelectedTailWindow(query, tailIndex.Value) : SelectedWindow(query, fetch);
        if(traverses)FinishDescendantEnvelope(source,pipeline);else source.AddRange(pipeline);
        pipeline=source;
        ValidatePipeline(pipeline);
        return new MongoCqlCommand(pipeline.ToArray(),fetch,query.Predicates.Any(HasRelationships),traverses);
    }
    private List<BsonDocument> CompileSource<T>(CosmicQuery<T> query,out bool traverses) where T:IHolon
    {
        if (query.Predicates.Any(NeedsComposedSource))
        {
            // Relationship-containing Boolean expressions cannot enter the scalar
            // translator or be filtered before their complete protected scopes.
            var composed = new List<BsonDocument> { new("$match", CompileRootPredicatesCore(query, true)) };
            var errors = new List<BsonValue>();
            var matched = AppendAllPredicate(composed, query.Predicates.Where(HasRelationships), query.ReadAccess, errors);
            composed.Add(new("$set", new BsonDocument { { "allMatched", matched }, { "error", AllErrorCode(errors) } }));
            composed.Add(new("$match", new BsonDocument("$expr", Op("$or", Op("$ne", "$error", Literal(0)), "$allMatched"))));
            traverses = true; // The existing envelope owner protects errors outside paging.
            return composed;
        }
        var localPredicates = new BsonArray();
        var rootFilter=CompileRootPredicates(query, localPredicates); // also registers any ancestor conditions
        var pipeline=new List<BsonDocument>();
        // Reduce candidate roots before the expensive inherited walk, but retain the
        // full original predicate after it. Foreign ancestors remain unfiltered.
        if (ancestorFields.Count != 0 && localPredicates.Count != 0)
            pipeline.Add(new("$match", new BsonDocument("$expr", new BsonDocument("$and", localPredicates))));
        if(AddAncestorStages(pipeline))
        {
            pipeline.Add(new("$match",rootFilter));
            pipeline.Add(new("$unset",new BsonArray(ancestorFields.Values.SelectMany(name=>new[]{name,name+"Next"}))));
        }
        else pipeline.Add(new("$match",rootFilter));
        AddChildExistence(pipeline,query);
        traverses=AddDescendantExistence(pipeline,query);
        return pipeline;
    }

    private static void ValidatePipeline(List<BsonDocument> pipeline)
    {
        var envelope=new BsonDocument("pipeline",new BsonArray(pipeline));
        var pending=new Stack<(BsonValue Value,int Depth)>();pending.Push((envelope,0));
        while(pending.Count!=0)
        {
            var entry=pending.Pop();if(entry.Depth>90)throw new MongoCqlLimitException();
            if(entry.Value is BsonDocument document)foreach(var element in document)pending.Push((element.Value,entry.Depth+1));
            else if(entry.Value is BsonArray array)foreach(var value in array)pending.Push((value,entry.Depth+1));
        }
        if(pipeline.Count>1000 || envelope.ToBson().Length>1024*1024)throw new MongoCqlLimitException();
    }

}
