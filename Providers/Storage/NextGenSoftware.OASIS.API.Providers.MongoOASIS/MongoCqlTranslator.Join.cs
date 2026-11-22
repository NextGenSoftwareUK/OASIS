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
        CosmicJoinSelection<TLeft, TRight, TKey, TResult> query, CosmicJoinExecutionLimits limits,
        IReadOnlyList<Guid> admittedParents = null)
        where TLeft : IHolon where TRight : IHolon
    {
        if (query == null || limits == null || limits.Admit(query.Join.Options).IsError)
            throw new ArgumentException("Join selection and admitted remaining limits are required.");
        var left = Source(query.Join.Left, true, limits.MaximumLeftRows);
        if (query.Join.RestrictsRightToLeftParents && admittedParents == null)
            return new MongoCqlJoinCommand(left, null, Array.Empty<BsonDocument>());
        var right = Source(query.Join.Right, false, limits.MaximumRightRows);
        var pairs = Plain(left);
        var lookup = Plain(right);
        var bindings = new BsonDocument();
        var equality = new List<BsonValue>();
        var reverse = new List<BsonValue>();
        for (var index = 0; index < query.Join.Keys.Count; index++)
        {
            var key = "k" + index;
            bindings.Add(key, "$" + key);
            var operation = query.Join.Keys[index].Comparison;
            var comparison = operation switch
            {
                CosmicJoinComparison.Equal or CosmicJoinComparison.NullSafeEqual => "$eq",
                CosmicJoinComparison.NotEqual or CosmicJoinComparison.NullSafeNotEqual => "$ne",
                CosmicJoinComparison.LessThan => "$lt",
                CosmicJoinComparison.LessThanOrEqual => "$lte",
                CosmicJoinComparison.GreaterThan => "$gt",
                CosmicJoinComparison.GreaterThanOrEqual => "$gte",
                _ => throw new NotSupportedException("Unknown native join comparison.")
            };
            var variable = "$$" + key;
            var document = "$" + key;
            equality.Add(PairComparison(variable,document));
            reverse.Add(PairComparison(document,variable));

            BsonValue PairComparison(string leftCell,string rightCell)
            {
                var leftPresent = Op("$eq", leftCell + ".rank", Literal(2));
                var rightPresent = Op("$eq", rightCell + ".rank", Literal(2));
                var bothPresent = Op("$and",leftPresent,rightPresent);
                var matchedValues = Op(comparison,leftCell + ".value",rightCell + ".value");
                return operation is CosmicJoinComparison.NullSafeEqual or CosmicJoinComparison.NullSafeNotEqual
                    ? Op("$cond",bothPresent,matchedValues,Op(comparison,leftPresent.DeepClone(),rightPresent.DeepClone()))
                    : Op("$and",bothPresent,matchedValues);
            }
        }
        BsonValue CombineKeys(IReadOnlyList<BsonValue> values) => query.Join.CombineKeyPredicates(
            values, group => Op("$and", group), groups => Op("$or", groups));
        // Matching follows the right source's window, not its unpaged collection.
        if (query.Join.Kind != CosmicJoinKind.Cross)
            lookup.Add(new("$match", new BsonDocument("$expr", CombineKeys(equality))));
        var selectedOrders = query.OutputOrdering.Select((term, index) =>
        {
            var column = query.Columns[term.Column];
            return (Key: "outputOrder" + index, OnLeft: column.Source == CosmicJoinColumnSource.Left,
                Cell: query.Columns.Take(term.Column).Count(item => item.Source == column.Source), term.Descending);
        }).ToArray();
        var rightProjection = new BsonDocument { { "_id", 0 }, { "id", 1 } };
        foreach (var order in selectedOrders.Where(term => !term.OnLeft))
            rightProjection.Add(order.Key, OutputOrderValue("$values", order.Cell));
        for (var index = 0; index < query.Join.Right.Orderings.Count; index++) rightProjection.Add("o" + index, 1);
        lookup.Add(new("$project", rightProjection));
        var pairLimit = (query.OutputLimit.HasValue
            ? limits.MaximumPairs
            : Math.Min(limits.MaximumPairs, (long)limits.MaximumResults)) + 1;
        lookup.Add(new("$limit", pairLimit));
        pairs.Add(new("$lookup", new BsonDocument
        {
            { "from", "Holon" }, { "let", bindings }, { "pipeline", new BsonArray(lookup) }, { "as", "matched" }
        }));
        pairs.Add(new("$unwind", new BsonDocument
        {
            { "path", "$matched" }, { "preserveNullAndEmptyArrays", query.Join.Kind is CosmicJoinKind.Left or CosmicJoinKind.Full }
        }));
        var projection = new BsonDocument
        {
            { "_id", 0 }, { "left", "$id" }, { "right", Op("$ifNull", "$matched.id", Literal(BsonNull.Value)) }
        };
        var sort = new BsonDocument();
        foreach (var order in selectedOrders)
        {
            projection.Add(order.Key, order.OnLeft
                ? OutputOrderValue("$values", order.Cell)
                : Op("$ifNull", "$matched." + order.Key, Literal(BsonNull.Value)));
            sort.Add(order.Key, order.Descending ? -1 : 1);
        }
        if (query.Join.Kind == CosmicJoinKind.Full)
        {
            projection.Add("leftPresent", Literal(true));
            sort.Add("leftPresent", -1);
        }
        PairOrdering(query.Join.Left, "l", false);
        sort.Add("left", 1);
        PairOrdering(query.Join.Right, "r", true);
        sort.Add("right", 1);
        pairs.Add(new("$project", projection));
        if (query.Join.Kind == CosmicJoinKind.Full)
        {
            // Match from the right window against the same left window at the session's snapshot time.
            // Only an existence probe is needed to exclude right rows already emitted by the left join.
            var antiProbe = Plain(left);
            antiProbe.Add(new("$match", new BsonDocument("$expr", CombineKeys(reverse))));
            antiProbe.Add(new("$project", new BsonDocument { { "_id", 0 }, { "id", 1 } }));
            antiProbe.Add(new("$limit", 1));
            var unmatchedRight = Plain(right);
            unmatchedRight.Add(new("$lookup", new BsonDocument
            {
                { "from", "Holon" }, { "let", bindings.DeepClone() },
                { "pipeline", new BsonArray(antiProbe) }, { "as", "matched" }
            }));
            unmatchedRight.Add(new("$match", new BsonDocument("matched.0", new BsonDocument("$exists", false))));
            unmatchedRight.Add(new("$limit", pairLimit));
            var rightOnly = new BsonDocument
            {
                { "_id", 0 }, { "leftPresent", Literal(false) },
                { "left", Literal(BsonNull.Value) }, { "right", "$id" }
            };
            foreach (var order in selectedOrders)
                rightOnly.Add(order.Key, order.OnLeft ? Literal(BsonNull.Value) : OutputOrderValue("$values", order.Cell));
            for (var index = 0; index < query.Join.Left.Orderings.Count; index++)
                rightOnly.Add("l" + index, Literal(BsonNull.Value));
            for (var index = 0; index < query.Join.Right.Orderings.Count; index++)
                rightOnly.Add("r" + index, "$o" + index);
            unmatchedRight.Add(new("$project", rightOnly));
            pairs.Add(new("$unionWith", new BsonDocument
                { { "coll", "Holon" }, { "pipeline", new BsonArray(unmatchedRight) } }));
        }
        if (query.OutputLimit is int requestedLimit)
        {
            var page = new List<BsonDocument>();
            if (requestedLimit == 0)
                page.Add(new("$match", new BsonDocument("$expr", Literal(false))));
            else if (query.OutputFromEnd > 0)
            {
                page.Add(new("$setWindowFields", new BsonDocument
                {
                    { "sortBy", sort },
                    { "output", new BsonDocument
                    {
                        // The full sort tuple includes deterministic tie breakers.
                        { "position", new BsonDocument
                        {
                            { "$sum", 1 },
                            { "window", new BsonDocument("documents", new BsonArray { "unbounded", "current" }) }
                        } },
                        { "pairCount", new BsonDocument
                        {
                            { "$count", new BsonDocument() },
                            { "window", new BsonDocument("documents", new BsonArray { "unbounded", "unbounded" }) }
                        } }
                    } }
                }));
                BsonValue end = query.OutputTailWindowLimit is int window
                    ? new BsonDocument("$min", new BsonArray { "$pairCount", (long)query.OutputOffset + window })
                    : new BsonString("$pairCount");
                var position = new BsonDocument("$subtract", new BsonArray { end, query.OutputFromEnd - 1 });
                page.Add(new("$match", new BsonDocument("$expr", new BsonDocument("$and", new BsonArray
                {
                    new BsonDocument("$eq", new BsonArray { "$position", position }),
                    new BsonDocument("$gt", new BsonArray { "$position", query.OutputOffset })
                }))));
            }
            else
            {
                page.Add(new("$sort", sort));
                if (query.OutputOffset != 0) page.Add(new("$skip", query.OutputOffset));
                page.Add(new("$limit", requestedLimit));
            }
            page.Add(new("$project", new BsonDocument { { "_id", 0 }, { "left", 1 }, { "right", 1 } }));
            pairs.Add(new("$limit", pairLimit));
            pairs.Add(new("$facet", new BsonDocument
            {
                { "total", new BsonArray { new BsonDocument("$count", "count") } },
                { "page", new BsonArray(page) }
            }));
        }
        else
        {
            pairs.Add(new("$sort", sort));
            pairs.Add(new("$limit", pairLimit));
            pairs.Add(CountWindow());
            pairs.Add(new("$sort", (BsonDocument)sort.DeepClone()));
            var hidden = new BsonDocument();
            if (query.Join.Kind == CosmicJoinKind.Full) hidden.Add("leftPresent", 0);
            foreach (var order in selectedOrders) hidden.Add(order.Key, 0);
            if (hidden.ElementCount != 0) pairs.Add(new("$project", hidden));
        }
        ValidatePipeline(pairs);
        return new MongoCqlJoinCommand(left, right, pairs.ToArray());

        void PairOrdering<T>(CosmicQuery<T> source, string prefix, bool matched) where T : IHolon
        {
            for (var index = 0; index < source.Orderings.Count; index++)
            {
                var name = prefix + index;
                // $project omits a missing lookup path on an unmatched left row.
                // Preserve the fixed pair proof shape with explicit null ordering
                // cells; the decoder must not guess which absent fields were intended.
                projection.Add(name, matched
                    ? Op("$ifNull", "$matched.o" + index, Literal(BsonNull.Value))
                    : (BsonValue)("$o" + index));
                var direction = source.Orderings[index].Descending ? -1 : 1;
                sort.Add(name + ".rank", direction); sort.Add(name + ".value", direction);
            }
        }

        MongoCqlProjectionCommand Source<T>(CosmicQuery<T> source,bool isLeft,int maximumRows) where T:IHolon
            =>CompileKeyedSource(source,query.Join.Keys.Select(key=>isLeft?key.Left:key.Right).ToArray(),
                query.Columns.Where(column=>column.Source==(isLeft?CosmicJoinColumnSource.Left:CosmicJoinColumnSource.Right))
                    .Select(column=>column.Path).ToArray(),maximumRows,
                !isLeft && query.Join.RestrictsRightToLeftParents?ScopedChildSource(admittedParents):null);

        static BsonDocument[] ScopedChildSource(IReadOnlyList<Guid> parents)
        {
            const string admittedParent = "cqlJoinParent";
            BsonValue Id(string field) => Op("$toLower", Op("$ifNull", "$" + field, Literal("")));
            var ids = new BsonArray(parents.Select(parent => parent.ToString("D")));
            var membership = Op("$or",
                Op("$eq", "$CosmicCqlMembership.declared", Literal(false)),
                Op("$in", "$$joinChild", Op("$ifNull", "$CosmicCqlMembership.members", Literal(new BsonArray()))));
            return new BsonDocument[]
            {
                new("$match", new BsonDocument("$expr", Op("$and",
                    Op("$in", Id("ParentHolonId"), Literal(ids)),
                    Op("$ne", Id("HolonId"), Id("ParentHolonId"))))),
                new("$lookup", new BsonDocument
                {
                    { "from", "Holon" },
                    { "let", new BsonDocument { { "joinParent", Id("ParentHolonId") }, { "joinChild", Id("HolonId") } } },
                    { "pipeline", new BsonArray
                        {
                            new BsonDocument("$match", new BsonDocument("$expr", Op("$and",
                                Op("$eq", Id("HolonId"), "$$joinParent"), membership))),
                            new BsonDocument("$project", new BsonDocument("_id", 1)),
                            new BsonDocument("$limit", 1)
                        }
                    },
                    { "as", admittedParent }
                }),
                new("$match", new BsonDocument(admittedParent + ".0", new BsonDocument("$exists", true))),
                new("$unset", admittedParent)
            };
        }

        static BsonValue OutputOrderValue(string path, int index)
            => new BsonDocument("$let", new BsonDocument
            {
                { "vars", new BsonDocument("cell", Op("$arrayElemAt", path, Literal(index))) },
                { "in", Op("$cond", Op("$eq", "$$cell.s", Literal(2)), "$$cell.v", Literal(BsonNull.Value)) }
            });

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
