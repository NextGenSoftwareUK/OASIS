using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlProjectionCommand CompileKeyedSource<T>(CosmicQuery<T> source,
        IReadOnlyList<CosmicQueryPath> keyPaths,IReadOnlyList<CosmicQueryPath> valuePaths,
        int maximumRows,IReadOnlyList<BsonDocument> prefix=null) where T:IHolon
        {
            var pipeline = CompileSource(source, out var traverses);
            if (prefix!=null) pipeline.InsertRange(0,prefix);
            var fetch = source.Limit.HasValue ? Math.Min(source.Limit.Value, maximumRows + 1) : maximumRows + 1;
            var page = SelectedWindow(source, fetch);
            var proof = new BsonDocument
            {
                { "_id", 0 }, { "id", "$HolonId" },
                { "visible", Op("$ifNull", "$IsPublic", Literal(false)) },
                { "creator", Op("$ifNull", "$CreatedByAvatarId", Literal(Guid.Empty.ToString("D"))) }
            };
            var keys = new BsonArray(); var values = new BsonArray(); var ordering = new BsonArray();
            for (var index = 0; index < keyPaths.Count; index++)
            {
                var path = keyPaths[index];
                keys.Add(ProjectionCell(path, "joinKey" + index));
                proof.Add("k" + index, OrderingKey(path, "joinComparable" + index));
            }
            for (var index = 0; index < valuePaths.Count; index++)
                values.Add(ProjectionCell(valuePaths[index], "joinValue" + index));
            var sourceSort = new BsonDocument();
            for (var index = 0; index < source.Orderings.Count; index++)
            {
                var item = source.Orderings[index]; var name = "o" + index;
                ordering.Add(ProjectionCell(item.Path, "joinOrdering" + index));
                proof.Add(name, OrderingKey(item.Path, "joinOrderComparable" + index));
                var direction = item.Descending ? -1 : 1;
                sourceSort.Add(name + ".rank", direction); sourceSort.Add(name + ".value", direction);
            }
            proof.Add("keys", keys); proof.Add("values", values); proof.Add("ordering", ordering);
            sourceSort.Add("id", 1);
            page.Add(new("$project", proof)); page.Add(CountWindow()); page.Add(new("$sort", sourceSort));
            if (traverses) FinishDescendantEnvelope(pipeline, page); else pipeline.AddRange(page);
            ValidatePipeline(pipeline);
            var membership = source.Predicates.Any(HasRelationships);
            return new MongoCqlProjectionCommand(pipeline.ToArray(), membership, true, traverses, fetch == 0);
        }


    private static BsonDocument CountWindow() => new("$setWindowFields", new BsonDocument("output",
            new BsonDocument("nativeCount", new BsonDocument
            {
                { "$count", new BsonDocument() },
                { "window", new BsonDocument("documents", new BsonArray { "unbounded", "unbounded" }) }
            })));

}
