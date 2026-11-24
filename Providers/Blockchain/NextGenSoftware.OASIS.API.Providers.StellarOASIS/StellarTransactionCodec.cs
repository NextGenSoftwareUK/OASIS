extern alias BouncyCastleCryptography;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BouncyCastleCryptography::Org.BouncyCastle.Crypto.Parameters;
using BouncyCastleCryptography::Org.BouncyCastle.Crypto.Signers;

namespace NextGenSoftware.OASIS.API.Providers.StellarOASIS;

/// <summary>
/// XDR encoding and Ed25519 signing for the one transaction shape OASIS storage submits on Stellar: a single
/// ManageData operation (set or remove one account data entry) from the storage account.
/// </summary>
internal sealed class StellarEd25519Signer
{
    private const int EnvelopeTypeTx = 2;
    private const int ManageDataOperation = 10;
    private readonly Ed25519PrivateKeyParameters _privateKey;
    private readonly byte[] _networkId;

    public StellarEd25519Signer(string privateKeyHex, string networkPassphrase)
    {
        var raw = Convert.FromHexString(privateKeyHex ?? throw new ArgumentNullException(nameof(privateKeyHex)));
        if (raw.Length != 32)
            throw new ArgumentException("A Stellar Ed25519 private key is exactly 32 bytes (64 hex characters).", nameof(privateKeyHex));
        if (string.IsNullOrWhiteSpace(networkPassphrase))
            throw new ArgumentException("An explicit Stellar network passphrase is required.", nameof(networkPassphrase));
        _privateKey = new Ed25519PrivateKeyParameters(raw, 0);
        CryptographicOperations.ZeroMemory(raw);
        PublicKey = _privateKey.GeneratePublicKey().GetEncoded();
        AccountId = EncodeAccountId(PublicKey);
        _networkId = SHA256.HashData(Encoding.UTF8.GetBytes(networkPassphrase));
    }

    public byte[] PublicKey { get; }
    public string AccountId { get; }

    /// <summary>Builds and signs a ManageData transaction; a null value removes the data entry.</summary>
    public (string EnvelopeBase64, string Hash) SignManageData(long sequence, uint fee, string name, byte[] value)
    {
        if (Encoding.ASCII.GetByteCount(name) > 64) throw new ArgumentException("Stellar data entry names are at most 64 bytes.", nameof(name));
        if (value != null && (value.Length == 0 || value.Length > 64)) throw new ArgumentException("Stellar data entry values are 1-64 bytes.", nameof(value));

        var transaction = new XdrWriter();
        transaction.Int32(0);                 // MuxedAccount: KEY_TYPE_ED25519
        transaction.Opaque(PublicKey);        // uint256 (fixed 32 bytes)
        transaction.UInt32(fee);
        transaction.Int64(sequence);
        transaction.Int32(0);                 // Preconditions: PRECOND_NONE
        transaction.Int32(0);                 // Memo: MEMO_NONE
        transaction.UInt32(1);                // operations<100>: one operation
        transaction.UInt32(0);                // operation sourceAccount: absent
        transaction.Int32(ManageDataOperation);
        transaction.String(name);             // string64 dataName
        if (value == null) transaction.UInt32(0);
        else { transaction.UInt32(1); transaction.VarOpaque(value); } // DataValue* dataValue
        transaction.Int32(0);                 // ext: v0
        var body = transaction.ToArray();

        var payload = new XdrWriter();
        payload.Opaque(_networkId);
        payload.Int32(EnvelopeTypeTx);
        var hash = SHA256.HashData(payload.ToArray().Concat(body).ToArray());
        var signer = new Ed25519Signer();
        signer.Init(true, _privateKey);
        signer.BlockUpdate(hash, 0, hash.Length);
        var signature = signer.GenerateSignature();

        var envelope = new XdrWriter();
        envelope.Int32(EnvelopeTypeTx);
        envelope.Opaque(body);
        envelope.UInt32(1);                   // signatures<20>
        envelope.Opaque(PublicKey.AsSpan(28, 4).ToArray()); // SignatureHint: last 4 bytes of the public key
        envelope.VarOpaque(signature);
        return (Convert.ToBase64String(envelope.ToArray()), Convert.ToHexString(hash).ToLowerInvariant());
    }

    internal static string EncodeAccountId(byte[] publicKey)
    {
        var payload = new byte[] { 6 << 3 }.Concat(publicKey).ToArray(); // version byte 'G'
        var crc = Crc16XModem(payload);
        var data = payload.Concat(new[] { (byte)(crc & 0xFF), (byte)(crc >> 8) }).ToArray();
        return Base32(data);
    }

    private static ushort Crc16XModem(byte[] data)
    {
        ushort crc = 0;
        foreach (var b in data)
        {
            crc ^= (ushort)(b << 8);
            for (var i = 0; i < 8; i++) crc = (ushort)((crc & 0x8000) != 0 ? (crc << 1) ^ 0x1021 : crc << 1);
        }
        return crc;
    }

    private static string Base32(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var text = new StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b; bits += 8;
            while (bits >= 5) { text.Append(alphabet[(buffer >> (bits - 5)) & 31]); bits -= 5; }
        }
        if (bits > 0) text.Append(alphabet[(buffer << (5 - bits)) & 31]);
        return text.ToString();
    }

    private sealed class XdrWriter
    {
        private readonly MemoryStream _stream = new();
        public void Int32(int v) => UInt32(unchecked((uint)v));
        public void UInt32(uint v) { for (var s = 24; s >= 0; s -= 8) _stream.WriteByte((byte)(v >> s)); }
        public void Int64(long v) { for (var s = 56; s >= 0; s -= 8) _stream.WriteByte((byte)(v >> s)); }
        public void Opaque(byte[] fixedBytes) { _stream.Write(fixedBytes); Pad(fixedBytes.Length); }
        public void VarOpaque(byte[] bytes) { UInt32((uint)bytes.Length); Opaque(bytes); }
        public void String(string text) => VarOpaque(Encoding.ASCII.GetBytes(text));
        private void Pad(int length) { for (var i = length % 4; i != 0 && i < 4; i++) _stream.WriteByte(0); }
        public byte[] ToArray() => _stream.ToArray();
    }
}
