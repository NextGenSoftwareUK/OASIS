using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.AkashOASIS;

/// <summary>Bounded indexed provider reads. Never follows hostUri or provisions compute.</summary>
public sealed class AkashPublicProviderReader
{
    private readonly HttpClient client;
    private readonly Uri endpoint;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static long lastStart;

    public AkashPublicProviderReader(HttpClient client, Uri endpoint)
    {
        if (endpoint.Scheme != "https" || endpoint.AbsolutePath != "/" || endpoint.AbsoluteUri.Length > 256 ||
            !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment) ||
            client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie") || client.DefaultRequestHeaders.Contains("x-api-key"))
            throw new ArgumentException("Explicit anonymous HTTPS API root required.");
        this.client = client; this.endpoint = endpoint;
    }

    /// <summary>Initial qualification only. Query execution must use the retained catalogue, not rediscover it.</summary>
    public async Task<IReadOnlyList<string>> DiscoverThreeAsync(CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        return await Get("v1/providers", ReadDiscoveryPrefix, deadline.Token);
    }

    public async Task<IReadOnlyList<AkashPublicProviderCodec.Provider>> ReadAsync(IReadOnlyList<string> owners, CancellationToken cancellationToken)
    {
        if (owners.Count is < 1 or > 8 || owners.Distinct(StringComparer.Ordinal).Count() != owners.Count)
            throw new ArgumentException("One to eight unique configured owners required.");
        foreach (var owner in owners) AkashPublicProviderCodec.ValidateOwner(owner);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var rows = new List<AkashPublicProviderCodec.Provider>();
        foreach (var owner in owners)
        {
            var root = await Get("v1/providers/" + Uri.EscapeDataString(owner), ReadPoint, deadline.Token);
            rows.Add(AkashPublicProviderCodec.Decode(root, owner));
        }
        deadline.Token.ThrowIfCancellationRequested();
        return rows.AsReadOnly();
    }

    // Select a sample, not a globally sorted network catalogue. Stop after three
    // complete records; the unconsumed response tail is deliberately not certified.
    private static async Task<IReadOnlyList<string>> ReadDiscoveryPrefix(Stream stream, CancellationToken token)
    {
        const int maximumBytes = 2097152;
        const int maximumBufferedRowBytes = 262144;
        var buffer = new byte[4096];
        var state = new JsonReaderState(new JsonReaderOptions { MaxDepth = 16 });
        var owners = new List<string>();
        var buffered = 0;
        var total = 0;
        var final = false;
        while (true)
        {
            var consumed = ConsumePrefix(buffer.AsSpan(0, buffered), final, ref state, owners);
            token.ThrowIfCancellationRequested();
            if (owners.Count == 3)
                return Array.AsReadOnly(owners.OrderBy(key => key, StringComparer.Ordinal).ToArray());

            buffer.AsSpan(consumed, buffered - consumed).CopyTo(buffer);
            buffered -= consumed;
            if (final) throw new InvalidDataException("Three complete provider records required.");
            if (buffered == buffer.Length)
            {
                if (buffer.Length == maximumBufferedRowBytes)
                    throw new InvalidDataException("Akash discovery record byte budget exceeded.");
                Array.Resize(ref buffer, Math.Min(buffer.Length * 2, maximumBufferedRowBytes));
            }
            var allowance = Math.Min(buffer.Length - buffered, maximumBytes - total);
            if (allowance == 0) throw new InvalidDataException("Akash discovery byte budget exceeded.");
            var count = await stream.ReadAsync(buffer.AsMemory(buffered, allowance), token);
            buffered += count;
            total += count;
            final = count == 0;
        }
    }

    private static int ConsumePrefix(ReadOnlySpan<byte> bytes, bool final,
        ref JsonReaderState state, List<string> owners)
    {
        var reader = new Utf8JsonReader(bytes, final, state);
        if (reader.TokenType == JsonTokenType.None)
        {
            if (!reader.Read())
            {
                state = reader.CurrentState;
                return checked((int)reader.BytesConsumed);
            }
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new InvalidDataException("Provider discovery array required.");
        }
        while (owners.Count < 3)
        {
            var previous = reader;
            if (!reader.Read())
            {
                state = reader.CurrentState;
                return checked((int)reader.BytesConsumed);
            }
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new InvalidDataException("Three provider objects required.");
            if (!JsonDocument.TryParseValue(ref reader, out var document))
            {
                state = previous.CurrentState;
                return checked((int)previous.BytesConsumed);
            }
            using (document)
            {
                var row = document.RootElement;
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var field in row.EnumerateObject())
                    if (!names.Add(field.Name)) throw new InvalidDataException("Duplicate provider field.");
                if (!row.TryGetProperty("owner", out var value) || value.ValueKind != JsonValueKind.String)
                    throw new InvalidDataException("Provider owner string required.");
                var owner = value.GetString()!;
                AkashPublicProviderCodec.ValidateOwner(owner);
                if (owners.Contains(owner, StringComparer.Ordinal))
                    throw new InvalidDataException("Duplicate provider owner.");
                owners.Add(owner);
            }
        }
        state = reader.CurrentState;
        return checked((int)reader.BytesConsumed);
    }

    private static async Task<JsonElement> ReadPoint(Stream stream, CancellationToken token)
    {
        using var bytes = new MemoryStream();
        var buffer = new byte[4096];
        int count;
        while ((count = await stream.ReadAsync(buffer, token)) != 0)
        {
            if (bytes.Length + count > 262144) throw new InvalidDataException("Akash response byte budget exceeded.");
            bytes.Write(buffer, 0, count);
        }
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
        token.ThrowIfCancellationRequested();
        return document.RootElement.Clone();
    }

    private async Task<T> Get<T>(string path, Func<Stream, CancellationToken, Task<T>> read, CancellationToken token)
    {
        // Conservative process-local admission, not an asserted upstream SLA.
        await Gate.WaitAsync(token);
        try
        {
            var delay = TimeSpan.FromSeconds(1) - Stopwatch.GetElapsedTime(lastStart);
            if (lastStart != 0 && delay > TimeSpan.Zero) await Task.Delay(delay, token);
            lastStart = Stopwatch.GetTimestamp();
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(endpoint, path));
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(token);
            return await read(stream, token);
        }
        finally { Gate.Release(); }
    }
}
