using System;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Helpers;

/// <summary>
/// SLIP-0010 key derivation for Ed25519 (https://github.com/satoshilabs/slips/blob/master/slip-0010.md).
/// Ed25519 supports hardened derivation only; every index here is hardened.
/// </summary>
public static class Slip10Ed25519
{
    private const uint Hardened = 0x80000000;
    private static readonly byte[] Curve = System.Text.Encoding.ASCII.GetBytes("ed25519 seed");

    public static (byte[] Key, byte[] ChainCode) Master(byte[] seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        if (seed.Length < 16 || seed.Length > 64) throw new ArgumentException("SLIP-10 seeds are 16 to 64 bytes.", nameof(seed));
        return Split(HMACSHA512.HashData(Curve, seed));
    }

    public static (byte[] Key, byte[] ChainCode) Child((byte[] Key, byte[] ChainCode) parent, uint index)
    {
        if (index >= Hardened) throw new ArgumentOutOfRangeException(nameof(index), "Pass the unhardened index; it is hardened here.");
        var data = new byte[1 + 32 + 4];
        Buffer.BlockCopy(parent.Key, 0, data, 1, 32);
        var hardened = index | Hardened;
        data[33] = (byte)(hardened >> 24); data[34] = (byte)(hardened >> 16); data[35] = (byte)(hardened >> 8); data[36] = (byte)hardened;
        return Split(HMACSHA512.HashData(parent.ChainCode, data));
    }

    /// <summary>Derives the private key at a fully hardened path given as unhardened indices, e.g. {44, 1022, 1, 525, 1460, 0}.</summary>
    public static byte[] DerivePrivateKey(byte[] seed, params uint[] path)
    {
        var node = Master(seed);
        foreach (var index in path)
            node = Child(node, index);
        return node.Key;
    }

    private static (byte[] Key, byte[] ChainCode) Split(byte[] i) => (i[..32], i[32..]);
}
