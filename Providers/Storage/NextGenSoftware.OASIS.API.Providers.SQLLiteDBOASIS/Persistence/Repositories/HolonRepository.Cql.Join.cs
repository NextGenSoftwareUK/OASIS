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
    public async Task<OASISResult<CosmicJoinPacket>> ExecuteCosmicJoinAsync<TLeft, TRight, TKey, TResult>(
        CosmicJoinPlan<TLeft, TRight, TKey, TResult> plan, CosmicJoinExecutionLimits limits,
        CancellationToken cancellationToken = default) where TLeft : IHolon where TRight : IHolon
    {
        try
        {
            var admission = CosmicJoinPlanner.AdmitExecution(plan, ProviderType.SQLLiteDBOASIS,
                SqliteCqlContract.Capabilities, SQLLiteDBOASIS.CqlJoinCapabilities, limits);
            if (admission.IsError)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, CosmicJoinPacket>(admission);
            var compiled = CompileJoinSql(plan.Query, limits);
            cancellationToken.ThrowIfCancellationRequested();
            using var exclusive = _dbContext.GetService<IConcurrencyDetector>().EnterCriticalSection();
            var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
            var opened = connection.State != ConnectionState.Open;
            if (opened) await _dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var budget = new SqliteCqlReadBudget(connection, cancellationToken);
                using var traversal = compiled.HasRelationship ? new SqliteCqlTraversalGuard(connection, budget.Check) : null;
                if (_dbContext.Database.CurrentTransaction != null
                    && _dbContext.Database.CurrentTransaction.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
                    return JoinCqlError("CQL_CONSISTENCY_UNSUPPORTED", "Both SQLite join sources require one serializable read owner.");
                using var transaction = _dbContext.Database.CurrentTransaction == null
                    ? connection.BeginTransaction(IsolationLevel.Serializable, deferred: true) : null;
                using var owner = transaction == null ? null : _dbContext.Database.UseTransaction(transaction);
                try
                {
                    // Relationship guards are evaluated outside each source page;
                    // a page cannot hide an invalid protected traversal scope.
                    if (compiled.HasRelationship)
                    {
                        await ValidateScope(compiled.LeftPredicate).ConfigureAwait(false);
                        await ValidateScope(compiled.RightPredicate).ConfigureAwait(false);
                    }
                    var left = await ReadSource(plan.Query.Join.Left, true, compiled.Left,
                        limits.MaximumLeftRows, limits.MaximumLeftPayloadBytes).ConfigureAwait(false);
                    var right = await ReadSource(plan.Query.Join.Right, false, compiled.Right,
                        limits.MaximumRightRows, limits.MaximumRightPayloadBytes).ConfigureAwait(false);
                    var rows = new List<CosmicJoinRow>();
                    long bytes = 0, pairCount = 0;
                    using (var command = Command("SELECT *,count(*) OVER () AS nativeCount FROM (" + compiled.Pairs + ") ORDER BY 1,2"))
                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            Check();
                            pairCount = reader.GetInt64(2);
                            if (pairCount > limits.MaximumPairs || pairCount > limits.MaximumResults)
                                throw new SqliteCqlResourceException("Native join exceeds remaining pair or output work.");
                            var l = left.Rows[reader.GetInt64(0)];
                            var r = reader.IsDBNull(1) ? null : right.Rows[reader.GetInt64(1)];
                            var values = new object[plan.Query.Columns.Count];
                            var li = 0; var ri = 0;
                            bytes = checked(bytes + 48);
                            for (var index = 0; index < values.Length; index++)
                            {
                                var column = plan.Query.Columns[index];
                                values[index] = column.Source switch
                                {
                                    CosmicJoinColumnSource.Left => l.Values[li++],
                                    CosmicJoinColumnSource.Right => r == null ? MissingRight() : r.Values[ri++],
                                    _ => column.PresenceWhenMissing ? r == null : r != null
                                };
                                Reserve(values[index], ref bytes, limits.MaximumPayloadBytes);
                            }
                            rows.Add(new CosmicJoinRow(l.Id, r?.Id, values));

                            object MissingRight() { ri++; return null; }
                        }
                    }
                    Check();
                    var packet = new CosmicJoinPacket(left.Rows.Values.ToArray(), right.Rows.Values.ToArray(), rows.ToArray(),
                        left.Count, right.Count, pairCount);
                    transaction?.Commit(); Check();
                    return new OASISResult<CosmicJoinPacket>(packet) { IsLoaded = true };

                    void Check() { budget.Check(); traversal?.Check(); }

                    SqliteCommand Command(string sql)
                    {
                        var command = connection.CreateCommand();
                        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction() as SqliteTransaction;
                        command.CommandText = sql;
                        foreach (var parameter in compiled.Parameters)
                            command.Parameters.Add(new SqliteParameter(parameter.ParameterName, parameter.SqliteType) { Value = parameter.Value });
                        return command;
                    }

                    async Task ValidateScope(string predicate)
                    {
                        using var command = Command("SELECT count(*) FROM Holon WHERE " + predicate);
                        await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false); Check();
                    }

                    async Task<(Dictionary<long, CosmicJoinInputRow> Rows, long Count)> ReadSource<T>(
                        CosmicQuery<T> query, bool isLeft, string sql, int maximumRows, int maximumBytes) where T : IHolon
                    {
                        var result = new Dictionary<long, CosmicJoinInputRow>();
                        long measured = 0, count = 0;
                        var paths = plan.Query.Join.Keys.Select(key => isLeft ? key.Left : key.Right).ToArray();
                        var selected = plan.Query.Columns.Where(column => column.Source ==
                            (isLeft ? CosmicJoinColumnSource.Left : CosmicJoinColumnSource.Right)).ToArray();
                        using var command = Command("SELECT *,count(*) OVER () AS nativeCount FROM (" + sql + ") ORDER BY position");
                        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            Check();
                            count = reader.GetInt64(reader.FieldCount - 1);
                            if (count > maximumRows) throw new SqliteCqlResourceException("Native join source exceeds its remaining row budget.");
                            var position = reader.GetInt64(0);
                            var id = (Guid)DecodeProjectionCell(reader.GetValue(1), typeof(Guid));
                            var visible = (bool)DecodeProjectionCell(reader.GetValue(2), typeof(bool));
                            var creator = reader.IsDBNull(3) ? Guid.Empty : (Guid)DecodeProjectionCell(reader.GetValue(3), typeof(Guid));
                            measured = checked(measured + 48);
                            var offset = 4;
                            var keys = paths.Select(path => Cell(path.ValueType)).ToArray();
                            var values = selected.Select(column => Cell(column.Path.ValueType)).ToArray();
                            var states = new int[query.Orderings.Count];
                            var order = new object[states.Length];
                            for (var index = 0; index < states.Length; index++)
                            {
                                states[index] = checked((int)reader.GetInt64(offset));
                                order[index] = Cell(query.Orderings[index].Path.ValueType, allowAbsent: true);
                            }
                            if (measured > maximumBytes) throw new SqliteCqlResourceException("Native join source exceeds its remaining byte budget.");
                            result.Add(position, new CosmicJoinInputRow(id, visible, creator, keys, values, order, states));

                            object Cell(Type declared, bool allowAbsent = false)
                            {
                                Check();
                                var state = reader.GetInt64(offset++);
                                var raw = reader.GetValue(offset++);
                                if (state is < 0 or > 2) throw new InvalidDataException("Join source scalar path is ambiguous or invalid.");
                                var value = state == 2 ? DecodeProjectionCell(raw, declared) : null;
                                if (value == null && !allowAbsent && declared.IsValueType && Nullable.GetUnderlyingType(declared) == null)
                                    throw new InvalidDataException("A required join source scalar is null or missing.");
                                Reserve(value, ref measured, maximumBytes);
                                return value;
                            }
                        }
                        Check(); return (result, count);
                    }
                }
                catch (SqliteException cause) when (cause.SqliteErrorCode == 9) { budget.Check(); throw; }
            }
            finally { if (opened) await _dbContext.Database.CloseConnectionAsync().ConfigureAwait(false); }
        }
        catch (OverflowException cause) { return JoinCqlError("CQL_DATA_INVALID", cause.Message, cause); }
        catch (InvalidDataException cause) { return JoinCqlError("CQL_DATA_INVALID", cause.Message, cause); }
        catch (SqliteCqlDeadlineException cause) { return JoinCqlError("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (SqliteCqlResourceException cause) { return JoinCqlError("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (NotSupportedException cause) { return JoinCqlError("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return JoinCqlError("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (Exception cause) { return JoinCqlError("CQL_QUERY_FAILED", "SQLite native join did not complete.", cause); }
    }

    private static void Reserve(object value, ref long bytes, int maximum)
    {
        bytes = checked(bytes + 16 + (value is string text ? ProjectionUtf8.GetByteCount(text) : 16));
        if (bytes > maximum) throw new SqliteCqlResourceException("Native join exceeds its remaining byte budget.");
    }

    private static OASISResult<CosmicJoinPacket> JoinCqlError(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCode = code, Message = message, Exception = cause };
}
