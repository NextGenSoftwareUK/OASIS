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
        private string MetadataTest(CosmicQueryMetadataTest test)
        {
            var sources = new List<string>(); var conditions = new List<string>();
            for (var i = 0; i < test.Segments.Count; i++)
            {
                var alias = "m" + i;
                var document = i == 0 ? "\"MetaDataJson\"" : "CASE WHEN m" + (i - 1) + ".type = 'object' THEN m" + (i - 1) + ".value ELSE json_object() END";
                sources.Add("json_each(" + document + ") AS " + alias);
                conditions.Add(alias + ".key COLLATE BINARY = " + Bind(test.Segments[i]));
            }
            if (test.Kind == CosmicQueryMetadataTestKind.IsNull) conditions.Add("m" + (test.Segments.Count - 1) + ".type = 'null'");
            return "EXISTS (SELECT 1 FROM " + string.Join(", ", sources) + " WHERE " + string.Join(" AND ", conditions) + ")";
        }
        internal string Translate(CosmicQueryPredicate predicate)
        {
            if (predicate is CosmicQueryStringMatch match)
            {
                if (match.Path.IsMetadata || match.Path.Segments.Count != 1 || !SqliteCqlContract.Fields.TryGetValue(match.Path.Segments[0], out var stringField)
                    || stringField.ValueType != typeof(string) || !stringField.StringOperations.Contains(match.Operator))
                    throw new NotSupportedException("SQLite CQL string path/operator is unsupported.");
                // BLOB operations preserve embedded NUL bytes and exact Unicode sequences; TEXT length stops at NUL.
                var stringColumn = "CAST(\"" + stringField.Name + "\" AS BLOB)";
                if (match.Value.Length == 0) return "(" + stringColumn + " IS NOT NULL)";
                var stringPattern = "CAST(" + Bind(match.Value) + " AS BLOB)";
                var stringLength = "length(" + stringPattern + ")";
                var stringSql = match.Operator == CosmicQueryStringOperator.Contains ? "instr(" + stringColumn + "," + stringPattern + ") > 0"
                    : match.Operator == CosmicQueryStringOperator.StartsWith ? "substr(" + stringColumn + ",1," + stringLength + ") = " + stringPattern
                    : "substr(" + stringColumn + ",-" + stringLength + ") = " + stringPattern;
                return "(COALESCE((" + stringSql + "),0))";
            }
            if (predicate is CosmicQueryMetadataTest test) return MetadataTest(test);
            if (predicate is CosmicQueryBoolean boolean) return boolean.Value ? "1" : "0";
            if (predicate is CosmicQueryLogical logical)
                return "(" + Translate(logical.Left) + (logical.IsAnd ? " AND " : " OR ") + Translate(logical.Right) + ")";
            if (predicate is CosmicQueryNot not) return "(NOT " + Translate(not.Operand) + ")";
            if (predicate is not CosmicQueryComparison comparison || comparison.Path.IsMetadata || comparison.Path.Segments.Count != 1)
                throw new NotSupportedException("SQLite CQL does not yet admit this predicate/path.");
            var name = comparison.Path.Segments[0];
            if (!SqliteCqlContract.Fields.TryGetValue(name, out var field) || field.ValueType != comparison.Path.ValueType || !field.Comparisons.Contains(comparison.Operator))
                throw new NotSupportedException("SQLite CQL field/operator is unsupported: " + name + "/" + comparison.Operator);
            var text = field.ValueType == typeof(string); var booleanField = field.ValueType == typeof(bool); var guid = field.ValueType == typeof(Guid);
            // Identifiers originate only in the fixed column whitelist, never in caller text.
            var column = "\"" + name + "\"";
            if (guid)
            {
                // Nullable legacy audit strings materialize as Guid.Empty, so the SQL operand must agree.
                if (name == "CreatedByAvatarId" || name == "ModifiedByAvatarId") column = "COALESCE(" + column + ", '00000000-0000-0000-0000-000000000000')";
                column += " COLLATE NOCASE";
            }
            else if (text) column += " COLLATE BINARY";
            if (comparison.Literal.Kind == CosmicQueryScalarKind.Null)
                return "(" + column + (comparison.Operator == CosmicQueryComparisonOperator.Equal ? " IS NULL)" : " IS NOT NULL)");
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
