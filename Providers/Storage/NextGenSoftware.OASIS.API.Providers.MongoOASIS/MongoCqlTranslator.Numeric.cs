using System;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlScalarCommand CompileNumeric<T, TResult>(CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        if (query.Selector == null || (query.Operation != CosmicScalarOperation.Sum && query.Operation != CosmicScalarOperation.Average))
            throw new NotSupportedException("An admitted integer numeric aggregate is required.");
        var source = CompileSource(query.Source, out var traverses);
        var page = SelectedWindow(query.Source, query.Source.Limit);
        // One extra input detects excess work. It cannot become a truncated
        // successful sum, including when many inputs are nullable/missing.
        page.Add(new("$limit", (long)CosmicNumericScalarResults.MaximumParticipatingValues + 1));
        page.Add(new("$project", new BsonDocument { { "_id", 0 }, { "cell", ProjectionCell(query.Selector, "cqlnumeric") } }));
        var zero = Literal(new BsonDecimal128(Decimal128.Zero));
        var limb = Literal(new BsonDecimal128(new Decimal128(CosmicNumericScalarResults.LimbBase)));
        page.Add(new("$set", new BsonDocument("number", Choose(Op("$eq", "$cell.s", Literal(2)),
            new BsonDocument("$toDecimal", "$cell.v"), zero))));
        // Decimal128 has 34 significant digits. The quotient has at most ten
        // integer digits, while its distance from the next integer is >=2^-32.
        // Thus Decimal128 division cannot cross a truncation boundary. The
        // resulting integer limbs and their bounded sums are exact.
        page.Add(new("$set", new BsonDocument("high", new BsonDocument("$trunc",
            new BsonArray { Op("$divide", "$number", limb), Literal(0) } ))));
        var required = Nullable.GetUnderlyingType(query.Selector.ValueType) == null;
        var invalid = required ? Op("$ne", "$cell.s", Literal(2)) : Op("$eq", "$cell.s", Literal(3));
        page.Add(new("$group", new BsonDocument
        {
            { "_id", BsonNull.Value }, { "inputs", new BsonDocument("$sum", Literal(1L)) },
            { "invalid", new BsonDocument("$sum", Choose(invalid, Literal(1L), Literal(0L))) },
            { "present", new BsonDocument("$sum", Choose(Op("$eq", "$cell.s", Literal(2)), Literal(1L), Literal(0L))) },
            { "high", new BsonDocument("$sum", "$high") },
            { "low", new BsonDocument("$sum", Op("$subtract", "$number", Op("$multiply", "$high", limb))) }
        }));
        page.Add(new("$project", new BsonDocument { { "_id", 0 }, { "inputs", 1 }, { "invalid", 1 }, { "present", 1 }, { "high", 1 }, { "low", 1 } }));
        if (traverses) FinishDescendantEnvelope(source, page); else source.AddRange(page);
        ValidatePipeline(source);
        var membership = query.Source.Predicates.Any(p => p is CosmicQueryAnyChild || p is CosmicQueryAnyDescendant);
        return new MongoCqlScalarCommand(source.ToArray(), membership, membership || query.Source.GraphOptions != null,
            query.Source.Limit == 0, traverses);
    }
}
