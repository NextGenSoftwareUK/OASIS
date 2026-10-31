using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    public async Task<OASISResult<TResult>> ExecuteCosmicScalarAsync<T, TResult>(CosmicScalarPlan<T, TResult> plan,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            var admission = CosmicScalarPlanner.Admit(plan, ProviderType.SQLLiteDBOASIS,
                SqliteCqlContract.Capabilities, SQLLiteDBOASIS.CqlScalarCapabilities);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, TResult>(admission);
            var source = plan.Query.Source;
            var translator = new CqlSqlTranslator(source.ReadAccess);
            var predicate = translator.TranslateSource(source.Predicates);
            cancellationToken.ThrowIfCancellationRequested();
            var selected = plan.Query.Selector != null;
            var numeric = plan.Query.Operation is CosmicScalarOperation.Sum or CosmicScalarOperation.Average;
            var all = plan.Query.Operation == CosmicScalarOperation.All;
            var postWindow = plan.Query.TerminalPredicate != null;
            // Empty output cannot suppress protected relationship validation.
            // Plain empty queries still avoid opening an unnecessary connection.
            if (source.Limit == 0 && !translator.HasRelationship) return selected ? CosmicSelectedScalarResults.FromValue(plan.Query, null, false)
                : CosmicScalarResults.FromCount(plan.Query, 0);

            // Count cardinality is independent of ordering. Count the complete
            // eligible source first so a page/Any cannot hide a traversal error,
            // then apply the immutable window with exact native integer math.
            translator.Parameters.Add(new SqliteParameter("$countOffset", (long)source.Offset));
            translator.Parameters.Add(new SqliteParameter("$countTake", source.Limit.HasValue ? (long)source.Limit.Value : long.MaxValue));
            var sql = "SELECT CASE WHEN total <= $countOffset THEN 0 ELSE min(total - $countOffset, $countTake) END AS Value "
                + "FROM (SELECT count(*) AS total FROM Holon WHERE " + predicate + ")";
            if (postWindow)
            {
                var ordering = translator.TranslateOrdering(source.Orderings);
                var test = string.Join(" AND ", plan.Query.TerminalPredicate.Predicates.Select(translator.Translate));
                sql = "SELECT count(*) AS Value FROM (SELECT * FROM Holon WHERE " + predicate
                    + " ORDER BY " + ordering + " LIMIT $countTake OFFSET $countOffset) AS Holon"
                    + " WHERE COALESCE((" + test + "),0)" + (all ? "=0" : "<>0");
                if (CosmicScalarCapabilities.HasAllDescendants(plan.Query.TerminalPredicate.Predicates))
                {
                    // Each protected traversal is evaluated before the business
                    // Boolean, including under false AND / true OR branches.
                    var reduction = translator.ProtectTraversalScope(plan.Query.TerminalPredicate.Predicates,
                        "CASE WHEN COALESCE((" + test + "),0)<>0 THEN " + (all ? "0 ELSE 1" : "1 ELSE 0") + " END");
                    sql = "SELECT coalesce(sum(" + reduction + "),0) AS Value FROM (SELECT * FROM Holon WHERE " + predicate
                        + " ORDER BY " + ordering + " LIMIT $countTake OFFSET $countOffset) AS Holon";
                }
            }
            if (selected) sql = numeric
                ? translator.TranslateNumericAggregate(plan.Query.Selector, predicate, translator.TranslateOrdering(source.Orderings), plan.Query.UsesInt64DoubleAverage)
                : translator.TranslateExtremum(plan.Query.Selector, plan.Query.Operation, predicate, translator.TranslateOrdering(source.Orderings));

            using var exclusive = _dbContext.GetService<IConcurrencyDetector>().EnterCriticalSection();
            var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
            var opened = connection.State != ConnectionState.Open;
            if (opened) await _dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var budget = new SqliteCqlReadBudget(connection, cancellationToken);
                using var floating = numeric && FloatingNumeric(plan.Query.Selector.ValueType) ? new SqliteCqlFloatingAggregates(connection, budget.Check) : null;
                using var checkedAverage = plan.Query.UsesInt64DoubleAverage ? new SqliteCqlCheckedInt64Average(connection, budget.Check) : null;
                using var traversal = translator.HasRelationship ? new SqliteCqlTraversalGuard(connection, budget.Check) : null;
                var needsSnapshot = source.GraphOptions != null || translator.HasTraversal || (selected || postWindow) && translator.HasRelationship;
                if (needsSnapshot && _dbContext.Database.CurrentTransaction != null
                    && _dbContext.Database.CurrentTransaction.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
                    return ScalarCqlError<TResult>("CQL_CONSISTENCY_UNSUPPORTED", "SQLite scalar graph/traversal intent requires a serializable read owner.");
                using var transaction = needsSnapshot && _dbContext.Database.CurrentTransaction == null
                    ? connection.BeginTransaction(IsolationLevel.Serializable, deferred: true) : null;
                using var owner = transaction == null ? null : _dbContext.Database.UseTransaction(transaction);
                try
                {
                    budget.Check();
                    OASISResult<TResult> result;
                    if (selected)
                    {
                        await ValidateCqlRelationshipScopeAsync(translator, predicate, budget, traversal, cancellationToken).ConfigureAwait(false);
                        using var command = connection.CreateCommand();
                        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction() as SqliteTransaction;
                        command.CommandText = sql;
                        foreach (var parameter in translator.Parameters)
                            command.Parameters.Add(new SqliteParameter(parameter.ParameterName, parameter.SqliteType) { Value = parameter.Value });
                        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                        {
                            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                throw new System.IO.InvalidDataException("Native scalar envelope is missing.");
                            budget.Check(); traversal?.Check(); floating?.Check(); checkedAverage?.Check();
                            if (reader.GetInt64(0) != 0) throw new System.IO.InvalidDataException("A participating selected value has an invalid domain.");
                            if (numeric)
                            {
                                if (FloatingNumeric(plan.Query.Selector.ValueType))
                                {
                                    if (reader.GetInt64(3) != 0) throw new SqliteCqlResourceException("Floating aggregation exceeds its input work bound.");
                                    var normalized=DecodeFloatingNumeric(plan.Query.Operation,plan.Query.Selector.ValueType,
                                        reader.GetInt64(4),reader.GetInt64(1),(byte[])reader.GetValue(2),grouped:false);
                                    result=normalized.IsError ? OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<object,TResult>(normalized)
                                        : new OASISResult<TResult>((TResult)normalized.Result) { IsLoaded=true };
                                }
                                else
                                {
                                var decimalInput = (Nullable.GetUnderlyingType(plan.Query.Selector.ValueType) ?? plan.Query.Selector.ValueType) == typeof(decimal);
                                if (reader.GetInt64(decimalInput ? 9 : 4) != 0)
                                    throw new SqliteCqlResourceException("Numeric aggregation exceeds its explicit native input work bound.");
                                if (plan.Query.UsesInt64DoubleAverage && (reader.IsDBNull(5) || reader.GetInt64(5) is not (0 or 1)))
                                    throw new System.IO.InvalidDataException("Checked Int64 reduction requires its exact prefix acknowledgement.");
                                result = plan.Query.UsesInt64DoubleAverage
                                    ? CosmicNumericScalarResults.FromCheckedInt64AverageParts(plan.Query,
                                        reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(1), reader.GetInt64(5) == 1)
                                    : decimalInput
                                    ? CosmicDecimalScalarResults.FromParts(plan.Query,
                                        Enumerable.Range(0, CosmicDecimalScalarResults.LimbCount).Select(index => reader.GetInt64(2 + index)).ToArray(), reader.GetInt64(1))
                                    : CosmicNumericScalarResults.FromParts(plan.Query, reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(1));
                                }
                            }
                            else
                            {
                                if (reader.GetInt64(2) != 0) throw new SqliteCqlResourceException("Selected scalar exceeds its native payload budget.");
                                var present = reader.GetInt64(1) > 0;
                                var value = present ? DecodeProjectionCell(reader.GetValue(3), plan.Query.Selector.ValueType) : null;
                                result = CosmicSelectedScalarResults.FromValue(plan.Query, value, present);
                            }
                            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                                throw new System.IO.InvalidDataException("Native scalar returned multiple envelopes.");
                        }
                    }
                    else
                    {
                        if (postWindow)
                            await ValidateCqlRelationshipScopeAsync(translator, predicate, budget, traversal, cancellationToken).ConfigureAwait(false);
                        var count = await _dbContext.Database.SqlQueryRaw<long>(sql, translator.Parameters.Cast<object>().ToArray())
                            .SingleAsync(cancellationToken).ConfigureAwait(false);
                        result = CosmicScalarResults.FromCount(plan.Query, count);
                    }
                    budget.Check(); traversal?.Check(); floating?.Check(); checkedAverage?.Check();
                    if (result.IsError) return result;
                    transaction?.Commit(); budget.Check();
                    return result;
                }
                catch (SqliteException cause) when (cause.SqliteErrorCode == 9) { budget.Check(); throw; }
            }
            finally
            {
                if (opened) await _dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
            }
        }
        catch (OverflowException cause) { return ScalarCqlError<TResult>("CQL_DATA_INVALID", "Selected scalar exceeds its exact CLR domain.", cause); }
        catch (System.IO.InvalidDataException cause) { return ScalarCqlError<TResult>("CQL_DATA_INVALID", cause.Message, cause); }
        catch (SqliteCqlDeadlineException cause) { return ScalarCqlError<TResult>("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (SqliteCqlResourceException cause) { return ScalarCqlError<TResult>("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (SqliteException cause) when (cause.SqliteErrorCode == 18) { return ScalarCqlError<TResult>("CQL_QUERY_LIMIT", "SQLite scalar query exceeds its native record-size budget.", cause); }
        catch (CqlTranslationLimitException cause) { return ScalarCqlError<TResult>("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (SqliteCqlNumericException cause) { return ScalarCqlError<TResult>(cause.Code,cause.Message,cause); }
        catch (NotSupportedException cause) { return ScalarCqlError<TResult>("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return ScalarCqlError<TResult>("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (Exception cause) { return ScalarCqlError<TResult>("CQL_QUERY_FAILED", "SQLite scalar execution failed: " + cause.Message, cause); }
    }

    private static OASISResult<T> ScalarCqlError<T>(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
