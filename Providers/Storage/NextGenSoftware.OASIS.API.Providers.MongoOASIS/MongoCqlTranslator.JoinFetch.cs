using System;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    internal const string JoinFetchChildrenField = "CosmicJoinFetchChildren";

    /// <summary>
    /// A13 join fetch: one correlated <c>$lookup</c> appended to the admitted root pipeline. The child
    /// source's complete predicate set (including its mandatory read access) runs inside the lookup, so a
    /// child the caller cannot read never leaves the server. Each root carries at most bound + 1 children.
    /// </summary>
    internal MongoCqlCommand CompileJoinFetch<T>(MongoCqlCommand roots, CosmicQuery<T> children, int maximumChildren) where T : IHolon
    {
        if (roots.RequiresMembership || roots.HasTraversalEnvelope || roots.EntityComposition != null || roots.FetchCount == 0)
            throw new NotSupportedException("MongoDB join fetch requires a plain root window without relationship predicates.");
        var pipeline = roots.Pipeline.Concat(new[] { new MongoCqlTranslator().CompileJoinFetchLookup(children, maximumChildren) }).ToList();
        ValidatePipeline(pipeline);
        return new MongoCqlCommand(pipeline.ToArray(), roots.FetchCount);
    }

    private BsonDocument CompileJoinFetchLookup<T>(CosmicQuery<T> children, int maximumChildren) where T : IHolon
    {
        if (children.Predicates.Any(predicate => predicate is CosmicQueryAnyChild || predicate is CosmicQueryAnyDescendant || NeedsComposedSource(predicate)))
            throw new NotSupportedException("MongoDB join fetch admits direct-child predicates without relationship traversal.");
        var filter = CompileRootPredicatesCore(children, false);
        if (ancestorFields.Count != 0)
            throw new NotSupportedException("MongoDB join fetch admits direct-child predicates without ancestor traversal.");
        static BsonValue Id(string field) => Op("$toLower", Op("$ifNull", "$" + field, Literal("")));
        return new BsonDocument("$lookup", new BsonDocument
        {
            { "from", "Holon" },
            { "let", new BsonDocument("joinFetchParent", Id("HolonId")) },
            { "pipeline", new BsonArray
                {
                    new BsonDocument("$match", new BsonDocument("$expr", Op("$eq", Id("ParentHolonId"), "$$joinFetchParent"))),
                    new BsonDocument("$match", filter),
                    new BsonDocument("$project", new BsonDocument("Children", 0)),
                    new BsonDocument("$sort", new BsonDocument("HolonId", 1)),
                    new BsonDocument("$limit", maximumChildren + 1)
                }
            },
            { "as", JoinFetchChildrenField }
        });
    }
}
