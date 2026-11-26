using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Text;

namespace NextGenSoftware.OASIS.API.Providers.BittensorOASIS;

/// <summary>
/// The hashers Substrate uses for storage keys and SS58 addresses: xxHash64 (twox_64/twox_128) and BLAKE2b
/// (blake2_128_concat; the SS58 checksum is BLAKE2b-512). .NET ships neither, so both are implemented here and pinned by
/// published test vectors in the unit tests.
/// </summary>
internal static class SubstrateHashing
{
    // ---- xxHash64 ----
    private const ulong P1 = 11400714785074694791UL, P2 = 14029467366897019727UL, P3 = 1609587929392839161UL, P4 = 9650029242287828579UL, P5 = 2870177450012600261UL;

    internal static ulong XxHash64(ReadOnlySpan<byte> data, ulong seed)
    {
        int i = 0, n = data.Length;
        ulong h;
        if (n >= 32)
        {
            ulong v1 = seed + P1 + P2, v2 = seed + P2, v3 = seed, v4 = seed - P1;
            for (; i <= n - 32; i += 32)
            {
                v1 = Round(v1, BinaryPrimitives.ReadUInt64LittleEndian(data[i..]));
                v2 = Round(v2, BinaryPrimitives.ReadUInt64LittleEndian(data[(i + 8)..]));
                v3 = Round(v3, BinaryPrimitives.ReadUInt64LittleEndian(data[(i + 16)..]));
                v4 = Round(v4, BinaryPrimitives.ReadUInt64LittleEndian(data[(i + 24)..]));
            }
            h = BitOperations.RotateLeft(v1, 1) + BitOperations.RotateLeft(v2, 7) + BitOperations.RotateLeft(v3, 12) + BitOperations.RotateLeft(v4, 18);
            h = Merge(h, v1); h = Merge(h, v2); h = Merge(h, v3); h = Merge(h, v4);
        }
        else h = seed + P5;
        h += (ulong)n;
        for (; i <= n - 8; i += 8)
        {
            h ^= Round(0, BinaryPrimitives.ReadUInt64LittleEndian(data[i..]));
            h = BitOperations.RotateLeft(h, 27) * P1 + P4;
        }
        if (i <= n - 4)
        {
            h ^= BinaryPrimitives.ReadUInt32LittleEndian(data[i..]) * P1;
            h = BitOperations.RotateLeft(h, 23) * P2 + P3;
            i += 4;
        }
        for (; i < n; i++)
        {
            h ^= data[i] * P5;
            h = BitOperations.RotateLeft(h, 11) * P1;
        }
        h ^= h >> 33; h *= P2; h ^= h >> 29; h *= P3; h ^= h >> 32;
        return h;
    }

    private static ulong Round(ulong acc, ulong input) => BitOperations.RotateLeft(acc + input * P2, 31) * P1;
    private static ulong Merge(ulong acc, ulong v) => (acc ^ Round(0, v)) * P1 + P4;

    /// <summary>twox_128: xxHash64 with seeds 0 and 1, each written little-endian.</summary>
    internal static byte[] Twox128(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var output = new byte[16];
        BinaryPrimitives.WriteUInt64LittleEndian(output, XxHash64(bytes, 0));
        BinaryPrimitives.WriteUInt64LittleEndian(output.AsSpan(8), XxHash64(bytes, 1));
        return output;
    }

    /// <summary>twox_64_concat: xxHash64 (seed 0, little-endian) followed by the key itself.</summary>
    internal static byte[] Twox64Concat(ReadOnlySpan<byte> key)
    {
        var output = new byte[8 + key.Length];
        BinaryPrimitives.WriteUInt64LittleEndian(output, XxHash64(key, 0));
        key.CopyTo(output.AsSpan(8));
        return output;
    }

    // ---- BLAKE2b (RFC 7693), unkeyed ----
    private static readonly ulong[] IV =
    {
        0x6a09e667f3bcc908UL, 0xbb67ae8584caa73bUL, 0x3c6ef372fe94f82bUL, 0xa54ff53a5f1d36f1UL,
        0x510e527fade682d1UL, 0x9b05688c2b3e6c1fUL, 0x1f83d9abfb41bd6bUL, 0x5be0cd19137e2179UL,
    };

