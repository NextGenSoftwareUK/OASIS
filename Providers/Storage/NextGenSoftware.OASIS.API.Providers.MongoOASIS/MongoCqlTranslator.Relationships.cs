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
        BsonValue GuidField(string name) => new BsonDocument("$toLower",
            new BsonDocument("$ifNull", new BsonArray { "$" + name, Guid.Empty.ToString("D") }));
        foreach (var child in conditions)
        {
            BsonValue visibility = Literal(true);
            if (query.ReadAccess != null)
            {
                var secured = CosmicQuery<Core.Holons.Holon>.Create().WithReadAccess(query.ReadAccess);
                if (secured.IsError) throw new NotSupportedException(secured.Message);
                visibility = Translate(secured.Result.Predicates.Single(), 0);
            }
            var match = Op("$and",
                Op("$eq", GuidField("ParentHolonId"), "$$parent"),
                Op("$ne", GuidField("HolonId"), "$$parent"),
                Op("$or", Op("$eq", "$$declared", Literal(false)), Op("$in", GuidField("HolonId"), "$$members")),
                visibility, Translate(child.Predicate, 0));
            pipeline.Add(new("$lookup", new BsonDocument
            {
                { "from", "Holon" },
                { "let", new BsonDocument { { "parent", new BsonDocument("$toLower", "$row.HolonId") },
                    { "declared", "$row.CosmicCqlMembership.declared" }, { "members", "$row.CosmicCqlMembership.members" } } },
                { "pipeline", new BsonArray { new BsonDocument("$match", new BsonDocument("$expr", match)),
                    new BsonDocument("$limit", 1),
                    new BsonDocument("$project", new BsonDocument { { "_id", 0 }, { "present", Literal(1) } }) } },
                { "as", "exists" }
            }));
            pipeline.Add(new("$match", new BsonDocument("exists.0", new BsonDocument("$exists", true))));
            pipeline.Add(new("$unset", "exists"));
        }
        pipeline.Add(new("$replaceRoot", new BsonDocument("newRoot", "$row")));
    }
}
