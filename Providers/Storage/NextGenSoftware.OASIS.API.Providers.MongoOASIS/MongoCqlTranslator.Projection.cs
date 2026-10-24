using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

internal sealed class MongoCqlProjectionCommand
{
    internal BsonDocument[] Pipeline { get; }
    internal bool RequiresMembership { get; }
    internal bool RequiresSnapshot { get; }
    internal bool HasTraversalEnvelope { get; }
    internal bool IsEmpty { get; }
    internal MongoCqlProjectionCommand(BsonDocument[] pipeline, bool membership, bool snapshot, bool traverses, bool empty)
    { Pipeline = pipeline; RequiresMembership = membership; RequiresSnapshot = snapshot; HasTraversalEnvelope = traverses; IsEmpty = empty; }
}

internal sealed partial class MongoCqlTranslator
{
    internal MongoCqlProjectionCommand CompileProjection<T, TResult>(CosmicProjectionQuery<T, TResult> query, int maximumResults) where T : IHolon
    {
        var source = CompileSource(query.Source, out var traverses);
        var fetch = query.Source.Limit.HasValue ? Math.Min(query.Source.Limit.Value, maximumResults + 1) : maximumResults + 1;
        var page = SelectedWindow(query.Source, fetch);
        var cells = new BsonArray();
        for (var index = 0; index < query.Columns.Count; index++)
        {
            cells.Add(ProjectionCell(query.Columns[index].Path, "cqlp" + index));
        }
        page.Add(new("$project", new BsonDocument { { "_id", 0 }, { "cells", cells } }));
        // Reuse the source-owned invalid branch, outside ordering/paging/columns.
        if (traverses) FinishDescendantEnvelope(source, page); else source.AddRange(page);
        ValidatePipeline(source);
        var membership = query.Source.Predicates.Any(predicate => predicate is CosmicQueryAnyChild || predicate is CosmicQueryAnyDescendant);
        return new MongoCqlProjectionCommand(source.ToArray(), membership, membership || query.Source.GraphOptions != null, traverses, fetch == 0);
    }

    private static List<BsonDocument> SelectedWindow<T>(CosmicQuery<T> query, int? limit) where T : IHolon
    {
        var page = new List<BsonDocument> { new("$project", new BsonDocument("Children", 0)) };
        // Empty source windows still permit protected traversal/membership
        // validation, but no selected value may enter a reduction or decoder.
        if (query.Limit == 0) page.Add(new("$match", new BsonDocument("$expr", Literal(false))));
        if (query.Orderings.Count != 0)
        {
            var wrapped = new BsonDocument { { "_id", 0 }, { "row", "$$ROOT" } };
            var sort = new BsonDocument();
            for (var index = 0; index < query.Orderings.Count; index++)
            {
                var ordering = query.Orderings[index]; var key = "s" + index; var direction = ordering.Descending ? -1 : 1;
                wrapped.Add(key, OrderingKey(ordering.Path, "cqlpsort" + index));
                sort.Add(key + ".rank", direction); sort.Add(key + ".value", direction);
            }
            sort.Add("row.HolonId", 1);
            page.Add(new("$project", wrapped)); page.Add(new("$sort", sort));
        }
        else page.Add(new("$sort", new BsonDocument("HolonId", 1)));
        if (query.Offset != 0) page.Add(new("$skip", query.Offset));
        if (limit.HasValue) page.Add(new("$limit", Math.Max(1, limit.Value)));
        if (query.Orderings.Count != 0) page.Add(new("$replaceRoot", new BsonDocument("newRoot", "$row")));
        return page;
    }