    private static readonly byte[,] Sigma =
    {
        { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, { 14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3 },
        { 11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4 }, { 7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8 },
        { 9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13 }, { 2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9 },
        { 12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11 }, { 13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10 },
        { 6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5 }, { 10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0 },
    };

    internal static byte[] Blake2b(ReadOnlySpan<byte> data, int outputLength)
    {
        if (outputLength is < 1 or > 64) throw new ArgumentOutOfRangeException(nameof(outputLength));
        var h = (ulong[])IV.Clone();
        h[0] ^= 0x01010000UL ^ (ulong)outputLength;
        var block = new byte[128];
        ulong counter = 0;
        int offset = 0;
        // Every block but the last is compressed with the final flag clear; the last (possibly empty or partial) sets it.
        while (data.Length - offset > 128)
        {
            counter += 128;
            Compress(h, data.Slice(offset, 128), counter, false);
            offset += 128;
        }
        var remaining = data.Length - offset;
        Array.Clear(block);
        data[offset..].CopyTo(block);
        counter += (ulong)remaining;
        Compress(h, block, counter, true);
        var output = new byte[outputLength];
        var full = new byte[64];
        for (var i = 0; i < 8; i++) BinaryPrimitives.WriteUInt64LittleEndian(full.AsSpan(i * 8), h[i]);
        Array.Copy(full, output, outputLength);
        return output;
    }

    private static void Compress(ulong[] h, ReadOnlySpan<byte> block, ulong counter, bool last)
    {
        Span<ulong> m = stackalloc ulong[16];
        for (var i = 0; i < 16; i++) m[i] = BinaryPrimitives.ReadUInt64LittleEndian(block[(i * 8)..]);
        Span<ulong> v = stackalloc ulong[16];
        for (var i = 0; i < 8; i++) { v[i] = h[i]; v[i + 8] = IV[i]; }
        v[12] ^= counter;
        if (last) v[14] = ~v[14];
        for (var r = 0; r < 12; r++)
        {
            var s = r % 10;
            G(v, 0, 4, 8, 12, m[Sigma[s, 0]], m[Sigma[s, 1]]);
            G(v, 1, 5, 9, 13, m[Sigma[s, 2]], m[Sigma[s, 3]]);
            G(v, 2, 6, 10, 14, m[Sigma[s, 4]], m[Sigma[s, 5]]);
            G(v, 3, 7, 11, 15, m[Sigma[s, 6]], m[Sigma[s, 7]]);
            G(v, 0, 5, 10, 15, m[Sigma[s, 8]], m[Sigma[s, 9]]);
            G(v, 1, 6, 11, 12, m[Sigma[s, 10]], m[Sigma[s, 11]]);
            G(v, 2, 7, 8, 13, m[Sigma[s, 12]], m[Sigma[s, 13]]);
            G(v, 3, 4, 9, 14, m[Sigma[s, 14]], m[Sigma[s, 15]]);
        }
        for (var i = 0; i < 8; i++) h[i] ^= v[i] ^ v[i + 8];
    }

    private static void G(Span<ulong> v, int a, int b, int c, int d, ulong x, ulong y)
    {
        v[a] = v[a] + v[b] + x; v[d] = BitOperations.RotateRight(v[d] ^ v[a], 32);
        v[c] = v[c] + v[d]; v[b] = BitOperations.RotateRight(v[b] ^ v[c], 24);
        v[a] = v[a] + v[b] + y; v[d] = BitOperations.RotateRight(v[d] ^ v[a], 16);
        v[c] = v[c] + v[d]; v[b] = BitOperations.RotateRight(v[b] ^ v[c], 63);
    }

    /// <summary>blake2_128_concat: BLAKE2b-128 of the key followed by the key itself.</summary>
    internal static byte[] Blake2_128Concat(ReadOnlySpan<byte> key)
    {
        var output = new byte[16 + key.Length];
        Blake2b(key, 16).CopyTo(output, 0);
        key.CopyTo(output.AsSpan(16));
        return output;
    }

    // ---- SS58 ----
    private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private static readonly byte[] Ss58Prefix = Encoding.ASCII.GetBytes("SS58PRE");

    /// <summary>Decodes an SS58 address with a one-byte network prefix (Bittensor uses 42) to its 32-byte account id,
    /// verifying the two-byte BLAKE2b-512 checksum.</summary>
    internal static byte[] DecodeSs58(string address, byte expectedPrefix = 42)
    {
        if (string.IsNullOrWhiteSpace(address)) throw new FormatException("An SS58 address is required.");
        BigInteger value = 0;
        foreach (var c in address)
        {
            var digit = Base58Alphabet.IndexOf(c);
            if (digit < 0) throw new FormatException($"'{c}' is not a base58 character.");
            value = value * 58 + digit;
        }
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        var leadingZeros = 0;
        while (leadingZeros < address.Length && address[leadingZeros] == '1') leadingZeros++;
        var decoded = new byte[leadingZeros + bytes.Length];
        bytes.CopyTo(decoded, leadingZeros);
        if (decoded.Length != 35) throw new FormatException("Only one-byte-prefix SS58 account addresses (35 bytes decoded) are supported.");
        if (decoded[0] != expectedPrefix) throw new FormatException($"SS58 prefix {decoded[0]} is not the expected network prefix {expectedPrefix}.");
        var body = decoded.AsSpan(0, 33);
        var checksum = Blake2b([.. Ss58Prefix, .. body], 64);
        if (checksum[0] != decoded[33] || checksum[1] != decoded[34]) throw new FormatException("The SS58 address checksum does not match.");
        return decoded[1..33];
    }
}
