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

namespace NextGenSoftware.OASIS.API.Providers.LivepeerOASIS;

internal sealed class LivepeerBondingManagerReader : IDisposable
{
    private const string Endpoint = "https://arb1.arbitrum.io/rpc";
    private const string Contract = "0x35bcf3c30594191d53231e4ff333e8a770453e40";
    private const string Genesis = "0x7ee576b35482195fc49205cec9af72ce14f003b9ae69f6ba0faef4514be8b442";
    internal sealed record Observation(string Address, bool IsActive, string BlockNumber, string BlockHash);

    private readonly HttpClient client;

    public LivepeerBondingManagerReader(HttpMessageHandler? handler = null)
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

    public async Task VerifyChainAsync(CancellationToken token)
    {
        var chain = await CallAsync("eth_chainId", Array.Empty<object>(), 2048, token);
        if (chain.GetString() != "0xa4b1")
            throw new InvalidDataException("Livepeer BondingManager requires Arbitrum One.");

        var genesis = await HeaderAsync("0x0", token);
        if (genesis.Number != "0x0" || genesis.Hash != Genesis)
            throw new InvalidDataException("The Arbitrum One genesis identity differs from the reviewed deployment.");
    }

    public async Task<Observation[]> ReadAsync(IReadOnlyList<string> addresses, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        var (number, hash) = await HeaderAsync("finalized", deadline.Token);
        var rows = new List<Observation>(addresses.Count);
        foreach (var address in addresses)
        {
            var call = new { to = Contract, data = "0x08802374" + address[2..].PadLeft(64, '0') };
            var at = new { blockHash = hash, requireCanonical = true };
            var reply = await CallAsync("eth_call", new object[] { call, at }, 2048, deadline.Token);
            var result = reply.GetString();
            if (!IsHex(result, 64) || result!.AsSpan(2, 63).ContainsAnyExcept('0') || result[^1] is not ('0' or '1'))
                throw new InvalidDataException("Livepeer BondingManager must return one ABI Boolean.");

            rows.Add(new Observation(address, result[^1] == '1', number, hash));
        }
        return rows.ToArray();
    }

    private async Task<(string Number, string Hash)> HeaderAsync(string selector, CancellationToken token)
    {
        var result = await CallAsync("eth_getBlockByNumber", new object[] { selector, false }, 262144, token);
        var number = result.GetProperty("number").GetString();
        var hash = result.GetProperty("hash").GetString();
        if (result.EnumerateObject().Count(field => field.Name is "hash" or "number") != 2 ||
            !IsHex(hash, 64) || number is not { Length: >= 3 and <= 18 } ||
            !IsHex(number, number.Length - 2) || (number.Length > 3 && number[2] == '0'))
            throw new InvalidDataException("Livepeer reads require a canonical Arbitrum block header.");

        return (number, hash!);
    }

    private async Task<JsonElement> CallAsync(string method, object[] parameters, int maximumBytes, CancellationToken token)
    {
        const int id = 1;
        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
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
            throw new InvalidDataException("The Arbitrum RPC response differs from the reviewed request.");

        return root.GetProperty("result").Clone();
    }

    private static bool IsHex(string? value, int digits) => value != null && value.Length == digits + 2 &&
        value.StartsWith("0x", StringComparison.Ordinal) && !value.AsSpan(2).ContainsAnyExcept("0123456789abcdef");

    public void Dispose() => client.Dispose();
}
