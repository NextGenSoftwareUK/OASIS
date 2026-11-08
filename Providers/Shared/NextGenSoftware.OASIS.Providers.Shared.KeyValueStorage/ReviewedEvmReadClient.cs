#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Bounded unsigned EVM reads for an explicitly reviewed contract profile.</summary>
public sealed class ReviewedEvmReadClient : IDisposable
{
    public sealed record Block(string Number, string Hash);

    private readonly HttpClient client;

    public ReviewedEvmReadClient(HttpMessageHandler? handler = null)
    {
        if (handler is null)
        {
            var transport = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            transport.UseProxy = false;
            transport.UseCookies = false;
            handler = transport;
        }
        client = new HttpClient(handler);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("OASISAPI/1.0");
    }

    public async Task VerifyChainAsync(Uri origin, string chainId, string genesisHash, CancellationToken token)
    {
        var chain = await CallAsync(origin, "eth_chainId", Array.Empty<object>(), 2048, token);
        if (chain.GetString() != chainId)
            throw new InvalidDataException("The RPC chain differs from the reviewed deployment.");

        var genesis = await HeaderAsync(origin, "0x0", token);
        if (genesis.Number != "0x0" || genesis.Hash != genesisHash)
            throw new InvalidDataException("The RPC genesis differs from the reviewed deployment.");
    }

    public Task<Block> ReadFinalizedBlockAsync(Uri origin, CancellationToken token) =>
        HeaderAsync(origin, "finalized", token);

    public async Task<Block> VerifyCanonicalBlockAsync(Uri origin, Block expected, CancellationToken token)
    {
        var observed = await HeaderAsync(origin, expected.Number, token);
        if (observed != expected)
            throw new InvalidDataException("The canonical block differs from the reviewed observation.");
        return observed;
    }

    public async Task<string> ReadDeployedCodeAsync(Uri origin, string contract, Block block, CancellationToken token)
    {
        var at = new { blockHash = block.Hash, requireCanonical = true };
        var result = await CallAsync(origin, "eth_getCode", new object[] { contract, at }, 65536, token);
        var code = result.GetString();
        if (code is not { Length: >= 4 } || code.Length % 2 != 0 || !IsHex(code, code.Length - 2))
            throw new InvalidDataException("The reviewed contract must have nonempty canonical EVM code.");
        return code;
    }

    public async Task<string> ReadAbiWordAsync(Uri origin, string contract, string data, Block block, CancellationToken token)
    {
        var call = new { to = contract, data };
        var at = new { blockHash = block.Hash, requireCanonical = true };
        var reply = await CallAsync(origin, "eth_call", new object[] { call, at }, 2048, token);
        var word = reply.GetString();
        if (!IsHex(word, 64))
            throw new InvalidDataException("The reviewed EVM call must return one canonical ABI word.");

        return word!;
    }

    private async Task<Block> HeaderAsync(Uri origin, string selector, CancellationToken token)
    {
        var result = await CallAsync(origin, "eth_getBlockByNumber", new object[] { selector, false }, 262144, token);
        var number = result.GetProperty("number").GetString();
        var hash = result.GetProperty("hash").GetString();
        if (result.EnumerateObject().Count(field => field.Name is "hash" or "number") != 2 ||
            !IsHex(hash, 64) || number is not { Length: >= 3 and <= 18 } ||
            !IsHex(number, number.Length - 2) || (number.Length > 3 && number[2] == '0'))
            throw new InvalidDataException("A canonical EVM block header is required.");

        return new Block(number, hash!);
    }

    private async Task<JsonElement> CallAsync(Uri origin, string method, object[] parameters, int maximumBytes, CancellationToken token)
    {
        const int id = 1;
        using var request = new HttpRequestMessage(HttpMethod.Post, origin)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { jsonrpc = "2.0", id, method, @params = parameters }),
                Encoding.UTF8, "application/json")
        };
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        await response.Content.LoadIntoBufferAsync(maximumBytes, token);
        using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token),
            new JsonDocumentOptions { MaxDepth = 24 });
        var root = document.RootElement;
        var fields = new HashSet<string>(StringComparer.Ordinal);
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Any(field => !fields.Add(field.Name)) ||
            root.GetProperty("jsonrpc").GetString() != "2.0" || root.GetProperty("id").GetInt32() != id ||
            root.TryGetProperty("error", out _))
            throw new InvalidDataException("The EVM RPC response differs from the reviewed request.");

        return root.GetProperty("result").Clone();
    }

    private static bool IsHex(string? value, int digits) => value != null && value.Length == digits + 2 &&
        value.StartsWith("0x", StringComparison.Ordinal) && !value.AsSpan(2).ContainsAnyExcept("0123456789abcdef");

    public void Dispose() => client.Dispose();
}
