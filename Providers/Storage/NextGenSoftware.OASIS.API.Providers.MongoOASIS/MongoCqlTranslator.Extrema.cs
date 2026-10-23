using System;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlScalarCommand CompileExtremum<T, TResult>(CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        if (query.Selector == null || (query.Operation != CosmicScalarOperation.Min && query.Operation != CosmicScalarOperation.Max))
            throw new NotSupportedException("Native extrema require one admitted typed selector.");
        var source = CompileSource(query.Source, out var traverses);
        // Apply the caller's source window before inspecting selected values.
        // The normal row-publication cap must not truncate aggregate inputs.
        var page = SelectedWindow(query.Source, query.Source.Limit);
        page.Add(new("$project", new BsonDocument
        {
            { "_id", 0 }, { "cell", ProjectionCell(query.Selector, "cqlscalar") },
            { "key", OrderingKey(query.Selector, "cqlscalarkey") }
        }));
        var required = query.Selector.ValueType.IsValueType && Nullable.GetUnderlyingType(query.Selector.ValueType) == null;
        var invalid = required ? Op("$ne", "$cell.s", Literal(2)) : Op("$eq", "$cell.s", Literal(3));
        page.Add(new("$group", new BsonDocument
        {
            { "_id", BsonNull.Value },
            { "invalid", new BsonDocument("$sum", Choose(invalid, Literal(1L), Literal(0L))) },
            { "present", new BsonDocument("$sum", Choose(Op("$eq", "$cell.s", Literal(2)), Literal(1L), Literal(0L))) },
            // One native reduction keeps the comparable key and original typed
            // payload together; it does not sort/buffer all selected input rows.
            { "selected", new BsonDocument(query.Operation == CosmicScalarOperation.Min ? "$min" : "$max",
                Choose(Op("$eq", "$cell.s", Literal(2)),
                    new BsonDocument { { "k", "$key.value" }, { "v", "$cell.v" } }, Literal(BsonNull.Value))) }
        }));
        page.Add(new("$project", new BsonDocument
        {
            { "_id", 0 }, { "invalid", 1 }, { "present", 1 },
            { "cell", new BsonDocument
                { { "s", Choose(Op("$gt", "$present", Literal(0L)), Literal(2), Literal(0)) },
                  { "v", Choose(Op("$gt", "$present", Literal(0L)), "$selected.v", Literal(BsonNull.Value)) } } }
        }));
        // Mandatory traversal failures live outside the source window and reduction.
        if (traverses) FinishDescendantEnvelope(source, page); else source.AddRange(page);
        ValidatePipeline(source);
        var membership = query.Source.Predicates.Any(predicate => predicate is CosmicQueryAnyChild || predicate is CosmicQueryAnyDescendant);
        return new MongoCqlScalarCommand(source.ToArray(), membership, membership || query.Source.GraphOptions != null,
            query.Source.Limit == 0, traverses);
    }
}
