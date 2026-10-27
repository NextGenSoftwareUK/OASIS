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
            throw new NotSupportedException("An admitted numeric aggregate is required.");
        var source = CompileSource(query.Source, out var traverses);
        var page = SelectedWindow(query.Source, query.Source.Limit);
        // One extra input detects excess work. It cannot become a truncated
        // successful sum, including when many inputs are nullable/missing.
        page.Add(new("$limit", (long)CosmicNumericScalarResults.MaximumParticipatingValues + 1));
        var cells = new BsonDocument { { "_id", 0 }, { "cell", ProjectionCell(query.Selector, "cqlnumeric") } };
        var prefixOrder = new BsonDocument();
        if (query.UsesInt64DoubleAverage)
        {
            // Recreate the source's exact key order after its window, before
            // the cell projection drops canonical/metadata ordering paths.
            for (var index = 0; index < query.Source.Orderings.Count; index++)
            {
                var ordering = query.Source.Orderings[index]; var key = "checkedSort" + index;
                cells.Add(key, OrderingKey(ordering.Path, "checkedkey" + index));
                prefixOrder.Add(key + ".rank", ordering.Descending ? -1 : 1);
                prefixOrder.Add(key + ".value", ordering.Descending ? -1 : 1);
            }
            cells.Add("checkedIdentity", "$HolonId"); prefixOrder.Add("checkedIdentity", 1);
        }
        page.Add(new("$project", cells));
        var zero = Literal(new BsonDecimal128(Decimal128.Zero));
        var limb = Literal(new BsonDecimal128(new Decimal128(CosmicNumericScalarResults.LimbBase)));
        var inputType = Nullable.GetUnderlyingType(query.Selector.ValueType) ?? query.Selector.ValueType;
        var floatingInput = inputType == typeof(float) || inputType == typeof(double);
        var decimalInput = inputType == typeof(decimal);
        // Floating values have already been admitted/normalized by ProjectionCell.
        // Preserve those binary bits; Decimal128 conversion cannot own this domain.
        page.Add(new("$set", new BsonDocument("number", Choose(Op("$eq", "$cell.s", Literal(2)),
            floatingInput ? (BsonValue)"$cell.v" : new BsonDocument("$toDecimal", "$cell.v"),
            floatingInput ? Literal(0d) : zero))));
        if (decimalInput)
            page.Add(new("$set", new BsonDocument("decimalParts", DecimalScaledParts("$number"))));
        else if (floatingInput)
            page.Add(new("$set", new BsonDocument("floatingParts", FloatingScaledParts("$number", inputType == typeof(float)))));
        else
        {
            // The bounded integer quotient is exact at the truncation boundary.
            page.Add(new("$set", new BsonDocument("high", new BsonDocument("$trunc",
                new BsonArray { Op("$divide", "$number", limb), Literal(0) }))));
        }
        if (query.UsesInt64DoubleAverage)
            page.Add(new("$setWindowFields", new BsonDocument
            {
                { "sortBy", prefixOrder },
                { "output", new BsonDocument("checkedPrefix", new BsonDocument
                    { { "$sum", "$number" }, { "window", new BsonDocument("documents",new BsonArray { "unbounded", "current" }) } }) }
            }));
        var required = Nullable.GetUnderlyingType(query.Selector.ValueType) == null;
        var invalid = required ? Op("$ne", "$cell.s", Literal(2)) : Op("$eq", "$cell.s", Literal(3));
        var reduction = new BsonDocument
        {
            { "_id", BsonNull.Value }, { "inputs", new BsonDocument("$sum", Literal(1L)) },
            { "invalid", new BsonDocument("$sum", Choose(invalid, Literal(1L), Literal(0L))) },
            { "present", new BsonDocument("$sum", Choose(Op("$eq", "$cell.s", Literal(2)), Literal(1L), Literal(0L))) }
        };
        var output = new BsonDocument { { "_id", 0 }, { "inputs", 1 }, { "invalid", 1 }, { "present", 1 } };
        if (decimalInput || floatingInput)
        {
            var words = new BsonArray();
            var width = decimalInput ? CosmicDecimalScalarResults.LimbCount
                : inputType == typeof(float) ? CosmicFloatingAggregateResults.SingleLimbCount : CosmicFloatingAggregateResults.DoubleLimbCount;
            var digits = decimalInput ? "$decimalParts" : "$floatingParts";
            for (var part = 0; part < width; part++)
            {
                reduction.Add("p" + part, new BsonDocument("$sum", Op("$arrayElemAt", digits, Literal(part))));
                words.Add("$p" + part);
            }
            output.Add("parts", words);
        }
        else
        {
            reduction.Add("high", new BsonDocument("$sum", "$high"));
            reduction.Add("low", new BsonDocument("$sum", Op("$subtract", "$number", Op("$multiply", "$high", limb))));
            output.Add("high", 1); output.Add("low", 1);
        }
        if (query.UsesInt64DoubleAverage)
        {
            var overflow = Op("$or", Op("$lt", "$checkedPrefix", Literal(new BsonDecimal128(new Decimal128(long.MinValue)))),
                Op("$gt", "$checkedPrefix", Literal(new BsonDecimal128(new Decimal128(long.MaxValue)))));
            reduction.Add("overflow", new BsonDocument("$max", Choose(overflow, Literal(1L), Literal(0L))));
            output.Add("prefixOverflow", Op("$gt", "$overflow", Literal(0L)));
        }
        page.Add(new("$group", reduction));
        page.Add(new("$project", output));
        if (traverses) FinishDescendantEnvelope(source, page); else source.AddRange(page);
        ValidatePipeline(source);
        var membership = query.Source.Predicates.Any(p => p is CosmicQueryAnyChild || p is CosmicQueryAnyDescendant);
        return new MongoCqlScalarCommand(source.ToArray(), membership, membership || query.Source.GraphOptions != null,
            query.Source.Limit == 0, traverses);
    }
}
