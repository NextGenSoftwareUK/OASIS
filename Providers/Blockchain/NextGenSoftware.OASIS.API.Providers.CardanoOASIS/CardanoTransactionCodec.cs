extern alias BouncyCastleCryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BouncyCastleCryptography::Org.BouncyCastle.Crypto.Digests;
using BouncyCastleCryptography::Org.BouncyCastle.Crypto.Parameters;
using BouncyCastleCryptography::Org.BouncyCastle.Crypto.Signers;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS;

/// <summary>
/// Builds and signs the one transaction shape OASIS storage submits on Cardano: spend one key-hash UTxO back to the same
/// enterprise address minus the fee, carrying transaction metadata under one label. Encoding follows the Cardano CDDL
/// (body map keys 0 inputs, 1 outputs, 2 fee, 3 ttl, 7 auxiliary data hash) with an Ed25519 vkey witness.
/// </summary>
internal sealed class CardanoEd25519Signer
{
    private readonly Ed25519PrivateKeyParameters _privateKey;

    public CardanoEd25519Signer(string privateKeyHex, bool mainnet)
    {
        var raw = Convert.FromHexString(privateKeyHex ?? throw new ArgumentNullException(nameof(privateKeyHex)));
        if (raw.Length != 32) throw new ArgumentException("A Cardano Ed25519 signing key is exactly 32 bytes (64 hex characters).", nameof(privateKeyHex));
        _privateKey = new Ed25519PrivateKeyParameters(raw, 0);
        CryptographicOperations.ZeroMemory(raw);
        VerificationKey = _privateKey.GeneratePublicKey().GetEncoded();
        // Enterprise address: header (type 6 = key hash, no stake part) | network id, then blake2b-224(vkey).
        AddressBytes = new[] { (byte)(0x60 | (mainnet ? 1 : 0)) }.Concat(Blake2b(VerificationKey, 28)).ToArray();
        Address = Bech32.Encode(mainnet ? "addr" : "addr_test", AddressBytes);
    }

    public byte[] VerificationKey { get; }
    public byte[] AddressBytes { get; }
    public string Address { get; }

    /// <summary>Returns the signed transaction CBOR and its id (blake2b-256 of the body).</summary>
    public (byte[] Transaction, string Id) Build(string inputTxHash, uint inputIndex, ulong inputLovelace, ulong fee, ulong ttl,
        ulong metadataLabel, CborValue metadatum)
    {
        if (fee >= inputLovelace) throw new InvalidOperationException("The Cardano UTxO cannot pay the storage fee.");
        var auxiliary = CborValue.Map((CborValue.UInt(metadataLabel), metadatum));
        var auxiliaryBytes = auxiliary.Encode();
        var body = CborValue.Map(
            (CborValue.UInt(0), CborValue.Array(CborValue.Array(CborValue.Bytes(Convert.FromHexString(inputTxHash)), CborValue.UInt(inputIndex)))),
            (CborValue.UInt(1), CborValue.Array(CborValue.Array(CborValue.Bytes(AddressBytes), CborValue.UInt(inputLovelace - fee)))),
            (CborValue.UInt(2), CborValue.UInt(fee)),
            (CborValue.UInt(3), CborValue.UInt(ttl)),
            (CborValue.UInt(7), CborValue.Bytes(Blake2b(auxiliaryBytes, 32))));
        var bodyBytes = body.Encode();
        var id = Blake2b(bodyBytes, 32);
        var signer = new Ed25519Signer();
        signer.Init(true, _privateKey);
        signer.BlockUpdate(id, 0, id.Length);
        var signature = signer.GenerateSignature();
        var witnesses = CborValue.Map((CborValue.UInt(0), CborValue.Array(CborValue.Array(CborValue.Bytes(VerificationKey), CborValue.Bytes(signature)))));
        var transaction = CborValue.Array(CborValue.Raw(bodyBytes), witnesses, CborValue.Bool(true), CborValue.Raw(auxiliaryBytes));
        return (transaction.Encode(), Convert.ToHexString(id).ToLowerInvariant());
    }

