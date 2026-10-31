using System;
using System.IO;
using System.Numerics;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

// SQLite executes these aggregates over its admitted source/window. Each group
// retains only ten/seventy integer sums; no source rows or holons are buffered.
internal sealed class SqliteCqlFloatingAggregates : IDisposable
{
    internal const string SingleFunction = "cosmic_cql_single_parts";
    internal const string DoubleFunction = "cosmic_cql_double_parts";
    internal const string KeyFunction = "cosmic_cql_floating_aggregate_key";
    private readonly SqliteConnection connection;
    private readonly Action checkBudget;
    private string failureCode, failureMessage;
    private Exception budgetFailure;
    private bool disposed;

    private sealed class State
    {
        internal readonly long[] Parts;
        internal long Count;
        internal State(bool single) => Parts = new long[Width(single)];
    }

    internal SqliteCqlFloatingAggregates(SqliteConnection connection, Action checkBudget)
    {
        if (connection.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Floating aggregates require an open owned connection.");
        this.connection = connection;
        this.checkBudget = checkBudget;
        Register(true); Register(false);
        connection.CreateFunction<byte[], int, int, string>(KeyFunction, (packet, mode, domain) =>
        {
            if (!WithinBudget()) return null;
            if (mode < 0 || mode > 3 || domain < 0 || domain > 1)
                return Fail("CQL_DATA_INVALID", "Floating aggregate key requires its declared operation/domain.");
            var single = domain == 0;
            var decoded = ReadPacket(packet, single);
            if (decoded.IsError) return Fail(decoded.ErrorCode, decoded.Message);
            var result = CosmicFloatingAggregateResults.FromParts(
                mode < 2 ? CosmicScalarOperation.Sum : CosmicScalarOperation.Average,
                InputType(single, mode % 2 == 1), decoded.Result.Parts, decoded.Result.Count);
            if (result.IsError) return Fail(result.ErrorCode, result.Message);
            return result.Result == null ? null : single
                ? CosmicFloatingScalarResults.SingleOrderKey((float)result.Result)
                : CosmicFloatingScalarResults.DoubleOrderKey((double)result.Result);
        }, isDeterministic: false);
    }

    private static int Width(bool single) => single
        ? CosmicFloatingAggregateResults.SingleLimbCount : CosmicFloatingAggregateResults.DoubleLimbCount;
    private static Type InputType(bool single, bool optional) => single
        ? (optional ? typeof(float?) : typeof(float)) : (optional ? typeof(double?) : typeof(double));

    private void Register(bool single)
        => connection.CreateAggregate<string, State, byte[]>(single ? SingleFunction : DoubleFunction,
            seed: null, func: (state, key) => Accumulate(state, key, single),
            resultSelector: state => WritePacket(state, single), isDeterministic: false);

    private State Accumulate(State state, string key, bool single)
    {
        if (!WithinBudget() || key == null) return state;
        if (state != null && state.Count >= CosmicNumericScalarResults.MaximumParticipatingValues)
        {
            Fail("CQL_QUERY_LIMIT", "Floating reduction exceeds its participating-input budget.");
            return state;
        }
        ulong bits;
        if (single)
        {
            var value = CosmicFloatingScalarResults.SingleFromOrderKey(key);
            if (value.IsError) { Fail(value.ErrorCode, value.Message); return state; }
            bits = unchecked((uint)BitConverter.SingleToInt32Bits(value.Result));
        }
        else
        {
            var value = CosmicFloatingScalarResults.DoubleFromOrderKey(key);
            if (value.IsError) { Fail(value.ErrorCode, value.Message); return state; }
            bits = unchecked((ulong)BitConverter.DoubleToInt64Bits(value.Result));
        }

        state ??= new State(single);
        var sign = single ? 1UL << 31 : 1UL << 63;
        var magnitude = bits & ~sign;
        var fractionBits = single ? 23 : 52;
        var exponent = (int)(magnitude >> fractionBits);
        var fraction = new BigInteger(magnitude & ((1UL << fractionBits) - 1));
        var units = exponent == 0 ? fraction : ((BigInteger.One << fractionBits) + fraction) << (exponent - 1);
        for (var part = state.Parts.Length - 1; part >= 0; part--)
        {
            var digit = (long)(units & (CosmicFloatingAggregateResults.LimbBase - 1));
            units >>= 30;
            state.Parts[part] = checked(state.Parts[part] + ((bits & sign) == 0 ? digit : -digit));
        }
        if (!units.IsZero) throw new InvalidOperationException("An admitted floating input must fit its declared packet.");
        state.Count++;
        return state;
    }

    private byte[] WritePacket(State state, bool single)
    {
        if (!WithinBudget()) return null;
        using var stream = new MemoryStream(10 + Width(single) * 8);
        using var writer = new BinaryWriter(stream);
        writer.Write((byte)1); writer.Write((byte)(single ? 1 : 2));
        writer.Write(state?.Count ?? 0);
        foreach (var part in state?.Parts ?? new long[Width(single)]) writer.Write(part);
        return stream.ToArray();
    }

    internal sealed class Packet
    {
        internal long[] Parts { get; }
        internal long Count { get; }
        internal Packet(long[] parts, long count) { Parts = parts; Count = count; }
    }

    internal static OASISResult<Packet> ReadPacket(byte[] bytes, bool single)
    {
        if (bytes == null || bytes.Length != 10 + Width(single) * 8
            || bytes[0] != 1 || bytes[1] != (single ? 1 : 2))
            return InvalidPacket("Floating aggregate packet has an invalid version, domain or exact length.");
        using var stream = new MemoryStream(bytes, writable: false);
        using var reader = new BinaryReader(stream);
        reader.ReadByte(); reader.ReadByte();
        var count = reader.ReadInt64();
        if (count < 0) return InvalidPacket("Floating aggregate packet has a negative count.");
        if (count > CosmicNumericScalarResults.MaximumParticipatingValues)
            return new OASISResult<Packet> { IsError = true, ErrorCount = 1, ErrorCode = "CQL_QUERY_LIMIT", Message = "Floating packet exceeds its work bound." };
        var words = new long[Width(single)];
        for (var index = 0; index < words.Length; index++) words[index] = reader.ReadInt64();
        return new OASISResult<Packet>(new Packet(words, count)) { IsLoaded = true };
    }

    private static OASISResult<Packet> InvalidPacket(string message)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_DATA_INVALID", Message = message };
    private string Fail(string code, string message)
    {
        failureCode ??= code; failureMessage ??= message;
        return null;
    }
    private bool WithinBudget()
    {
        try { checkBudget(); return true; }
        catch (Exception cause) when (cause is OperationCanceledException || cause is SqliteCqlDeadlineException || cause is SqliteCqlResourceException)
        {
            budgetFailure ??= cause;
            return false;
        }
    }
    internal void Check()
    {
        if (budgetFailure != null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(budgetFailure).Throw();
        if (failureCode != null) throw new SqliteCqlNumericException(failureCode, failureMessage);
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        connection.CreateAggregate<string, State, byte[]>(SingleFunction, seed: null, func: null, resultSelector: null);
        connection.CreateAggregate<string, State, byte[]>(DoubleFunction, seed: null, func: null, resultSelector: null);
        connection.CreateFunction<byte[], int, int, string>(KeyFunction, null);
    }
}
