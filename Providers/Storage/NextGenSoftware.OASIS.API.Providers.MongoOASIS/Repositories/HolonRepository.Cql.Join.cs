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
            if (query.Join.RestrictsRightToLeftParents || command.Left.RequiresMembership || command.Right.RequiresMembership)
                await ValidateCqlMembershipAsync(session, Options, deadline.Token).ConfigureAwait(false);

            var left = await ReadSource(command.Left, query.Join.Left, true, limits.MaximumLeftRows,
                limits.MaximumLeftPayloadBytes).ConfigureAwait(false);
            if (query.Join.RestrictsRightToLeftParents)
                command = new MongoCqlTranslator().CompileJoin(query, limits, left.Rows.Keys.ToArray());
            var right = await ReadSource(command.Right, query.Join.Right, false, limits.MaximumRightRows,
                limits.MaximumRightPayloadBytes).ConfigureAwait(false);
            var rows = new List<CosmicJoinRow>(); long bytes = 0, pairCount = 0;
            var receivedFacet = false;
            using (var cursor = await _dbContext.Holon.AggregateAsync<BsonDocument>(session, command.Pairs,
                Options(), deadline.Token).ConfigureAwait(false))
                while (await cursor.MoveNextAsync(deadline.Token).ConfigureAwait(false))
                    foreach (var data in cursor.Current)
                    {
                        deadline.Token.ThrowIfCancellationRequested();
                        if (query.OutputLimit.HasValue)
                        {
                            if (receivedFacet || data.ElementCount != 2 ||
                                !data.TryGetValue("total", out var total) || !total.IsBsonArray ||
                                !data.TryGetValue("page", out var page) || !page.IsBsonArray)
                                throw new InvalidDataException("Native join page proof shape is invalid.");
                            receivedFacet = true;
                            var proof = total.AsBsonArray;
                            if (proof.Count > 1 || proof.Count == 1 && (!proof[0].IsBsonDocument ||
                                !proof[0].AsBsonDocument.TryGetValue("count", out var count) ||
                                !(count.IsInt32 || count.IsInt64) || count.ToInt64() < 1))
                                throw new InvalidDataException("Native join page total is invalid.");
                            pairCount = proof.Count == 0 ? 0 : proof[0].AsBsonDocument["count"].ToInt64();
                            if (pairCount > limits.MaximumPairs) throw new MongoCqlLimitException();
                            foreach (var pair in page.AsBsonArray)
                            {
                                deadline.Token.ThrowIfCancellationRequested();
                                if (!pair.IsBsonDocument) throw new InvalidDataException("Native join page pair shape is invalid.");
                                AddPair(pair.AsBsonDocument, paged: true);
                            }
                        }
                        else
                        {
                            var reported = JoinCount(data);
                            if (reported > limits.MaximumPairs || reported > limits.MaximumResults)
                                throw new MongoCqlLimitException();
                            if (rows.Count > 0 && pairCount != reported) throw new InvalidDataException("Native join pair count changed.");
                            pairCount = reported;
                            AddPair(data, paged: false);
                        }
                    }
            deadline.Token.ThrowIfCancellationRequested();
            if (query.OutputLimit.HasValue ? !receivedFacet : pairCount != rows.Count)
                throw new InvalidDataException("Native join pair count disagrees with its complete stream.");
            var leftEntities = query.SelectsLeftEntity ? await ReadSelectedEntities(true).ConfigureAwait(false) : null;
            var rightEntities = query.SelectsChildEntity ? await ReadSelectedEntities(false).ConfigureAwait(false) : null;
            return new CosmicJoinPacket(left.Rows.Values.ToArray(), right.Rows.Values.ToArray(), rows.ToArray(),
                left.Count, right.Count, pairCount, rightEntities, leftEntities);

            async Task<IHolon[]> ReadSelectedEntities(bool isLeft)
            {
                var identities = rows.Select(row => isLeft ? (Guid?)row.LeftId : row.RightId).ToArray();
                if(isLeft && identities.Any(id=>!id.HasValue || id.Value==Guid.Empty))
                    throw new InvalidDataException("A selected left entity requires its preserved native identity.");
                var uniqueIdentities = identities.Where(id=>id.HasValue).Select(id=>id.Value).Distinct().ToArray();
                var scope = isLeft ? query.Join.Left.Scope : query.Join.Right.Scope;
                var access = isLeft ? query.Join.Left.ReadAccess : query.Join.Right.ReadAccess;
                var byId=await ReadNativeCqlIdentitySnapshotsAsync(uniqueIdentities,scope,access,
                    checked((int)(limits.MaximumPayloadBytes-bytes)),session,Options,deadline.Token,
                    payloadBytes=>bytes=checked(bytes+payloadBytes)).ConfigureAwait(false);
                return identities.Select(id => id.HasValue ? byId[id.Value] : null).ToArray();
            }

            void AddPair(BsonDocument data, bool paged)
            {
                if (rows.Count >= limits.MaximumPairs || rows.Count >= limits.MaximumResults)
                    throw new MongoCqlLimitException();
                if (data.ElementCount != (paged ? 2 : 3 + query.Join.Left.Orderings.Count + query.Join.Right.Orderings.Count)
                    || !data.TryGetValue("left", out var leftId) || !data.TryGetValue("right", out var rightId))
                    throw new InvalidDataException("Native join pair shape is invalid.");
                CosmicJoinInputRow l = null;
                if (leftId.IsBsonNull)
                {
                    if (query.Join.Kind != CosmicJoinKind.Full || rightId.IsBsonNull)
                        throw new InvalidDataException("Native join cannot omit its left identity.");
                }
                else if (!left.Rows.TryGetValue((Guid)DecodeNativeProjectionCell(leftId, typeof(Guid)), out l))
                    throw new InvalidDataException("Native join left identity is outside its source.");
                CosmicJoinInputRow r = null;
                if (!rightId.IsBsonNull && !right.Rows.TryGetValue((Guid)DecodeNativeProjectionCell(rightId, typeof(Guid)), out r))
                    throw new InvalidDataException("Native join right identity is outside its source.");
                var values = new object[query.Columns.Count]; var li = 0; var ri = 0;
                bytes = checked(bytes + 48);
                for (var index = 0; index < values.Length; index++)
                {
                    var column = query.Columns[index];
                    if (column.Source == CosmicJoinColumnSource.Left) { values[index] = l?.Values[li]; li++; }
                    else if (column.Source == CosmicJoinColumnSource.Right)
                    { values[index] = r == null ? null : r.Values[ri]; ri++; }
                    else values[index] = column.PresenceWhenMissing ? r == null : r != null;
                    ReserveJoinCell(values[index], ref bytes, limits.MaximumPayloadBytes);
                }
                rows.Add(new CosmicJoinRow(l?.Id ?? Guid.Empty, r?.Id, values));
            }

            async Task<(Dictionary<Guid,CosmicJoinInputRow> Rows,long Count)> ReadSource<T>(
                MongoCqlProjectionCommand source,CosmicQuery<T> intent,bool isLeft,int maximumRows,int maximumBytes) where T:IHolon
            {
                var observed=await ReadNativeCqlKeyedSourceAsync(source,intent,
                    query.Join.Keys.Select(key=>isLeft?key.Left:key.Right).ToArray(),
                    query.Columns.Where(column=>column.Source==(isLeft?CosmicJoinColumnSource.Left:CosmicJoinColumnSource.Right))
                        .Select(column=>column.Path).ToArray(),
                    maximumRows,maximumBytes,session,Options,deadline.Token).ConfigureAwait(false);
                return (observed.Rows,observed.Count);
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
