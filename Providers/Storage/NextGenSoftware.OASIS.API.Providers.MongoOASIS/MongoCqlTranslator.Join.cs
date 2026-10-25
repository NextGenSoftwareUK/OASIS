using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed class MongoCqlJoinCommand
{
    internal MongoCqlProjectionCommand Left { get; }
    internal MongoCqlProjectionCommand Right { get; }
    internal BsonDocument[] Pairs { get; }
    internal MongoCqlJoinCommand(MongoCqlProjectionCommand left, MongoCqlProjectionCommand right, BsonDocument[] pairs)
    { Left = left; Right = right; Pairs = pairs; }
}

internal sealed partial class MongoCqlTranslator
{
    // This compiler does not enroll joins. A single snapshot owner must validate
    // both complete proof streams before executing the native matching stream.
    internal MongoCqlJoinCommand CompileJoin<TLeft, TRight, TKey, TResult>(
        CosmicJoinSelection<TLeft, TRight, TKey, TResult> query, CosmicJoinExecutionLimits limits)
        where TLeft : IHolon where TRight : IHolon
    {
        if (query == null || limits == null || limits.Admit(query.Join.Options).IsError)
            throw new ArgumentException("Join selection and admitted remaining limits are required.");
        var left = Source(query.Join.Left, true, limits.MaximumLeftRows);
        var right = Source(query.Join.Right, false, limits.MaximumRightRows);
        var pairs = Plain(left);
        var lookup = Plain(right);
        var bindings = new BsonDocument();
        var equality = new List<BsonValue>();
        for (var index = 0; index < query.Join.Keys.Count; index++)
        {
            var key = "k" + index;
            bindings.Add(key, "$" + key);
            equality.Add(Op("$and", Op("$eq", "$$" + key + ".rank", Literal(2)),
                Op("$eq", "$" + key + ".rank", Literal(2)),
                Op("$eq", "$$" + key + ".value", "$" + key + ".value")));
        }
        // Matching follows the right source's window, not its unpaged collection.
        lookup.Add(new("$match", new BsonDocument("$expr", Op("$and", equality.ToArray()))));
        var rightProjection = new BsonDocument { { "_id", 0 }, { "id", 1 } };
        for (var index = 0; index < query.Join.Right.Orderings.Count; index++) rightProjection.Add("o" + index, 1);
        lookup.Add(new("$project", rightProjection));
        var pairLimit = Math.Min(limits.MaximumPairs, (long)limits.MaximumResults) + 1;
        lookup.Add(new("$limit", pairLimit));
        pairs.Add(new("$lookup", new BsonDocument
        {
            { "from", "Holon" }, { "let", bindings }, { "pipeline", new BsonArray(lookup) }, { "as", "matched" }
        }));
        pairs.Add(new("$unwind", new BsonDocument
        {
            { "path", "$matched" }, { "preserveNullAndEmptyArrays", query.Join.Kind == CosmicJoinKind.Left }
        }));
        var projection = new BsonDocument
        {
            { "_id", 0 }, { "left", "$id" }, { "right", Op("$ifNull", "$matched.id", Literal(BsonNull.Value)) }
        };
        var sort = new BsonDocument();
        PairOrdering(query.Join.Left, "l", false);
        sort.Add("left", 1);
        PairOrdering(query.Join.Right, "r", true);
        sort.Add("right", 1);
        pairs.Add(new("$project", projection));
        pairs.Add(new("$sort", sort));
        pairs.Add(new("$limit", pairLimit));
        pairs.Add(CountWindow());
        pairs.Add(new("$sort", (BsonDocument)sort.DeepClone()));
        ValidatePipeline(pairs);
        return new MongoCqlJoinCommand(left, right, pairs.ToArray());

        void PairOrdering<T>(CosmicQuery<T> source, string prefix, bool matched) where T : IHolon
        {
            for (var index = 0; index < source.Orderings.Count; index++)
            {
                var name = prefix + index;
                projection.Add(name, (matched ? "$matched.o" : "$o") + index);
                var direction = source.Orderings[index].Descending ? -1 : 1;
                sort.Add(name + ".rank", direction); sort.Add(name + ".value", direction);
            }
        }

        MongoCqlProjectionCommand Source<T>(CosmicQuery<T> source, bool isLeft, int maximumRows) where T : IHolon
        {
            var pipeline = CompileSource(source, out var traverses);
            var fetch = source.Limit.HasValue ? Math.Min(source.Limit.Value, maximumRows + 1) : maximumRows + 1;
            var page = SelectedWindow(source, fetch);
            var proof = new BsonDocument
            {
                { "_id", 0 }, { "id", "$HolonId" },
                { "visible", Op("$ifNull", "$IsPublic", Literal(false)) },
                { "creator", Op("$ifNull", "$CreatedByAvatarId", Literal(Guid.Empty.ToString("D"))) }
            };
            var keys = new BsonArray(); var values = new BsonArray(); var ordering = new BsonArray();
            for (var index = 0; index < query.Join.Keys.Count; index++)
            {
                var path = isLeft ? query.Join.Keys[index].Left : query.Join.Keys[index].Right;
                keys.Add(ProjectionCell(path, "joinKey" + index));
                proof.Add("k" + index, OrderingKey(path, "joinComparable" + index));
            }
            var selected = query.Columns.Where(column => column.Source ==
                (isLeft ? CosmicJoinColumnSource.Left : CosmicJoinColumnSource.Right)).ToArray();
            for (var index = 0; index < selected.Length; index++)
                values.Add(ProjectionCell(selected[index].Path, "joinValue" + index));
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
            var membership = source.Predicates.Any(predicate => predicate is CosmicQueryAnyChild || predicate is CosmicQueryAnyDescendant);
            return new MongoCqlProjectionCommand(pipeline.ToArray(), membership, true, traverses, fetch == 0);
        }

        static BsonDocument CountWindow() => new("$setWindowFields", new BsonDocument("output",
            new BsonDocument("nativeCount", new BsonDocument
            {
                { "$count", new BsonDocument() },
                { "window", new BsonDocument("documents", new BsonArray { "unbounded", "unbounded" }) }
            })));

        static List<BsonDocument> Plain(MongoCqlProjectionCommand command)
        {
            var pipeline = command.Pipeline.Select(stage => (BsonDocument)stage.DeepClone()).ToList();
            if (command.HasTraversalEnvelope)
            {
                // Complete source errors must already have been validated by the
                // snapshot owner. They cannot become rows eligible for matching.
                pipeline.Add(new("$match", new BsonDocument("error", 0)));
                pipeline.Add(new("$replaceRoot", new BsonDocument("newRoot", "$row")));
            }
            return pipeline;
        }
    }
}
