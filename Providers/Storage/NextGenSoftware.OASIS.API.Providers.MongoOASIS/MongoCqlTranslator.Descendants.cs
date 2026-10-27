using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    private bool AddDescendantExistence<T>(List<BsonDocument> pipeline, CosmicQuery<T> query) where T : IHolon
    {
        return AddDescendantEvaluation(pipeline, query.Predicates.OfType<CosmicQueryAnyDescendant>().ToArray(), query.ReadAccess, true);
    }

    private bool AddDescendantEvaluation(List<BsonDocument> pipeline, CosmicQueryAnyDescendant[] conditions,
        CosmicQueryReadAccess access, bool filterMatches)
    {
        if(conditions.Length==0)return false;
        // Reserve flat stages before allocating lookup trees. A small AST can
        // otherwise expand into thousands of stages before the final BSON guard.
        long stageCount=pipeline.Count+6L; // root envelope, conjunction and outcome stages
        foreach(var condition in conditions)
        {
            stageCount+=2L+3L*condition.Traversal.MaximumDepth;
            if(stageCount>1000)throw new MongoCqlLimitException();
        }
        pipeline.Add(new("$replaceRoot",new BsonDocument("newRoot",new BsonDocument
            {{"row","$$ROOT"},{"error",Literal(0)},{"allMatched",Literal(true)}})));
        foreach(var condition in conditions)
        {
            // Flat breadth-first lookup stages avoid nested-pipeline BSON depth.
            // The root is visited but never evaluated as its own descendant.
            pipeline.Add(new("$set",new BsonDocument
            {
                {"frontier",new BsonArray {new BsonDocument {
                    {"id",new BsonDocument("$toLower","$row.HolonId")},
                    {"declared","$row.CosmicCqlMembership.declared"},
                    {"members","$row.CosmicCqlMembership.members"}}}},
                {"visited",new BsonArray {new BsonDocument("$toLower","$row.HolonId")}},
                {"count",Literal(0)},{"matched",Literal(false)},
                {"cycle",Literal(false)},{"overBudget",Literal(false)}
            }));
            BsonValue visibility=Literal(true);
            if(access!=null)
            {
                var secured=CosmicQuery<Core.Holons.Holon>.Create().WithReadAccess(access);
                if(secured.IsError)throw new NotSupportedException(secured.Message);
                visibility=Translate(secured.Result.Predicates.Single(),0);
            }
            var nested = CosmicScalarCapabilities.HasAllChildren(new[] { condition.Predicate })
                || CosmicScalarCapabilities.HasAllDescendants(new[] { condition.Predicate });
            var predicate = nested ? null : Translate(condition.Predicate, 0);
            BsonValue Id(string field)=>new BsonDocument("$toLower",new BsonDocument("$ifNull",
                new BsonArray {"$"+field,Guid.Empty.ToString("D")}));
            var parent=new BsonDocument("$arrayElemAt",new BsonArray {
                new BsonDocument("$filter",new BsonDocument {{"input","$$frontier"},{"as","parent"},
                    {"cond",Op("$eq","$$parent.id",Id("ParentHolonId"))}}),0});
            var membership=new BsonDocument("$let",new BsonDocument {
                {"vars",new BsonDocument("parent",parent)},
                {"in",Op("$and",Op("$ne",Op("$ifNull","$$parent",Literal(BsonNull.Value)),Literal(BsonNull.Value)),
                    Op("$or",Op("$eq","$$parent.declared",Literal(false)),
                        Op("$in",Id("HolonId"),Op("$ifNull","$$parent.members",Literal(new BsonArray())))))}});
            var maximum=condition.Traversal.MaximumNodes;
            for(var level=0;level<condition.Traversal.MaximumDepth;level++)
            {
                var active=Op("$and",Op("$eq","$error",Literal(0)),
                    Op("$not","$cycle"),Op("$not","$overBudget"));
                var candidates = new List<BsonDocument> {
                    new("$match", new BsonDocument("$expr", Op("$and",
                        Op("$gt", new BsonDocument("$size", "$$frontier"), Literal(0)), membership, visibility))),
                    new("$limit", maximum + 1)
                };
                var errors = new List<BsonValue>();
                var matched = nested ? AppendAllPredicate(candidates, new[] { condition.Predicate }, access, errors) : predicate;
                var prefix = nested ? "row." : "";
                var projected = new BsonDocument {
                    { "_id", 0 }, { "id", Id(prefix + "HolonId") },
                    { "declared", "$" + prefix + "CosmicCqlMembership.declared" },
                    { "members", "$" + prefix + "CosmicCqlMembership.members" }, { "matched", matched },
                    { "cycle", Op("$in", Id(prefix + "HolonId"), "$$visited") }
                };
                if (nested)
                {
                    var nestedError = AllErrorCode(errors);
                    projected.Add("invalid", Op("$eq", nestedError, Literal(1)));
                    projected.Add("limited", Op("$eq", nestedError, Literal(2)));
                }
                candidates.Add(new("$project", projected));
                pipeline.Add(new("$lookup", new BsonDocument {
                    { "from", "Holon" },
                    { "let", new BsonDocument { { "frontier", Choose(active, "$frontier", Literal(new BsonArray())) }, { "visited", "$visited" } } },
                    { "pipeline", new BsonArray(candidates) }, { "as", "next" }
                }));
                var ids=new BsonDocument("$map",new BsonDocument {{"input","$next"},{"as","node"},{"in","$$node.id"}});
                var size=new BsonDocument("$size","$next");
                BsonValue Any(string field)=>new BsonDocument("$anyElementTrue",new BsonArray {
                    new BsonDocument("$map",new BsonDocument {{"input","$next"},{"as","node"},{"in","$$node."+field}})});
                var updates = new BsonDocument {
                    {"count",Op("$add","$count",size)},
                    {"cycle",Op("$or","$cycle",Any("cycle"),Op("$ne",size,new BsonDocument("$size",Op("$setUnion",ids,Literal(new BsonArray())))))},
                    {"overBudget",Op("$or","$overBudget",Op("$gt",Op("$add","$count",size),Literal(maximum)))},
                    {"matched",Op("$or","$matched",Any("matched"))},
                    {"visited",Op("$setUnion","$visited",ids)},
                    {"frontier",new BsonDocument("$map",new BsonDocument {{"input","$next"},{"as","node"},
                        {"in",new BsonDocument {{"id","$$node.id"},{"declared","$$node.declared"},{"members","$$node.members"}}}})}
                };
                if (nested)
                    updates.Add("error", Choose(Any("invalid"), Literal(1),
                        Choose(Op("$ne", "$error", Literal(0)), "$error", Choose(Any("limited"), Literal(2), Literal(0)))));
                pipeline.Add(new("$set", updates));
                pipeline.Add(new("$unset","next"));
            }
            pipeline.Add(new("$set",new BsonDocument {
                {"error", nested
                    ? Choose("$cycle", Literal(1), Choose(Op("$ne", "$error", Literal(0)), "$error", Choose("$overBudget", Literal(2), Literal(0))))
                    : Choose(Op("$ne","$error",Literal(0)),"$error",Choose("$cycle",Literal(1),Choose("$overBudget",Literal(2),Literal(0))))},
                {"allMatched",Op("$and","$allMatched","$matched")}
            }));
        }
        // Complete every condition's scope before the business conjunction.
        // An earlier negative match cannot suppress a later scope error.
        if (filterMatches)
            pipeline.Add(new("$match",new BsonDocument("$expr",Op("$or",Op("$gt","$error",Literal(0)),"$allMatched"))));
        return true;
    }

    private static void FinishDescendantEnvelope(List<BsonDocument> source,List<BsonDocument> page)
    {
        var roots=new List<BsonDocument> {
            new("$match",new BsonDocument("error",0)),
            new("$replaceRoot",new BsonDocument("newRoot","$row"))};
        roots.AddRange(page);
        roots.Add(new("$replaceRoot",new BsonDocument("newRoot",new BsonDocument {{"row","$$ROOT"},{"error",Literal(0)}})));
        source.Add(new("$facet",new BsonDocument {
            {"invalid",new BsonArray {
                new BsonDocument("$match",new BsonDocument("error",new BsonDocument("$gt",0))),
                new BsonDocument("$limit",1),
                new BsonDocument("$project",new BsonDocument {{"_id",0},{"error",1},{"row",Literal(BsonNull.Value)}})}},
            {"roots",new BsonArray(roots)}}));
        source.Add(new("$project",new BsonDocument("outcomes",Op("$concatArrays","$invalid","$roots"))));
        source.Add(new("$unwind","$outcomes"));
        source.Add(new("$replaceRoot",new BsonDocument("newRoot","$outcomes")));
    }
}
