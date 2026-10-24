using System;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed partial class MongoCqlTranslator
{
    // Compilation does not enroll a capability. Actual adapter admission and
    // bounded native packet decoding are required before this can be dispatched.
    internal MongoCqlProjectionCommand CompileGrouped<T, TKey, TResult>(
        CosmicGroupedSelection<T, TKey, TResult> query, int maximumGroups) where T : IHolon
    {
        if (maximumGroups < 0 || maximumGroups > query.Grouping.Options.MaximumGroups)
            throw new ArgumentOutOfRangeException(nameof(maximumGroups));

        var source = CompileSource(query.Source, out var traverses);
        var page = SelectedWindow(query.Source, query.Source.Limit);
        page.Add(new("$limit", (long)CosmicNumericScalarResults.MaximumParticipatingValues + 1));

        var cells = new BsonDocument { { "_id", 0 } };
        var identity = new BsonDocument();
        var reduction = new BsonDocument();
        var invalid = new BsonArray();
        var sort = new BsonDocument();
        BsonValue Bad(CosmicQueryPath path, string cell)
            => path.ValueType.IsValueType && Nullable.GetUnderlyingType(path.ValueType) == null
                ? Op("$ne", cell + ".s", Literal(2))
                : Op("$eq", cell + ".s", Literal(3));

        for (var index = 0; index < query.Grouping.Key.Columns.Count; index++)
        {
            var path = query.Grouping.Key.Columns[index].Path;
            var name = "k" + index;
            cells.Add(name, ProjectionCell(path, "cqlgroupkey" + index));
            cells.Add(name + "order", OrderingKey(path, "cqlgrouporder" + index));
            // Comparable keys own GUID and exact timestamp normalization. Keep
            // one typed cell separately for the detached result decoder.
            identity.Add(name, Choose(Op("$eq", "$" + name + ".s", Literal(2)),
                "$" + name + "order.value", Literal(BsonNull.Value)));
            reduction.Add(name, new BsonDocument("$first", "$" + name));
            invalid.Add(Bad(path, "$" + name));
            sort.Add("_id." + name, 1);
        }

        var zero = Literal(new BsonDecimal128(Decimal128.Zero));
        var limb = Literal(new BsonDecimal128(new Decimal128(CosmicNumericScalarResults.LimbBase)));
        var numeric = new BsonDocument();
        var high = new BsonDocument();
        for (var index = 0; index < query.Columns.Count; index++)
        {
            var column = query.Columns[index];
            if (column.KeyIndex >= 0 || column.Selector == null) continue;
            var name = "a" + index;
            cells.Add(name, ProjectionCell(column.Selector, "cqlgroupaggregate" + index));
            invalid.Add(Bad(column.Selector, "$" + name));
            var present = Op("$eq", "$" + name + ".s", Literal(2));
            reduction.Add(name + "present", new BsonDocument("$sum", Choose(present, Literal(1L), Literal(0L))));
            if (column.Operation is CosmicScalarOperation.Sum or CosmicScalarOperation.Average)
            {
                numeric.Add(name + "number", Choose(present, new BsonDocument("$toDecimal", "$" + name + ".v"), zero));
                high.Add(name + "high", new BsonDocument("$trunc", new BsonArray
                    { Op("$divide", "$" + name + "number", limb), Literal(0) }));
                reduction.Add(name + "high", new BsonDocument("$sum", "$" + name + "high"));
                reduction.Add(name + "low", new BsonDocument("$sum", Op("$subtract", "$" + name + "number",
                    Op("$multiply", "$" + name + "high", limb))));
            }
            else if (column.Operation is CosmicScalarOperation.Min or CosmicScalarOperation.Max)
            {
                cells.Add(name + "order", OrderingKey(column.Selector, "cqlgroupextreme" + index));
                reduction.Add(name, new BsonDocument(column.Operation == CosmicScalarOperation.Min ? "$min" : "$max",
                    Choose(present, new BsonDocument { { "k", "$" + name + "order.value" },
                        { "v", "$" + name + ".v" } }, Literal(BsonNull.Value))));
            }
            else throw new NotSupportedException("Unknown grouped native aggregate.");
        }

        page.Add(new("$project", cells));
        if (numeric.ElementCount != 0) page.Add(new("$set", numeric));
        if (high.ElementCount != 0) page.Add(new("$set", high));
        reduction.InsertAt(0, new BsonElement("_id", identity));
        reduction.Add("inputs", new BsonDocument("$sum", Literal(1L)));
        reduction.Add("invalid", new BsonDocument("$sum", Choose(new BsonDocument("$or", invalid), Literal(1L), Literal(0L))));
        page.Add(new("$group", reduction));
        // Compute global participation before the group output sentinel. Neither
        // excess work nor excess groups can silently become a partial success.
        page.Add(new("$setWindowFields", new BsonDocument("output", new BsonDocument("work", new BsonDocument
        {
            { "$sum", "$inputs" },
            { "window", new BsonDocument("documents", new BsonArray { "unbounded", "unbounded" }) }
        }))));
        var sortFields = new BsonDocument();
        var selectedSort = new BsonDocument();
        for (var index = 0; index < query.Orderings.Count; index++)
        {
            var ordering = query.Orderings[index];
            var column = query.Columns[ordering.ColumnIndex];
            BsonValue value;
            if (column.KeyIndex >= 0) value = "$_id.k" + column.KeyIndex;
            else if (column.Operation is CosmicScalarOperation.Count or CosmicScalarOperation.LongCount) value = "$inputs";
            else if (column.Operation is CosmicScalarOperation.Min or CosmicScalarOperation.Max)
                value = "$a" + ordering.ColumnIndex + ".k";
            else if (column.Operation == CosmicScalarOperation.Sum)
                // Bounded Int64 input has at most29 decimal digits in total.
                // Decimal128 arithmetic is exact here, including cancelling sums.
                value = Op("$add", Op("$multiply", "$a" + ordering.ColumnIndex + "high", limb), "$a" + ordering.ColumnIndex + "low");
            else throw new NotSupportedException("Exact native ordering is not enrolled for this aggregate.");
            var alias = "cqlGroupSort" + index;
            sortFields.Add(alias, value);
            selectedSort.Add(alias, ordering.Descending ? -1 : 1);
        }
        if (sortFields.ElementCount != 0)
        {
            page.Add(new("$set", sortFields));
            // The complete normalized identity is always the final stable tie.
            selectedSort.AddRange(sort);
            sort = selectedSort;
        }
        // Page membership is computed natively, while every bounded group
        // packet remains available to prove off-page domains and arithmetic.
        page.Add(new("$setWindowFields", new BsonDocument
        {
            { "sortBy", sort },
            { "output", new BsonDocument("cqlGroupPosition", new BsonDocument
                { { "$sum", Literal(1L) }, { "window", new BsonDocument("documents", new BsonArray { "unbounded", "current" }) } }) }
        }));
        page.Add(new("$set", new BsonDocument("resultMember", Op("$and",
            Op("$gt", "$cqlGroupPosition", Literal(query.ResultOffset)),
            query.ResultLimit.HasValue
                ? Op("$lte", "$cqlGroupPosition", Literal((long)query.ResultOffset + query.ResultLimit.Value))
                : Literal(true)))));
        page.Add(new("$sort", sort));
        page.Add(new("$limit", maximumGroups + 1));
        page.Add(new("$unset", new BsonArray(sortFields.Names.Concat(new[] { "cqlGroupPosition" }))));
        if (traverses) FinishDescendantEnvelope(source, page); else source.AddRange(page);
        ValidatePipeline(source);
        var membership = query.Source.Predicates.Any(p => p is CosmicQueryAnyChild || p is CosmicQueryAnyDescendant);
        return new MongoCqlProjectionCommand(source.ToArray(), membership,
            membership || query.Source.GraphOptions != null, traverses, query.Source.Limit == 0);
    }
}
