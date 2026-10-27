using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    private void AddChildExistence<T>(List<BsonDocument> pipeline, CosmicQuery<T> query) where T : IHolon
    {
        var conditions = query.Predicates.OfType<CosmicQueryAnyChild>().ToArray();
        if (conditions.Length == 0) return;
        // Keep the original row inside an envelope. Persisted fields named row or
        // exists cannot collide with derived lookup state or disappear on return.
        pipeline.Add(new("$replaceRoot", new BsonDocument("newRoot", new BsonDocument("row", "$$ROOT"))));
        foreach (var child in conditions)
        {
            pipeline.Add(ChildExistenceLookup(child, query.ReadAccess, "exists"));
            pipeline.Add(new("$match", new BsonDocument("exists.0", new BsonDocument("$exists", true))));
            pipeline.Add(new("$unset", "exists"));
        }
        pipeline.Add(new("$replaceRoot", new BsonDocument("newRoot", "$row")));
    }
    // One native membership/visibility owner for source filters and terminal tests.
    private BsonDocument ChildExistenceLookup(CosmicQueryAnyChild child, CosmicQueryReadAccess access, string alias)
    {
        var nested = CosmicScalarCapabilities.HasAllChildren(new[] { child.Predicate });
        var match = Op("$and", ChildScopeCondition(access), nested ? Literal(true) : Translate(child.Predicate, 0));
        var childPipeline = new List<BsonDocument> { new("$match", new BsonDocument("$expr", match)) };
        if (nested)
        {
            var predicate = AppendAllPredicate(childPipeline, new[] { child.Predicate }, access);
            childPipeline.Add(new("$match", new BsonDocument("$expr", predicate)));
        }
        childPipeline.Add(new("$limit", 1));
        childPipeline.Add(new("$project", new BsonDocument { { "_id", 0 }, { "present", Literal(1) } }));
        return ChildLookup(alias, childPipeline);
    }

    private BsonValue ChildScopeCondition(CosmicQueryReadAccess access)
    {
        BsonValue GuidField(string name) => new BsonDocument("$toLower",
            new BsonDocument("$ifNull", new BsonArray { "$" + name, Guid.Empty.ToString("D") }));
        BsonValue visibility = Literal(true);
        if (access != null)
        {
            var secured = CosmicQuery<Core.Holons.Holon>.Create().WithReadAccess(access);
            if (secured.IsError) throw new NotSupportedException(secured.Message);
            visibility = Translate(secured.Result.Predicates.Single(), 0);
        }
        return Op("$and",
            Op("$eq", GuidField("ParentHolonId"), "$$parent"),
            Op("$ne", GuidField("HolonId"), "$$parent"),
            Op("$or", Op("$eq", "$$declared", Literal(false)), Op("$in", GuidField("HolonId"), "$$members")),
            visibility);
    }

    private static BsonDocument ChildLookup(string alias, IEnumerable<BsonDocument> pipeline)
        => new("$lookup", new BsonDocument
        {
            { "from", "Holon" },
            { "let", new BsonDocument { { "parent", new BsonDocument("$toLower", "$row.HolonId") },
                { "declared", "$row.CosmicCqlMembership.declared" }, { "members", "$row.CosmicCqlMembership.members" } } },
            { "pipeline", new BsonArray(pipeline) }, { "as", alias }
        });
}
