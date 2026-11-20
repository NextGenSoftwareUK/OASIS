using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public partial class SQLLiteDBOASIS : INativeDataProjectionQueryProvider
    {
        public async Task<OASISResult<IReadOnlyList<NativeProjectionRow>>> QueryNativeProjectionRecordsAsync(
            string sourceName, IReadOnlyList<NativeRecordFilter> filters,
            IReadOnlyList<NativeProjectionColumn> columns, int limit, NativeConnection connection = null,
            CancellationToken cancellationToken = default)
        {
            if (connection != null || string.IsNullOrWhiteSpace(sourceName) || sourceName.Length > 128
                || sourceName.Any(char.IsControl) || limit is < 1 or > 100)
                return ProjectionError("NATIVE_PROJECTION_QUERY_INVALID", "Use a configured source and a limit of one to100.");
            IReadOnlyList<NativeRecordFilter> matches;
            IReadOnlyList<NativeProjectionColumn> selected;
            try
            {
                matches = NativeRecordFilter.Capture(filters);
                selected = NativeProjectionColumn.Capture(columns);
            }
            catch (ArgumentException)
            { return ProjectionError("NATIVE_PROJECTION_QUERY_INVALID", "A bounded predicate and typed projection are required."); }
            try
            {
                using var lease = await OpenNativeAsync(null, cancellationToken);
                var schema = await NativeTableColumnsAsync(lease.Connection, sourceName, cancellationToken);
                var fields = schema.Select(column => column.Name).ToHashSet(StringComparer.Ordinal);
                if (matches.Any(match => !fields.Contains(match.Field)) || selected.Any(column => !fields.Contains(column.Field)))
                    return ProjectionError("NATIVE_PROJECTION_QUERY_INVALID", "Every input must name an existing native column.");
                var keys = schema.Where(column => column.Position > 0).OrderBy(column => column.Position)
                    .Select(column => column.Name).ToArray();
                using var budget = new SqliteCqlReadBudget(lease.Connection, cancellationToken,
                    deadline: TimeSpan.FromSeconds(Math.Min(30, NativeImportOptions.ReadTimeout.TotalSeconds)));
                using var command = lease.Connection.CreateCommand();
                command.CommandTimeout = Math.Max(1, (int)Math.Ceiling(NativeImportOptions.ReadTimeout.TotalSeconds));
                var predicates = CompileNativePredicateSql(matches, command);
                var outputs = new List<string> { "rowid" };
                outputs.AddRange(keys.Select(Quote));
                foreach (var column in selected)
                {
                    var input = Quote(column.Field);
                    var domain = column.InputType == typeof(string) ? $"typeof({input}) = 'text'"
                        : column.InputType == typeof(Guid) ? SqliteCqlGuid.TextDomain(input)
                        : column.InputType == typeof(bool) ? $"typeof({input}) = 'integer' AND {input} IN (0,1)"
                        : column.InputType == typeof(int) ? $"typeof({input}) = 'integer' AND {input} BETWEEN -2147483648 AND 2147483647"
                        : column.InputType == typeof(double) ? $"typeof({input}) = 'real' AND {input} BETWEEN $nativeDoubleMin AND $nativeDoubleMax"
                        : $"typeof({input}) = 'integer'";
                    outputs.Add($"CASE WHEN {input} IS NULL THEN 1 WHEN {domain} THEN 2 ELSE 3 END");
                    // Read size before retrieving text into managed memory. Computed lengths transfer only integers.
                    outputs.Add(column.ValueType == typeof(string)
                        ? $"CASE WHEN {input} IS NULL THEN 8 WHEN {domain} THEN length(CAST({input} AS BLOB)) ELSE 0 END"
                        : column.ValueType == typeof(Guid) ? $"CASE WHEN {input} IS NULL THEN 8 ELSE 36 END" : "8");
                    var value = column.Function == NativeRecordFunction.Utf16Length
                        ? $"{SqliteCqlReadBudget.TextStringLengthFunction}({input})" : input;
                    outputs.Add($"CASE WHEN {domain} THEN {value} ELSE NULL END");
                }
                command.CommandText = $"SELECT {string.Join(", ", outputs)} FROM {Quote(sourceName)} WHERE {predicates} ORDER BY rowid LIMIT $take";
                command.Parameters.AddWithValue("$take", limit);
                command.Parameters.AddWithValue("$nativeDoubleMin", -double.MaxValue);
                command.Parameters.AddWithValue("$nativeDoubleMax", double.MaxValue);
                try
                {
                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    var rows = new List<NativeProjectionRow>(limit);
                    long remaining = 1048576;
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        budget.Check();
                        var key = keys.Length == 0 ? "rowid:" + reader.GetInt64(0).ToString(System.Globalization.CultureInfo.InvariantCulture)
                            : System.Text.Json.JsonSerializer.Serialize(Enumerable.Range(0, keys.Length).Select(index => ToScalar(reader.GetValue(index + 1))));
                        if (key.Length > 4096)
                            return ProjectionError("NATIVE_PROJECTION_PAYLOAD_LIMIT", "The native key exceeds its identity size bound.");
                        var cells = new NativeProjectionCell[selected.Count];
                        for (var index = 0; index < selected.Count; index++)
                        {
                            var offset = 1 + keys.Length + index * 3;
                            var state = reader.GetInt64(offset);
                            if (state == 3 || state != 1 && state != 2 || state == 1 && !selected[index].Nullable)
                                return ProjectionError("NATIVE_PROJECTION_DATA_INVALID", "A native input does not match its declared domain or nullability.");
                            var bytes = reader.GetInt64(offset + 1);
                            if (bytes < 0 || bytes > remaining)
                                return ProjectionError("NATIVE_PROJECTION_PAYLOAD_LIMIT", "The native output exceeds its byte limit.");
                            remaining -= bytes;
                            if (state == 1) { cells[index] = new NativeProjectionCell(NativeProjectionValueState.Null); continue; }
                            var valueType = selected[index].ValueType;
                            object value;
                            if (valueType == typeof(Guid))
                            {
                                if (!Guid.TryParseExact(reader.GetString(offset + 2), "D", out var guid))
                                    return ProjectionError("NATIVE_PROJECTION_DATA_INVALID", "The declared UUID output is invalid.");
                                value = guid;
                            }
                            else value = valueType == typeof(string) ? reader.GetString(offset + 2)
                                : valueType == typeof(bool) ? reader.GetInt64(offset + 2) == 1
                                : valueType == typeof(int) ? checked((int)reader.GetInt64(offset + 2))
                                : valueType == typeof(double) ? (object)reader.GetDouble(offset + 2)
                                : (object)reader.GetInt64(offset + 2);
                            cells[index] = new NativeProjectionCell(NativeProjectionValueState.Present, value);
                        }
                        rows.Add(new NativeProjectionRow(sourceName, key, cells));
                    }
                    budget.Check();
                    return NativeProjectionResults.Capture(sourceName, selected, rows, limit, cancellationToken: cancellationToken);
                }
                catch (SqliteException)
                {
                    budget.Check();
                    throw;
                }
            }
            catch (SqliteCqlDeadlineException)
            { return ProjectionError("NATIVE_PROJECTION_TIMEOUT", "The native projection exceeded its deadline."); }
            catch (SqliteCqlResourceException)
            { return ProjectionError("NATIVE_PROJECTION_RESOURCE_LIMIT", "The native projection exceeded its instruction budget."); }
            catch (OverflowException)
            { return ProjectionError("NATIVE_PROJECTION_DATA_INVALID", "The native output exceeds its exact declared integer domain."); }
            catch (Exception cause) when (cause is SqliteException or InvalidOperationException or ArgumentException or IOException)
            { return ProjectionError("NATIVE_PROJECTION_QUERY_FAILED", "The configured native projection failed."); }
        }

        private static OASISResult<IReadOnlyList<NativeProjectionRow>> ProjectionError(string code, string message)
            => new OASISResult<IReadOnlyList<NativeProjectionRow>> { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };

        private static string CompileNativePredicateSql(IReadOnlyList<NativeRecordFilter> matches, SqliteCommand command)
        {
                var predicates = new List<string>(matches.Count);
                for (var index = 0; index < matches.Count; index++)
                {
                    var match = matches[index];
                    var column = Quote(match.Field);
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
                        predicates.Add($"CASE WHEN typeof({column}) = 'text' THEN {SqliteCqlReadBudget.TextStringLengthFunction}({column}) {symbol} $match{index} ELSE 0 END");
                    else if (match.Function is NativeRecordFunction.Contains or NativeRecordFunction.StartsWith or NativeRecordFunction.EndsWith)
                    {
                        var expression = $"CASE WHEN typeof({column}) = 'text' THEN {SqliteCqlReadBudget.TextStringMatchFunction}({column}, $match{index}, $function{index}) ELSE 0 END";
                        predicates.Add(match.Operator == CosmicQueryComparisonOperator.NotEqual ? $"NOT ({expression})" : expression);
                        command.Parameters.AddWithValue($"$function{index}", (long)match.Function);
                    }
                    else if (match.Value is Guid guid)
                    {
                        predicates.Add($"({SqliteCqlGuid.TextDomain(column)} AND lower({column}) COLLATE BINARY {symbol} $match{index})");
                        command.Parameters.AddWithValue($"$match{index}", guid.ToString("D"));
                        continue;
                    }
                    else
                    {
                        var atomic = $"typeof({column}) = $type{index} AND {column} COLLATE BINARY {symbol} $match{index}";
                        if (match.Value is int)
                            atomic += $" AND {column} BETWEEN -2147483648 AND 2147483647";
                        if (match.Value is double)
                        {
                            atomic += $" AND {column} BETWEEN $finiteMin{index} AND $finiteMax{index}";
                            command.Parameters.AddWithValue($"$finiteMin{index}", -double.MaxValue);
                            command.Parameters.AddWithValue($"$finiteMax{index}", double.MaxValue);
                        }
                        if (match.Operator == CosmicQueryComparisonOperator.NotEqual && match.Value is string)
                            atomic = $"({atomic}) OR {column} IS NULL";
                        predicates.Add("(" + atomic + ")");
                        command.Parameters.AddWithValue($"$type{index}", match.Value is string ? "text" : match.Value is double ? "real" : "integer");
                    }
                    command.Parameters.AddWithValue($"$match{index}", match.Value);
                }
                return string.Join(" AND ", predicates);
        }
    }
}
