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
        var predicate = query.AllPredicate.Predicates.Select(value => Translate(value, 0)).ToArray();
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
        var membership = query.Source.Predicates.Any(value => value is CosmicQueryAnyChild || value is CosmicQueryAnyDescendant);
        return new MongoCqlScalarCommand(source.ToArray(), membership, membership || query.Source.GraphOptions != null,
            query.Source.Limit == 0, traverses);
    }
}
