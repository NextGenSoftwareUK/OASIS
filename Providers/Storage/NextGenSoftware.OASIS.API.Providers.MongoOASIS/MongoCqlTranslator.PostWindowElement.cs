using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlCommand CompilePostWindowElement<T>(CosmicQueryPlan<T> plan, int maximumResults) where T : IHolon
    {
        if (plan?.PostWindowPredicate == null || plan.PostWindowLimit is not (1 or 2)
            || maximumResults < plan.PostWindowLimit)
            throw new ArgumentException("One admitted bounded post-window element plan is required.");
        var query = plan.Query;
        var source = CompileSource(query, out var sourceEnvelope);
        var page = SelectedWindow(query, query.Limit);
        var errors = new List<BsonValue>();
        var matched = AppendAllPredicate(page, plan.PostWindowPredicate.Predicates, query.ReadAccess, errors);
        var relationships = plan.PostWindowPredicate.Predicates.Any(HasRelationships);
        page.Add(new("$project", new BsonDocument
        {
            { "_id", 0 }, { "row", relationships ? "$row" : "$$ROOT" },
            { "matched", matched }, { "error", AllErrorCode(errors) }
        }));
        BsonDocument Flag(int code) => new("$max", Choose(Op("$eq", "$error", Literal(code)), Literal(1), Literal(0)));
        BsonDocument Window(int code)
        {
            var value = Flag(code);
            value.Add("window", new BsonDocument("documents", new BsonArray { "unbounded", "unbounded" }));
            return value;
        }
        // Every paged root contributes its protected terminal outcome before
        // filtering/limiting. A first match cannot conceal a later scope error.
        page.Add(new("$setWindowFields", new BsonDocument("output", new BsonDocument
            { { "invalid", Window(1) }, { "limited", Window(2) } })));
        var outcome = Choose(Op("$gt", "$invalid", Literal(0)), Literal(1),
            Choose(Op("$gt", "$limited", Literal(0)), Literal(2), Literal(0)));
        page.Add(new("$set", new BsonDocument("error", outcome)));
        page.Add(new("$match", new BsonDocument("$expr", Op("$or", Op("$ne", "$error", Literal(0)), "$matched"))));
        var keys = new BsonDocument();
        var ordering = new BsonDocument();
        var reverse = plan.TailIndex.HasValue;
        for (var index = 0; index < query.Orderings.Count; index++)
        {
            var item = query.Orderings[index];
            var key = "elementSort" + index;
            keys.Add(key, AllRowExpression(OrderingKey(item.Path, key)));
            var direction = item.Descending == reverse ? 1 : -1;
            ordering.Add(key + ".rank", direction);
            ordering.Add(key + ".value", direction);
        }
        if (keys.ElementCount != 0) page.Add(new("$set", keys));
        ordering.Add("row.HolonId", reverse ? -1 : 1);
        page.Add(new("$sort", ordering));
        page.Add(new("$limit", plan.PostWindowLimit.Value));
        page.Add(new("$project", new BsonDocument { { "_id", 0 }, { "row", 1 }, { "error", 1 } }));
        if (sourceEnvelope)
        {
            FinishDescendantEnvelope(source, page);
            source.Add(new("$project", new BsonDocument
            {
                { "_id", 0 }, { "error", Choose(Op("$ne", "$error", Literal(0)), "$error", "$row.error") },
                { "row", "$row.row" }
            }));
        }
        else source.AddRange(page);
        ValidatePipeline(source);
        return new MongoCqlCommand(source.ToArray(), query.Limit == 0 ? 0 : plan.PostWindowLimit.Value,
            relationships || query.Predicates.Any(HasRelationships), hasTraversalEnvelope: true);
    }
}
