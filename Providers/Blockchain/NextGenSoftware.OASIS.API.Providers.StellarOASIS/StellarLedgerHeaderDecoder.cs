using System;
using System.Buffers.Binary;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Providers.StellarOASIS;

/// <summary>Hash-bound native LedgerHeader XDR; does not verify SCP consensus signatures.</summary>
public static class StellarLedgerHeaderDecoder
{
    public sealed record Header(uint ProtocolVersion, string Hash, string ParentHash,
        uint Sequence, ulong CloseTime, ulong? CloseTimeMilliseconds, uint ValueType,
        long TotalStroops, long FeePool, ulong IdPool, uint BaseFee, uint BaseReserve,
        uint MaximumTransactionSetSize, uint Flags, string TransactionSetHash,
        string TransactionResultHash, string BucketListHash);

    public static Header Decode(string headerXdr, string expectedHash, uint expectedSequence)
    {
        if (headerXdr == null || headerXdr.Length is < 1 or > 8192 || expectedSequence == 0 ||
            expectedHash == null || !Regex.IsMatch(expectedHash, "\\A[0-9a-f]{64}\\z"))
            throw new InvalidDataException("Bounded canonical header identity required.");

        byte[] bytes;
        try { bytes = Convert.FromBase64String(headerXdr); }
        catch (FormatException error) { throw new InvalidDataException("Canonical base64 XDR required.", error); }
        if (bytes.Length > 4096 || Convert.ToBase64String(bytes) != headerXdr ||
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() != expectedHash)
            throw new InvalidDataException("Canonical XDR/native SHA256 identity differs.");

        var cursor = new Cursor(bytes);
        uint version = cursor.U32();
        string parent = cursor.Hash();
        string transactionSet = cursor.Hash();
        ulong closeTime = cursor.U64();
        uint upgrades = cursor.U32();
        if (upgrades > 6) throw new InvalidDataException("Upgrade vector exceeds native bound.");
        for (uint index = 0; index < upgrades; index++) cursor.Opaque(128);

        uint valueType = cursor.U32();
        ulong? milliseconds = null;
        switch (valueType)
        {
            case 0: break;
            case 1: cursor.Signature(); break;
            case 2: cursor.ProposedValue(); cursor.Signature(); break;
            case 3:
                milliseconds = cursor.U64();
                cursor.Signature();
                break;
            case 4:
                milliseconds = cursor.U64();
                cursor.ProposedValue();
                cursor.Signature();
                break;
            default: throw new InvalidDataException("Unsupported StellarValue union variant.");
        }
        if (milliseconds.HasValue && milliseconds.Value / 1000 != closeTime)
            throw new InvalidDataException("Native seconds/milliseconds differ.");

        string transactionResult = cursor.Hash();
        string bucketList = cursor.Hash();
        uint sequence = cursor.U32();
        if (sequence != expectedSequence) throw new InvalidDataException("Native/API ledger sequence differs.");
        long total = cursor.I64();
        long feePool = cursor.I64();
        cursor.U32(); // inflation sequence, consumed in its declared wire position
        ulong idPool = cursor.U64();
        uint baseFee = cursor.U32();
        uint baseReserve = cursor.U32();
        uint maximumTransactions = cursor.U32();
        for (int index = 0; index < 4; index++) cursor.Hash();

        uint flags = 0;
        switch (cursor.U32())
        {
            case 0: break;
            case 1:
                flags = cursor.U32();
                if ((flags & ~7u) != 0 || cursor.U32() != 0)
                    throw new InvalidDataException("Unsupported header flags/extension variant.");
                break;
            default: throw new InvalidDataException("Unsupported LedgerHeader extension variant.");
        }
        cursor.Complete();
        return new(version, expectedHash, parent, sequence, closeTime, milliseconds, valueType,
            total, feePool, idPool, baseFee, baseReserve, maximumTransactions, flags,
            transactionSet, transactionResult, bucketList);
    }

    private sealed class Cursor(byte[] bytes)
    {
        private int position;

        private ReadOnlySpan<byte> Take(int count)
        {
            if (count < 0 || count > bytes.Length - position)
                throw new InvalidDataException("Truncated native XDR.");
            var value = bytes.AsSpan(position, count);
            position += count;
            return value;
        }

        public uint U32() => BinaryPrimitives.ReadUInt32BigEndian(Take(4));
        public ulong U64() => BinaryPrimitives.ReadUInt64BigEndian(Take(8));
        public long I64() => BinaryPrimitives.ReadInt64BigEndian(Take(8));
        public string Hash() => Convert.ToHexString(Take(32)).ToLowerInvariant();

        public void Opaque(uint maximum)
        {
            uint length = U32();
            if (length > maximum) throw new InvalidDataException("Opaque value exceeds native bound.");
            Take((int)length);
            int padding = (int)((4 - (length % 4)) % 4);
            foreach (byte value in Take(padding))
                if (value != 0) throw new InvalidDataException("Nonzero XDR padding.");
        }

        public void Signature()
        {
            if (U32() != 0) throw new InvalidDataException("Unsupported native NodeID key type.");
            Take(32);
            Opaque(64); // Native Signature is bounded opaque data, not a consensus proof.
        }

        public void ProposedValue()
        {
            Hash();
            Hash();
            U32();
        }

        public void Complete()
        {
            if (position != bytes.Length) throw new InvalidDataException("Trailing native XDR data.");
        }
    }
}
