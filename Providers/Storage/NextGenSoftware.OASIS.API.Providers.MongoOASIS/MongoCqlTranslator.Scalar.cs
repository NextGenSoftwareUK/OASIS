using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed class MongoCqlScalarCommand
{
    internal BsonDocument[] Pipeline { get; }
    internal bool RequiresMembership { get; }
    internal bool RequiresSnapshot { get; }
    internal bool IsEmpty { get; }
    internal bool HasTraversalEnvelope { get; }
    internal MongoCqlScalarCommand(BsonDocument[] pipeline, bool membership, bool snapshot, bool empty, bool traverses = false)
    { Pipeline = pipeline; RequiresMembership = membership; RequiresSnapshot = snapshot; IsEmpty = empty; HasTraversalEnvelope = traverses; }
}

internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlScalarCommand CompileCount<T>(CosmicQuery<T> query) where T : IHolon
    {
        var pipeline = CompileSource(query, out var traverses);
        var one = Literal(new BsonInt64(1));
        var zero = Literal(new BsonInt64(0));
        var group = new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "count", new BsonDocument("$sum", traverses ? Choose(Op("$eq", "$error", Literal(0)), one, zero) : one) },
            { "invalid", new BsonDocument("$max", traverses ? Choose(Op("$eq", "$error", Literal(1)), Literal(1), Literal(0)) : Literal(0)) },
            { "limited", new BsonDocument("$max", traverses ? Choose(Op("$eq", "$error", Literal(2)), Literal(1), Literal(0)) : Literal(0)) }
        };
        pipeline.Add(new BsonDocument("$group", group));

        // Validate every eligible traversal scope before window cardinality.
        // Scalar reduction never buffers stored rows or applies a row-publication cap.
        BsonValue count = Op("$max", zero, Op("$subtract", "$count", Literal(new BsonInt64(query.Offset))));
        if (query.Limit.HasValue) count = Op("$min", count, Literal(new BsonInt64(query.Limit.Value)));
        pipeline.Add(new BsonDocument("$project", new BsonDocument
        {
            { "_id", 0 }, { "count", count },
            { "error", Choose(Op("$gt", "$invalid", Literal(0)), Literal(1),
                Choose(Op("$gt", "$limited", Literal(0)), Literal(2), Literal(0))) }
        }));
        ValidatePipeline(pipeline);
        var membership = query.Predicates.Any(predicate => predicate is CosmicQueryAnyChild || predicate is CosmicQueryAnyDescendant);
        return new MongoCqlScalarCommand(pipeline.ToArray(), membership, membership || query.GraphOptions != null, query.Limit == 0);
    }
}
