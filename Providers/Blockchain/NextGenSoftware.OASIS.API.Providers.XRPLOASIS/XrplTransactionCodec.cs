extern alias BouncyCastleCryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using BouncyCastleCryptography::Org.BouncyCastle.Crypto.Digests;
using BouncyCastleCryptography::Org.BouncyCastle.Crypto.Parameters;
using BouncyCastleCryptography::Org.BouncyCastle.Crypto.Signers;

namespace NextGenSoftware.OASIS.API.Providers.XRPLOASIS;

/// <summary>
/// Canonical XRPL binary encoding and Ed25519 signing for the one transaction shape OASIS storage submits:
/// an <c>AccountSet</c> carrying a single memo. Field order follows the XRPL canonical (type code, field code) rule.
/// </summary>
internal sealed class XrplEd25519Signer
{
    private const string RippleAlphabet = "rpshnaf39wBUDNEGHJKLM4PQRST7VWXYZ2bcdeCg65jkm8oFqi1tuvAxyz";
    private static readonly byte[] SigningPrefix = { 0x53, 0x54, 0x58, 0x00 };     // "STX\0"
    private static readonly byte[] TransactionIdPrefix = { 0x54, 0x58, 0x4E, 0x00 }; // "TXN\0"

    private readonly Ed25519PrivateKeyParameters _privateKey;

    public XrplEd25519Signer(string privateKeyHex)
    {
        var raw = Convert.FromHexString(privateKeyHex ?? throw new ArgumentNullException(nameof(privateKeyHex)));
        if (raw.Length != 32)
            throw new ArgumentException("An XRPL Ed25519 private key is exactly 32 bytes (64 hex characters).", nameof(privateKeyHex));
        _privateKey = new Ed25519PrivateKeyParameters(raw, 0);
        CryptographicOperations.ZeroMemory(raw);
        // XRPL marks Ed25519 public keys with a 0xED prefix byte.
        SigningPublicKey = new byte[] { 0xED }.Concat(_privateKey.GeneratePublicKey().GetEncoded()).ToArray();
        AccountId = ComputeAccountId(SigningPublicKey);
        Address = EncodeAddress(AccountId);
    }

    public byte[] SigningPublicKey { get; }
    public byte[] AccountId { get; }
    public string Address { get; }

    /// <summary>Signs an AccountSet that carries one memo and returns the hex blob and its transaction hash.</summary>
    public (string Blob, string Hash) SignAccountSetWithMemo(uint sequence, uint lastLedgerSequence, ulong feeDrops,
        byte[] memoType, byte[] memoData)
    {
        var unsigned = Serialize(sequence, lastLedgerSequence, feeDrops, memoType, memoData, signature: null);
        var signer = new Ed25519Signer();
        signer.Init(true, _privateKey);
        var message = SigningPrefix.Concat(unsigned).ToArray();
        signer.BlockUpdate(message, 0, message.Length);
        var signature = signer.GenerateSignature();
        var signed = Serialize(sequence, lastLedgerSequence, feeDrops, memoType, memoData, signature);
        var hash = SHA512.HashData(TransactionIdPrefix.Concat(signed).ToArray()).AsSpan(0, 32).ToArray();
        return (Convert.ToHexString(signed), Convert.ToHexString(hash));
    }

    private byte[] Serialize(uint sequence, uint lastLedgerSequence, ulong feeDrops, byte[] memoType, byte[] memoData, byte[] signature)
    {
        using var stream = new MemoryStream();
        // UInt16 (1): TransactionType = AccountSet (3)
        stream.WriteByte(0x12); WriteUInt16(stream, 3);
        // UInt32 (2): Flags (2), Sequence (4), LastLedgerSequence (27)
        stream.WriteByte(0x22); WriteUInt32(stream, 0);
        stream.WriteByte(0x24); WriteUInt32(stream, sequence);
        stream.WriteByte(0x20); stream.WriteByte(27); WriteUInt32(stream, lastLedgerSequence);
        // Amount (6): Fee (8), native XRP drops with the positive bit set
        stream.WriteByte(0x68); WriteUInt64(stream, 0x4000000000000000UL | feeDrops);
        // Blob (7): SigningPubKey (3), TxnSignature (4, excluded while signing)
        stream.WriteByte(0x73); WriteVariableLength(stream, SigningPublicKey);
        if (signature != null) { stream.WriteByte(0x74); WriteVariableLength(stream, signature); }
        // AccountID (8): Account (1)
        stream.WriteByte(0x81); WriteVariableLength(stream, AccountId);
        // STArray (15): Memos (9) -> STObject Memo (10) -> Blob MemoType (12), MemoData (13)
        stream.WriteByte(0xF9);
        stream.WriteByte(0xEA);
        stream.WriteByte(0x7C); WriteVariableLength(stream, memoType);
        stream.WriteByte(0x7D); WriteVariableLength(stream, memoData);
        stream.WriteByte(0xE1); // end of Memo object
        stream.WriteByte(0xF1); // end of Memos array
        return stream.ToArray();
    }

    private static void WriteUInt16(Stream s, ushort v) { s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }
    private static void WriteUInt32(Stream s, uint v) { for (var shift = 24; shift >= 0; shift -= 8) s.WriteByte((byte)(v >> shift)); }
    private static void WriteUInt64(Stream s, ulong v) { for (var shift = 56; shift >= 0; shift -= 8) s.WriteByte((byte)(v >> shift)); }

    private static void WriteVariableLength(Stream s, byte[] value)
    {
        var length = value.Length;
        if (length <= 192) s.WriteByte((byte)length);
        else if (length <= 12480)
        {
            length -= 193;
            s.WriteByte((byte)(193 + (length >> 8)));
            s.WriteByte((byte)(length & 0xFF));
        }
        else throw new InvalidDataException("XRPL variable-length field exceeds the two-byte length encoding.");
        s.Write(value, 0, value.Length);
    }

    internal static byte[] ComputeAccountId(byte[] publicKey)
    {
        var sha = SHA256.HashData(publicKey);
        var ripemd = new RipeMD160Digest();
        ripemd.BlockUpdate(sha, 0, sha.Length);
        var id = new byte[20];
        ripemd.DoFinal(id, 0);
        return id;
    }

    internal static string EncodeAddress(byte[] accountId)
    {
        var payload = new byte[] { 0x00 }.Concat(accountId).ToArray();
        var checksum = SHA256.HashData(SHA256.HashData(payload)).AsSpan(0, 4).ToArray();
        var data = payload.Concat(checksum).ToArray();
        var value = new BigInteger(data, isUnsigned: true, isBigEndian: true);
        var text = new StringBuilder();
        while (value > 0) { value = BigInteger.DivRem(value, 58, out var digit); text.Insert(0, RippleAlphabet[(int)digit]); }
        foreach (var b in data) { if (b != 0) break; text.Insert(0, RippleAlphabet[0]); }
        return text.ToString();
    }
}
