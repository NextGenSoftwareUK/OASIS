using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    private static readonly Encoding JoinUtf8 = new UTF8Encoding(false, true);

    internal async Task<CosmicJoinPacket> ExecuteNativeCqlJoinAsync<TLeft, TRight, TKey, TResult>(
        MongoCqlJoinCommand command, CosmicJoinSelection<TLeft, TRight, TKey, TResult> query,
        CosmicJoinExecutionLimits limits, CancellationToken cancellationToken) where TLeft : IHolon where TRight : IHolon
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
            if (command.Left.RequiresMembership || command.Right.RequiresMembership)
                await ValidateCqlMembershipAsync(session, Options, deadline.Token).ConfigureAwait(false);

            var left = await ReadSource(command.Left, query.Join.Left, true, limits.MaximumLeftRows,
                limits.MaximumLeftPayloadBytes).ConfigureAwait(false);
            var right = await ReadSource(command.Right, query.Join.Right, false, limits.MaximumRightRows,
                limits.MaximumRightPayloadBytes).ConfigureAwait(false);
            var rows = new List<CosmicJoinRow>(); long bytes = 0, pairCount = 0;
            using (var cursor = await _dbContext.Holon.AggregateAsync<BsonDocument>(session, command.Pairs,
                Options(), deadline.Token).ConfigureAwait(false))
                while (await cursor.MoveNextAsync(deadline.Token).ConfigureAwait(false))
                    foreach (var data in cursor.Current)
                    {
                        deadline.Token.ThrowIfCancellationRequested();
                        var reported = JoinCount(data);
                        if (reported > limits.MaximumPairs || reported > limits.MaximumResults
                            || rows.Count >= limits.MaximumPairs || rows.Count >= limits.MaximumResults) throw new MongoCqlLimitException();
                        if (rows.Count > 0 && pairCount != reported) throw new InvalidDataException("Native join pair count changed.");
                        pairCount = reported;
                        if (data.ElementCount != 3 + query.Join.Left.Orderings.Count + query.Join.Right.Orderings.Count
                            || !data.TryGetValue("left", out var leftId) || !data.TryGetValue("right", out var rightId))
                            throw new InvalidDataException("Native join pair shape is invalid.");
                        var id = (Guid)DecodeNativeProjectionCell(leftId, typeof(Guid));
                        if (!left.Rows.TryGetValue(id, out var l)) throw new InvalidDataException("Native join left identity is outside its source.");
                        CosmicJoinInputRow r = null;
                        if (!rightId.IsBsonNull && !right.Rows.TryGetValue((Guid)DecodeNativeProjectionCell(rightId, typeof(Guid)), out r))
                            throw new InvalidDataException("Native join right identity is outside its source.");
                        var values = new object[query.Columns.Count]; var li = 0; var ri = 0;
                        bytes = checked(bytes + 48);
                        for (var index = 0; index < values.Length; index++)
                        {
                            var column = query.Columns[index];
                            if (column.Source == CosmicJoinColumnSource.Left) values[index] = l.Values[li++];
                            else if (column.Source == CosmicJoinColumnSource.Right)
                            { values[index] = r == null ? null : r.Values[ri]; ri++; }
                            else values[index] = column.PresenceWhenMissing ? r == null : r != null;
                            ReserveJoinCell(values[index], ref bytes, limits.MaximumPayloadBytes);
                        }
                        rows.Add(new CosmicJoinRow(l.Id, r?.Id, values));
                    }
            deadline.Token.ThrowIfCancellationRequested();
            if (pairCount != rows.Count) throw new InvalidDataException("Native join pair count disagrees with its complete stream.");
            return new CosmicJoinPacket(left.Rows.Values.ToArray(), right.Rows.Values.ToArray(), rows.ToArray(), left.Count, right.Count, pairCount);

            async Task<(Dictionary<Guid, CosmicJoinInputRow> Rows, long Count)> ReadSource<T>(
                MongoCqlProjectionCommand source, CosmicQuery<T> intent, bool isLeft, int maximumRows, int maximumBytes) where T : IHolon
            {
                var result = new Dictionary<Guid, CosmicJoinInputRow>(); long measured = 0, count = 0;
                var paths = query.Join.Keys.Select(key => isLeft ? key.Left : key.Right).ToArray();
                var columns = query.Columns.Where(column => column.Source ==
                    (isLeft ? CosmicJoinColumnSource.Left : CosmicJoinColumnSource.Right)).ToArray();
                using var cursor = await _dbContext.Holon.AggregateAsync<BsonDocument>(session, source.Pipeline,
                    Options(), deadline.Token).ConfigureAwait(false);
                while (await cursor.MoveNextAsync(deadline.Token).ConfigureAwait(false))
                    foreach (var document in cursor.Current)
                    {
                        deadline.Token.ThrowIfCancellationRequested();
                        var data = UnwrapNativeSelectedScalar(document, source.HasTraversalEnvelope);
                        var reported = JoinCount(data);
                        if (reported > maximumRows || result.Count >= maximumRows) throw new MongoCqlLimitException();
                        if (result.Count > 0 && count != reported) throw new InvalidDataException("Native join source count changed.");
                        count = reported;
                        if (data.ElementCount != 7 + paths.Length + intent.Orderings.Count
                            || !data.TryGetValue("id", out var idCell) || !data.TryGetValue("visible", out var visibleCell)
                            || !data.TryGetValue("creator", out var creatorCell))
                            throw new InvalidDataException("Native join source shape is invalid.");
                        var id = (Guid)DecodeNativeProjectionCell(idCell, typeof(Guid));
                        var visible = (bool)DecodeNativeProjectionCell(visibleCell, typeof(bool));
                        var creator = (Guid)DecodeNativeProjectionCell(creatorCell, typeof(Guid));
                        if (id == Guid.Empty || result.ContainsKey(id)
                            || intent.ReadAccess != null && !visible && creator != intent.ReadAccess.ActorId)
                            throw new InvalidDataException("Native join source identity/access proof is invalid.");
                        measured = checked(measured + 48);
                        var keys = Cells("keys", paths.Select(path => path.ValueType).ToArray(), false, out _);
                        var values = Cells("values", columns.Select(column => column.Path.ValueType).ToArray(), false, out _);
                        var order = Cells("ordering", intent.Orderings.Select(item => item.Path.ValueType).ToArray(), true, out var states);
                        if (measured > maximumBytes) throw new MongoCqlLimitException();
                        result.Add(id, new CosmicJoinInputRow(id, visible, creator, keys, values, order, states));

                        object[] Cells(string name, Type[] domains, bool allowAbsent, out int[] states)
                        {
                            if (!data.TryGetValue(name, out var cells) || !cells.IsBsonArray || cells.AsBsonArray.Count != domains.Length)
                                throw new InvalidDataException("Native join source cells do not match their selected shape.");
                            var decoded = new object[domains.Length]; states = new int[domains.Length];
                            for (var index = 0; index < decoded.Length; index++)
                            {
                                deadline.Token.ThrowIfCancellationRequested();
                                if (!cells[index].IsBsonDocument) throw new InvalidDataException("Native join source cell is invalid.");
                                var cell = cells[index].AsBsonDocument;
                                if (cell.ElementCount != 2 || !cell.TryGetValue("s", out var state) || !state.IsInt32
                                    || state.AsInt32 < 0 || state.AsInt32 > 2 || !cell.TryGetValue("v", out var value))
                                    throw new InvalidDataException("Native join source cell is ambiguous or has an invalid domain.");
                                states[index] = state.AsInt32;
                                var declared = domains[index];
                                if (state.AsInt32 < 2)
                                {
                                    if (!value.IsBsonNull || !allowAbsent && declared.IsValueType && Nullable.GetUnderlyingType(declared) == null)
                                        throw new InvalidDataException("Required native join source value is null or missing.");
                                }
                                else decoded[index] = DecodeNativeProjectionCell(value, Nullable.GetUnderlyingType(declared) ?? declared);
                                ReserveJoinCell(decoded[index], ref measured, maximumBytes);
                            }
                            return decoded;
                        }
                    }
                deadline.Token.ThrowIfCancellationRequested();
                if (count != result.Count) throw new InvalidDataException("Native join source count disagrees with its complete stream.");
                return (result, count);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        { throw new MongoCqlGraphTimeoutException(); }
    }

    private static long JoinCount(BsonDocument data)
    {
        if (!data.TryGetValue("nativeCount", out var count) || !(count.IsInt32 || count.IsInt64) || count.ToInt64() < 1)
            throw new InvalidDataException("Native join requires a positive exact count for every streamed row.");
        return count.ToInt64();
    }

    private static void ReserveJoinCell(object value, ref long bytes, int maximum)
    {
        bytes = checked(bytes + 16 + (value is string text ? JoinUtf8.GetByteCount(text) : 16));
        if (bytes > maximum) throw new MongoCqlLimitException();
    }
}
