using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal Task<long> ExecuteNativeCqlCountAsync(MongoCqlScalarCommand command, CancellationToken cancellationToken)
        => ExecuteNativeCqlScalarAsync(command, ReadNativeCqlCount, 0L, cancellationToken);

    private async Task<TValue> ExecuteNativeCqlScalarAsync<TValue>(MongoCqlScalarCommand command,
        Func<BsonDocument, TValue> decode, TValue empty, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (command.IsEmpty) return empty;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        AggregateOptions Options()
        {
            deadline.Token.ThrowIfCancellationRequested();
            var remaining = TimeSpan.FromSeconds(30) - elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero) throw new MongoCqlGraphTimeoutException();
            return new AggregateOptions { Collation = Collation.Simple, MaxTime = TimeSpan.FromMilliseconds(Math.Ceiling(remaining.TotalMilliseconds)),
                BatchSize = 1, AllowDiskUse = false };
        }
        try
        {
            using var session = command.RequiresSnapshot
                ? await _dbContext.MongoClient.StartSessionAsync(new ClientSessionOptions { Snapshot = true, CausalConsistency = false }, deadline.Token).ConfigureAwait(false)
                : null;
            if (command.RequiresMembership)
            {
                if (session == null) throw new InvalidOperationException("Scalar membership validation requires its snapshot owner.");
                await ValidateCqlMembershipAsync(session, Options, deadline.Token).ConfigureAwait(false);
            }
            using var cursor = session == null
                ? await _dbContext.Holon.AggregateAsync<BsonDocument>(command.Pipeline, Options(), deadline.Token).ConfigureAwait(false)
                : await _dbContext.Holon.AggregateAsync<BsonDocument>(session, command.Pipeline, Options(), deadline.Token).ConfigureAwait(false);
            var observed = false;
            var value = empty;
            while (await cursor.MoveNextAsync(deadline.Token).ConfigureAwait(false))
                foreach (var document in cursor.Current)
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    if (observed) throw new InvalidDataException("A scalar returned multiple native values.");
                    value = decode(document);
                    observed = true;
                }
            deadline.Token.ThrowIfCancellationRequested();
            return value;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        { throw new MongoCqlGraphTimeoutException(); }
    }

    internal static long ReadNativeCqlCount(BsonDocument document)
    {
        if (document.ElementCount != 2 || !document.TryGetValue("error", out var error) || !error.IsInt32
            || !document.TryGetValue("count", out var count) || !(count.IsInt32 || count.IsInt64))
            throw new InvalidDataException("Native scalar count requires an exact integer and a valid outcome envelope.");
        if (error.AsInt32 == 1) throw new InvalidDataException("MongoDB scalar traversal encountered a cycle or conflicting identity.");
        if (error.AsInt32 == 2) throw new MongoCqlLimitException();
        if (error.AsInt32 != 0 || count.ToInt64() < 0) throw new InvalidDataException("Native scalar count outcome is invalid.");
        return count.ToInt64();
    }
}
