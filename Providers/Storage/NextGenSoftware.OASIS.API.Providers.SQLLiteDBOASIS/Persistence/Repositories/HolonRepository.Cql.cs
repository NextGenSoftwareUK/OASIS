using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults, CancellationToken cancellationToken) where T : IHolon
    {
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != ProviderType.SQLLiteDBOASIS)
                return CqlError("CQL_PLAN_INVALID", "SQLite execution requires an exclusively SQLite plan.");
            if (maximumResults < 1 || maximumResults > 10000)
                return CqlError("CQL_QUERY_LIMIT", "SQLite CQL result limit must be between one and 10,000.");
            // Translate completely before EF can open the connection. Never evaluate a predicate on fetched rows.
            var translator = new CqlSqlTranslator();
            var predicate = plan.Query.Predicates.Count == 0 ? "1" : string.Join(" AND ", plan.Query.Predicates.Select(translator.Translate));
            translator.Parameters.Add(new SqliteParameter("$limit", maximumResults + 1));
            cancellationToken.ThrowIfCancellationRequested();
            var rows = await _dbContext.Holons.FromSqlRaw("SELECT * FROM Holon WHERE " + predicate + " ORDER BY Id COLLATE BINARY LIMIT $limit",
                translator.Parameters.Cast<object>().ToArray()).AsNoTracking().ToListAsync(cancellationToken);
            if (rows.Count > maximumResults)
                return CqlError("CQL_QUERY_LIMIT", "Query exceeds the declared result limit; no partial result was returned.");
            IReadOnlyList<IHolon> result = rows.Select(GetHolonFromEntity).ToList().AsReadOnly();
            return new OASISResult<IReadOnlyList<IHolon>>(result) { IsLoaded = true };
        }
        catch (CqlTranslationLimitException cause) { return CqlError("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (NotSupportedException cause) { return CqlError("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return CqlError("CQL_QUERY_CANCELLED", "SQLite CQL execution was cancelled.", cause); }
        catch (Exception cause) { return CqlError("CQL_QUERY_FAILED", "SQLite CQL execution failed: " + cause.Message, cause); }
    }

    private static OASISResult<IReadOnlyList<IHolon>> CqlError(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

    private sealed class CqlTranslationLimitException : Exception
    { internal CqlTranslationLimitException() : base("SQLite CQL exceeds its 900-bound-parameter translation budget.") { } }

    private sealed class CqlSqlTranslator
    {
        internal readonly List<SqliteParameter> Parameters = new();
        private string Bind(object value)
        {
            if (Parameters.Count >= 900) throw new CqlTranslationLimitException();
            var name = "$cql" + Parameters.Count; Parameters.Add(new SqliteParameter(name, value)); return name;
        }
        private string MetadataPredicate(IReadOnlyList<string> segments, Func<string, string> condition, string prefix = "m")
        {
            var sources = new List<string>(); var conditions = new List<string>();
            for (var i = 0; i < segments.Count; i++)
            {
                var alias = prefix + i;
                var document = i == 0 ? CanonicalColumn("MetaDataJson") : "CASE WHEN " + prefix + (i - 1) + ".type = 'object' THEN " + prefix + (i - 1) + ".value ELSE json_object() END";
                sources.Add("json_each(" + document + ") AS " + alias);
                conditions.Add(alias + ".key COLLATE BINARY = " + Bind(segments[i]));
            }
            conditions.Add(condition(prefix + (segments.Count - 1)));
            return "EXISTS (SELECT 1 FROM " + string.Join(", ", sources) + " WHERE " + string.Join(" AND ", conditions) + ")";
        }
        // json_each exposes its own `id` column. Every canonical operand must
        // remain bound to the outer record, including inside a correlated path.
        private static string CanonicalColumn(string name) => "\"Holon\".\"" + name + "\"";
        private string FieldOperand(CosmicQueryPath path, string prefix, Func<string, string, string> condition)
        {
            if (path.IsMetadata)
                return MetadataPredicate(path.Segments, alias => {
                    var guard = MetadataTypeGuard(path.ValueType, alias);
                    if (MetadataNullable(path.ValueType)) guard = "(" + guard + " OR " + alias + ".type = 'null')";
                    return condition(MetadataColumn(alias, path.ValueType), guard);
                }, prefix);
            var name = path.Segments.Single();
            if (!SqliteCqlContract.Fields.TryGetValue(name, out var field) || field.ValueType != path.ValueType)
                throw new NotSupportedException("SQLite CQL field comparison path is unsupported.");
            var column = CanonicalColumn(name);
            if (field.ValueType == typeof(Guid))
            {
                if (name == "CreatedByAvatarId" || name == "ModifiedByAvatarId")
                    column = "COALESCE(" + column + ", '00000000-0000-0000-0000-000000000000')";
                column += " COLLATE NOCASE";
            }
            else if (field.ValueType == typeof(string)) column += " COLLATE BINARY";
            return condition(column, "1");
        }
        private string FieldComparison(CosmicQueryFieldComparison fields)
        {
            var operation = fields.Operator switch {
                CosmicQueryComparisonOperator.Equal => "IS", CosmicQueryComparisonOperator.NotEqual => "IS NOT",
                CosmicQueryComparisonOperator.GreaterThan => ">", CosmicQueryComparisonOperator.GreaterThanOrEqual => ">=",
                CosmicQueryComparisonOperator.LessThan => "<", CosmicQueryComparisonOperator.LessThanOrEqual => "<=",
                _ => throw new NotSupportedException("SQLite CQL comparison operator is unsupported.") };
            return FieldOperand(fields.Left, "cqlleft", (left, leftGuard) =>
                FieldOperand(fields.Right, "cqlright", (right, rightGuard) =>
                    "(" + leftGuard + " AND " + rightGuard + " AND COALESCE((" + left + " " + operation + " " + right + "), 0))"));
        }
        private string MetadataTest(CosmicQueryMetadataTest test)
            => MetadataPredicate(test.Segments, alias => test.Kind == CosmicQueryMetadataTestKind.IsNull ? alias + ".type = 'null'" : "1");

        private static string MetadataTypeGuard(Type declaredType, string alias)
        {
            var type = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            if (type == typeof(string)) return alias + ".type = 'text'";
            if (type == typeof(DateTime)) return SqliteCqlDate.Guard(alias);
            if (type == typeof(Guid))
            {
                // JSON stores Guid as canonical D text. Reject malformed text instead of coercing it.
                const string hex = "[0-9a-fA-F]";
                var pattern = string.Join("-", new[] { 8, 4, 4, 4, 12 }.Select(count => string.Concat(Enumerable.Repeat(hex, count))));
                return alias + ".type = 'text' AND length(CAST(" + alias + ".value AS BLOB)) = 36 AND " + alias + ".value GLOB '" + pattern + "'";
            }
            if (type == typeof(bool)) return alias + ".type IN ('true','false')";
            if (type == typeof(int) || type == typeof(long))
            {
                // JSON integers outside Int64 are surfaced as REAL by SQLite; never round them into this typed domain.
                var guard = alias + ".type = 'integer' AND typeof(" + alias + ".value) = 'integer'";
                return type == typeof(int) ? guard + " AND " + alias + ".value BETWEEN -2147483648 AND 2147483647" : guard;
            }
            throw new NotSupportedException("SQLite CQL scalar metadata type is unsupported.");
        }
        private static bool MetadataNullable(Type type) => type == typeof(string) || Nullable.GetUnderlyingType(type) != null;

        private string MetadataComparison(CosmicQueryComparison comparison)
        {
            if (!SqliteCqlContract.MetadataTypes.TryGetValue(comparison.Path.ValueType, out var capability) || !capability.Comparisons.Contains(comparison.Operator)
                || comparison.Literal.Kind != CosmicQueryScalarKind.Null && comparison.Literal.Kind != comparison.Path.ScalarKind)
                throw new NotSupportedException("SQLite CQL metadata type/comparison semantics are unsupported.");
            return MetadataPredicate(comparison.Path.Segments, alias => {
                var guard = MetadataTypeGuard(comparison.Path.ValueType, alias);
                if (comparison.Literal.Kind == CosmicQueryScalarKind.Null)
                {
                    if (!MetadataNullable(comparison.Path.ValueType) || comparison.Operator != CosmicQueryComparisonOperator.Equal && comparison.Operator != CosmicQueryComparisonOperator.NotEqual)
                        throw new NotSupportedException("SQLite CQL metadata null comparison is unsupported.");
                    return comparison.Operator == CosmicQueryComparisonOperator.Equal ? alias + ".type = 'null'" : "(" + guard + ")";
                }
                object value = comparison.Literal.Value;
                if (comparison.Path.ScalarKind == CosmicQueryScalarKind.Boolean) value = (bool)value ? 1L : 0L;
                if (comparison.Path.ScalarKind == CosmicQueryScalarKind.Guid) value = ((Guid)value).ToString("D");
                if (comparison.Path.ScalarKind == CosmicQueryScalarKind.UtcDateTime) value = SqliteCqlDate.Literal((DateTime)value);
                var symbol = comparison.Operator switch {
                    CosmicQueryComparisonOperator.Equal => "=", CosmicQueryComparisonOperator.NotEqual => "<>",
                    CosmicQueryComparisonOperator.GreaterThan => ">", CosmicQueryComparisonOperator.GreaterThanOrEqual => ">=",
                    CosmicQueryComparisonOperator.LessThan => "<", CosmicQueryComparisonOperator.LessThanOrEqual => "<=",
                    _ => throw new NotSupportedException("Unknown metadata comparison operator.")
                };
                var column = MetadataColumn(alias, comparison.Path.ValueType);
                var test = "((" + guard + ") AND COALESCE((" + column + " " + symbol + " " + Bind(value) + "),0))";
                // An explicit nullable null has CLR inequality semantics; missing and wrong-type paths remain false.
                return comparison.Operator == CosmicQueryComparisonOperator.NotEqual && MetadataNullable(comparison.Path.ValueType)
                    ? "(" + test + " OR " + alias + ".type = 'null')" : test;
            });
        }

        private static string MetadataColumn(string alias, Type declaredType)
        {
            var type = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
            if (type == typeof(DateTime)) return SqliteCqlDate.Column(alias);
            return alias + ".value" + (type == typeof(Guid) ? " COLLATE NOCASE" : type == typeof(string) ? " COLLATE BINARY" : "");
        }

        private string StringMatch(string column, CosmicQueryStringMatch match)
        {
            var bytes = "CAST(" + column + " AS BLOB)";
            if (match.Value.Length == 0) return "(" + column + " IS NOT NULL)";
            var pattern = "CAST(" + Bind(match.Value) + " AS BLOB)";
            var length = "length(" + pattern + ")";
            var test = match.Operator == CosmicQueryStringOperator.Contains ? "instr(" + bytes + "," + pattern + ") > 0"
                : match.Operator == CosmicQueryStringOperator.StartsWith ? "substr(" + bytes + ",1," + length + ") = " + pattern
                : "substr(" + bytes + ",-" + length + ") = " + pattern;
            return "(COALESCE((" + test + "),0))";
        }

        private string Membership(string column, Type valueType, IReadOnlyList<CosmicQueryLiteral> values)
        {
            var parameters = new List<string>(); var includesNull = false;
            foreach (var item in values)
            {
                if (item.Kind == CosmicQueryScalarKind.Null) { includesNull = true; continue; }
                object value = item.Value;
                var type = Nullable.GetUnderlyingType(valueType) ?? valueType;
                if (type == typeof(Guid)) value = ((Guid)value).ToString("D");
                else if (type == typeof(DateTime)) value = SqliteCqlDate.Literal((DateTime)value);
                else if (type == typeof(bool)) value = (bool)value ? 1L : 0L;
                parameters.Add(Bind(value));
            }
            var test = parameters.Count == 0 ? "0" : "COALESCE((" + column + " IN (" + string.Join(",", parameters) + ")),0)";
            return includesNull ? "((" + column + " IS NULL) OR " + test + ")" : "(" + test + ")";
        }

        private string MixedIntegerComparison(CosmicQueryComparison comparison, string column)
        {
            long floor = 0, ceiling = 0; var outside = 0;
            if (comparison.Literal.Kind == CosmicQueryScalarKind.Decimal)
            {
                var literal = (decimal)comparison.Literal.Value;
                if (literal < int.MinValue) outside = -1;
                else if (literal > int.MaxValue) outside = 1;
                else { floor = (long)decimal.Floor(literal); ceiling = (long)decimal.Ceiling(literal); }
            }
            else if (comparison.Literal.Kind == CosmicQueryScalarKind.Double)
            {
                var literal = (double)comparison.Literal.Value;
                if (double.IsNaN(literal) || double.IsInfinity(literal)) throw new NotSupportedException("Mixed numeric literals must be finite.");
                if (literal < int.MinValue) outside = -1;
                else if (literal > int.MaxValue) outside = 1;
                else { floor = (long)Math.Floor(literal); ceiling = (long)Math.Ceiling(literal); }
            }
            else throw new NotSupportedException("SQLite CQL mixed literals must be decimal or floating point.");
            var operation = comparison.Operator;
            if (outside != 0)
            {
                var always = operation == CosmicQueryComparisonOperator.NotEqual || (outside < 0
                    ? operation == CosmicQueryComparisonOperator.GreaterThan || operation == CosmicQueryComparisonOperator.GreaterThanOrEqual
                    : operation == CosmicQueryComparisonOperator.LessThan || operation == CosmicQueryComparisonOperator.LessThanOrEqual);
                return always ? "(" + column + " IS NOT NULL)" : "0";
            }
            if (floor != ceiling && (operation == CosmicQueryComparisonOperator.Equal || operation == CosmicQueryComparisonOperator.NotEqual))
                return operation == CosmicQueryComparisonOperator.Equal ? "0" : "(" + column + " IS NOT NULL)";
            // Integer n > x iff n > floor(x); n >= x iff n >= ceil(x), with symmetric upper bounds.
            // Only exact integer parameters reach SQLite; it never coerces the decimal/double literal.
            var boundary = operation == CosmicQueryComparisonOperator.GreaterThanOrEqual || operation == CosmicQueryComparisonOperator.LessThan ? ceiling : floor;
            var symbol = operation switch {
                CosmicQueryComparisonOperator.Equal => "=", CosmicQueryComparisonOperator.NotEqual => "<>",
                CosmicQueryComparisonOperator.GreaterThan => ">", CosmicQueryComparisonOperator.GreaterThanOrEqual => ">=",
                CosmicQueryComparisonOperator.LessThan => "<", CosmicQueryComparisonOperator.LessThanOrEqual => "<=",
                _ => throw new NotSupportedException("Unknown mixed numeric comparison operator.")
            };
            return "(COALESCE((" + column + " " + symbol + " " + Bind(boundary) + "),0))";
        }

        internal string Translate(CosmicQueryPredicate predicate)
        {
            if (predicate is CosmicQueryMembership membership)
            {
                if (membership.Path.IsMetadata)
                {
                    if (!SqliteCqlContract.MetadataTypes.TryGetValue(membership.Path.ValueType, out var metadata) || !metadata.SupportsMembership)
                        throw new NotSupportedException("SQLite CQL metadata membership type is unsupported.");
                    return MetadataPredicate(membership.Path.Segments, alias => {
                        var guard = MetadataTypeGuard(membership.Path.ValueType, alias);
                        if (MetadataNullable(membership.Path.ValueType)) guard = "(" + guard + ") OR " + alias + ".type = 'null'";
                        return "((" + guard + ") AND " + Membership(MetadataColumn(alias, membership.Path.ValueType), membership.Path.ValueType, membership.Values) + ")";
                    });
                }
                if (membership.Path.IsMetadata || membership.Path.Segments.Count != 1 || !SqliteCqlContract.Fields.TryGetValue(membership.Path.Segments[0], out var memberField)
                    || memberField.ValueType != membership.Path.ValueType || !memberField.Comparisons.Contains(CosmicQueryComparisonOperator.Equal))
                    throw new NotSupportedException("SQLite CQL membership path is unsupported.");
                var memberColumn = CanonicalColumn(memberField.Name);
                if (memberField.ValueType == typeof(Guid))
                {
                    if (memberField.Name == "CreatedByAvatarId" || memberField.Name == "ModifiedByAvatarId") memberColumn = "COALESCE(" + memberColumn + ", '00000000-0000-0000-0000-000000000000')";
                    memberColumn += " COLLATE NOCASE";
                }
                else if (memberField.ValueType == typeof(string)) memberColumn += " COLLATE BINARY";
                return Membership(memberColumn, memberField.ValueType, membership.Values);
            }
            if (predicate is CosmicQueryStringMatch match)
            {
                if (match.Path.IsMetadata)
                {
                    if (!SqliteCqlContract.MetadataTypes.TryGetValue(match.Path.ValueType, out var metadata) || !metadata.StringOperations.Contains(match.Operator))
                        throw new NotSupportedException("SQLite CQL metadata string operator is unsupported.");
                    return MetadataPredicate(match.Path.Segments, alias => "(" + MetadataTypeGuard(match.Path.ValueType, alias) + " AND " + StringMatch(alias + ".value", match) + ")");
                }
                if (match.Path.IsMetadata || match.Path.Segments.Count != 1 || !SqliteCqlContract.Fields.TryGetValue(match.Path.Segments[0], out var stringField)
                    || stringField.ValueType != typeof(string) || !stringField.StringOperations.Contains(match.Operator))
                    throw new NotSupportedException("SQLite CQL string path/operator is unsupported.");
                return StringMatch(CanonicalColumn(stringField.Name), match);
            }
            if (predicate is CosmicQueryMetadataTest test) return MetadataTest(test);
            if (predicate is CosmicQueryBoolean boolean) return boolean.Value ? "1" : "0";
            if (predicate is CosmicQueryLogical logical)
                return "(" + Translate(logical.Left) + (logical.IsAnd ? " AND " : " OR ") + Translate(logical.Right) + ")";
            if (predicate is CosmicQueryFieldComparison fields) return FieldComparison(fields);
            if (predicate is CosmicQueryNot not) return "(NOT " + Translate(not.Operand) + ")";
            if (predicate is CosmicQueryComparison metadataComparison && metadataComparison.Path.IsMetadata) return MetadataComparison(metadataComparison);
            if (predicate is not CosmicQueryComparison comparison || comparison.Path.Segments.Count != 1)
                throw new NotSupportedException("SQLite CQL does not yet admit this predicate/path.");
            var name = comparison.Path.Segments[0];
            if (!SqliteCqlContract.Fields.TryGetValue(name, out var field) || field.ValueType != comparison.Path.ValueType || !field.Comparisons.Contains(comparison.Operator))
                throw new NotSupportedException("SQLite CQL field/operator is unsupported: " + name + "/" + comparison.Operator);
            var text = field.ValueType == typeof(string); var booleanField = field.ValueType == typeof(bool); var guid = field.ValueType == typeof(Guid);
            // Identifiers originate only in the fixed column whitelist, never in caller text.
            var column = CanonicalColumn(name);
            if (guid)
            {
                // Nullable legacy audit strings materialize as Guid.Empty, so the SQL operand must agree.
                if (name == "CreatedByAvatarId" || name == "ModifiedByAvatarId") column = "COALESCE(" + column + ", '00000000-0000-0000-0000-000000000000')";
                column += " COLLATE NOCASE";
            }
            else if (text) column += " COLLATE BINARY";
            if (comparison.Literal.Kind == CosmicQueryScalarKind.Null)
                return "(" + column + (comparison.Operator == CosmicQueryComparisonOperator.Equal ? " IS NULL)" : " IS NOT NULL)");
            if (comparison.Literal.Kind != comparison.Path.ScalarKind)
            {
                if (comparison.Path.ScalarKind != CosmicQueryScalarKind.Int64 ||
                    !(field.ValueType == typeof(int) || field.ValueType.IsEnum && System.Enum.GetUnderlyingType(field.ValueType) == typeof(int)))
                    throw new NotSupportedException("SQLite CQL mixed comparisons require a canonical Int32 field.");
                return MixedIntegerComparison(comparison, column);
            }
            object value = comparison.Literal.Value;
            if (guid) value = ((Guid)value).ToString("D");
            else if (booleanField) value = (bool)value ? 1L : 0L;
            var parameter = Bind(value);
            var operation = comparison.Operator switch
            {
                CosmicQueryComparisonOperator.Equal => "=", CosmicQueryComparisonOperator.NotEqual => "=",
                CosmicQueryComparisonOperator.GreaterThan => ">", CosmicQueryComparisonOperator.GreaterThanOrEqual => ">=",
                CosmicQueryComparisonOperator.LessThan => "<", CosmicQueryComparisonOperator.LessThanOrEqual => "<=",
                _ => throw new NotSupportedException("Unknown CQL comparison operator.")
            };
            // Normalize SQL NULL to false before NOT/AND/OR; CLR string inequality is the complement of equality.
            var comparisonSql = "COALESCE((" + column + " " + operation + " " + parameter + "), 0)";
            return comparison.Operator == CosmicQueryComparisonOperator.NotEqual ? "(NOT " + comparisonSql + ")" : "(" + comparisonSql + ")";
        }
    }
}
