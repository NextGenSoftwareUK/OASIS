using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using NBitcoin.Secp256k1;

namespace NextGenSoftware.OASIS.API.Providers.NostrOASIS;

/// <summary>NIP-01 event identity and constant-time BIP-340 Schnorr signing/verification (libsecp256k1 port).</summary>
internal sealed class NostrEventSigner
{
    private readonly ECPrivKey _key;

    public NostrEventSigner(string secretKeyHex)
    {
        var raw = Convert.FromHexString(secretKeyHex ?? throw new ArgumentNullException(nameof(secretKeyHex)));
        if (raw.Length != 32 || !ECPrivKey.TryCreate(raw, out var key))
            throw new ArgumentException("A Nostr secret key is a valid 32-byte secp256k1 scalar (64 hex characters).", nameof(secretKeyHex));
        CryptographicOperations.ZeroMemory(raw);
        _key = key;
        PublicKeyHex = Convert.ToHexString(_key.CreateXOnlyPubKey().ToBytes()).ToLowerInvariant();
    }

    public string PublicKeyHex { get; }

    /// <summary>Builds a signed NIP-01 event.</summary>
    public JsonObject Sign(int kind, long createdAt, JsonArray tags, string content)
    {
        var id = EventId(PublicKeyHex, createdAt, kind, tags, content);
        var signature = new byte[64];
        _key.SignBIP340(id).WriteToSpan(signature);
        return new JsonObject
        {
            ["id"] = Convert.ToHexString(id).ToLowerInvariant(), ["pubkey"] = PublicKeyHex, ["created_at"] = createdAt,
            ["kind"] = kind, ["tags"] = tags.DeepClone(), ["content"] = content,
            ["sig"] = Convert.ToHexString(signature).ToLowerInvariant()
        };
    }

    /// <summary>True only when the event id is the NIP-01 hash of its fields and the signature verifies for its pubkey.</summary>
    public static bool Verify(JsonElement evt)
    {
        var pubkey = Convert.FromHexString(evt.GetProperty("pubkey").GetString()!);
        var tags = JsonNode.Parse(evt.GetProperty("tags").GetRawText())!.AsArray();
        var id = EventId(evt.GetProperty("pubkey").GetString()!, evt.GetProperty("created_at").GetInt64(),
            evt.GetProperty("kind").GetInt32(), tags, evt.GetProperty("content").GetString()!);
        if (!string.Equals(Convert.ToHexString(id), evt.GetProperty("id").GetString(), StringComparison.OrdinalIgnoreCase)) return false;
        return ECXOnlyPubKey.TryCreate(pubkey, out var key) &&
               SecpSchnorrSignature.TryCreate(Convert.FromHexString(evt.GetProperty("sig").GetString()!), out var signature) &&
               key.SigVerifyBIP340(signature, id);
    }

    private static byte[] EventId(string pubkeyHex, long createdAt, int kind, JsonArray tags, string content)
    {
        // NIP-01 canonical serialisation: [0, pubkey, created_at, kind, tags, content] without whitespace.
        var canonical = new JsonArray(0, pubkeyHex, createdAt, kind, tags.DeepClone(), content)
            .ToJsonString(new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        return System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
    }
}
