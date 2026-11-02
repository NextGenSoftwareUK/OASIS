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
        var array = await Get("v1/providers", 2097152, deadline.Token);
        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() is < 3 or > 1024)
            throw new InvalidDataException("Bounded provider discovery array required.");
        var owners = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in array.EnumerateArray())
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in row.EnumerateObject())
                if (!names.Add(field.Name)) throw new InvalidDataException("Duplicate provider field.");
            var owner = row.GetProperty("owner").GetString()!;
            AkashPublicProviderCodec.ValidateOwner(owner);
            if (!owners.Add(owner)) throw new InvalidDataException("Duplicate provider owner.");
        }
        return Array.AsReadOnly(owners.OrderBy(key => key, StringComparer.Ordinal).Take(3).ToArray());
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
            var root = await Get("v1/providers/" + Uri.EscapeDataString(owner), 262144, deadline.Token);
            rows.Add(AkashPublicProviderCodec.Decode(root, owner));
        }
        deadline.Token.ThrowIfCancellationRequested();
        return rows.AsReadOnly();
    }

    private async Task<JsonElement> Get(string path, int maximumBytes, CancellationToken token)
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
            using var bytes = new MemoryStream();
            var buffer = new byte[4096]; int count;
            while ((count = await stream.ReadAsync(buffer, token)) != 0)
            {
                if (bytes.Length + count > maximumBytes) throw new InvalidDataException("Akash response byte budget exceeded.");
                bytes.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
            token.ThrowIfCancellationRequested();
            return document.RootElement.Clone();
        }
        finally { Gate.Release(); }
    }
}
