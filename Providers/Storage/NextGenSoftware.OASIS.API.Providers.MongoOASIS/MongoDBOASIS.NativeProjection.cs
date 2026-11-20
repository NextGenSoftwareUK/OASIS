using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : INativeDataProjectionQueryProvider
{
    public async Task<OASISResult<IReadOnlyList<NativeProjectionRow>>> QueryNativeProjectionRecordsAsync(
        string sourceName, IReadOnlyList<NativeRecordFilter> filters,
        IReadOnlyList<NativeProjectionColumn> columns, int limit, NativeConnection connection = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(sourceName) || sourceName.Length > 128 || sourceName.Any(char.IsControl)
            || sourceName.StartsWith("system.", StringComparison.Ordinal) || limit is < 1 or > 100)
            return NativeProjectionError("NATIVE_PROJECTION_QUERY_INVALID", "A bounded native collection and row limit are required.");
        IReadOnlyList<NativeProjectionColumn> selected;
        BsonDocument match;
        try
        {
            var matches = NativeRecordFilter.Capture(filters);
            selected = NativeProjectionColumn.Capture(columns);
            if (matches.Any(item => !IsNativeScalarField(item.Field))
                || selected.Any(item => item.Field == "_id" || !IsNativeScalarField(item.Field)))
                return NativeProjectionError("NATIVE_PROJECTION_QUERY_INVALID", "Use admitted scalar source fields.");
            match = CompileNativePredicateFilter(matches);
        }
        catch (ArgumentException)
        { return NativeProjectionError("NATIVE_PROJECTION_QUERY_INVALID", "A bounded typed predicate and projection are required."); }

        var unique = new List<NativeProjectionColumn>();
        var cells = new Dictionary<(string, Type, NativeRecordFunction), int>();
        var positions = new int[selected.Count];
        for (var index = 0; index < selected.Count; index++)
        {
            var column = selected[index]; var key = (column.Field, column.InputType, column.Function);
            if (!cells.TryGetValue(key, out var position))
            { position = unique.Count; unique.Add(column); cells.Add(key, position); }
            positions[index] = position;
        }
        var keyBytes = new BsonDocument("$bsonSize", new BsonDocument("v", "$_id"));
        var first = new BsonDocument
        {
            { "_id", ProjectionCondition(ProjectionOperation("$lte", keyBytes, 4096), "$_id", BsonNull.Value) },
            { "_keyBytes", keyBytes }
        };
        for (var index = 0; index < unique.Count; index++)
            first.Add("c" + index, NativeProjectionCellExpression(unique[index]));
        BsonValue outputBytes = positions.Length == 0 ? new BsonDocument("$literal", 0)
            : new BsonDocument("$add", new BsonArray(positions.Select(index => (BsonValue)("$c" + index + ".b"))));
        var second = new BsonDocument { { "_id", 1 }, { "_keyBytes", 1 }, { "_outputBytes", outputBytes } };
        for (var index = 0; index < unique.Count; index++)
            second.Add("c" + index, new BsonDocument("$let", new BsonDocument
            {
                { "vars", new BsonDocument("cell", "$c" + index) },
                { "in", new BsonDocument
                    {
                        { "s", "$$cell.s" }, { "b", "$$cell.b" },
                        { "v", ProjectionCondition(ProjectionOperation("$lte", outputBytes, 1048576), "$$cell.v", BsonNull.Value) }
                    }
                }
            }));
        var pipeline = new[]
        {
            new BsonDocument("$match", match), new BsonDocument("$sort", new BsonDocument("_id", 1)),
            new BsonDocument("$limit", limit), new BsonDocument("$project", first), new BsonDocument("$project", second)
        };
        IDisposable lease = null;
        try
        {
            var resolved = await ResolveNativeDatabaseAsync(connection, cancellationToken).ConfigureAwait(false);
            lease = resolved.Lease;
            using var cursor = await resolved.Database.GetCollection<BsonDocument>(sourceName)
                .AggregateAsync<BsonDocument>(pipeline, new AggregateOptions
                { MaxTime = NativeOperationLimit, BatchSize = 8, Collation = new Collation("simple") }, cancellationToken).ConfigureAwait(false);
            var rows = new List<NativeProjectionRow>(limit);
            long remaining = 1048576;
            while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                foreach (var document in cursor.Current)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (rows.Count >= limit)
                        return NativeProjectionError("NATIVE_PROJECTION_RESULT_INVALID", "The native response exceeds its row limit.");
                    remaining -= document.ToBson().Length;
                    if (remaining < 0 || ProjectionInteger(document["_keyBytes"]) > 4096
                        || ProjectionInteger(document["_outputBytes"]) > 1048576)
                        return NativeProjectionError("NATIVE_PROJECTION_PAYLOAD_LIMIT", "The native output exceeds its byte limit.");
                    if (!document.TryGetValue("_id", out var id))
                        return NativeProjectionError("NATIVE_PROJECTION_RESULT_INVALID", "The source has no original native identity.");
                    var key = id.ToJson(CanonicalJson);
                    if (key.Length > 4096)
                        return NativeProjectionError("NATIVE_PROJECTION_PAYLOAD_LIMIT", "The native identity exceeds its size limit.");
                    var values = new NativeProjectionCell[selected.Count];
                    for (var index = 0; index < selected.Count; index++)
                    {
                        var cell = document["c" + positions[index]].AsBsonDocument;
                        var state = ProjectionInteger(cell["s"]);
                        if (state == 4)
                            return NativeProjectionError("NATIVE_PROJECTION_RESOURCE_LIMIT", "The source text exceeds its computation budget.");
                        if (state is < 0 or > 2 || state != 2 && !selected[index].Nullable)
                            return NativeProjectionError("NATIVE_PROJECTION_DATA_INVALID", "The input differs from its declared domain or nullability.");
                        object value = state == 2 ? ToScalar(cell["v"]) : null;
                        if (state == 2 && selected[index].ValueType == typeof(Guid))
                        {
                            if (value is not string text || !Guid.TryParseExact(text, "N", out var guid))
                                return NativeProjectionError("NATIVE_PROJECTION_DATA_INVALID", "The normalized UUID output is invalid.");
                            value = guid;
                        }
                        if (state == 2 && (value == null || value.GetType() != selected[index].ValueType))
                            return NativeProjectionError("NATIVE_PROJECTION_DATA_INVALID", "The output differs from its exact declared domain.");
                        values[index] = new NativeProjectionCell((NativeProjectionValueState)state, value);
                    }
                    rows.Add(new NativeProjectionRow(sourceName, key, values));
                }
            return NativeProjectionResults.Capture(sourceName, selected, rows, limit, cancellationToken: cancellationToken);
        }
        catch (MongoCommandException cause)
        { return NativeProjectionError("NATIVE_PROJECTION_QUERY_FAILED", "The native MongoDB projection command failed (code " + cause.Code + ")."); }
        catch (NativeConnectionRefusedException cause)
        { return NativeProjectionError("NATIVE_IMPORT_CONNECTION_REFUSED", Redact(connection, cause.Message)); }
        catch (Exception cause) when (cause is MongoException or InvalidOperationException or FormatException or OverflowException or ArgumentException)
        { return NativeProjectionError("NATIVE_PROJECTION_QUERY_FAILED", "The native MongoDB projection failed."); }
        finally { lease?.Dispose(); }
    }

    private static BsonValue NativeProjectionCellExpression(NativeProjectionColumn column)
    {
        var expected = column.InputType == typeof(string) ? "string" : column.InputType == typeof(bool) ? "bool"
            : column.InputType == typeof(int) ? "int" : column.InputType == typeof(double) ? "double" : "long";
        BsonValue domain = column.InputType == typeof(Guid)
            ? MongoCqlTranslator.GuidDomain("$$input", allowCSharpLegacy: false) : ProjectionOperation("$eq", "$$kind", expected);
        if (column.InputType == typeof(double))
            domain = ProjectionOperation("$and", domain,
                ProjectionOperation("$gte", "$$input", -double.MaxValue),
                ProjectionOperation("$lte", "$$input", double.MaxValue));
        var textBound = ProjectionCondition(domain,
            ProjectionOperation("$lte", new BsonDocument("$strLenBytes", "$$input"), 1048576), false);
        var admissible = column.InputType == typeof(string) ? textBound : domain;
        var state = new BsonDocument("$switch", new BsonDocument
        {
            { "branches", new BsonArray
                {
                    new BsonDocument { { "case", ProjectionOperation("$eq", "$$kind", "missing") }, { "then", 0 } },
                    new BsonDocument { { "case", ProjectionOperation("$eq", "$$kind", "null") }, { "then", 1 } },
                    new BsonDocument { { "case", domain }, { "then", column.Function == NativeRecordFunction.Utf16Length
                        ? ProjectionCondition(admissible, 2, 4) : (BsonValue)2 } }
                }
            },
            { "default", 3 }
        });
        var bytes = column.ValueType == typeof(string)
            ? ProjectionCondition(domain, new BsonDocument("$strLenBytes", "$$input"), 8)
            : column.ValueType == typeof(Guid) ? (BsonValue)32 : (BsonValue)8;
        var value = column.Function == NativeRecordFunction.Utf16Length
            ? (BsonValue)new BsonDocument("$toInt", MongoCqlTranslator.Utf16LengthUnits("$$input"))
            : column.InputType == typeof(Guid) ? MongoCqlTranslator.ComparableGuid("$$input") : (BsonValue)"$$input";
        return new BsonDocument("$let", new BsonDocument
        {
            { "vars", new BsonDocument { { "input", "$" + column.Field }, { "kind", new BsonDocument("$type", "$" + column.Field) } } },
            { "in", new BsonDocument { { "s", state }, { "b", bytes }, { "v", ProjectionCondition(admissible, value, BsonNull.Value) } } }
        });
    }

    private static BsonDocument ProjectionOperation(string name, params BsonValue[] values)
        => new BsonDocument(name, new BsonArray(values));
    private static BsonDocument ProjectionCondition(BsonValue condition, BsonValue yes, BsonValue no)
        => new BsonDocument("$cond", new BsonArray { condition, yes, no });
    private static long ProjectionInteger(BsonValue value) => value.BsonType switch
    {
        BsonType.Int32 => value.AsInt32, BsonType.Int64 => value.AsInt64,
        _ => throw new FormatException("A native count/state must retain its integer domain.")
    };
    private static OASISResult<IReadOnlyList<NativeProjectionRow>> NativeProjectionError(string code, string message)
        => new OASISResult<IReadOnlyList<NativeProjectionRow>> { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
    private static BsonDocument CompileNativePredicateFilter(IReadOnlyList<NativeRecordFilter> matches)
    {
        var filters = new List<BsonDocument>(matches.Count);
        foreach (var match in matches)
        {
            if (match.Function is NativeRecordFunction.Contains or NativeRecordFunction.StartsWith or NativeRecordFunction.EndsWith)
            {
                var operation = match.Function switch
                {
                    NativeRecordFunction.Contains => CosmicQueryStringOperator.Contains,
                    NativeRecordFunction.StartsWith => CosmicQueryStringOperator.StartsWith,
                    _ => CosmicQueryStringOperator.EndsWith
                };
                var expression = MongoCqlTranslator.OrdinalStringMatch(
                    new BsonString("$" + match.Field), operation, (string)match.Value);
                if (match.Operator == CosmicQueryComparisonOperator.NotEqual)
                    expression = new BsonDocument("$not", new BsonArray { expression });
                filters.Add(new BsonDocument("$expr", expression));
                continue;
            }
            if (match.Function == NativeRecordFunction.Utf16Length)
            {
                filters.Add(new BsonDocument("$expr", MongoCqlTranslator.Utf16LengthComparison(
                    new BsonString("$" + match.Field), match.Operator, (long)match.Value)));
                continue;
            }
            var symbol = match.Operator switch
            {
                CosmicQueryComparisonOperator.Equal => "$eq",
                CosmicQueryComparisonOperator.NotEqual => "$ne",
                CosmicQueryComparisonOperator.GreaterThan => "$gt",
                CosmicQueryComparisonOperator.GreaterThanOrEqual => "$gte",
                CosmicQueryComparisonOperator.LessThan => "$lt",
                CosmicQueryComparisonOperator.LessThanOrEqual => "$lte",
                _ => throw new ArgumentException("Unknown native comparison.")
            };
            (BsonValue literal, string type) = match.Value switch
            {
                string text when text.Length <= 4096 => ((BsonValue)new BsonString(text), "string"),
                bool flag => (new BsonBoolean(flag), "bool"),
                int number => (new BsonInt32(number), "int"),
                long number => (new BsonInt64(number), "long"),
                double number when double.IsFinite(number) => (new BsonDouble(number), "double"),
                _ => (null, null)
            };
            if (literal == null) throw new ArgumentException("Unsupported exact native predicate scalar.");

            // Native comparisons can use a source index. The exact type guard excludes arrays and numeric coercion.
            var atomic = new BsonDocument
            {
                { match.Field, new BsonDocument(symbol, literal) },
                { "$expr", new BsonDocument("$eq", new BsonArray
                    { new BsonDocument("$type", "$" + match.Field), type }) }
            };
            if (match.Value is double)
                atomic["$expr"] = ProjectionOperation("$and", atomic["$expr"],
                    ProjectionOperation("$gte", "$" + match.Field, -double.MaxValue),
                    ProjectionOperation("$lte", "$" + match.Field, double.MaxValue));
            if (match.Operator == CosmicQueryComparisonOperator.NotEqual && match.Value is string)
                atomic = new BsonDocument("$or", new BsonArray
                {
                    atomic, new BsonDocument("$expr", new BsonDocument("$eq", new BsonArray
                        { new BsonDocument("$type", "$" + match.Field), "null" }))
                });
            filters.Add(atomic);
        }
        return filters.Count == 1 ? filters[0] : new BsonDocument("$and", new BsonArray(filters));

    }

}
