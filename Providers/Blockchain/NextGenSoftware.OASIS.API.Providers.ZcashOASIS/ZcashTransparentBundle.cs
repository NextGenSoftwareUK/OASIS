using System;
using System.Buffers.Binary;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS;

/// <summary>
/// Edits the transparent bundle of an unsigned Zcash transaction. The transparent inputs and outputs sit at a fixed
/// place in every transaction format (v1-v2: after the header; v3-v4 Overwinter/Sapling: after the version group id;
/// v5 NU5: after the version group id, consensus branch id, lock time and expiry height), so an output can be added
/// without understanding the shielded bundles that follow.
/// </summary>
internal static class ZcashTransparentBundle
{
    /// <summary>Appends a zero-value output with <paramref name="script"/> (an OP_RETURN script) to an unsigned transaction.</summary>
    public static string AppendNullDataOutput(string transactionHex, byte[] script)
    {
        ArgumentNullException.ThrowIfNull(transactionHex);
        ArgumentNullException.ThrowIfNull(script);
        if (script.Length == 0 || script[0] != 0x6a)
            throw new ArgumentException("Only an OP_RETURN script can be appended.", nameof(script));
        var tx = Convert.FromHexString(transactionHex);
        var header = BinaryPrimitives.ReadUInt32LittleEndian(tx);
        var overwintered = (header & 0x8000_0000) != 0;
        var version = header & 0x7FFF_FFFF;
        var offset = !overwintered ? 4 : version >= 5 ? 20 : 8;

        var inputs = ReadCompactSize(tx, ref offset);
        for (ulong index = 0; index < inputs; index++)
        {
            offset += 32 + 4; // prevout txid and index
            offset = SkipLengthPrefixed(tx, offset); // scriptSig
            offset += 4; // sequence
        }

        var countAt = offset;
        var outputs = ReadCompactSize(tx, ref offset);
        for (ulong index = 0; index < outputs; index++)
        {
            offset += 8; // value
            offset = SkipLengthPrefixed(tx, offset); // scriptPubKey
        }
        if (offset > tx.Length)
            throw new InvalidDataException("The Zcash transaction ends inside its transparent outputs.");

        byte[] output = [.. new byte[8], .. CompactSize((ulong)script.Length), .. script];
        byte[] rebuilt = [.. tx.AsSpan(0, countAt), .. CompactSize(outputs + 1), .. tx.AsSpan(countAt + CompactSizeLength(tx, countAt), offset - countAt - CompactSizeLength(tx, countAt)), .. output, .. tx.AsSpan(offset)];
        return Convert.ToHexString(rebuilt).ToLowerInvariant();
    }

    /// <summary>Skips a CompactSize length prefix and the bytes it counts; returns the offset after them.</summary>
    private static int SkipLengthPrefixed(byte[] tx, int offset)
    {
        // Read the length first: it advances offset past the prefix, and the skip must start after it.
        var length = ReadCompactSize(tx, ref offset);
        var end = checked(offset + (int)length);
        if (end > tx.Length) throw new InvalidDataException("The Zcash transaction is truncated.");
        return end;
    }

    private static int CompactSizeLength(byte[] tx, int offset) => tx[offset] switch
    {
        < 0xFD => 1,
        0xFD => 3,
        0xFE => 5,
        _ => 9
    };

    private static ulong ReadCompactSize(byte[] tx, ref int offset)
    {
        if (offset >= tx.Length) throw new InvalidDataException("The Zcash transaction is truncated.");
        var first = tx[offset];
        ulong value;
        switch (first)
        {
            case < 0xFD: value = first; offset += 1; break;
            case 0xFD: value = BinaryPrimitives.ReadUInt16LittleEndian(tx.AsSpan(offset + 1)); offset += 3; break;
            case 0xFE: value = BinaryPrimitives.ReadUInt32LittleEndian(tx.AsSpan(offset + 1)); offset += 5; break;
            default: value = BinaryPrimitives.ReadUInt64LittleEndian(tx.AsSpan(offset + 1)); offset += 9; break;
        }
        return value;
    }

    private static byte[] CompactSize(ulong value) => value switch
    {
        < 0xFD => [(byte)value],
        <= 0xFFFF => [0xFD, (byte)value, (byte)(value >> 8)],
        <= 0xFFFF_FFFF => [0xFE, (byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24)],
        _ => [0xFF, .. BitConverter.GetBytes(value)]
    };
}