    private static BsonValue ProjectionCell(CosmicQueryPath path, string prefix)
    {
        BsonValue Cell(BsonValue raw)
        {
            var type = Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType;
            var value = path.IsMetadata && type == typeof(DateTime) ? MetadataDate(raw, path.Segments) : raw;
            var valid = type == typeof(Guid) ? GuidDomain(value) : Domain(value, path.ValueType, path.IsMetadata);
            if (path.IsMetadata && type == typeof(DateTime))
            {
                var pointer = "/" + string.Join("/", path.Segments.Select(segment => segment.Replace("~", "~0").Replace("/", "~1")));
                var matches = new BsonDocument("$filter", new BsonDocument
                {
                    { "input", Op("$ifNull", "$MetaDataExactUtcTicks", Literal(new BsonArray())) },
                    { "as", "cqlprojectiondates" },
                    { "cond", Op("$eq", "$$cqlprojectiondates.Path", Literal(pointer)) }
                });
                // Precision ownership is singular. Do not choose the first
                // conflicting companion when projecting an exact timestamp.
                valid = Op("$and", valid, Op("$lte", new BsonDocument("$size", matches), Literal(1)));
            }
            var state = Choose(Op("$eq", Type(raw), Literal("missing")), Literal(0),
                Choose(Op("$eq", Type(raw), Literal("null")), Literal(1), Choose(valid, Literal(2), Literal(3))));
            return new BsonDocument { { "s", state }, { "v", Choose(valid, value, Literal(BsonNull.Value)) } };
        }
        return path.IsMetadata ? ProjectionMetadata(path.Segments, Cell, prefix) : Cell(Canonical(path));
    }

    private static BsonValue ProjectionMetadata(IReadOnlyList<string> segments, Func<BsonValue, BsonValue> cell, string prefix)
    {
        if (segments.Any(segment => segment.IndexOf('\0') >= 0)) throw new NotSupportedException("MongoDB metadata paths cannot contain NUL.");
        var missing = new BsonDocument { { "s", Literal(0) }, { "v", Literal(BsonNull.Value) } };
        var invalid = new BsonDocument { { "s", Literal(3) }, { "v", Literal(BsonNull.Value) } };
        BsonValue Walk(BsonValue input, int index)
        {
            if (index == segments.Count) return cell(input);
            var current = prefix + "n" + index;
            var matches = prefix + "m" + index;
            var entry = prefix + "e" + index;
            var first = prefix + "f" + index;
            BsonValue value = "$$" + current;
            var objectEntries = new BsonDocument("$objectToArray", value);
            var entries = Choose(Op("$eq", Type(value), Literal("object")), objectEntries,
                index == 0 ? Choose(Op("$eq", Type(value), Literal("array")), value, Literal(new BsonArray())) : Literal(new BsonArray()));
            var key = Choose(Op("$eq", Type("$$" + entry), Literal("array")),
                Op("$arrayElemAt", "$$" + entry, Literal(0)), "$$" + entry + ".k");
            var filtered = new BsonDocument("$filter", new BsonDocument
            { { "input", entries }, { "as", entry }, { "cond", Op("$eq", key, Literal(segments[index])) } });
            var candidate = "$$" + first;
            var isArray = Op("$eq", Type(candidate), Literal("array"));
            var wellFormed = Choose(isArray, Op("$eq", new BsonDocument("$size", candidate), Literal(2)),
                Op("$ne", Type(candidate + ".v"), Literal("missing")));
            var child = Choose(isArray, Op("$arrayElemAt", candidate, Literal(1)), candidate + ".v");
            var selected = new BsonDocument("$let", new BsonDocument
            {
                { "vars", new BsonDocument(first, Op("$arrayElemAt", "$$" + matches, Literal(0))) },
                { "in", Choose(wellFormed, Walk(child, index + 1), invalid) }
            });
            var body = new BsonDocument("$let", new BsonDocument
            {
                { "vars", new BsonDocument(matches, filtered) },
                { "in", Choose(Op("$eq", new BsonDocument("$size", "$$" + matches), Literal(0)), missing,
                    Choose(Op("$eq", new BsonDocument("$size", "$$" + matches), Literal(1)), selected, invalid)) }
            });
            return new BsonDocument("$let", new BsonDocument { { "vars", new BsonDocument(current, input) }, { "in", body } });
        }
        return Walk("$MetaData", 0);
    }
}
