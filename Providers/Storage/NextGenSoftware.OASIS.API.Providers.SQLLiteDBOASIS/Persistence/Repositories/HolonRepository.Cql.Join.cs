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
                    var leftStorageKeys = plan.Query.SelectsLeftEntity ? new Dictionary<Guid,string>() : null;
                    var rightStorageKeys = plan.Query.SelectsChildEntity ? new Dictionary<Guid,string>() : null;
                    var left = await ReadSource(plan.Query.Join.Left, true, compiled.Left,
                        limits.MaximumLeftRows, limits.MaximumLeftPayloadBytes).ConfigureAwait(false);
                    var right = await ReadSource(plan.Query.Join.Right, false, compiled.Right,
                        limits.MaximumRightRows, limits.MaximumRightPayloadBytes).ConfigureAwait(false);
                    var rows = new List<CosmicJoinRow>();
                    long bytes = 0, pairCount = 0;
                    using (var command = Command(compiled.Pairs))
                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            Check();
                            pairCount = reader.GetInt64(2);
                            if (pairCount > limits.MaximumPairs ||
                                (!plan.Query.OutputLimit.HasValue && pairCount > limits.MaximumResults))
                                throw new SqliteCqlResourceException("Native join exceeds remaining pair or output work.");
                            if (reader.IsDBNull(3)) continue;
                            if (rows.Count == limits.MaximumResults)
                                throw new SqliteCqlResourceException("Native join exceeds remaining output rows.");
                            var l = reader.IsDBNull(0) ? null : left.Rows[reader.GetInt64(0)];
                            var r = reader.IsDBNull(1) ? null : right.Rows[reader.GetInt64(1)];
                            var values = new object[plan.Query.Columns.Count];
                            var li = 0; var ri = 0;
                            bytes = checked(bytes + 48);
                            for (var index = 0; index < values.Length; index++)
                            {
                                var column = plan.Query.Columns[index];
                                values[index] = column.Source switch
                                {
                                    CosmicJoinColumnSource.Left => l?.Values[li++],
                                    CosmicJoinColumnSource.Right => r?.Values[ri++],
                                    _ => column.PresenceWhenMissing ? r == null : r != null
                                };
                                Reserve(values[index], ref bytes, limits.MaximumPayloadBytes);
                            }
                            rows.Add(new CosmicJoinRow(l?.Id ?? Guid.Empty, r?.Id, values));
                        }
                    }
                    Check();
                    var entityBytes=bytes;
                    var leftEntities=plan.Query.SelectsLeftEntity ? await ReadSelectedEntities(rows,true).ConfigureAwait(false) : null;
                    var rightEntities=plan.Query.SelectsChildEntity ? await ReadSelectedEntities(rows,false).ConfigureAwait(false) : null;
                    var packet = new CosmicJoinPacket(left.Rows.Values.ToArray(), right.Rows.Values.ToArray(), rows.ToArray(),
                        left.Count, right.Count, pairCount, rightEntities, leftEntities);
                    transaction?.Commit(); Check();
                    return new OASISResult<CosmicJoinPacket>(packet) { IsLoaded = true };

                    async Task<IHolon[]> ReadSelectedEntities(IReadOnlyList<CosmicJoinRow> pairs, bool isLeft)
                    {
                        if (pairs.Count == 0)
                            return Array.Empty<IHolon>();
                        var identities = pairs.Select(pair => isLeft ? (Guid?)pair.LeftId : pair.RightId).ToArray();
                        if(isLeft && identities.Any(id=>!id.HasValue || id.Value==Guid.Empty))
                            throw new InvalidDataException("A selected left entity requires its preserved native identity.");
                        var uniqueIdentities = identities.Where(id=>id.HasValue).Select(id=>id.Value).Distinct().ToArray();
                        if(uniqueIdentities.Length==0)return new IHolon[identities.Length];
                        var byId=await ReadNativeCqlIdentitySnapshotsAsync(uniqueIdentities,
                            isLeft?leftStorageKeys:rightStorageKeys,limits.MaximumPayloadBytes-entityBytes,Check,cancellationToken,
                            payloadBytes=>entityBytes=checked(entityBytes+payloadBytes)).ConfigureAwait(false);
                        return identities.Select(id => id.HasValue ? byId[id.Value] : null).ToArray();
                    }

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

                    async Task<(Dictionary<long,CosmicJoinInputRow> Rows,long Count)> ReadSource<T>(
                        CosmicQuery<T> query,bool isLeft,string sql,int maximumRows,int maximumBytes) where T:IHolon
                    {
                        var observed=await ReadNativeCqlKeyedSourceAsync(query,sql,
                            plan.Query.Join.Keys.Select(key=>isLeft?key.Left:key.Right).ToArray(),
                            plan.Query.Columns.Where(column=>column.Source==(isLeft?CosmicJoinColumnSource.Left:CosmicJoinColumnSource.Right))
                                .Select(column=>column.Path).ToArray(),maximumRows,maximumBytes,
                            isLeft?leftStorageKeys:rightStorageKeys,Command,Check,cancellationToken).ConfigureAwait(false);
                        return (observed.Rows,observed.Count);
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
        if (bytes > maximum) throw new SqliteCqlResourceException("Native detached cells exceed their remaining byte budget.");
    }

    private static OASISResult<CosmicJoinPacket> JoinCqlError(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCode = code, Message = message, Exception = cause };
}
