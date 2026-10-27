using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task<CosmicProjectionCompositionPacket> ExecuteNativeCqlCompositionAsync<T, TResult>(
        MongoCqlCompositionCommand command, CosmicProjectionQuery<T, TResult> query,
        CosmicProjectionCompositionExecutionLimits limits, CancellationToken cancellationToken) where T : IHolon
        => (await ExecuteNativeSelectedSnapshotAsync(command, query, limits, cancellationToken).ConfigureAwait(false)).Proof;

    internal async Task<CosmicProjectedAllPacket> ExecuteNativeCqlProjectedAllAsync<T, TResult>(
        MongoCqlCompositionCommand command, CosmicProjectionQuery<T, TResult> query,
        CosmicProjectionCompositionExecutionLimits limits, CancellationToken cancellationToken) where T : IHolon
    {
        if (command.Reduction == null) throw new InvalidDataException("Projected All requires a native reduction command.");
        var result = await ExecuteNativeSelectedSnapshotAsync(command, query, limits, cancellationToken).ConfigureAwait(false);
        if (!result.Counterexamples.HasValue) throw new InvalidDataException("Projected All did not acknowledge its native reduction.");
        return new CosmicProjectedAllPacket(result.Proof, result.Counterexamples.Value);
    }

    private async Task<(CosmicProjectionCompositionPacket Proof, long? Counterexamples)> ExecuteNativeSelectedSnapshotAsync<T, TResult>(
        MongoCqlCompositionCommand command, CosmicProjectionQuery<T, TResult> query,
        CosmicProjectionCompositionExecutionLimits limits, CancellationToken cancellationToken) where T : IHolon
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        AggregateOptions Options()
        {
            deadline.Token.ThrowIfCancellationRequested();
            var remaining = TimeSpan.FromSeconds(30) - elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero) throw new MongoCqlGraphTimeoutException();
            return new AggregateOptions { Collation = Collation.Simple, AllowDiskUse = false, BatchSize = 1,
                MaxTime = TimeSpan.FromMilliseconds(Math.Ceiling(remaining.TotalMilliseconds)) };
        }
        try
        {
            using var session = await _dbContext.MongoClient.StartSessionAsync(
                new ClientSessionOptions { Snapshot = true, CausalConsistency = false }, deadline.Token).ConfigureAwait(false);
            if (command.Source.RequiresMembership)
                await ValidateCqlMembershipAsync(session, Options, deadline.Token).ConfigureAwait(false);

            // Read and validate the entire source before a later filter/window can
            // hide a malformed required cell, access violation or overflow row.
            var input = new List<CosmicProjectionInputRow>();
            var byId = new Dictionary<Guid, CosmicProjectionInputRow>();
            long inputBytes = 0, sourceCount = 0;
            using (var cursor = await _dbContext.Holon.AggregateAsync<BsonDocument>(session, command.Source.Pipeline,
                Options(), deadline.Token).ConfigureAwait(false))
                while (await cursor.MoveNextAsync(deadline.Token).ConfigureAwait(false))
                    foreach (var document in cursor.Current)
                    {
                        deadline.Token.ThrowIfCancellationRequested();
                        var data = UnwrapNativeSelectedScalar(document, command.Source.HasTraversalEnvelope);
                        var count = CompositionCount(data, "nativeCount");
                        if (count > limits.MaximumInputRows || input.Count >= limits.MaximumInputRows) throw new MongoCqlLimitException();
                        if (input.Count > 0 && count != sourceCount) throw new InvalidDataException("Composition source count changed.");
                        sourceCount = count;
                        if (data.ElementCount != 7 + query.Columns.Count + query.Source.Orderings.Count
                            || !data.TryGetValue("id", out var rawId) || !data.TryGetValue("visible", out var rawVisible)
                            || !data.TryGetValue("creator", out var rawCreator))
                            throw new InvalidDataException("Composition source proof shape is invalid.");
                        var id = (Guid)DecodeNativeProjectionCell(rawId, typeof(Guid));
                        var visible = (bool)DecodeNativeProjectionCell(rawVisible, typeof(bool));
                        var creator = (Guid)DecodeNativeProjectionCell(rawCreator, typeof(Guid));
                        if (id == Guid.Empty || byId.ContainsKey(id) || CompositionCount(data, "rank") != input.Count + 1
                            || query.Source.ReadAccess != null && !query.Source.ReadAccess.CanRead(id,visible,creator))
                            throw new InvalidDataException("Composition source identity/access/rank proof is invalid.");
                        inputBytes = checked(inputBytes + 48);
                        var values = Cells("values", query.Columns.Select(column => column.Path.ValueType).ToArray(), false, out _);
                        var ordering = Cells("ordering", query.Source.Orderings.Select(item => item.Path.ValueType).ToArray(), true, out var states);
                        if (inputBytes > limits.MaximumInputPayloadBytes) throw new MongoCqlLimitException();
                        var row = new CosmicProjectionInputRow(id, visible, creator, values, ordering, states);
                        input.Add(row); byId.Add(id, row);

                        object[] Cells(string name, Type[] domains, bool allowAbsent, out int[] states)
                        {
                            if (!data.TryGetValue(name, out var array) || !array.IsBsonArray || array.AsBsonArray.Count != domains.Length)
                                throw new InvalidDataException("Composition proof cells do not match their selected shape.");
                            var result = new object[domains.Length]; states = new int[domains.Length];
                            for (var index = 0; index < result.Length; index++)
                            {
                                deadline.Token.ThrowIfCancellationRequested();
                                if (!array[index].IsBsonDocument) throw new InvalidDataException("Composition proof cell is invalid.");
                                var cell = array[index].AsBsonDocument;
                                if (cell.ElementCount != 2 || !cell.TryGetValue("s", out var state) || !state.IsInt32
                                    || state.AsInt32 < 0 || state.AsInt32 > 2 || !cell.TryGetValue("v", out var value))
                                    throw new InvalidDataException("Composition proof cell is ambiguous or invalid.");
                                states[index] = state.AsInt32;
                                var declared = domains[index];
                                if (state.AsInt32 < 2)
                                {
                                    if (!value.IsBsonNull || !allowAbsent && declared.IsValueType && Nullable.GetUnderlyingType(declared) == null)
                                        throw new InvalidDataException("A required composition source value is missing or null.");
                                }
                                else result[index] = DecodeNativeProjectionCell(value, Nullable.GetUnderlyingType(declared) ?? declared);
                                ReserveJoinCell(result[index], ref inputBytes, limits.MaximumInputPayloadBytes);
                            }
                            return result;
                        }
                    }
            if (sourceCount != input.Count) throw new InvalidDataException("Composition source count disagrees with its complete stream.");

            var stages = new List<CosmicProjectionStageProof>();
            var current = input.Select(row => row.Id).ToList();
            long work = 0, proofBytes = 0;
            foreach (var pipeline in command.Stages)
            {
                deadline.Token.ThrowIfCancellationRequested();
                work = checked(work + current.Count);
                if (work > limits.MaximumStageRows) throw new MongoCqlLimitException();
                var next = new List<Guid>(); var seen = new HashSet<Guid>();
                long count = 0, priorRank = 0;
                using var cursor = await _dbContext.Holon.AggregateAsync<BsonDocument>(session, pipeline,
                    Options(), deadline.Token).ConfigureAwait(false);
                while (await cursor.MoveNextAsync(deadline.Token).ConfigureAwait(false))
                    foreach (var data in cursor.Current)
                    {
                        deadline.Token.ThrowIfCancellationRequested();
                        var reported = CompositionCount(data, "stageCount");
                        if (data.ElementCount != 3 || !data.TryGetValue("id", out var rawId))
                            throw new InvalidDataException("Composition stage proof shape is invalid.");
                        var id = (Guid)DecodeNativeProjectionCell(rawId, typeof(Guid));
                        var rank = CompositionCount(data, "rank");
                        if (!byId.ContainsKey(id) || !seen.Add(id) || rank <= priorRank || next.Count > 0 && count != reported)
                            throw new InvalidDataException("Composition stage identity/count/rank is invalid.");
                        count = reported; priorRank = rank;
                        proofBytes = checked(proofBytes + 16);
                        if (proofBytes > limits.MaximumProofPayloadBytes || next.Count >= input.Count) throw new MongoCqlLimitException();
                        next.Add(id);
                    }
                if (count != next.Count) throw new InvalidDataException("Composition stage count disagrees with its complete stream.");
                stages.Add(new CosmicProjectionStageProof(next)); current = next;
            }
            if (current.Count > limits.MaximumResults) throw new MongoCqlLimitException();
            long outputBytes = 0;
            var output = new List<CosmicProjectionCompositionRow>();
            foreach (var id in current)
            {
                deadline.Token.ThrowIfCancellationRequested();
                if (command.Reduction != null || command.Cardinality != null) outputBytes = checked(outputBytes + 16);
                var values = byId[id].Values.ToArray();
                foreach (var value in values) ReserveJoinCell(value, ref outputBytes, limits.MaximumPayloadBytes);
                output.Add(new CosmicProjectionCompositionRow(id, values));
            }
            long? counterexamples = null;
            if (command.Reduction != null)
            {
                if (checked(work + current.Count) > limits.MaximumStageRows
                    || checked(proofBytes + outputBytes) > limits.MaximumProofPayloadBytes)
                    throw new MongoCqlLimitException();
                var seen = false; long count = 0;
                using var cursor = await _dbContext.Holon.AggregateAsync<BsonDocument>(session, command.Reduction,
                    Options(), deadline.Token).ConfigureAwait(false);
                while (await cursor.MoveNextAsync(deadline.Token).ConfigureAwait(false))
                    foreach (var document in cursor.Current)
                    {
                        deadline.Token.ThrowIfCancellationRequested();
                        if (seen || document.ElementCount != 3 || !document.TryGetValue("_id", out var key) || !key.IsBsonNull
                            || !document.TryGetValue("selectedCount", out var selected) || !(selected.IsInt32 || selected.IsInt64)
                            || selected.ToInt64() != current.Count
                            || !document.TryGetValue("counterexamples", out var value) || !(value.IsInt32 || value.IsInt64)
                            || value.ToInt64() < 0 || value.ToInt64() > current.Count)
                            throw new InvalidDataException("Native All count disagrees with its complete selected sequence.");
                        seen = true; count = value.ToInt64();
                    }
                if (!seen && current.Count != 0) throw new InvalidDataException("Native All reduction is incomplete.");
                counterexamples = count;
            }
            deadline.Token.ThrowIfCancellationRequested();
            long? nativeSelectedCount = null;
            if (command.Cardinality != null)
            {
                if (checked(work + current.Count) > limits.MaximumStageRows
                    || checked(proofBytes + outputBytes) > limits.MaximumProofPayloadBytes)
                    throw new MongoCqlLimitException();
                using var countCursor = await _dbContext.Holon.AggregateAsync<BsonDocument>(session, command.Cardinality,
                    Options(), deadline.Token).ConfigureAwait(false);
                while (await countCursor.MoveNextAsync(deadline.Token).ConfigureAwait(false))
                    foreach (var document in countCursor.Current)
                    {
                        deadline.Token.ThrowIfCancellationRequested();
                        if (nativeSelectedCount.HasValue || document.ElementCount != 1)
                            throw new InvalidDataException("Native cardinality requires one exact acknowledgement.");
                        nativeSelectedCount = CompositionCount(document, "selectedCount", allowZero: true);
                    }
                if (!nativeSelectedCount.HasValue || nativeSelectedCount.Value != output.Count)
                    throw new InvalidDataException("Native selected count disagrees with its complete snapshot proof.");
            }
            return (new CosmicProjectionCompositionPacket(input, stages, output, sourceCount, work, nativeSelectedCount), counterexamples);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        { throw new MongoCqlGraphTimeoutException(); }
    }

    private static long CompositionCount(BsonDocument data, string name, bool allowZero = false)
    {
        if (!data.TryGetValue(name, out var value) || !(value.IsInt32 || value.IsInt64) || value.ToInt64() < (allowZero ? 0 : 1))
            throw new InvalidDataException("Composition proof requires positive exact counts and ranks.");
        return value.ToInt64();
    }
}
