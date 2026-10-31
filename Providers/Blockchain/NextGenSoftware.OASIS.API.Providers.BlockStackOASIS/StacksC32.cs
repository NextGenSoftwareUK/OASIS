using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.Crypto;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;

/// <summary>
/// Stacks c32check addresses (https://github.com/stacks-network/c32check): 'S' + c32(version) +
/// c32(hash160 + checksum), checksum = first 4 bytes of SHA-256(SHA-256(version + hash160)), Crockford base-32 alphabet.
/// Mainnet single-sig version is 22 ('P', SP...), testnet 26 ('T', ST...).
/// </summary>
public static class StacksC32
{
    public const byte MainnetSingleSig = 22;
    public const byte TestnetSingleSig = 26;
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string Address(byte version, byte[] hash160)
    {
        if (version >= 32) throw new ArgumentOutOfRangeException(nameof(version), "c32 versions are 0-31.");
        if (hash160 == null || hash160.Length != 20) throw new ArgumentException("A hash160 is 20 bytes.", nameof(hash160));
        return "S" + Alphabet[version] + Encode(hash160.Concat(Checksum(version, hash160)).ToArray());
    }

    public static (byte Version, byte[] Hash160) Decode(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Length < 3 || char.ToUpperInvariant(address[0]) != 'S')
            throw new FormatException("A Stacks address starts with 'S'.");
        var upper = address.ToUpperInvariant().Replace('O', '0').Replace('L', '1').Replace('I', '1');
        var version = Alphabet.IndexOf(upper[1]);
        if (version < 0) throw new FormatException("Invalid Stacks address version character.");
        var data = DecodeC32(upper[2..]);
        if (data.Length != 24) throw new FormatException("Stacks single-sig addresses carry a 20-byte hash160 and 4-byte checksum.");
        var hash160 = data[..20];
        if (!Checksum((byte)version, hash160).SequenceEqual(data[20..])) throw new FormatException("Invalid Stacks address checksum.");
        return ((byte)version, hash160);
    }

    private static byte[] Checksum(byte version, byte[] hash160) =>
        Hashes.DoubleSHA256(new[] { version }.Concat(hash160).ToArray()).ToBytes()[..4];

    // c32 writes the bytes as a big-endian base-32 number and keeps one '0' per leading zero byte.
    private static string Encode(byte[] data)
    {
        var chars = new List<char>();
        int carry = 0, carryBits = 0;
        for (var i = data.Length - 1; i >= 0; i--)
        {
            carry |= data[i] << carryBits;
            carryBits += 8;
            while (carryBits >= 5)
            {
                chars.Add(Alphabet[carry & 31]);
                carry >>= 5; carryBits -= 5;
            }
        }
        if (carryBits > 0) chars.Add(Alphabet[carry & 31]);
        chars.Reverse();
        var digits = new string(chars.ToArray()).TrimStart('0');
        return new string('0', data.TakeWhile(b => b == 0).Count()) + digits;
    }

    private static byte[] DecodeC32(string text)
    {
        var bytes = new List<byte>();
        int carry = 0, carryBits = 0;
        for (var i = text.Length - 1; i >= 0; i--)
        {
            var v = Alphabet.IndexOf(text[i]);
            if (v < 0) throw new FormatException($"Invalid c32 character '{text[i]}'.");
            carry |= v << carryBits;
            carryBits += 5;
            if (carryBits >= 8)
            {
                bytes.Add((byte)(carry & 0xff));
                carry >>= 8; carryBits -= 8;
            }
        }
        if (carryBits > 0 && carry != 0) bytes.Add((byte)carry);
        bytes.Reverse();
        var significant = bytes.SkipWhile(b => b == 0);
        return Enumerable.Repeat((byte)0, text.TakeWhile(c => c == '0').Count()).Concat(significant).ToArray();
    }
}
