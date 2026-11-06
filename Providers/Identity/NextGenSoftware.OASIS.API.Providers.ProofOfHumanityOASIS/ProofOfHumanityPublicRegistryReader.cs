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

namespace NextGenSoftware.OASIS.API.Providers.ProofOfHumanityOASIS;

/// <summary>Reads the documented cross-chain registry on each reviewed mainnet.</summary>
internal sealed class ProofOfHumanityPublicRegistryReader : IDisposable
{
    internal sealed record Snapshot(string Chain, string BlockNumber, string BlockHash, IReadOnlyDictionary<string, bool> Records);

    private readonly HttpClient client;

    public ProofOfHumanityPublicRegistryReader(HttpMessageHandler? transport = null)
    {
        if (transport == null)
        {
            var guarded = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
            guarded.UseProxy = false;
            guarded.UseCookies = false;
            transport = guarded;
        }
        client = new HttpClient(transport);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("OASISAPI/1.0");
    }

    internal static bool IsCanonicalAddress(string? value) => IsHex(value, 40);

    public async Task<IReadOnlyList<Snapshot>> ReadAsync(IReadOnlyList<string> addresses, CancellationToken cancellationToken)
    {
        if (addresses.Count is < 1 or > 8 || addresses.Any(address => !IsCanonicalAddress(address)) ||
            addresses.Distinct(StringComparer.Ordinal).Count() != addresses.Count)
            throw new ArgumentException("One to eight unique canonical public registry addresses are required.");

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        return await Task.WhenAll(ProofOfHumanityOASIS.MainnetDeployments.Select(
            deployment => ReadChainAsync(deployment, addresses, deadline.Token)));
    }

    private async Task<Snapshot> ReadChainAsync(PohDeployment deployment, IReadOnlyList<string> addresses, CancellationToken token)
    {
        var chainId = deployment.Chain switch
        {
            "gnosis" => "0x64",
            "ethereum" => "0x1",
            _ => throw new InvalidOperationException("The registry chain is outside the reviewed deployment set.")
        };
        var observed = await CallAsync(deployment.RpcUrl, "eth_chainId", Array.Empty<object>(), 2048, token);
        if (observed.GetString() != chainId)
            throw new InvalidDataException("The Proof of Humanity endpoint differs from its reviewed mainnet.");
        var header = await CallAsync(deployment.RpcUrl, "eth_getBlockByNumber", new object[] { "finalized", false }, 262144, token);
        var hash = header.GetProperty("hash").GetString();
        var number = header.GetProperty("number").GetString();
        if (header.EnumerateObject().Count(field => field.Name is "hash" or "number") != 2 ||
            !IsHex(hash, 64) || number is not { Length: >= 3 and <= 18 } ||
            !IsHex(number, number.Length - 2) || (number.Length > 3 && number[2] == '0'))
            throw new InvalidDataException("The registry read requires a canonical finalized block identity.");

        var records = new Dictionary<string, bool>(StringComparer.Ordinal);
        var at = new { blockHash = hash, requireCanonical = true };
        foreach (var address in addresses)
        {
            var call = new { to = deployment.CrossChainContract, data = "0xf72c436f" + address[2..].PadLeft(64, '0') };
            var reply = await CallAsync(deployment.RpcUrl, "eth_call", new object[] { call, at }, 2048, token);
            var result = reply.GetString();
            if (!IsHex(result, 64) || result!.AsSpan(2, 63).ContainsAnyExcept('0') || result[^1] is not ('0' or '1'))
                throw new InvalidDataException("The Proof of Humanity contract did not return an ABI Boolean.");
            records.Add(address, result[^1] == '1');
        }
        return new Snapshot(deployment.Chain, number, hash!, records);
    }

    private async Task<JsonElement> CallAsync(string endpoint, string method, object[] parameters, int maximumBytes, CancellationToken token)
    {
        const int id = 1;
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
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
        var fields = root.EnumerateObject().Select(field => field.Name).ToArray();
        if (fields.Distinct(StringComparer.Ordinal).Count() != fields.Length ||
            root.GetProperty("jsonrpc").GetString() != "2.0" || root.GetProperty("id").GetInt32() != id ||
            root.TryGetProperty("error", out _))
            throw new InvalidDataException("The Proof of Humanity RPC response differs from its request.");
        return root.GetProperty("result").Clone();
    }

    private static bool IsHex(string? value, int digits) => value != null && value.Length == digits + 2 &&
        value.StartsWith("0x", StringComparison.Ordinal) && !value.AsSpan(2).ContainsAnyExcept("0123456789abcdef");

    public void Dispose() => client.Dispose();
}
