using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS
{
    public partial class Neo4jOASIS : INativeDataProjectionQueryProvider
    {
        public async Task<OASISResult<IReadOnlyList<NativeProjectionRow>>> QueryNativeProjectionRecordsAsync(
            string sourceName, IReadOnlyList<NativeRecordFilter> filters,
            IReadOnlyList<NativeProjectionColumn> columns, int limit, NativeConnection connection = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (sourceName == null || !LabelPattern.IsMatch(sourceName) || limit is < 1 or > 100)
                return NativeProjectionError("NATIVE_PROJECTION_QUERY_INVALID", "An admitted label and bounded row limit are required.");
            IReadOnlyList<NativeProjectionColumn> selected;
            var parameters = new Dictionary<string, object> { ["take"] = limit, ["projectionUtf8Surrogate"] = @"[\uD800-\uDFFF]", ["projectionUtf8One"] = @"[\x{0}-\x{7F}]", ["projectionUtf8Two"] = @"[\x{80}-\x{7FF}]", ["projectionSupplementary"] = @"[\x{10000}-\x{10FFFF}]" };
            parameters["nativeFiniteDoubleMax"] = double.MaxValue;
            string where;
            try
            {
                var matches = NativeRecordFilter.CaptureForProjection(filters);
                selected = NativeProjectionColumn.Capture(columns);
                if (matches.Any(match => !LabelPattern.IsMatch(match.Field))
                    || selected.Any(column => !LabelPattern.IsMatch(column.Field)))
                    return NativeProjectionError("NATIVE_PROJECTION_QUERY_INVALID", "Select admitted native property names.");
                where = CompileNativePredicateWhere(matches, parameters);
            }
            catch (ArgumentException)
            { return NativeProjectionError("NATIVE_PROJECTION_QUERY_INVALID", "A bounded typed predicate and projection are required."); }
            if (selected.Any(column => column.InputType == typeof(Guid)))
                parameters.Add("nativeGuidPattern", NeoCqlProjection.GuidTextPattern);
            var unique = new List<NativeProjectionColumn>();
            var indexes = new Dictionary<(string, Type, NativeRecordFunction, string, string, string, int?), int>();
            var positions = new int[selected.Count];
            for (var index = 0; index < selected.Count; index++)
            {
                var column = selected[index]; var key = column.ValueKey;
                if (!indexes.TryGetValue(key, out var position))
                { position = unique.Count; unique.Add(column); indexes.Add(key, position); }
                positions[index] = position;
            }
            var fields = unique.Select(column => column.Field).Distinct(StringComparer.Ordinal).ToArray();
            var fieldIndexes = fields.Select((field, index) => (field, index))
                .ToDictionary(item => item.field, item => item.index, StringComparer.Ordinal);
            // Detach each source property once before computing different output aliases.
            var captured = new List<string> { "elementId(n) AS key" };
            var inputBytes = new List<string> { "key" };
            for (var index = 0; index < fields.Length; index++)
            {
                captured.Add($"n.`{fields[index]}` AS raw{index}");
                inputBytes.Add("raw" + index);
                inputBytes.Add($"CASE WHEN raw{index} IS NOT NULL AND raw{index} IS :: STRING THEN {NativeProjectionUtf8Bytes("raw" + index)} ELSE 8 END AS ib{index}");
            }
            var cells = new List<string> { "key" };
            for (var index = 0; index < unique.Count; index++)
            {
                var column = unique[index]; var fieldIndex = fieldIndexes[column.Field];
                var property = "raw" + fieldIndex;
                if (column.InputType == typeof(DateTime))
                {
                    cells.Add(NeoCqlDate.NativeCell(property, value => BindNativeUtcParameter(parameters, value)) + $" AS c{index}");
                    continue;
                }
                var type = column.InputType == typeof(string) || column.InputType == typeof(Guid) ? "STRING" : column.InputType == typeof(bool) ? "BOOLEAN" : column.InputType == typeof(double) ? "FLOAT" : "INTEGER";
                var domain = $"{property} IS NOT NULL AND {property} IS :: {type}";
                if (column.InputType == typeof(Guid))
                    domain += $" AND CASE WHEN {property} IS :: STRING THEN {property} =~ $nativeGuidPattern ELSE false END";
                if (column.InputType == typeof(int)) domain += $" AND {property} >= -2147483648 AND {property} <= 2147483647";
                if (column.InputType == typeof(double))
                    domain += $" AND {property} >= -$nativeFiniteDoubleMax AND {property} <= $nativeFiniteDoubleMax";
                // Neo4j null properties are absent. Preserve that native missing state.
                var state = $"CASE WHEN {property} IS NULL THEN 0 WHEN {domain} THEN "
                    + (column.Function == NativeRecordFunction.Utf16Length ? $"CASE WHEN ib{fieldIndex} <= 1048576 THEN 2 ELSE 4 END" : "2") + " ELSE 3 END";
                var bytes = column.ValueType == typeof(string) ? "ib" + fieldIndex
                    : column.ValueType == typeof(Guid) ? "36" : "8";
                var value = column.Function == NativeRecordFunction.Utf16Length
                    ? NeoCqlTranslator.Utf16LengthUnits(property, "$projectionSupplementary")
                    : column.InputType == typeof(Guid) ? "toLower(" + property + ")" : property;
                var output=$"CASE WHEN {domain} AND ib{fieldIndex} <= 1048576 THEN {value} ELSE null END";
                string LiteralParameter(object argument)
                {
                    var key="nativeProjectionArg"+parameters.Count;
                    parameters.Add(key,argument);return "$"+key;
                }
                if(column.Function==NativeRecordFunction.BooleanTextCase)
                {
                    var yes=LiteralParameter(column.WhenTrue);var no=LiteralParameter(column.WhenFalse);
                    var yesBytes=System.Text.Encoding.UTF8.GetByteCount(column.WhenTrue);
                    var noBytes=System.Text.Encoding.UTF8.GetByteCount(column.WhenFalse);
                    output=$"CASE WHEN {domain} THEN CASE WHEN {property}=true THEN {yes} ELSE {no} END ELSE null END";
                    bytes=$"CASE WHEN {domain} THEN CASE WHEN {property}=true THEN {yesBytes} ELSE {noBytes} END ELSE 8 END";
                }
                else if(column.Function is NativeRecordFunction.TextCoalesce or NativeRecordFunction.Int32Coalesce)
                {
                    var replacement=column.Function==NativeRecordFunction.TextCoalesce
                        ? LiteralParameter(column.NullReplacement):LiteralParameter(column.Int32Replacement.Value);
                    var replacementBytes=column.Function==NativeRecordFunction.TextCoalesce
                        ? System.Text.Encoding.UTF8.GetByteCount(column.NullReplacement):8;
                    state=$"CASE WHEN {property} IS NULL THEN 2 ELSE ({state}) END";
                    bytes=$"CASE WHEN {property} IS NULL THEN {replacementBytes} ELSE ({bytes}) END";
                    output=$"CASE WHEN {property} IS NULL THEN {replacement} ELSE ({output}) END";
                }
                cells.Add($"{{s:{state}, b:{bytes}, v:{output}}} AS c{index}");
            }
            var total = positions.Length == 0 ? "0" : string.Join(" + ", positions.Select(index => $"c{index}.b"));
            var returned = new List<string> { "key", total + " AS outputBytes" };
            for (var index = 0; index < unique.Count; index++)
            {
                returned.Add($"c{index}.s AS s{index}");
                returned.Add($"CASE WHEN {total} <= 1048576 THEN c{index}.v ELSE null END AS v{index}");
            }
            var query = $"MATCH (n:`{sourceName}`) WHERE {where} WITH n ORDER BY elementId(n) LIMIT $take "
                + "WITH " + string.Join(", ", captured) + " WITH " + string.Join(", ", inputBytes)
                + " WITH " + string.Join(", ", cells) + " RETURN " + string.Join(", ", returned) + " ORDER BY key";
            try
            {
                await using var lease = await OpenNativeAsync(connection, cancellationToken, maximumFetchSize: 8);
                if (!(await LabelsAsync(lease, connection, cancellationToken)).Contains(sourceName, StringComparer.Ordinal))
                    return NativeProjectionError("NATIVE_PROJECTION_QUERY_INVALID", "The connection does not admit that source label.");
                using var transaction = await lease.Session.BeginTransactionAsync(options => options.WithTimeout(NativeImportOptions.ReadTimeout));
                var cursor = await transaction.RunAsync(query, parameters);
                var rows = new List<NativeProjectionRow>(limit);
                long remaining = 1048576;
                while (await cursor.FetchAsync())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (rows.Count >= limit)
                        return NativeProjectionError("NATIVE_PROJECTION_RESULT_INVALID", "The source exceeds its row limit.");
                    var key = cursor.Current["key"].As<string>();
                    var bytes = cursor.Current["outputBytes"].As<long>();
                    if (key.Length > 4096 || bytes < 0 || bytes > remaining)
                        return NativeProjectionError("NATIVE_PROJECTION_PAYLOAD_LIMIT", "The native result exceeds its byte limit.");
                    remaining -= bytes + Encoding.UTF8.GetByteCount(key) + Encoding.UTF8.GetByteCount(sourceName);
                    if (remaining < 0)
                        return NativeProjectionError("NATIVE_PROJECTION_PAYLOAD_LIMIT", "The native identity/output exceeds its byte limit.");
                    var values = new NativeProjectionCell[selected.Count];
                    for (var index = 0; index < selected.Count; index++)
                    {
                        var state = cursor.Current["s" + positions[index]].As<long>();
                        if (state == 4) return NativeProjectionError("NATIVE_PROJECTION_RESOURCE_LIMIT", "Source text exceeds its computation budget.");
                        if (state is < 0 or > 2 || state != 2 && !selected[index].Nullable)
                            return NativeProjectionError("NATIVE_PROJECTION_DATA_INVALID", "A native input differs from its declared domain/nullability.");
                        object value = null;
                        if (state == 2)
                        {
                            var native = cursor.Current["v" + positions[index]];
                            if (selected[index].ValueType == typeof(DateTime) && native is long ticks
                                && ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks)
                                value = new DateTime(ticks, DateTimeKind.Utc);
                            else if (selected[index].ValueType == typeof(int) && native is long number && number >= int.MinValue && number <= int.MaxValue)
                                value = (int)number;
                            else if (selected[index].ValueType == typeof(Guid) && native is string text
                                && Guid.TryParseExact(text, "D", out var guid)) value = guid;
                            else if (native != null && native.GetType() == selected[index].ValueType) value = native;
                            else return NativeProjectionError("NATIVE_PROJECTION_DATA_INVALID", "A native output differs from its exact declared domain.");
                        }
                        values[index] = new NativeProjectionCell((NativeProjectionValueState)state, value);
                    }
                    rows.Add(new NativeProjectionRow(sourceName, key, values));
                }
                await transaction.CommitAsync();
                return NativeProjectionResults.Capture(sourceName, selected, rows, limit, cancellationToken: cancellationToken);
            }
            catch (NativeConnectionRefusedException)
            { return NativeProjectionError("NATIVE_IMPORT_CONNECTION_REFUSED", "The native Neo4j connection cannot be used."); }
            catch (Neo4jException cause)
            { return NativeProjectionError("NATIVE_PROJECTION_QUERY_FAILED", "The native Neo4j projection failed (" + cause.Code + ")."); }
            catch (Exception cause) when (cause is InvalidOperationException or ArgumentException or OverflowException)
            { return NativeProjectionError("NATIVE_PROJECTION_QUERY_FAILED", "The native Neo4j projection did not complete."); }
        }

        // Neo4j 5.26 split-on-empty yields UTF16 code units (StringValue.splitOnEmptySeparator).
        // A valid supplementary pair contributes two units at two UTF8 bytes each.
        private static string NativeProjectionUtf8Bytes(string value) =>
            "CASE WHEN size(" + value + ") > 1048576 THEN 1048577 ELSE reduce(nativeBytes = 0, nativeCharacter IN split("
            + value + ", '') | nativeBytes + CASE WHEN nativeCharacter = '' THEN 0 WHEN nativeCharacter =~ $projectionUtf8One THEN 1"
            + " WHEN nativeCharacter =~ $projectionUtf8Two THEN 2 WHEN nativeCharacter =~ $projectionSupplementary THEN 4 WHEN nativeCharacter =~ $projectionUtf8Surrogate THEN 2 ELSE 3 END) END";

        private static OASISResult<IReadOnlyList<NativeProjectionRow>> NativeProjectionError(string code, string message)
            => new OASISResult<IReadOnlyList<NativeProjectionRow>> { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };
        private static string BindNativeUtcParameter(IDictionary<string, object> parameters, object value)
        {
            var name = "nativeUtcValue" + parameters.Count;
            parameters.Add(name, value);
            return "$" + name;
        }

        private static string CompileNativePredicateWhere(IReadOnlyList<NativeRecordFilter> matches, IDictionary<string, object> parameters)
        {
            var predicates = new List<string>(matches.Count);
            for (var index = 0; index < matches.Count; index++)
            {
                var match = matches[index];
                var type = match.Value switch
                {
                    string text when text.Length <= 4096 => "STRING",
                    bool => "BOOLEAN",
                    Guid => "STRING",
                    DateTime => "ZONED DATETIME",
                    int or long => "INTEGER",
                    double number when double.IsFinite(number) => "FLOAT",
                    _ => null
                };
                if (type == null) throw new ArgumentException("Unsupported native predicate scalar.");
                var property = $"n.`{match.Field}`";
                var parameter = $"match{index}";
                var symbol = match.Operator switch
                {
                    CosmicQueryComparisonOperator.Equal => "=",
                    CosmicQueryComparisonOperator.NotEqual => "<>",
                    CosmicQueryComparisonOperator.GreaterThan => ">",
                    CosmicQueryComparisonOperator.GreaterThanOrEqual => ">=",
                    CosmicQueryComparisonOperator.LessThan => "<",
                    CosmicQueryComparisonOperator.LessThanOrEqual => "<=",
                    _ => throw new ArgumentException("Unknown native comparison.")
                };
                if (match.Function == NativeRecordFunction.Utf16Length)
                {
                    var pattern = $"utf16Pattern{index}";
                    parameters.Add(pattern, @"[\x{10000}-\x{10FFFF}]");
                    var units = NeoCqlTranslator.Utf16LengthUnits(property, "$" + pattern);
                    predicates.Add($"CASE WHEN {property} IS :: STRING THEN {units} {symbol} ${parameter} ELSE false END");
                }
                else if (match.Function is NativeRecordFunction.Contains or NativeRecordFunction.StartsWith or NativeRecordFunction.EndsWith)
                {
                    var method = match.Function switch
                    {
                        NativeRecordFunction.Contains => "CONTAINS",
                        NativeRecordFunction.StartsWith => "STARTS WITH",
                        _ => "ENDS WITH"
                    };
                    var expression = $"CASE WHEN {property} IS NOT NULL AND {property} IS :: STRING THEN {property} {method} ${parameter} ELSE false END";
                    predicates.Add(match.Operator == CosmicQueryComparisonOperator.NotEqual ? $"NOT ({expression})" : expression);
                }
                else if (match.Value is DateTime stamp)
                {
                    parameters.Add(parameter, stamp.Ticks);
                    predicates.Add(NeoCqlDate.NativeComparison(property, symbol, "$" + parameter,
                        value => BindNativeUtcParameter(parameters, value)));
                    continue;
                }
                else if (match.Value is Guid guid)
                {
                    var pattern = $"nativeGuidFilterPattern{index}";
                    parameters.Add(pattern, NeoCqlProjection.GuidTextPattern);
                    parameters.Add(parameter, guid.ToString("D"));
                    predicates.Add($"CASE WHEN {property} IS :: STRING THEN {property} =~ ${pattern} AND toLower({property}) {symbol} ${parameter} ELSE false END");
                    continue;
                }
                else
                {
                    var domain = $"{property} IS NOT NULL AND {property} IS :: {type}";
                    if (match.Value is int)
                        domain += $" AND {property} >= -2147483648 AND {property} <= 2147483647";
                    if (match.Value is double)
                    {
                        parameters["nativeFiniteDoubleMax"] = double.MaxValue;
                        domain += $" AND {property} >= -$nativeFiniteDoubleMax AND {property} <= $nativeFiniteDoubleMax";
                    }
                    predicates.Add($"{domain} AND {property} {symbol} ${parameter}");
                }
                parameters.Add(parameter, match.Value);
            }

            return predicates.Count == 0 ? "true" : string.Join(" AND ", predicates);
        }

    }
}
