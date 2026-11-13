using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.RitualOASIS;

/// <summary>Bounded public block reads against the qualified Ritual Chain genesis.</summary>
internal sealed class RitualPublicBlockReader : IDisposable
{
    private readonly HttpClient client;
    private static readonly Uri Endpoint = new("https://rpc.ritualfoundation.org");
    private const string GenesisHash = "0x220f7b8da21377ee959d6bf3dc6397ffb5ec0737c37b08974c08247d0822ba93";
    internal sealed record Observation(long Height, string BlockHash, string Timestamp);

    public RitualPublicBlockReader()
    {
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        handler.UseProxy = false;
        client = new HttpClient(handler);
    }

    public async Task<IReadOnlyList<Observation>> ReadAsync(IReadOnlyList<long> heights, CancellationToken cancellationToken)
    {
        if (heights.Count is < 1 or > 8 || new HashSet<long>(heights).Count != heights.Count)
            throw new ArgumentException("One to eight unique explicit Ritual heights are required.");
        foreach (var height in heights)
            if (height <= 0) throw new ArgumentException("Ritual heights must be positive.");

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        var token = deadline.Token;
        var aggregate = 0;

        async Task<JsonElement> Rpc(string method, object[] parameters)
        {
            const int requestId = 1;
            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new { jsonrpc = "2.0", id = requestId, method, @params = parameters }),
                    Encoding.UTF8, "application/json")
            };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            await using var body = await response.Content.ReadAsStreamAsync(token);
            using var bounded = new MemoryStream();
            var buffer = new byte[4096];
            int count;
            while ((count = await body.ReadAsync(buffer, token)) != 0)
            {
                aggregate += count;
                if (bounded.Length + count > 262144 || aggregate > 1048576)
                    throw new InvalidDataException("Ritual RPC response budget exceeded.");
                bounded.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(bounded.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            var fields = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in root.EnumerateObject())
                if (!fields.Add(field.Name)) throw new InvalidDataException("Duplicate RPC envelope field.");
            if (root.GetProperty("jsonrpc").GetString() != "2.0" || root.GetProperty("id").GetInt32() != requestId)
                throw new InvalidDataException("Ritual RPC envelope mismatch.");
            if (root.TryGetProperty("error", out _))
                throw new InvalidDataException("Ritual RPC returned an error.");
            return root.GetProperty("result").Clone();
        }

        if ((await Rpc("eth_chainId", Array.Empty<object>())).GetString() != "0x310")
            throw new InvalidDataException("Ritual chain ID 784 is required.");
        var genesis = await Rpc("eth_getBlockByNumber", new object[] { "0x0", false });
        if (genesis.GetProperty("number").GetString() != "0x0" ||
            genesis.GetProperty("hash").GetString() != GenesisHash)
            throw new InvalidDataException("Ritual genesis identity mismatch.");

        var records = new List<Observation>(heights.Count);
        foreach (var height in heights)
        {
            var key = $"0x{height:x}";
            var block = await Rpc("eth_getBlockByNumber", new object[] { key, false });
            var hash = block.GetProperty("hash").GetString();
            var timestamp = block.GetProperty("timestamp").GetString();
            if (block.GetProperty("number").GetString() != key || !Hex(hash, 64) ||
                timestamp == null || timestamp.Length is < 3 or > 18 ||
                !timestamp.StartsWith("0x", StringComparison.Ordinal) ||
                timestamp.AsSpan(2).ContainsAnyExcept("0123456789abcdef") ||
                (timestamp.Length > 3 && timestamp[2] == '0'))
                throw new InvalidDataException($"Ritual returned an invalid block for height {height}.");
            records.Add(new Observation(height, hash!, timestamp));
        }
        return records.AsReadOnly();
    }

    public void Dispose() => client.Dispose();

    private static bool Hex(string? value, int digits) => value != null && value.Length == digits + 2 &&
        value.StartsWith("0x", StringComparison.Ordinal) && !value.AsSpan(2).ContainsAnyExcept("0123456789abcdef");
}
