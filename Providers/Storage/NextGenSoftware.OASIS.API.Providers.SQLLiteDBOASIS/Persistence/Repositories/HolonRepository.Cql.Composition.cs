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
    public async Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T, TResult>(
        CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        try
        {
            var admitted = CosmicProjectionCompositionPlanner.AdmitExecution(plan, ProviderType.SQLLiteDBOASIS,
                SqliteCqlContract.Capabilities, SQLLiteDBOASIS.CqlProjectionCapabilities, SQLLiteDBOASIS.CqlCompositionCapabilities, limits);
            if (admitted.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, CosmicProjectionCompositionPacket>(admitted);
            var compiled = CompileCompositionSql(plan, limits);
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
                    return CompositionError("CQL_CONSISTENCY_UNSUPPORTED", "SQLite composition requires one serializable read owner.");
                using var transaction = _dbContext.Database.CurrentTransaction == null
                    ? connection.BeginTransaction(IsolationLevel.Serializable, deferred: true) : null;
                using var owner = transaction == null ? null : _dbContext.Database.UseTransaction(transaction);
                try
                {
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
                    if (compiled.HasRelationship)
                    {
                        using var validation = Command("SELECT count(*) FROM Holon WHERE " + compiled.Predicate);
                        await validation.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false); Check();
                    }
                    var input = new Dictionary<long, CosmicProjectionInputRow>();
                    var identities = new HashSet<Guid>();
                    long inputBytes = 0, sourceCount = 0;
                    using (var command = Command("SELECT *,count(*) OVER () AS nativeCount FROM (" + compiled.Source + ") ORDER BY position"))
                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            Check();
                            var count = reader.GetInt64(reader.FieldCount - 1);
                            if (count > limits.MaximumInputRows) throw new SqliteCqlResourceException("Composition input exceeds its remaining row budget.");
                            if (input.Count > 0 && sourceCount != count) throw new InvalidDataException("Composition source count changed.");
                            sourceCount = count;
                            var position = reader.GetInt64(0);
                            var id = (Guid)DecodeProjectionCell(reader.GetValue(1), typeof(Guid));
                            var visible = (bool)DecodeProjectionCell(reader.GetValue(2), typeof(bool));
                            var creator = reader.IsDBNull(3) ? Guid.Empty : (Guid)DecodeProjectionCell(reader.GetValue(3), typeof(Guid));
                            if (id == Guid.Empty || position <= 0 || input.ContainsKey(position) || !identities.Add(id)
                                || plan.Query.Source.ReadAccess != null && !visible && creator != plan.Query.Source.ReadAccess.ActorId)
                                throw new InvalidDataException("Composition source identity/access proof is invalid.");
                            inputBytes = checked(inputBytes + 48); var offset = 4;
                            object Cell(Type declared, bool allowAbsent = false)
                            {
                                Check(); var state = reader.GetInt64(offset++); var raw = reader.GetValue(offset++);
                                if (state is < 0 or > 2) throw new InvalidDataException("Composition scalar path is ambiguous or invalid.");
                                var value = state == 2 ? DecodeProjectionCell(raw, declared) : null;
                                if (value == null && !allowAbsent && declared.IsValueType && Nullable.GetUnderlyingType(declared) == null)
                                    throw new InvalidDataException("A required composition source value is missing or null.");
                                Reserve(value, ref inputBytes, limits.MaximumInputPayloadBytes); return value;
                            }
                            var values = plan.Query.Columns.Select(column => Cell(column.Path.ValueType)).ToArray();
                            var states = new int[plan.Query.Source.Orderings.Count]; var ordering = new object[states.Length];
                            for (var index = 0; index < states.Length; index++)
                            { states[index] = checked((int)reader.GetInt64(offset)); ordering[index] = Cell(plan.Query.Source.Orderings[index].Path.ValueType, true); }
                            input.Add(position, new CosmicProjectionInputRow(id, visible, creator, values, ordering, states));
                        }
                    }
                    if (sourceCount != input.Count) throw new InvalidDataException("Composition source stream is incomplete.");
                    var stageIds = Enumerable.Range(0, plan.Query.ResultStages.Count).Select(_ => new List<Guid>()).ToArray();
                    var stagePositions = Enumerable.Range(0, stageIds.Length).Select(_ => new HashSet<long>()).ToArray();
                    var output = new List<CosmicProjectionCompositionRow>();
                    var header = false; long work = 0, proofBytes = 0, outputBytes = 0;
                    using (var command = Command(compiled.Proof))
                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            Check(); var phase = reader.GetInt64(0);
                            if (phase == -1)
                            {
                                if (header || reader.GetInt64(3) != sourceCount) throw new InvalidDataException("Composition proof header disagrees with its snapshot source.");
                                work = reader.GetInt64(4); header = true;
                                if (work > limits.MaximumStageRows) throw new SqliteCqlResourceException("Composition exceeds its remaining stage work.");
                                continue;
                            }
                            if (!header || phase < 0 || phase >= stageIds.Length) throw new InvalidDataException("Composition stage ordinal is invalid.");
                            var position = reader.GetInt64(1);
                            if (!input.TryGetValue(position, out var row) || !stagePositions[(int)phase].Add(position))
                                throw new InvalidDataException("Composition stage identity is missing or duplicated.");
                            proofBytes = checked(proofBytes + 16);
                            if (proofBytes > limits.MaximumProofPayloadBytes) throw new SqliteCqlResourceException("Composition proof exceeds its remaining byte budget.");
                            stageIds[(int)phase].Add(row.Id);
                            if (phase == stageIds.Length - 1)
                            {
                                if (output.Count >= limits.MaximumResults) throw new SqliteCqlResourceException("Composition exceeds its remaining result count.");
                                outputBytes = checked(outputBytes + 16);
                                foreach (var value in row.Values) { Check(); Reserve(value, ref outputBytes, limits.MaximumPayloadBytes); }
                                output.Add(new CosmicProjectionCompositionRow(row.Id, row.Values.ToArray()));
                            }
                        }
                    }
                    if (!header || work != sourceCount + stageIds.Take(stageIds.Length - 1).Sum(stage => (long)stage.Count))
                        throw new InvalidDataException("Composition proof work/header is incomplete.");
                    Check();
                    var packet = new CosmicProjectionCompositionPacket(input.Values.ToArray(),
                        stageIds.Select(ids => new CosmicProjectionStageProof(ids)).ToArray(), output, sourceCount, work);
                    transaction?.Commit(); Check();
                    return new OASISResult<CosmicProjectionCompositionPacket>(packet) { IsLoaded = true };
                }
                catch (SqliteException cause) when (cause.SqliteErrorCode == 9) { budget.Check(); throw; }
            }
            finally { if (opened) await _dbContext.Database.CloseConnectionAsync().ConfigureAwait(false); }
        }
        catch (InvalidDataException cause) { return CompositionError("CQL_DATA_INVALID", cause.Message, cause); }
        catch (OverflowException cause) { return CompositionError("CQL_DATA_INVALID", cause.Message, cause); }
        catch (SqliteCqlDeadlineException cause) { return CompositionError("CQL_QUERY_TIMEOUT", cause.Message, cause); }
        catch (SqliteCqlResourceException cause) { return CompositionError("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (CqlTranslationLimitException cause) { return CompositionError("CQL_QUERY_LIMIT", cause.Message, cause); }
        catch (NotSupportedException cause) { return CompositionError("CQL_CAPABILITY_UNSUPPORTED", cause.Message, cause); }
        catch (OperationCanceledException cause) { return CompositionError("CQL_QUERY_CANCELLED", cause.Message, cause); }
        catch (Exception cause) { return CompositionError("CQL_QUERY_FAILED", "SQLite native composition did not complete.", cause); }
    }
    private static OASISResult<CosmicProjectionCompositionPacket> CompositionError(string code, string message, Exception cause = null)
        => new() { IsError = true, ErrorCode = code, Message = message, Exception = cause };
}
