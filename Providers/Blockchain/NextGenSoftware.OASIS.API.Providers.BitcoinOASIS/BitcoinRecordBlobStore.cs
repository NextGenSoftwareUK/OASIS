using System;
using System.Net.Http;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

/// <summary>Stores encrypted record bytes in Kubo and verifies readback against a Bitcoin commitment.</summary>
internal sealed class BitcoinRecordBlobStore(HttpClient kubo, EncryptedRecordCodec codec)
{
    private const int MaximumEnvelopeBytes = 6 * 1024 * 1024;
    private static ReadOnlySpan<byte> Magic => "OASBTC1"u8;

    public static bool IsCommitment(ReadOnlySpan<byte> payload) => payload.StartsWith(Magic);

    public async Task<byte[]> PutAsync(string recordScope, string canonicalJson)
    {
        var bytes = Encoding.UTF8.GetBytes(codec.Encrypt(recordScope, canonicalJson));
        if (bytes.Length > MaximumEnvelopeBytes)
            throw new InvalidOperationException("The encrypted Bitcoin record exceeds the blob size limit.");

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent(bytes), "file", "record");
        using var response = await kubo.PostAsync("api/v0/add?cid-version=0&pin=true", form);
        response.EnsureSuccessStatusCode();
        using var acknowledgement = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var cid = acknowledgement.RootElement.GetProperty("Hash").GetString();

        var commitment = new byte[Magic.Length + 34 + 32];
        Magic.CopyTo(commitment);
        DecodeCid(cid).CopyTo(commitment.AsSpan(Magic.Length, 34));
        SHA256.HashData(bytes, commitment.AsSpan(Magic.Length + 34));
        await ReadVerifiedEnvelopeAsync(commitment);
        return commitment;
    }

    public async Task<string> GetAsync(string recordScope, byte[] commitment)
    {
        var bytes = await ReadVerifiedEnvelopeAsync(commitment);
        return codec.Decrypt(recordScope, Encoding.UTF8.GetString(bytes));
    }

    private async Task<byte[]> ReadVerifiedEnvelopeAsync(byte[] commitment)
    {
        if (commitment.Length != Magic.Length + 34 + 32 || !commitment.AsSpan(0, Magic.Length).SequenceEqual(Magic))
            throw new FormatException("The Bitcoin record commitment is invalid.");

        var cid = EncodeCid(commitment.AsSpan(Magic.Length, 34));
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/v0/cat?arg={cid}&length={MaximumEnvelopeBytes + 1}");
        using var response = await kubo.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync();
        var bytes = new byte[MaximumEnvelopeBytes + 1];
        var length = await stream.ReadAtLeastAsync(bytes, bytes.Length, throwIfEndOfStream: false);
        if (length > MaximumEnvelopeBytes ||
            !CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes.AsSpan(0, length)), commitment.AsSpan(Magic.Length + 34)))
            throw new CryptographicException("The encrypted record does not match the Bitcoin commitment.");
        return bytes.AsSpan(0, length).ToArray();
    }

    private const string Base58 = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    private static byte[] DecodeCid(string cid)
    {
        if (cid == null || cid.Length != 46 || !cid.StartsWith("Qm", StringComparison.Ordinal))
            throw new FormatException("Kubo did not return a CIDv0 record identifier.");

        var value = BigInteger.Zero;
        foreach (var character in cid)
        {
            var digit = Base58.IndexOf(character);
            if (digit < 0)
                throw new FormatException("The Bitcoin record contains an invalid CIDv0 identifier.");
            value = value * 58 + digit;
        }

        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (bytes.Length != 34 || bytes[0] != 0x12 || bytes[1] != 0x20)
            throw new FormatException("The Bitcoin record requires a SHA-256 CIDv0 identifier.");
        return bytes;
    }

    private static string EncodeCid(ReadOnlySpan<byte> bytes)
    {
        if (bytes[0] != 0x12 || bytes[1] != 0x20)
            throw new FormatException("The Bitcoin record requires a SHA-256 CIDv0 identifier.");

        var value = new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
        Span<char> result = stackalloc char[46];
        for (var index = result.Length - 1; index >= 0; index--)
        {
            value = BigInteger.DivRem(value, 58, out var digit);
            result[index] = Base58[(int)digit];
        }

        return new string(result);
    }
}
