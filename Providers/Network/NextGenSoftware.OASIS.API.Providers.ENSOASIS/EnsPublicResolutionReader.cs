using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.ENSOASIS;

/// <summary>Bounded direct-resolution read protocol; no transactions or CCIP gateways.</summary>
public sealed class EnsPublicResolutionReader
{
    private readonly HttpClient client;
    private readonly Uri endpoint;
    public sealed record Observation(string Name, string Address, string Resolver, string ResponseSha256);
    public sealed record Snapshot(string BlockHash, string BlockNumber, IReadOnlyList<Observation> Records);

    public EnsPublicResolutionReader(HttpClient client, Uri endpoint)
    {
        if (endpoint.Scheme != "https" || !string.IsNullOrEmpty(endpoint.UserInfo) ||
            !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment))
            throw new ArgumentException("Anonymous HTTPS RPC required.");
        this.client = client;
        this.endpoint = endpoint;
    }

    public async Task<Snapshot> ReadAsync(IReadOnlyList<string> names, CancellationToken cancellationToken)
    {
        if (names.Count is < 1 or > 8 || new HashSet<string>(names, StringComparer.Ordinal).Count != names.Count)
            throw new ArgumentException("One to eight unique explicit names required.");
        var calls = new List<string>();
        foreach (var name in names) calls.Add(EnsUniversalResolverCodec.EncodeAddressCall(name));

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        var token = deadline.Token;
        var id = 0;
        var aggregate = 0;

        async Task<JsonElement> Rpc(string method, object[] parameters)
        {
            var current = ++id;
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new { jsonrpc = "2.0", id = current, method, @params = parameters })
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
                    throw new InvalidDataException("ENS RPC response budget exceeded.");
                bounded.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(bounded.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            var fields = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in root.EnumerateObject())
                if (!fields.Add(field.Name)) throw new InvalidDataException("Duplicate RPC envelope field.");
            if (root.GetProperty("jsonrpc").GetString() != "2.0" || root.GetProperty("id").GetInt32() != current)
                throw new InvalidDataException("ENS RPC envelope mismatch.");
            if (root.TryGetProperty("error", out _))
                throw new InvalidDataException("ENS RPC returned an error; no direct-registry or gateway fallback.");
            return root.GetProperty("result").Clone();
        }

        if ((await Rpc("eth_chainId", Array.Empty<object>())).GetString() != "0x1")
            throw new InvalidDataException("Ethereum mainnet required.");
        var block = await Rpc("eth_getBlockByNumber", new object[] { "finalized", false });
        var hash = block.GetProperty("hash").GetString();
        var number = block.GetProperty("number").GetString();
        if (!Hex(hash, 64) || number == null || number.Length < 3 || number.Length > 18 ||
            !number.StartsWith("0x", StringComparison.Ordinal) ||
            number.AsSpan(2).ContainsAnyExcept("0123456789abcdef") ||
            (number.Length > 3 && number[2] == '0'))
            throw new InvalidDataException("Canonical finalized block identity required.");
        var selector = new { blockHash = hash, requireCanonical = true };
        var code = (await Rpc("eth_getCode", new object[] { EnsUniversalResolverCodec.MainnetProxy, selector })).GetString();
        if (code == null || code.Length < 4 || code.Length % 2 != 0 || !code.StartsWith("0x", StringComparison.Ordinal) ||
            code.AsSpan(2).ContainsAnyExcept("0123456789abcdefABCDEF"))
            throw new InvalidDataException("Universal Resolver proxy code required.");

        var records = new List<Observation>();
        for (var i = 0; i < names.Count; i++)
        {
            var result = (await Rpc("eth_call", new object[] {
                new { to = EnsUniversalResolverCodec.MainnetProxy, data = calls[i] }, selector })).GetString();
            var resolution = EnsUniversalResolverCodec.DecodeAddressResult(result!);
            if (names[i] == "ur.integration-tests.eth" && resolution.Address != "0x2222222222222222222222222222222222222222")
                throw new InvalidDataException("Universal Resolver integration sentinel failed.");
            records.Add(new Observation(names[i], resolution.Address, resolution.Resolver,
                Convert.ToHexString(SHA256.HashData(Convert.FromHexString(result![2..]))).ToLowerInvariant()));
        }
        return new Snapshot(hash!, number, records);
    }

    private static bool Hex(string? value, int digits) => value != null && value.Length == digits + 2 &&
        value.StartsWith("0x", StringComparison.Ordinal) && !value.AsSpan(2).ContainsAnyExcept("0123456789abcdef");
}