    internal static byte[] Blake2b(byte[] data, int size)
    {
        var digest = new Blake2bDigest(size * 8);
        digest.BlockUpdate(data, 0, data.Length);
        var output = new byte[size];
        digest.DoFinal(output, 0);
        return output;
    }
}

/// <summary>Minimal definite-length CBOR (RFC 8949) values sufficient for Cardano transactions and metadata.</summary>
internal sealed class CborValue
{
    private readonly Action<Stream> _write;
    private CborValue(Action<Stream> write) => _write = write;

    public static CborValue UInt(ulong value) => new(s => Head(s, 0, value));
    public static CborValue Bytes(byte[] value) => new(s => { Head(s, 2, (ulong)value.Length); s.Write(value); });
    public static CborValue Text(string value) { var b = Encoding.UTF8.GetBytes(value); return new(s => { Head(s, 3, (ulong)b.Length); s.Write(b); }); }
    public static CborValue Array(params CborValue[] items) => new(s => { Head(s, 4, (ulong)items.Length); foreach (var i in items) i._write(s); });
    public static CborValue Map(params (CborValue Key, CborValue Value)[] entries) => new(s =>
    {
        Head(s, 5, (ulong)entries.Length);
        foreach (var (k, v) in entries) { k._write(s); v._write(s); }
    });
    public static CborValue Bool(bool value) => new(s => s.WriteByte(value ? (byte)0xF5 : (byte)0xF4));
    /// <summary>Pre-encoded CBOR embedded verbatim (the signed body must be byte-identical to what was hashed).</summary>
    public static CborValue Raw(byte[] encoded) => new(s => s.Write(encoded));

    public byte[] Encode() { using var s = new MemoryStream(); _write(s); return s.ToArray(); }

    private static void Head(Stream s, int major, ulong value)
    {
        var m = (byte)(major << 5);
        if (value < 24) s.WriteByte((byte)(m | value));
        else if (value <= byte.MaxValue) { s.WriteByte((byte)(m | 24)); s.WriteByte((byte)value); }
        else if (value <= ushort.MaxValue) { s.WriteByte((byte)(m | 25)); for (var i = 8; i >= 0; i -= 8) s.WriteByte((byte)(value >> i)); }
        else if (value <= uint.MaxValue) { s.WriteByte((byte)(m | 26)); for (var i = 24; i >= 0; i -= 8) s.WriteByte((byte)(value >> i)); }
        else { s.WriteByte((byte)(m | 27)); for (var i = 56; i >= 0; i -= 8) s.WriteByte((byte)(value >> i)); }
    }
}

internal static class Bech32
{
    private const string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";

    public static string Encode(string hrp, byte[] data)
    {
        var values = ConvertBits(data, 8, 5);
        var checksum = Checksum(hrp, values);
        return hrp + "1" + new string(values.Concat(checksum).Select(v => Charset[v]).ToArray());
    }

    private static byte[] ConvertBits(byte[] data, int from, int to)
    {
        int acc = 0, bits = 0, max = (1 << to) - 1;
        var result = new List<byte>();
        foreach (var b in data)
        {
            acc = (acc << from) | b; bits += from;
            while (bits >= to) { bits -= to; result.Add((byte)((acc >> bits) & max)); }
        }
        if (bits > 0) result.Add((byte)((acc << (to - bits)) & max));
        return result.ToArray();
    }

    private static byte[] Checksum(string hrp, byte[] values)
    {
        var expanded = hrp.Select(c => (byte)(c >> 5)).Append((byte)0).Concat(hrp.Select(c => (byte)(c & 31)))
            .Concat(values).Concat(new byte[6]).ToArray();
        var polymod = Polymod(expanded) ^ 1;
        return Enumerable.Range(0, 6).Select(i => (byte)((polymod >> (5 * (5 - i))) & 31)).ToArray();
    }

    private static uint Polymod(byte[] values)
    {
        uint[] gen = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };
        uint chk = 1;
        foreach (var v in values)
        {
            var top = chk >> 25;
            chk = ((chk & 0x1ffffff) << 5) ^ v;
            for (var i = 0; i < 5; i++) if (((top >> i) & 1) != 0) chk ^= gen[i];
        }
        return chk;
    }
}
