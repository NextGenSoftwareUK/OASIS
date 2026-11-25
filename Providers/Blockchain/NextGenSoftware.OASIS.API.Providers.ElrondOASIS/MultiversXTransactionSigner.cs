extern alias BouncyCastleCryptography;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BouncyCastleCryptography::Org.BouncyCastle.Crypto.Parameters;
using BouncyCastleCryptography::Org.BouncyCastle.Crypto.Signers;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS;

/// <summary>
/// Signs MultiversX transactions (version 1): the Ed25519 signature covers the canonical JSON of the unsigned
/// transaction with fields in protocol order (nonce, value, receiver, sender, gasPrice, gasLimit, data, chainID,
/// version), data base64-encoded and omitted when empty.
/// </summary>
internal sealed class MultiversXTransactionSigner
{
    private readonly Ed25519PrivateKeyParameters _key;

    public MultiversXTransactionSigner(string secretKeyHex)
    {
        var raw = Convert.FromHexString(secretKeyHex ?? throw new ArgumentNullException(nameof(secretKeyHex)));
        if (raw.Length != 32) throw new ArgumentException("A MultiversX secret key is 32 bytes (64 hex characters).", nameof(secretKeyHex));
        _key = new Ed25519PrivateKeyParameters(raw, 0);
        CryptographicOperations.ZeroMemory(raw);
        Address = ElrondKeys.Address(_key.GeneratePublicKey().GetEncoded());
    }

    public string Address { get; }

    /// <summary>The unsigned canonical form, which is also the body /transaction/cost accepts.</summary>
    public static byte[] Canonical(ulong nonce, string receiver, string sender, ulong gasPrice, ulong gasLimit, byte[] data, string chainId)
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            json.WriteNumber("nonce", nonce);
            json.WriteString("value", "0");
            json.WriteString("receiver", receiver);
            json.WriteString("sender", sender);
            json.WriteNumber("gasPrice", gasPrice);
            json.WriteNumber("gasLimit", gasLimit);
            if (data is { Length: > 0 }) json.WriteString("data", Convert.ToBase64String(data));
            json.WriteString("chainID", chainId);
            json.WriteNumber("version", 1);
            json.WriteEndObject();
        }
        return stream.ToArray();
    }

    /// <summary>Returns the transaction JSON to POST to /transaction/send (canonical fields plus the hex signature).</summary>
    public string Sign(ulong nonce, ulong gasPrice, ulong gasLimit, byte[] data, string chainId)
    {
        var canonical = Canonical(nonce, Address, Address, gasPrice, gasLimit, data, chainId);
        var signer = new Ed25519Signer();
        signer.Init(true, _key);
        signer.BlockUpdate(canonical, 0, canonical.Length);
        var signature = Convert.ToHexString(signer.GenerateSignature()).ToLowerInvariant();
        var unsigned = Encoding.UTF8.GetString(canonical);
        return unsigned[..^1] + ",\"signature\":\"" + signature + "\"}";
    }
}
