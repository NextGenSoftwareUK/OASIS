using System;
using System.Linq;
using System.Collections.Generic;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlScalarCommand CompileAll<T, TResult>(CosmicScalarQuery<T, TResult> query) where T : IHolon
        => CompileTerminalPredicate(query, false);

    internal MongoCqlScalarCommand CompileCardinalityPredicate<T, TResult>(CosmicScalarQuery<T, TResult> query) where T : IHolon
        => CompileTerminalPredicate(query, true);

    private MongoCqlScalarCommand CompileTerminalPredicate<T, TResult>(CosmicScalarQuery<T, TResult> query, bool countMatches) where T : IHolon
    {
        var source = CompileSource(query.Source, out var traverses);
        var page = SelectedWindow(query.Source, query.Source.Limit);
        var hasChildren = CosmicScalarCapabilities.HasAllChildren(query.TerminalPredicate.Predicates);
        var hasDescendants = CosmicScalarCapabilities.HasAllDescendants(query.TerminalPredicate.Predicates);
        var errors = new List<BsonValue>();
        var predicate = AppendAllPredicate(page, query.TerminalPredicate.Predicates, query.Source.ReadAccess, errors);
        BsonValue terminalError = AllErrorCode(errors);
        page.Add(new("$project", new BsonDocument {
            { "bad", Choose(predicate, Literal(new BsonInt64(countMatches ? 1 : 0)), Literal(new BsonInt64(countMatches ? 0 : 1))) },
            { "error", terminalError }
        }));
        if (traverses) FinishDescendantEnvelope(source, page); else source.AddRange(page);
        BsonValue outcomeError = traverses
            ? Choose(Op("$ne", "$error", Literal(0)), "$error", "$row.error") : "$error";
        source.Add(new("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "count", new BsonDocument("$sum", Choose(Op("$eq", outcomeError, Literal(0)), traverses ? "$row.bad" : "$bad", Literal(new BsonInt64(0)))) },
            { "invalid", new BsonDocument("$max", Choose(Op("$eq", outcomeError, Literal(1)), Literal(1), Literal(0))) },
            { "limited", new BsonDocument("$max", Choose(Op("$eq", outcomeError, Literal(2)), Literal(1), Literal(0))) }
        }));
        source.Add(new("$project", new BsonDocument
        {
            { "_id", 0 }, { "count", 1 },
            { "error", Choose(Op("$gt", "$invalid", Literal(0)), Literal(1),
                Choose(Op("$gt", "$limited", Literal(0)), Literal(2), Literal(0))) }
        }));
        ValidatePipeline(source);
        var membership = hasChildren || hasDescendants || query.Source.Predicates.Any(HasRelationships);
        return new MongoCqlScalarCommand(source.ToArray(), membership, membership || query.Source.GraphOptions != null,
            query.Source.Limit == 0, traverses || hasDescendants);
    }
    private BsonValue AppendAllPredicate(List<BsonDocument> pipeline, IEnumerable<CosmicQueryPredicate> predicates,
        CosmicQueryReadAccess access, List<BsonValue> traversalErrors = null)
    {
        var captured = predicates.ToArray();
        var hasChildren = CosmicScalarCapabilities.HasAllChildren(captured) || CosmicScalarCapabilities.HasAllDescendants(captured);
        if (hasChildren)
            pipeline.Add(new("$replaceRoot", new BsonDocument("newRoot", new BsonDocument("row", "$$ROOT"))));
        var childIndex = 0;
        BsonValue Test(CosmicQueryPredicate node)
        {
            if (node is CosmicQueryAnyDescendant descendant)
            {
                if (traversalErrors == null) throw new NotSupportedException("Nested descendant matching requires its own error envelope.");
                var alias = "descendant" + childIndex++;
                var traversal = new List<BsonDocument> {
                    new("$match", new BsonDocument("$expr", Op("$eq", new BsonDocument("$toLower", "$HolonId"), "$$root")))
                };
                AddDescendantEvaluation(traversal, new[] { descendant }, access, false);
                traversal.Add(new("$project", new BsonDocument { { "_id", 0 }, { "matched", "$allMatched" }, { "error", 1 } }));
                pipeline.Add(new("$lookup", new BsonDocument {
                    { "from", "Holon" },
                    { "let", new BsonDocument("root", new BsonDocument("$toLower", "$row.HolonId")) },
                    { "pipeline", new BsonArray(traversal) }, { "as", alias }
                }));
                BsonValue First(string field) => new BsonDocument("$arrayElemAt", new BsonArray { "$" + alias + "." + field, 0 });
                traversalErrors.Add(Op("$ifNull", First("error"), Literal(1)));
                return Op("$ifNull", First("matched"), Literal(false));
            }
            if (node is CosmicQueryAnyChild child)
            {
                var alias = "child" + childIndex++;
                if (CosmicScalarCapabilities.HasAllDescendants(new[] { child.Predicate }))
                {
                    if (traversalErrors == null) throw new NotSupportedException("Protected child traversal requires a scalar error owner.");
                    pipeline.Add(ChildOutcomeLookup(child, access, alias));
                    BsonValue First(string field) => new BsonDocument("$arrayElemAt", new BsonArray { "$" + alias + "." + field, 0 });
                    traversalErrors.Add(Op("$ifNull", First("error"), Literal(0)));
                    return Op("$ifNull", First("matched"), Literal(false));
                }
                pipeline.Add(ChildExistenceLookup(child, access, alias));
                return Op("$gt", new BsonDocument("$size", "$" + alias), Literal(0));
            }
            if (node is CosmicQueryLogical logical)
                return Op(logical.IsAnd ? "$and" : "$or", Test(logical.Left), Test(logical.Right));
            if (node is CosmicQueryNot not) return Op("$not", Test(not.Operand));
            var value = Translate(node, 0);
            return hasChildren ? AllRowExpression(value) : value;
        }
        return Op("$and", captured.Select(Test).ToArray());
    }

    private static BsonValue AllErrorCode(IReadOnlyList<BsonValue> errors)
        => Choose(Op("$in", Literal(1), new BsonArray(errors)), Literal(1),
            Choose(Op("$in", Literal(2), new BsonArray(errors)), Literal(2), Literal(0)));

    private BsonDocument ChildOutcomeLookup(CosmicQueryAnyChild child, CosmicQueryReadAccess access, string alias)
    {
        var pipeline = new List<BsonDocument> { new("$match", new BsonDocument("$expr", ChildScopeCondition(access))) };
        var errors = new List<BsonValue>();
        var predicate = AppendAllPredicate(pipeline, new[] { child.Predicate }, access, errors);
        var error = AllErrorCode(errors);
        // Complete every visible child's protected scope before reducing Any.
        // LIMIT 1 or filtering on matched would discard a later child's error.
        pipeline.Add(new("$group", new BsonDocument {
            { "_id", BsonNull.Value }, { "matched", new BsonDocument("$max", Choose(predicate, Literal(1), Literal(0))) },
            { "invalid", new BsonDocument("$max", Choose(Op("$eq", error, Literal(1)), Literal(1), Literal(0))) },
            { "limited", new BsonDocument("$max", Choose(Op("$eq", error, Literal(2)), Literal(1), Literal(0))) }
        }));
        pipeline.Add(new("$project", new BsonDocument {
            { "_id", 0 }, { "matched", Op("$gt", "$matched", Literal(0)) },
            { "error", Choose(Op("$gt", "$invalid", Literal(0)), Literal(1),
                Choose(Op("$gt", "$limited", Literal(0)), Literal(2), Literal(0))) }
        }));
        return ChildLookup(alias, pipeline);
    }

    // Rebind native scalar field references to the collision-free row envelope.
    // Literal values and native lexical variables must remain byte-for-byte values.
    private static BsonValue AllRowExpression(BsonValue expression)
    {
        if (expression is BsonString text)
            return text.Value.StartsWith("$", StringComparison.Ordinal) && !text.Value.StartsWith("$$", StringComparison.Ordinal)
                ? new BsonString("$row." + text.Value.Substring(1)) : expression;
        if (expression is BsonArray array) return new BsonArray(array.Select(AllRowExpression));
        if (expression is BsonDocument document)
        {
            if (document.Contains("$literal")) return document;
            return new BsonDocument(document.Select(element => new BsonElement(element.Name, AllRowExpression(element.Value))));
        }
        return expression;
    }

}
