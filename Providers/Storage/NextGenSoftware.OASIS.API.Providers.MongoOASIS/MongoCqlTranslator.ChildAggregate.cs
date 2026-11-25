using System;
using System.Collections.Generic;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    // Q47: one correlated $lookup per aggregate, scoped exactly like child existence (membership + caller visibility),
    // reduced by $group inside the sub-pipeline. No matching visible child yields no group: Count and Sum are 0.
    private BsonDocument ChildAggregateLookup(CosmicQueryChildAggregate aggregate, CosmicQueryReadAccess access, string alias)
    {
        var match = Op("$and", ChildScopeCondition(access), Translate(aggregate.Predicate, 0));
        BsonValue contribution;
        if (aggregate.Function == CosmicQueryChildAggregateFunction.Count) contribution = Literal(new BsonInt64(1));
        else
        {
            // The shared ordering key yields the declared Int64 value, or null outside that domain ($sum ignores null).
            var key = OrderingKey(aggregate.Path, alias + "s");
            contribution = new BsonDocument("$let", new BsonDocument { { "vars", new BsonDocument("aggregateKey", key) },
                { "in", "$$aggregateKey.value" } });
        }
        var pipeline = new List<BsonDocument>
        {
            new("$match", new BsonDocument("$expr", match)),
            new("$group", new BsonDocument { { "_id", BsonNull.Value }, { "value", new BsonDocument("$sum", contribution) } }),
            new("$project", new BsonDocument { { "_id", 0 }, { "value", 1 } })
        };
        return ChildLookup(alias, pipeline);
    }

    private static BsonValue ChildAggregateTest(CosmicQueryChildAggregate aggregate, string alias)
    {
        var value = Op("$ifNull", new BsonDocument("$arrayElemAt", new BsonArray { "$" + alias + ".value", 0 }), Literal(new BsonInt64(0)));
        var name = aggregate.Operator switch
        {
            CosmicQueryComparisonOperator.Equal => "$eq", CosmicQueryComparisonOperator.NotEqual => "$ne",
            CosmicQueryComparisonOperator.GreaterThan => "$gt", CosmicQueryComparisonOperator.GreaterThanOrEqual => "$gte",
            CosmicQueryComparisonOperator.LessThan => "$lt", CosmicQueryComparisonOperator.LessThanOrEqual => "$lte",
            _ => throw new NotSupportedException("Unknown MongoDB CQL child aggregate comparison.")
        };
        return Op(name, value, Literal(new BsonInt64(aggregate.Value)));
    }
}
