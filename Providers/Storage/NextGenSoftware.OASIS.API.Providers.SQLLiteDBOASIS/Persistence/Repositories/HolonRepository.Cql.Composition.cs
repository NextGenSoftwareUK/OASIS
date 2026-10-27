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
    public Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteCosmicProjectionCompositionAsync<T, TResult>(
        CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken = default) where T : IHolon
        => ExecuteSelectedSnapshotAsync(plan, limits, cancellationToken);

    private sealed class AllReduction { internal long? Counterexamples; }

    public async Task<OASISResult<CosmicProjectedAllPacket>> ExecuteCosmicProjectedAllAsync<T, TResult>(
        CosmicProjectedAllPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        var reduction = new AllReduction();
        var result = await ExecuteSelectedSnapshotAsync<T, TResult>(null, limits, cancellationToken, plan, reduction).ConfigureAwait(false);
        if (result.IsError)
            return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<CosmicProjectionCompositionPacket, CosmicProjectedAllPacket>(result);
        if (!result.IsLoaded || !reduction.Counterexamples.HasValue)
            return new OASISResult<CosmicProjectedAllPacket> { IsError = true, ErrorCode = "CQL_PROVIDER_RESULT_INVALID",
                Message = "SQLite projected All did not acknowledge a complete native reduction." };
        return new OASISResult<CosmicProjectedAllPacket>(new CosmicProjectedAllPacket(result.Result, reduction.Counterexamples.Value)) { IsLoaded = true };
    }

    private async Task<OASISResult<CosmicProjectionCompositionPacket>> ExecuteSelectedSnapshotAsync<T, TResult>(
        CosmicProjectionCompositionPlan<T, TResult> plan, CosmicProjectionCompositionExecutionLimits limits,
        CancellationToken cancellationToken, CosmicProjectedAllPlan<T, TResult> all = null, AllReduction reduction = null) where T : IHolon
    {
        try
        {
            var admitted = all == null ? CosmicProjectionCompositionPlanner.AdmitExecution(plan, ProviderType.SQLLiteDBOASIS,
                SqliteCqlContract.Capabilities, SQLLiteDBOASIS.CqlProjectionCapabilities, SQLLiteDBOASIS.CqlCompositionCapabilities, limits)
                : CosmicProjectedAllPlanner.AdmitExecution(all, ProviderType.SQLLiteDBOASIS, SqliteCqlContract.Capabilities,
                    SQLLiteDBOASIS.CqlProjectionCapabilities, SQLLiteDBOASIS.CqlCompositionCapabilities, limits);
            if (admitted.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool, CosmicProjectionCompositionPacket>(admitted);
            var query = all == null ? plan.Query : all.Query.Projection;
            var compiled = all == null ? CompileCompositionSql(plan, limits)
                : CompileSelectedSql(query, all.Options, limits, all.Query.Predicate);
            cancellationToken.ThrowIfCancellationRequested();
            using var exclusive = _dbContext.GetService<IConcurrencyDetector>().EnterCriticalSection();
            var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
            var opened = connection.State != ConnectionState.Open;
            if (opened) await _dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var budget = new SqliteCqlReadBudget(connection, cancellationToken);
                using var checkedAverage = plan?.NumericReduction?.UsesInt64DoubleAverage == true
                    ? new SqliteCqlCheckedInt64Average(connection, budget.Check) : null;
                using var traversal = compiled.HasRelationship ? new SqliteCqlTraversalGuard(connection, budget.Check) : null;
                if (_dbContext.Database.CurrentTransaction != null
                    && _dbContext.Database.CurrentTransaction.GetDbTransaction().IsolationLevel != IsolationLevel.Serializable)
                    return CompositionError("CQL_CONSISTENCY_UNSUPPORTED", "SQLite composition requires one serializable read owner.");
                using var transaction = _dbContext.Database.CurrentTransaction == null
                    ? connection.BeginTransaction(IsolationLevel.Serializable, deferred: true) : null;
                using var owner = transaction == null ? null : _dbContext.Database.UseTransaction(transaction);
                try
                {
                    void Check() { budget.Check(); traversal?.Check(); checkedAverage?.Check(); }
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
                                || query.Source.ReadAccess != null && !query.Source.ReadAccess.CanRead(id,visible,creator))
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
                            var values = query.Columns.Select(column => Cell(column.Path.ValueType)).ToArray();
                            var states = new int[query.Source.Orderings.Count]; var ordering = new object[states.Length];
                            for (var index = 0; index < states.Length; index++)
                            { states[index] = checked((int)reader.GetInt64(offset)); ordering[index] = Cell(query.Source.Orderings[index].Path.ValueType, true); }
                            input.Add(position, new CosmicProjectionInputRow(id, visible, creator, values, ordering, states));
                        }
                    }
                    if (sourceCount != input.Count) throw new InvalidDataException("Composition source stream is incomplete.");
                    var stageIds = Enumerable.Range(0, query.ResultStages.Count).Select(_ => new List<Guid>()).ToArray();
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
                    if (!header || work != (stageIds.Length == 0 ? 0 : sourceCount + stageIds.Take(stageIds.Length - 1).Sum(stage => (long)stage.Count)))
                        throw new InvalidDataException("Composition proof work/header is incomplete.");
                    if (stageIds.Length == 0 && (all != null || plan?.IsScalarTerminal == true))
                    {
                            foreach (var row in input.Values)
                            {
                                Check();
                                if (output.Count >= limits.MaximumResults) throw new SqliteCqlResourceException("All evidence exceeds its row budget.");
                                outputBytes = checked(outputBytes + 16);
                                foreach (var value in row.Values) Reserve(value, ref outputBytes, limits.MaximumPayloadBytes);
                                output.Add(new CosmicProjectionCompositionRow(row.Id, row.Values.ToArray()));
                            }
                    }
                    if (all != null)
                    {
                        if (checked(work + output.Count) > limits.MaximumStageRows
                            || checked(proofBytes + outputBytes) > limits.MaximumProofPayloadBytes)
                            throw new SqliteCqlResourceException("All reduction/evidence exceeds its remaining work budget.");
                        using var command = Command(compiled.Reduction);
                        var counterexamples = (long)await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        Check();
                        if (counterexamples < 0 || counterexamples > output.Count)
                            throw new InvalidDataException("All reduction disagrees with its complete selected sequence.");
                        reduction.Counterexamples = counterexamples;
                    }
                    long? nativeSelectedCount = null;
                    if (plan?.RequestsCardinality == true)
                    {
                        if (checked(work + output.Count) > limits.MaximumStageRows
                            || checked(proofBytes + outputBytes) > limits.MaximumProofPayloadBytes)
                            throw new SqliteCqlResourceException("Selected cardinality exceeds its remaining work/proof budget.");
                        using var countCommand = Command(compiled.Cardinality);
                        var countValue = await countCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                        Check();
                        if (countValue is not long acknowledged || acknowledged < 0 || acknowledged != output.Count)
                            throw new InvalidDataException("Native selected count disagrees with its complete snapshot proof.");
                        nativeSelectedCount = acknowledged;
                    }
                    CosmicProjectedNumericPacket nativeNumeric = null;
                    if (plan?.NumericReduction != null)
                    {
                        var numeric = plan.NumericReduction;
                        var packetBytes = 25;
                        if (checked(work + output.Count) > limits.MaximumStageRows
                            || checked(proofBytes + outputBytes + packetBytes) > limits.MaximumProofPayloadBytes)
                            throw new SqliteCqlResourceException("Selected numeric reduction exceeds its remaining work/proof budget.");
                        using var numericCommand = Command(compiled.Numeric);
                        await using var reader = await numericCommand.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
                            || reader.FieldCount != (numeric.UsesInt64DoubleAverage ? 4 : 3))
                            throw new InvalidDataException("Selected numeric reduction requires one exact native packet.");
                        Check();
                        bool? prefixOverflow = null;
                        if (numeric.UsesInt64DoubleAverage)
                        {
                            var flag = reader.GetInt64(3);
                            if (flag is not (0 or 1)) throw new InvalidDataException("Checked numeric reduction requires its exact prefix acknowledgement.");
                            prefixOverflow = flag == 1;
                        }
                        nativeNumeric = new CosmicProjectedNumericPacket(reader.GetInt64(0),
                            new[] { reader.GetInt64(1), reader.GetInt64(2) }, prefixOverflow);
                        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                            throw new InvalidDataException("Selected numeric reduction returned multiple packets.");
                        Check();
                    }
                    Check();
                    var packet = new CosmicProjectionCompositionPacket(input.Values.ToArray(),
                        stageIds.Select(ids => new CosmicProjectionStageProof(ids)).ToArray(), output, sourceCount, work, nativeSelectedCount, nativeNumeric);
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
