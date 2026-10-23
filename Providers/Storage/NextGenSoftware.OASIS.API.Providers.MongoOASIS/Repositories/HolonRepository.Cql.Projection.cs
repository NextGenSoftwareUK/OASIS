using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    internal async Task<IReadOnlyList<CosmicProjectionRow>> ExecuteNativeCqlProjectionAsync(MongoCqlProjectionCommand command,
        IReadOnlyList<CosmicProjectionColumn> columns, int maximumResults, int maximumPayloadBytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (command.IsEmpty) return Array.Empty<CosmicProjectionRow>();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        AggregateOptions Options()
        {
            deadline.Token.ThrowIfCancellationRequested();
            var remaining = TimeSpan.FromSeconds(30) - elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero) throw new MongoCqlGraphTimeoutException();
            return new AggregateOptions { Collation = Collation.Simple, AllowDiskUse = false, BatchSize = 16,
                MaxTime = TimeSpan.FromMilliseconds(Math.Ceiling(remaining.TotalMilliseconds)) };
        }
        try
        {
            using var session = command.RequiresSnapshot
                ? await _dbContext.MongoClient.StartSessionAsync(new ClientSessionOptions { Snapshot = true, CausalConsistency = false }, deadline.Token).ConfigureAwait(false)
                : null;
            if (command.RequiresMembership)
            {
                if (session == null) throw new InvalidOperationException("Projection membership validation requires its snapshot owner.");
                await ValidateCqlMembershipAsync(session, Options, deadline.Token).ConfigureAwait(false);
            }
            using var cursor = session == null
                ? await _dbContext.Holon.AggregateAsync<BsonDocument>(command.Pipeline, Options(), deadline.Token).ConfigureAwait(false)
                : await _dbContext.Holon.AggregateAsync<BsonDocument>(session, command.Pipeline, Options(), deadline.Token).ConfigureAwait(false);
            var rows = new List<CosmicProjectionRow>(); long bytes = 0;
            while (await cursor.MoveNextAsync(deadline.Token).ConfigureAwait(false))
                foreach (var document in cursor.Current)
                {
                    deadline.Token.ThrowIfCancellationRequested();
                    var data = document;
                    if (command.HasTraversalEnvelope)
                    {
                        if (document.ElementCount != 2 || !document.TryGetValue("error", out var error) || !error.IsInt32
                            || !document.TryGetValue("row", out var wrapped))
                            throw new InvalidDataException("Native projection traversal envelope is invalid.");
                        if (error.AsInt32 == 1) throw new InvalidDataException("Projection traversal found a cycle or conflicting identity.");
                        if (error.AsInt32 == 2) throw new MongoCqlLimitException();
                        if (error.AsInt32 != 0 || !wrapped.IsBsonDocument) throw new InvalidDataException("Projection traversal outcome is invalid.");
                        data = wrapped.AsBsonDocument;
                    }
                    if (rows.Count >= maximumResults) throw new MongoCqlLimitException();
                    bytes += document.ToBson().Length;
                    if (bytes > maximumPayloadBytes) throw new MongoCqlLimitException();
                    rows.Add(ReadNativeCqlProjectionRow(data, columns));
                }
            deadline.Token.ThrowIfCancellationRequested();
            return rows.AsReadOnly();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && deadline.IsCancellationRequested)
        { throw new MongoCqlGraphTimeoutException(); }
    }

    internal static CosmicProjectionRow ReadNativeCqlProjectionRow(BsonDocument document, IReadOnlyList<CosmicProjectionColumn> columns)
    {
        if (document.ElementCount != 1 || !document.TryGetValue("cells", out var array) || !array.IsBsonArray
            || array.AsBsonArray.Count != columns.Count)
            throw new InvalidDataException("Native projection columns do not match the admitted shape.");
        var values = new object[columns.Count];
        for (var index = 0; index < columns.Count; index++)
        {
            if (!array[index].IsBsonDocument) throw new InvalidDataException("Native projection cell is invalid.");
            var cell = array[index].AsBsonDocument;
            if (cell.ElementCount != 2 || !cell.TryGetValue("s", out var state) || !state.IsInt32
                || !cell.TryGetValue("v", out var value) || state.AsInt32 < 0 || state.AsInt32 > 2)
                throw new InvalidDataException("Selected projection path is ambiguous or has an invalid domain.");
            var declared = columns[index].Path.ValueType;
            if (state.AsInt32 < 2)
            {
                if (!value.IsBsonNull || declared.IsValueType && Nullable.GetUnderlyingType(declared) == null)
                    throw new InvalidDataException("Required projection value is null or missing.");
                values[index] = null;
            }
            else values[index] = DecodeNativeProjectionCell(value, Nullable.GetUnderlyingType(declared) ?? declared);
        }
        return new CosmicProjectionRow(values);
    }

    private static object DecodeNativeProjectionCell(BsonValue value, Type type)
    {
        if (type == typeof(string) && value.IsString) return value.AsString;
        if (type == typeof(bool) && value.IsBoolean) return value.AsBoolean;
        if (type == typeof(Guid))
        {
            if (value.IsString && Guid.TryParseExact(value.AsString, "D", out var id)) return id;
            if (value.IsBsonBinaryData && value.AsBsonBinaryData.Bytes.Length == 16)
            {
                var binary = value.AsBsonBinaryData;
                if (binary.SubType == BsonBinarySubType.UuidStandard) return binary.ToGuid(GuidRepresentation.Standard);
                if (binary.SubType == BsonBinarySubType.UuidLegacy) return binary.ToGuid(GuidRepresentation.CSharpLegacy);
            }
        }
        if (type == typeof(DateTime) && value.IsInt64 && value.AsInt64 >= DateTime.MinValue.Ticks && value.AsInt64 <= DateTime.MaxValue.Ticks)
            return new DateTime(value.AsInt64, DateTimeKind.Utc);
        if (value.IsInt32 || value.IsInt64)
        {
            var integer = value.ToInt64();
            if (type.IsEnum)
            {
                var member = Convert.ChangeType(integer, Enum.GetUnderlyingType(type), CultureInfo.InvariantCulture);
                if (Enum.IsDefined(type, member)) return Enum.ToObject(type, member);
            }
            else if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort)
                || type == typeof(uint) || type == typeof(int) || type == typeof(long))
                return Convert.ChangeType(integer, type, CultureInfo.InvariantCulture);
        }
        throw new InvalidDataException("Selected native projection value does not match its exact CLR domain.");
    }
}
