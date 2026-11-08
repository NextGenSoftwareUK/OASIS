#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GelatoNetworkOASIS;

/// <summary>Read-only supported Gasless RPC client; never uses the deprecated Relay oracle.</summary>
public sealed class GelatoGaslessCapabilitiesClient : IDisposable
{
    public const string Endpoint = "https://api.gelato.cloud/rpc";
    private readonly HttpClient client;
    private readonly string apiKey;
    private static readonly BigInteger MaximumChain = (BigInteger.One << 256) - 1;

    public GelatoGaslessCapabilitiesClient(string apiKey, HttpMessageHandler? handler = null)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length > 2048 || apiKey.TrimEnd('=').Length == 0 || apiKey.TrimEnd('=').Contains('='))
            throw new ArgumentException("A configured Gasless API key is required.", nameof(apiKey));
        foreach (var character in apiKey)
            if (!(char.IsAsciiLetterOrDigit(character) || "-._~+/=".Contains(character)))
                throw new ArgumentException("The Gasless API key is not a Bearer token.", nameof(apiKey));
        this.apiKey = apiKey;
        client = handler is null
            ? new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false,
                UseCookies = false, ConnectTimeout = TimeSpan.FromSeconds(5) })
            : new HttpClient(handler, disposeHandler: false);
        client.Timeout = TimeSpan.FromSeconds(15);
    }

    public async Task<OASISResult<GaslessCapabilitiesObservation>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = new OASISResult<GaslessCapabilitiesObservation>();
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"relayer_getCapabilities\",\"params\":[]}", Encoding.UTF8, "application/json");
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Failure(result, "GELATO_GASLESS_HTTP", "Gasless capability HTTP status " + (int)response.StatusCode + ".");
            var contentType = response.Content.Headers.ContentType;
            if (!string.Equals(contentType?.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
                (contentType.CharSet is not null && !string.Equals(contentType.CharSet.Trim('"'), "utf-8", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException();
            if (response.Content.Headers.ContentLength > 131072) throw new InvalidDataException();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var buffer = new MemoryStream();
            var block = new byte[4096];
            while (true)
            {
                var count = await stream.ReadAsync(block.AsMemory(), cancellationToken);
                if (count == 0) break;
                if (buffer.Length + count > 131072) throw new InvalidDataException();
                buffer.Write(block, 0, count);
            }
            var raw = buffer.ToArray();
            var text = new UTF8Encoding(false, true).GetString(raw);
            using var document = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 32 });
            var envelope = document.RootElement;
            UniqueFields(envelope);
            if (envelope.ValueKind != JsonValueKind.Object || envelope.GetProperty("jsonrpc").GetString() != "2.0" ||
                !envelope.GetProperty("id").TryGetInt32(out var id) || id != 1)
                throw new InvalidDataException();
            if (envelope.TryGetProperty("error", out _))
                return Failure(result, "GELATO_GASLESS_RPC", "Gasless capability RPC returned an error.");
            var capabilities = envelope.GetProperty("result");
            if (capabilities.ValueKind != JsonValueKind.Object) throw new InvalidDataException();
            var chains = 0;
            foreach (var chain in capabilities.EnumerateObject())
            {
                if (++chains > 128 || chain.Name.Length is < 1 or > 78 || chain.Name[0] == '0' ||
                    !BigInteger.TryParse(chain.Name, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ||
                    number <= 0 || number > MaximumChain || number.ToString(CultureInfo.InvariantCulture) != chain.Name ||
                    chain.Value.ValueKind != JsonValueKind.Object || !Address(chain.Value.GetProperty("feeCollector")))
                    throw new InvalidDataException();
                var tokens = chain.Value.GetProperty("tokens");
                if (tokens.ValueKind != JsonValueKind.Array || tokens.GetArrayLength() > 64) throw new InvalidDataException();
                var addresses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var token in tokens.EnumerateArray())
                    if (token.ValueKind != JsonValueKind.Object || !Address(token.GetProperty("address")) ||
                        !addresses.Add(token.GetProperty("address").GetString()!) ||
                        !token.GetProperty("decimals").TryGetInt32(out var decimals) || decimals is < 0 or > 255)
                        throw new InvalidDataException();
            }
            if (chains == 0) throw new InvalidDataException();
            result.Result = new GaslessCapabilitiesObservation(capabilities.Clone(), text,
                Convert.ToHexStringLower(SHA256.HashData(raw)), DateTimeOffset.UtcNow);
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is HttpRequestException or IOException or InvalidDataException or JsonException or
            InvalidOperationException or KeyNotFoundException or DecoderFallbackException or FormatException)
        {
            // No server body, auth header or transport exception text enters public diagnostics.
            return Failure(result, "GELATO_GASLESS_INVALID_RESPONSE", "Gasless capability transport or response validation failed.");
        }
    }

    private static OASISResult<GaslessCapabilitiesObservation> Failure(OASISResult<GaslessCapabilitiesObservation> result, string code, string message)
    { result.IsError = true; result.ErrorCode = code; result.Message = message; return result; }

    private static bool Address(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String) return false;
        var text = value.GetString()!;
        if (text.Length != 42 || !text.StartsWith("0x", StringComparison.Ordinal)) return false;
        for (var i = 2; i < text.Length; i++) if (!Uri.IsHexDigit(text[i])) return false;
        return true;
    }

    private static void UniqueFields(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in element.EnumerateObject())
            { if (!names.Add(field.Name)) throw new InvalidDataException(); UniqueFields(field.Value); }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) UniqueFields(item);
    }
    public void Dispose() => client.Dispose();
}

public sealed record GaslessCapabilitiesObservation(JsonElement Capabilities, string RawJson, string RawSha256, DateTimeOffset ObservedAt);
