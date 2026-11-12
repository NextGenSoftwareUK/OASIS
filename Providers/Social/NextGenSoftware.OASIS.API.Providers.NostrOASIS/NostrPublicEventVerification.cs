using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.NostrOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.NostrOASIS;

/// <summary>Verified immutable kind-1 event data, not real-world identity or relay completeness.</summary>
public sealed record NostrVerifiedPublicEvent(string Id, string PublicKey, long CreatedAt,
    string Content, string TagsJson, string Signature);

/// <summary>
/// Single public-event integrity boundary for the bounded CQL source. No private
/// keys, signing, native I/O, image retrieval or legacy-provider behavior here.
/// </summary>
public static class NostrPublicEventVerification
{
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public static NostrVerifiedPublicEvent Parse(string json)
    {
        if (json == null || Utf8.GetByteCount(json) > 65536)
            throw new InvalidDataException("Bounded public event required.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 24 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Event object required.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in root.EnumerateObject())
            if (!names.Add(property.Name)) throw new InvalidDataException("Duplicate event property.");
        if (names.Count > 16 || new[] { "id", "pubkey", "created_at", "kind", "tags", "content", "sig" }.Any(name => !names.Contains(name)))
            throw new InvalidDataException("Complete bounded signed event required.");
        string id = Hex(root.GetProperty("id"), 64);
        string key = Hex(root.GetProperty("pubkey"), 64);
        string signature = Hex(root.GetProperty("sig"), 128);
        var kind = root.GetProperty("kind");
        var timestamp = root.GetProperty("created_at");
        if (kind.ValueKind != JsonValueKind.Number || kind.GetRawText() != "1" ||
            timestamp.ValueKind != JsonValueKind.Number ||
            !Regex.IsMatch(timestamp.GetRawText(), "\\A(?:0|[1-9][0-9]*)\\z") ||
            !timestamp.TryGetInt64(out long createdAt) || createdAt > 9007199254740991)
            throw new InvalidDataException("Exact bounded kind-1 Unix timestamp required.");
        string content = Text(root.GetProperty("content"), 32768);
        var tags = root.GetProperty("tags");
        if (tags.ValueKind != JsonValueKind.Array || tags.GetArrayLength() > 128)
            throw new InvalidDataException("Bounded tags required.");
        var arrays = new List<string>();
        foreach (var tag in tags.EnumerateArray())
        {
            if (tag.ValueKind != JsonValueKind.Array || tag.GetArrayLength() is < 1 or > 16)
                throw new InvalidDataException("Bounded nonempty string-array tags required.");
            arrays.Add("[" + string.Join(",", tag.EnumerateArray().Select(value => Quote(Text(value, 4096)))) + "]");
        }
        string tagsJson = "[" + string.Join(",", arrays) + "]";
        string canonical = "[0," + Quote(key) + "," + createdAt.ToString(CultureInfo.InvariantCulture) +
            ",1," + tagsJson + "," + Quote(content) + "]";
        byte[] hash = SHA256.HashData(Utf8.GetBytes(canonical));
        if (!CryptographicOperations.FixedTimeEquals(hash, Convert.FromHexString(id)))
            throw new InvalidDataException("NIP-01 event hash mismatch.");
        if (!VerifySchnorr(Convert.FromHexString(key), hash, Convert.FromHexString(signature)))
            throw new InvalidDataException("BIP-340 event signature rejected.");
        return new(id, key, createdAt, content, tagsJson, signature);
    }

    private static string Hex(JsonElement element, int length)
    {
        string value = Text(element, length);
        if (!Regex.IsMatch(value, "\\A[0-9a-f]{" + length + "}\\z"))
            throw new InvalidDataException("Canonical lowercase event hex required.");
        return value;
    }

    private static string Text(JsonElement element, int bound)
    {
        if (element.ValueKind != JsonValueKind.String) throw new InvalidDataException("Native string required.");
        string value = element.GetString()!;
        if (Utf8.GetByteCount(value) > bound) throw new InvalidDataException("Native string exceeds bound.");
        return value;
    }

    // NIP-01 uses literal UTF8, including U+2028/U+2029 and supplementary scalars.
    // General-purpose JSON encoders can escape these and change a valid event id.
    internal static string Quote(string value)
    {
        _ = Utf8.GetByteCount(value); // Reject unpaired UTF16 before serialization.
        var result = new StringBuilder("\"");
        foreach (char character in value)
        {
            result.Append(character switch
            {
                '"' => "\\\"", '\\' => "\\\\", '\n' => "\\n", '\r' => "\\r", '\t' => "\\t",
                '\b' => "\\b", '\f' => "\\f", < ' ' => "\\u" + ((int)character).ToString("x4", CultureInfo.InvariantCulture),
                _ => character.ToString()
            });
        }
        return result.Append('"').ToString();
    }

    private static readonly BigInteger Field = Integer("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEFFFFFC2F");
    private static readonly BigInteger Order = Integer("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFEBAAEDCE6AF48A03BBFD25E8CD0364141");
    private sealed record Point(BigInteger X, BigInteger Y);
    private static readonly Point Generator = new(
        Integer("79BE667EF9DCBBAC55A06295CE870B07029BFCDB2DCE28D959F2815B16F81798"),
        Integer("483ADA7726A3C4655DA4FBFC0E1108A8FD17B448A68554199C47D08FFB10D4B8"));

    // All inputs are public verification data. This deliberately contains no signer.
    internal static bool VerifySchnorr(byte[] publicKey, byte[] message, byte[] signature)
    {
        if (publicKey.Length != 32 || signature.Length != 64 || message.Length > 1024) return false;
        var x = Integer(publicKey); var r = Integer(signature.AsSpan(0, 32)); var s = Integer(signature.AsSpan(32, 32));
        if (x >= Field || r >= Field || s >= Order) return false;
        var square = Mod(x * x * x + 7, Field);
        var y = BigInteger.ModPow(square, (Field + 1) / 4, Field);
        if (Mod(y * y, Field) != square) return false;
        if (!y.IsEven) y = Field - y;
        byte[] tag = SHA256.HashData(Encoding.ASCII.GetBytes("BIP0340/challenge"));
        byte[] payload = new byte[128 + message.Length];
        tag.CopyTo(payload, 0); tag.CopyTo(payload, 32);
        signature.AsSpan(0, 32).CopyTo(payload.AsSpan(64)); publicKey.CopyTo(payload, 96); message.CopyTo(payload, 128);
        var e = Mod(Integer(SHA256.HashData(payload)), Order);
        Point? result = Add(Multiply(Generator, s), Multiply(new(x, y), Mod(Order - e, Order)));
        return result != null && result.Y.IsEven && result.X == r;
    }

    private static Point? Add(Point? left, Point? right)
    {
        if (left == null) return right;
        if (right == null) return left;
        if (left.X == right.X && Mod(left.Y + right.Y, Field).IsZero) return null;
        var slope = left == right
            ? Mod(3 * left.X * left.X * BigInteger.ModPow(2 * left.Y, Field - 2, Field), Field)
            : Mod((right.Y - left.Y) * BigInteger.ModPow(Mod(right.X - left.X, Field), Field - 2, Field), Field);
        var x = Mod(slope * slope - left.X - right.X, Field);
        return new(x, Mod(slope * (left.X - x) - left.Y, Field));
    }

    private static Point? Multiply(Point point, BigInteger scalar)
    {
        Point? result = null;
        Point? current = point;
        while (scalar > 0)
        {
            if (!scalar.IsEven) result = Add(result, current);
            current = Add(current, current);
            scalar >>= 1;
        }
        return result;
    }

    private static BigInteger Mod(BigInteger value, BigInteger modulus) => (value % modulus + modulus) % modulus;
    private static BigInteger Integer(ReadOnlySpan<byte> bytes) => new(bytes, isUnsigned: true, isBigEndian: true);
    private static BigInteger Integer(string hex) => Integer(Convert.FromHexString(hex));
}
