using System;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlScalarCommand CompileAll<T, TResult>(CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        var source = CompileSource(query.Source, out var traverses);
        var page = SelectedWindow(query.Source, query.Source.Limit);
        var hasChildren = CosmicScalarCapabilities.HasAllChildren(query.AllPredicate.Predicates);
        if (hasChildren)
            page.Add(new("$replaceRoot", new BsonDocument("newRoot", new BsonDocument("row", "$$ROOT"))));
        var childIndex = 0;
        BsonValue Test(CosmicQueryPredicate node)
        {
            if (node is CosmicQueryAnyChild child)
            {
                var alias = "child" + childIndex++;
                page.Add(ChildExistenceLookup(child, query.Source.ReadAccess, alias));
                return Op("$gt", new BsonDocument("$size", "$" + alias), Literal(0));
            }
            if (node is CosmicQueryLogical logical)
                return Op(logical.IsAnd ? "$and" : "$or", Test(logical.Left), Test(logical.Right));
            if (node is CosmicQueryNot not) return Op("$not", Test(not.Operand));
            var value = Translate(node, 0);
            return hasChildren ? AllRowExpression(value) : value;
        }
        var predicate = query.AllPredicate.Predicates.Select(Test).ToArray();
        page.Add(new("$project", new BsonDocument("bad",
            Choose(Op("$and", predicate), Literal(new BsonInt64(0)), Literal(new BsonInt64(1))))));
        if (traverses) FinishDescendantEnvelope(source, page); else source.AddRange(page);
        source.Add(new("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "count", new BsonDocument("$sum", traverses
                ? Choose(Op("$eq", "$error", Literal(0)), "$row.bad", Literal(new BsonInt64(0))) : "$bad") },
            { "invalid", new BsonDocument("$max", traverses ? Choose(Op("$eq", "$error", Literal(1)), Literal(1), Literal(0)) : Literal(0)) },
            { "limited", new BsonDocument("$max", traverses ? Choose(Op("$eq", "$error", Literal(2)), Literal(1), Literal(0)) : Literal(0)) }
        }));
        source.Add(new("$project", new BsonDocument
        {
            { "_id", 0 }, { "count", 1 },
            { "error", Choose(Op("$gt", "$invalid", Literal(0)), Literal(1),
                Choose(Op("$gt", "$limited", Literal(0)), Literal(2), Literal(0))) }
        }));
        ValidatePipeline(source);
        var membership = hasChildren || query.Source.Predicates.Any(value => value is CosmicQueryAnyChild || value is CosmicQueryAnyDescendant);
        return new MongoCqlScalarCommand(source.ToArray(), membership, membership || query.Source.GraphOptions != null,
            query.Source.Limit == 0, traverses);
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
