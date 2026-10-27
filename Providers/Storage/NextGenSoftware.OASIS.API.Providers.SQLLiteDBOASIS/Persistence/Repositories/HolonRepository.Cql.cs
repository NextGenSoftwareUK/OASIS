using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults, CancellationToken cancellationToken) where T : IHolon
        => ExecuteCqlRowsAsync(plan, maximumResults, cancellationToken);

    public Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicEntityCompositionAsync<T>(CosmicEntityCompositionPlan<T> plan,
        int maximumResults, CancellationToken cancellationToken) where T : IHolon
        => ExecuteCqlRowsAsync(plan?.Source, maximumResults, cancellationToken, plan);

    private async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCqlRowsAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults, CancellationToken cancellationToken, CosmicEntityCompositionPlan<T> composition = null) where T : IHolon
    {
        try
        {
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != ProviderType.SQLLiteDBOASIS)
                return CqlError("CQL_PLAN_INVALID", "SQLite execution requires an exclusively SQLite plan.");
            if (maximumResults < 1 || maximumResults > 10000)
                return CqlError("CQL_QUERY_LIMIT", "SQLite CQL result limit must be between one and 10,000.");
            var admission=SqliteCqlContract.Capabilities.AdmitPlan(plan);
            if(admission.IsError)return CqlError(admission.ErrorCode,admission.Message);
            if (composition != null)
            {
                var stages = new CosmicEntityCompositionCapabilities(
                    CosmicEntityStageFeatures.Filter | CosmicEntityStageFeatures.Ordering | CosmicEntityStageFeatures.Paging,
                    SqliteCqlContract.Capabilities).Admit(composition.Query);
                if (stages.IsError) return CqlError(stages.ErrorCode, stages.Message);
            }
            // Translate completely before EF can open the connection. Never evaluate a predicate on fetched rows.
            var translator = new CqlSqlTranslator(plan.Query.ReadAccess);
            var predicate = translator.TranslateSource(plan.Query.Predicates);
            var ordering=translator.TranslateOrdering(plan.Query.Orderings);
            var fetch=plan.TailIndex.HasValue ? (plan.Query.Limit == 0 ? 0 : 1)
                : plan.Query.Limit.HasValue?Math.Min(plan.Query.Limit.Value,maximumResults+1):maximumResults+1;
            if(plan.Query.GraphOptions!=null)fetch=Math.Min(fetch,plan.Query.GraphOptions.MaximumNodes+1);
            translator.Parameters.Add(new SqliteParameter("$limit", fetch));
            translator.Parameters.Add(new SqliteParameter("$offset",plan.Query.Offset));
            var sql = "SELECT * FROM Holon WHERE " + predicate + " ORDER BY " + ordering + " LIMIT $limit OFFSET $offset";
            if (plan.TailIndex.HasValue)
            {
                translator.Parameters.Add(new SqliteParameter("$sourceLimit", plan.Query.Limit ?? -1));
                translator.Parameters.Add(new SqliteParameter("$tailOffset", plan.TailIndex.Value - 1));
                // The complete source window is inside the subquery. Reverse
                // its full ordering, including identity ties, only afterwards.
                sql = "SELECT * FROM (SELECT * FROM Holon WHERE " + predicate + " ORDER BY " + ordering
                    + " LIMIT $sourceLimit OFFSET $offset) AS Holon ORDER BY "
                    + translator.TranslateOrdering(plan.Query.Orderings, reverse: true) + " LIMIT $limit OFFSET $tailOffset";
            }
            CqlSqlTranslator terminalTranslator = null;
            string terminalValidation = null;
            if (plan.PostWindowPredicate != null)
            {
                terminalTranslator = new CqlSqlTranslator("Holon", translator.Parameters, plan.Query.ReadAccess);
                var test = string.Join(" AND ", plan.PostWindowPredicate.Predicates.Select(terminalTranslator.Translate));
                var sourceLimit = translator.Bind(plan.Query.Limit ?? -1);
                var page = "SELECT * FROM Holon WHERE " + predicate + " ORDER BY " + ordering
                    + " LIMIT " + sourceLimit + " OFFSET $offset";
                var terminalLimit = translator.Bind(plan.PostWindowLimit.Value);
                var finalOrdering = plan.TailIndex.HasValue
                    ? translator.TranslateOrdering(plan.Query.Orderings, reverse: true) : ordering;
                sql = "SELECT * FROM (" + page + ") AS Holon WHERE COALESCE((" + test + "),0)<>0 ORDER BY "
                    + finalOrdering + " LIMIT " + terminalLimit;
                // Observe every protected terminal scope within the source page,
                // before First/Single/Last can hide a later cycle/limit error.
                if (terminalTranslator.HasRelationship)
                {
                    var observed = terminalTranslator.ProtectTraversalScope(plan.PostWindowPredicate.Predicates,
                        "CASE WHEN COALESCE((" + test + "),0)<>0 THEN 1 ELSE 0 END");
                    terminalValidation = "SELECT COALESCE(sum(" + observed + "),0) AS Value FROM (" + page + ") AS Holon";
                }
            }
            var entitySql = composition == null ? null : CompileEntityCompositionSql(composition);
            if (entitySql != null)
            {
                translator = entitySql.SourceTranslator;
                predicate = entitySql.SourcePredicate;
                sql = entitySql.Rows + " LIMIT " + translator.Bind(maximumResults + 1);
            }
            var hasRelationship = translator.HasRelationship || terminalTranslator?.HasRelationship == true
                || entitySql?.HasRelationship == true;
            cancellationToken.ThrowIfCancellationRequested();
            if(composition == null && fetch==0 && !translator.HasRelationship)return new OASISResult<IReadOnlyList<IHolon>>(Array.Empty<IHolon>()){IsLoaded=true};
            // Own the EF concurrency scope before changing any connection-local
            // native hook/limit. Nested EF enumeration uses the same logical scope.
            using var exclusive=_dbContext.GetService<IConcurrencyDetector>().EnterCriticalSection();
            var connection=(SqliteConnection)_dbContext.Database.GetDbConnection();
            var opened=connection.State!=ConnectionState.Open;
            if(opened)await _dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var budget=new SqliteCqlReadBudget(connection,cancellationToken);
                using var traversal=hasRelationship?new SqliteCqlTraversalGuard(connection,budget.Check):null;
                if ((composition != null || plan.Query.GraphOptions != null || hasRelationship) && _dbContext.Database.CurrentTransaction != null &&
                    _dbContext.Database.CurrentTransaction.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
                    return CqlError("CQL_CONSISTENCY_UNSUPPORTED", "SQLite graphs/traversals require a serializable native read owner.");
                // A deferred read transaction supports read-only connections and
                // holds the same SQLite snapshot for the root page and all children.
                using var graphTransaction = (composition != null || plan.Query.GraphOptions != null || hasRelationship) && _dbContext.Database.CurrentTransaction == null
                    ? connection.BeginTransaction(IsolationLevel.Serializable, deferred: true) : null;
                using var graphOwner = graphTransaction == null ? null : _dbContext.Database.UseTransaction(graphTransaction);
                var rows=new List<IHolon>();long bytes=0;
                try
                {
                    budget.Check();
                    await ValidateCqlRelationshipScopeAsync(translator, predicate, budget, traversal, cancellationToken).ConfigureAwait(false);
                    if (entitySql != null)
                    {
                        // Source bounds apply before filtering, including a later Take(0).
                        // Native rows are measured here, never filtered in managed code.
                        var sourceRows = 0;
                        long sourceBytes = 0;
                        var sourceQuery = _dbContext.Holons.FromSqlRaw(entitySql.Source,
                            translator.Parameters.Cast<object>().ToArray()).AsNoTracking();
                        await foreach (var row in sourceQuery.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
                        {
                            budget.Check(); traversal?.Check();
                            sourceBytes = checked(sourceBytes + CqlRecordSize(row));
                            if (++sourceRows > composition.Options.MaximumInputRows
                                || sourceBytes > composition.Options.MaximumInputPayloadBytes)
                                throw new SqliteCqlResourceException("Entity composition exceeds its native source row/payload budget.");
                        }
                        long work = 0;
                        for (var index = 0; index < entitySql.StageCounts.Count; index++)
                        {
                            var count = await _dbContext.Database.SqlQueryRaw<long>(entitySql.StageCounts[index],
                                translator.Parameters.Cast<object>().ToArray()).SingleAsync(cancellationToken).ConfigureAwait(false);
                            work = checked(work + count);
                            if (count < 0 || work > composition.Options.MaximumStageRows)
                                throw new SqliteCqlResourceException("Entity composition exceeds its cumulative native stage-work budget.");
                            if (entitySql.ProtectedScopes[index] != null)
                                await _dbContext.Database.SqlQueryRaw<long>(entitySql.ProtectedScopes[index],
                                    translator.Parameters.Cast<object>().ToArray()).SingleAsync(cancellationToken).ConfigureAwait(false);
                            budget.Check(); traversal?.Check();
                        }
                    }
                    if (terminalValidation != null)
                    {
                        await _dbContext.Database.SqlQueryRaw<long>(terminalValidation, translator.Parameters.Cast<object>().ToArray())
                            .SingleAsync(cancellationToken).ConfigureAwait(false);
                        budget.Check(); traversal.Check();
                    }
                    var query=_dbContext.Holons.FromSqlRaw(sql,
                        translator.Parameters.Cast<object>().ToArray()).AsNoTracking();
                    await foreach(var row in query.AsAsyncEnumerable().WithCancellation(cancellationToken).ConfigureAwait(false))
                    {
                        budget.Check();traversal?.Check();
                        if(rows.Count>=Math.Min(maximumResults,plan.Query.GraphOptions?.MaximumNodes ?? maximumResults))
                            throw new SqliteCqlResourceException("Query exceeds the declared root/graph result limit.");
                        bytes+=CqlRecordSize(row);
                        if(bytes>Math.Min(SqliteCqlReadBudget.MaximumPayloadBytes,
                            plan.Query.GraphOptions?.MaximumPayloadBytes ?? SqliteCqlReadBudget.MaximumPayloadBytes))
                            throw new SqliteCqlResourceException("SQLite CQL exceeds its streamed payload budget.");
                        rows.Add(GetHolonFromEntity(row));budget.Check();
                    }
                    budget.Check();traversal?.Check();
                    if (plan.Query.GraphOptions != null)
                    {
                        Task<IReadOnlyList<IHolon>> ReadChildren(CosmicQueryChildSelection selection, CancellationToken token)
                            => ReadCqlChildrenAsync(selection, budget, token);
                        var graph = plan.Query.ReadAccess == null
                            ? await HolonGraphQueryExpansion.ExpandAsync(rows.AsReadOnly(), plan.Query.GraphOptions,
                                ReadChildren, cancellationToken, budget.Check).ConfigureAwait(false)
                            : await HolonGraphQueryExpansion.ExpandAsync(rows.AsReadOnly(), plan.Query.GraphOptions,
                                plan.Query.ReadAccess, ReadChildren, cancellationToken, budget.Check).ConfigureAwait(false);
                        if (graph.IsError) return graph;
                        budget.Check();
                        graphTransaction?.Commit();
                        return graph;
                    }
                    return new OASISResult<IReadOnlyList<IHolon>>(rows.AsReadOnly()){IsLoaded=true};
                }
                catch(SqliteException cause) when(cause.SqliteErrorCode==9){budget.Check();throw;}
            }
            finally
            {
                if(opened)await _dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }
        catch (System.IO.InvalidDataException cause){return CqlError("CQL_DATA_INVALID",cause.Message,cause);}
        catch (SqliteCqlDeadlineException cause){return CqlError("CQL_QUERY_TIMEOUT",cause.Message,cause);}
        catch (SqliteCqlResourceException cause){return CqlError("CQL_QUERY_LIMIT",cause.Message,cause);}
        catch (SqliteException cause) when(cause.SqliteErrorCode==18){return CqlError("CQL_QUERY_LIMIT","SQLite CQL exceeds its native record-size budget.",cause);}
        catch (CqlTranslationLimitException cause) { return CqlError("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (NotSupportedException cause) { return CqlError("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return CqlError("CQL_QUERY_CANCELLED", "SQLite CQL execution was cancelled.", cause); }
        catch (Exception cause) { return CqlError("CQL_QUERY_FAILED", "SQLite CQL execution failed: " + cause.Message, cause); }
    }

    private static readonly System.Reflection.PropertyInfo[] CqlStringColumns=typeof(Entities.HolonModel).GetProperties()
        .Where(property=>property.PropertyType==typeof(string) && property.GetMethod!=null).ToArray();

    private static OASISResult<IReadOnlyList<IHolon>> CqlError(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

    private sealed class CqlTranslationLimitException : Exception
    { internal CqlTranslationLimitException() : base("SQLite CQL exceeds its 900-bound-parameter translation budget.") { } }

    private sealed partial class CqlSqlTranslator
    {
        internal readonly List<SqliteParameter> Parameters;
        private readonly int _relationshipDepth;
        private readonly string _table;
        private readonly CosmicQueryReadAccess _access;
        internal CqlSqlTranslator(CosmicQueryReadAccess access=null):this("Holon",new List<SqliteParameter>(),access){}
        internal CqlSqlTranslator(string table,List<SqliteParameter> parameters,CosmicQueryReadAccess access,int relationshipDepth=0)
        { _table=table;Parameters=parameters;_access=access;_relationshipDepth=relationshipDepth; }
        internal string Bind(object value)
        {
            if (Parameters.Count >= 900) throw new CqlTranslationLimitException();
            var name = "$cql" + Parameters.Count; Parameters.Add(new SqliteParameter(name, value)); return name;
        }
        private string MetadataPredicate(IReadOnlyList<string> segments, Func<string, string> condition, string prefix = "m")
            => "EXISTS (" + MetadataSubquery(segments, _ => "1", condition, prefix) + ")";
        private string MetadataSubquery(IReadOnlyList<string> segments, Func<string,string> projection,
            Func<string,string> condition, string prefix)
        {
            var sources = new List<string>(); var conditions = new List<string>();
            for (var i = 0; i < segments.Count; i++)
            {
                var alias = prefix + i;
                var document = i == 0 ? CanonicalColumn("MetaDataJson") : "CASE WHEN " + prefix + (i - 1) + ".type = 'object' THEN " + prefix + (i - 1) + ".value ELSE json_object() END";
                sources.Add("json_each(" + document + ") AS " + alias);
                conditions.Add(alias + ".key COLLATE BINARY = " + Bind(segments[i]));
            }
            var terminal=prefix + (segments.Count - 1);
            conditions.Add(condition(terminal));
            return "SELECT " + projection(terminal) + " FROM " + string.Join(", ", sources) + " WHERE " + string.Join(" AND ", conditions);
        }
        // json_each exposes its own `id` column. Every canonical operand must
        // remain bound to the outer record, including inside a correlated path.
        private string CanonicalColumn(string name) => "\""+_table+"\".\"" + name + "\"";
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
            if(type.IsEnum)
            {
                // Only explicitly registered enum path types reach this translator.
                // Values are trusted schema constants, never caller SQL fragments.
                var defined=System.Enum.GetValues(type).Cast<object>().Select(value=>Convert.ToInt64(value))
                    .Distinct().Select(value=>value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                var members=string.Join(",",defined);
                return members.Length==0 ? "0" : alias+".type = 'integer' AND typeof("+alias+".value) = 'integer' AND "+alias+".value IN ("+members+")";
            }
            if (type == typeof(string)) return alias + ".type = 'text' AND cosmic_cql_utf8_valid(CAST(" + alias + ".value AS BLOB))";
            if (type == typeof(DateTime)) return SqliteCqlDate.Guard(alias);
            if (type == typeof(Guid))
            {
                // JSON stores Guid as canonical D text. Reject malformed text instead of coercing it.
                const string hex = "[0-9a-fA-F]";
                var pattern = string.Join("-", new[] { 8, 4, 4, 4, 12 }.Select(count => string.Concat(Enumerable.Repeat(hex, count))));
                return alias + ".type = 'text' AND length(CAST(" + alias + ".value AS BLOB)) = 36 AND " + alias + ".value GLOB '" + pattern + "'";
            }
            if (type == typeof(decimal)) return "(" + SqliteCqlDecimal.Column(alias) + ") IS NOT NULL";
            if (type == typeof(double) || type == typeof(float)) return "(" + SqliteCqlFloating.Column(alias, type) + ") IS NOT NULL";
            if (type == typeof(ulong)) return SqliteCqlUInt64.Guard(alias);
            if (type == typeof(bool)) return alias + ".type IN ('true','false')";
            if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
                || type == typeof(uint) || type == typeof(int) || type == typeof(long))
            {
                // JSON integers outside Int64 are surfaced as REAL by SQLite; never round them into this typed domain.
                var guard = alias + ".type = 'integer' AND typeof(" + alias + ".value) = 'integer'";
                if(type==typeof(long))return guard;
                var bounds=type==typeof(byte)?"0 AND 255":type==typeof(sbyte)?"-128 AND 127":
                    type==typeof(short)?"-32768 AND 32767":type==typeof(ushort)?"0 AND 65535":
                    type==typeof(uint)?"0 AND 4294967295":"-2147483648 AND 2147483647";
                return guard+" AND "+alias+".value BETWEEN "+bounds;
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
                if ((Nullable.GetUnderlyingType(comparison.Path.ValueType) ?? comparison.Path.ValueType) == typeof(decimal)) value = CosmicNumericScalarResults.DecimalOrderKey((decimal)value);
                if ((Nullable.GetUnderlyingType(comparison.Path.ValueType) ?? comparison.Path.ValueType) == typeof(float)) value = CosmicFloatingScalarResults.DoubleOrderKey(Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture));
                if ((Nullable.GetUnderlyingType(comparison.Path.ValueType) ?? comparison.Path.ValueType) == typeof(double)) value = CosmicFloatingScalarResults.DoubleOrderKey(Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture));
                if ((Nullable.GetUnderlyingType(comparison.Path.ValueType) ?? comparison.Path.ValueType) == typeof(ulong)) value = SqliteCqlUInt64.Literal(value);
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
            if (type == typeof(ulong)) return SqliteCqlUInt64.Column(alias);
            if (type == typeof(decimal)) return SqliteCqlDecimal.Column(alias);
            if (type == typeof(double) || type == typeof(float)) return SqliteCqlFloating.Column(alias, type);
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
                else if (type == typeof(ulong)) value = SqliteCqlUInt64.Literal(value);
                else if (type == typeof(decimal)) value = CosmicNumericScalarResults.DecimalOrderKey((decimal)value);
                else if (type == typeof(float)) value = CosmicFloatingScalarResults.DoubleOrderKey(Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture));
                else if (type == typeof(double)) value = CosmicFloatingScalarResults.DoubleOrderKey(Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture));
                parameters.Add(Bind(value));
            }
            var test = parameters.Count == 0 ? "0" : "COALESCE((" + column + " IN (" + string.Join(",", parameters) + ")),0)";
            return includesNull ? "((" + column + " IS NULL) OR " + test + ")" : "(" + test + ")";
        }

        private string MixedIntegerComparison(CosmicQueryComparison comparison, string column)
        {
            var boundary = IntegerBoundary(comparison.Literal, comparison.Operator, int.MinValue, int.MaxValue);
            if (boundary.Constant.HasValue) return boundary.Constant.Value ? "(" + column + " IS NOT NULL)" : "0";
            return "(COALESCE((" + column + ComparisonSymbol(comparison.Operator) + Bind(boundary.Boundary) + "),0))";
        }

        internal string Translate(CosmicQueryPredicate predicate)
        {
            if(predicate is CosmicQueryAnyDescendant descendant)return TranslateDescendant(descendant);
            if(predicate is CosmicQueryAnyChild child)
            {
                HasRelationship=true;
                var alias="c"+_relationshipDepth.ToString(System.Globalization.CultureInfo.InvariantCulture)+"_"+Parameters.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var inner=new CqlSqlTranslator(alias,Parameters,_access,_relationshipDepth+1);
                var parent=CanonicalColumn("Id");
                var childId=inner.CanonicalColumn("Id");
                var membershipSql=MembershipSql(this,inner);
                var visibility=VisibilitySql(inner);
                var childMatch = inner.Translate(child.Predicate);
                var scope = " FROM Holon AS \""+alias+"\" WHERE lower("+inner.CanonicalColumn("ParentHolonId")
                    +")=lower("+parent+") AND lower("+childId+")<>lower("+parent+") AND "+membershipSql
                    +" AND ("+visibility+")";
                if (CosmicScalarCapabilities.HasAllChildren(new[] { child.Predicate })
                    || CosmicScalarCapabilities.HasAllDescendants(new[] { child.Predicate }))
                {
                    var protectedChildValue = inner.ProtectTraversalScope(new[] { child.Predicate },
                        "CASE WHEN COALESCE((" + childMatch + "),0)<>0 THEN 1 ELSE 0 END");
                    return "(CASE WHEN " + MembershipObservationSql(this) + "=0 THEN 0 ELSE (SELECT coalesce(sum("
                        + protectedChildValue + "),0)>0" + scope + ") END)";
                }
                return "(CASE WHEN "+MembershipObservationSql(this)+"=0 THEN 0 ELSE EXISTS (SELECT 1" + scope
                    +" AND ("+childMatch+")) END)";
            }
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
    // Relationship validation belongs outside every result window. Share the
    // existing native scope read across rows, projections, scalars and groups.
    private async Task ValidateCqlRelationshipScopeAsync(CqlSqlTranslator translator, string predicate,
        SqliteCqlReadBudget budget, SqliteCqlTraversalGuard traversal, CancellationToken cancellationToken)
    {
        if (!translator.HasRelationship) return;
        await _dbContext.Database.SqlQueryRaw<long>("SELECT count(*) AS Value FROM Holon WHERE " + predicate,
            translator.Parameters.Cast<object>().ToArray()).SingleAsync(cancellationToken).ConfigureAwait(false);
        budget.Check();
        traversal.Check();
    }
}
