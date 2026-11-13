using System;
using System.IO;
using System.Linq;
using System.Text;
using Nethereum.Util;

namespace NextGenSoftware.OASIS.API.Providers.ENSOASIS;

/// <summary>
/// Wire contract for the bounded, pre-normalized ASCII ENS demo catalogue.
/// This is not a general ENSIP-15 normalizer or a CCIP Read implementation.
/// </summary>
public static class EnsUniversalResolverCodec
{
    public const string MainnetProxy = "0xeeeeeeee14d718c2b47d9923deab1335e144eeee";

    public sealed record Resolution(string Address, string Resolver);

    public static byte[] DnsEncode(string name)
    {
        // Accept only an explicit ASCII subset. Never silently normalize input.
        if (string.IsNullOrEmpty(name) || name.Length > 253)
            throw new ArgumentException("Bounded pre-normalized ASCII name required.", nameof(name));

        using var stream = new MemoryStream();
        foreach (var label in name.Split('.'))
        {
            if (label.Length is < 1 or > 63 || label[0] == '-' || label[^1] == '-' ||
                label.Any(c => !(c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
                throw new ArgumentException("Unsupported name form; no implicit normalization.", nameof(name));

            stream.WriteByte((byte)label.Length);
            stream.Write(Encoding.ASCII.GetBytes(label));
        }

        stream.WriteByte(0);
        return stream.ToArray();
    }

    public static byte[] Namehash(string name)
    {
        _ = DnsEncode(name);
        var node = new byte[32];
        foreach (var label in name.Split('.').Reverse())
        {
            var input = new byte[64];
            node.CopyTo(input, 0);
            Sha3Keccack.Current.CalculateHash(Encoding.ASCII.GetBytes(label)).CopyTo(input, 32);
            node = Sha3Keccack.Current.CalculateHash(input);
        }
        return node;
    }

    public static string EncodeAddressCall(string name)
    {
        var dns = DnsEncode(name);
        var inner = Selector("addr(bytes32)").Concat(Namehash(name)).ToArray();
        var first = DynamicBytes(dns);
        var second = DynamicBytes(inner);
        return "0x" + Convert.ToHexString(Selector("resolve(bytes,bytes)")
            .Concat(Word(64)).Concat(Word(64 + first.Length))
            .Concat(first).Concat(second).ToArray()).ToLowerInvariant();
    }

    public static Resolution DecodeAddressResult(string value)
    {
        // For this single address record the canonical outer ABI is exactly
        // two head words followed by length=32 and one address word.
        if (value == null || value.Length != 258 || !value.StartsWith("0x", StringComparison.Ordinal) ||
            value.AsSpan(2).ContainsAnyExcept("0123456789abcdefABCDEF"))
            throw new InvalidDataException("Expected bounded Universal Resolver ABI result.");

        var bytes = Convert.FromHexString(value[2..]);
        if (!bytes.AsSpan(0, 32).SequenceEqual(Word(64)) ||
            !bytes.AsSpan(64, 32).SequenceEqual(Word(32)))
            throw new InvalidDataException("Noncanonical ABI offset or record length.");

        return new Resolution(Address(bytes.AsSpan(96, 32)), Address(bytes.AsSpan(32, 32)));
    }

    private static string Address(ReadOnlySpan<byte> word)
    {
        if (word[..12].ContainsAnyExcept((byte)0) || !word[12..].ContainsAnyExcept((byte)0))
            throw new InvalidDataException("Noncanonical or empty ENS address record.");
        return "0x" + Convert.ToHexString(word[12..]).ToLowerInvariant();
    }

    private static byte[] Selector(string signature) =>
        Sha3Keccack.Current.CalculateHash(Encoding.ASCII.GetBytes(signature))[..4];

    private static byte[] Word(int value)
    {
        var word = new byte[32];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(word.AsSpan(28), value);
        return word;
    }

    private static byte[] DynamicBytes(byte[] value) => Word(value.Length)
        .Concat(value).Concat(new byte[(32 - value.Length % 32) % 32]).ToArray();
}
