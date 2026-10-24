using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
    public async Task<OASISResult<IReadOnlyList<CosmicGroupedRow>>> ExecuteCosmicGroupedAsync<T, TKey, TResult>(
        CosmicGroupedPlan<T, TKey, TResult> plan, int maximumGroups = 1000,
        int maximumPayloadBytes = CosmicProjectionResults.MaximumPayloadLimit, CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            var admission = CosmicGroupedPlanner.Admit(plan, ProviderType.SQLLiteDBOASIS, SqliteCqlContract.Capabilities, SQLLiteDBOASIS.CqlGroupedCapabilities);
            if (admission.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, IReadOnlyList<CosmicGroupedRow>>(admission);
            var query = plan.Query; var source = query.Source;
            if (maximumGroups < 0 || maximumGroups > query.Grouping.Options.MaximumGroups
                || maximumPayloadBytes < 0 || maximumPayloadBytes > query.Grouping.Options.MaximumPayloadBytes)
                return GroupedCqlError("CQL_EXECUTION_INVALID", "Grouped remaining output bounds are invalid.");
            var translator = new CqlSqlTranslator(source.ReadAccess);
            var predicate = source.Predicates.Count == 0 ? "1" : string.Join(" AND ", source.Predicates.Select(translator.Translate));
            var ordering = translator.TranslateOrdering(source.Orderings);
            var sentinel = (long)CosmicNumericScalarResults.MaximumParticipatingValues + 1;
            translator.Parameters.Add(new SqliteParameter("$groupOffset", (long)source.Offset));
            translator.Parameters.Add(new SqliteParameter("$groupTake", Math.Min(source.Limit.HasValue ? (long)source.Limit.Value : long.MaxValue, sentinel)));
            translator.Parameters.Add(new SqliteParameter("$groupLimit", (long)maximumGroups + 1));
            var sql = translator.TranslateGroups(query, predicate, ordering);
            cancellationToken.ThrowIfCancellationRequested();
            using var exclusive = _dbContext.GetService<IConcurrencyDetector>().EnterCriticalSection();
            var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
            var opened = connection.State != ConnectionState.Open;
            if (opened) await _dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var budget = new SqliteCqlReadBudget(connection, cancellationToken);
                using var traversal = translator.HasRelationship ? new SqliteCqlTraversalGuard(connection, budget.Check) : null;
                var snapshot = source.GraphOptions != null || translator.HasRelationship;
                if (snapshot && _dbContext.Database.CurrentTransaction != null
                    && _dbContext.Database.CurrentTransaction.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
                    return GroupedCqlError("CQL_CONSISTENCY_UNSUPPORTED", "SQLite grouped graph/traversal intent requires a serializable read owner.");
                using var transaction = snapshot && _dbContext.Database.CurrentTransaction == null
                    ? connection.BeginTransaction(IsolationLevel.Serializable, deferred: true) : null;
                using var owner = transaction == null ? null : _dbContext.Database.UseTransaction(transaction);
                try
                {
                    budget.Check();
                    // Mandatory protected scope validation precedes source paging.
                    if (translator.HasRelationship)
                    {
                        await _dbContext.Database.SqlQueryRaw<long>("SELECT count(*) AS Value FROM Holon WHERE " + predicate,
                            translator.Parameters.Cast<object>().ToArray()).SingleAsync(cancellationToken).ConfigureAwait(false);
                        budget.Check(); traversal.Check();
                    }
                    using var command = connection.CreateCommand();
                    command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction() as SqliteTransaction;
                    command.CommandText = sql;
                    foreach (var parameter in translator.Parameters)
                        command.Parameters.Add(new SqliteParameter(parameter.ParameterName, parameter.SqliteType) { Value = parameter.Value });
                    var rows = new List<CosmicGroupedRow>(); long bytes = 0;
                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            budget.Check(); traversal?.Check();
                            if (reader.GetInt64(1) > CosmicNumericScalarResults.MaximumParticipatingValues)
                                throw new SqliteCqlResourceException("Grouped input exceeds its explicit native work bound.");
                            if (rows.Count >= maximumGroups) throw new SqliteCqlResourceException("Grouping exceeds its remaining output count.");
                            if (reader.GetInt64(0) != 0) throw new InvalidDataException("A participating grouping key or aggregate has an invalid domain.");
                            var inputs = reader.GetInt64(2); var position = 3;
                            var keys = new object[query.Grouping.Key.Columns.Count]; bytes += 32;
                            for (var index = 0; index < keys.Length; index++)
                            {
                                keys[index] = reader.IsDBNull(position) ? null : DecodeProjectionCell(reader.GetValue(position), query.Grouping.Key.Columns[index].Path.ValueType);
                                position++; Reserve(keys[index]);
                            }
                            var values = new object[query.Columns.Count];
                            for (var index = 0; index < values.Length; index++)
                            {
                                budget.Check(); var column = query.Columns[index];
                                if (column.KeyIndex >= 0) values[index] = keys[column.KeyIndex];
                                else if (column.Operation == CosmicScalarOperation.Count)
                                {
                                    if (inputs > int.MaxValue) return GroupedCqlError("CQL_SCALAR_OVERFLOW", "Grouped Count exceeds Int32.");
                                    values[index] = (int)inputs;
                                }
                                else if (column.Operation == CosmicScalarOperation.LongCount) values[index] = inputs;
                                else if (column.Operation is CosmicScalarOperation.Sum or CosmicScalarOperation.Average)
                                {
                                    var normalized = CosmicNumericScalarResults.FromParts(column.Operation.Value, column.Selector.ValueType,
                                        reader.GetInt64(position), reader.GetInt64(position + 1), reader.GetInt64(position + 2));
                                    position += 3;
                                    if (normalized.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<object, IReadOnlyList<CosmicGroupedRow>>(normalized);
                                    values[index] = normalized.Result;
                                }
                                else
                                {
                                    values[index] = reader.IsDBNull(position) ? null : DecodeProjectionCell(reader.GetValue(position), column.ValueType);
                                    position++;
                                }
                                Reserve(values[index]);
                            }
                            rows.Add(new CosmicGroupedRow(inputs, keys, values));
                        }
                    }
                    budget.Check(); traversal?.Check();
                    return new OASISResult<IReadOnlyList<CosmicGroupedRow>>(rows) { IsLoaded = true };

                    void Reserve(object value)
                    {
                        bytes += 16 + (value is string text ? ProjectionUtf8.GetByteCount(text) : 16);
                        if (bytes > maximumPayloadBytes) throw new SqliteCqlResourceException("Grouped proof/result payload exceeds its remaining budget.");
                    }
                }
                finally { transaction?.Rollback(); }
            }
            finally { if (opened) await _dbContext.Database.CloseConnectionAsync().ConfigureAwait(false); }
        }
        catch (OverflowException cause) { return GroupedCqlError("CQL_DATA_INVALID", "Grouped scalar exceeds its exact CLR domain.", cause); }
        catch (InvalidDataException cause) { return GroupedCqlError("CQL_DATA_INVALID", cause.Message, cause); }
        catch (SqliteCqlDeadlineException cause) { return GroupedCqlError("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (SqliteCqlResourceException cause) { return GroupedCqlError("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (SqliteException cause) when (cause.SqliteErrorCode == 18) { return GroupedCqlError("CQL_QUERY_LIMIT", "SQLite grouped record exceeds its native size budget.", cause); }
        catch (CqlTranslationLimitException cause) { return GroupedCqlError("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (NotSupportedException cause) { return GroupedCqlError("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return GroupedCqlError("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (Exception cause) { return GroupedCqlError("CQL_QUERY_FAILED", "SQLite grouped query failed: " + cause.Message, cause); }
    }

    private static OASISResult<IReadOnlyList<CosmicGroupedRow>> GroupedCqlError(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };
}
