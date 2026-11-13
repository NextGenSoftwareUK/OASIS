using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.EigenLayerOASIS;

/// <summary>Bounded operator registration reads at one finalized Ethereum block.</summary>
internal sealed class EigenLayerPublicOperatorReader : IDisposable
{
    private readonly HttpClient client;
    private static readonly Uri Endpoint = new("https://ethereum-rpc.publicnode.com");
    private const string DelegationManager = "0x39053d51b77dc0d36036fc1fcc8cb819df8ef37a";
    public sealed record Observation(string Address, bool IsOperator);
    public sealed record Snapshot(string BlockHash, string BlockNumber, IReadOnlyList<Observation> Records);

    public EigenLayerPublicOperatorReader()
    {
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        handler.UseProxy = false;
        client = new HttpClient(handler);
    }

    public async Task<Snapshot> ReadAsync(IReadOnlyList<string> addresses, CancellationToken cancellationToken)
    {
        if (addresses.Count is < 1 or > 8 || new HashSet<string>(addresses, StringComparer.Ordinal).Count != addresses.Count)
            throw new ArgumentException("One to eight unique explicit addresses required.");
        foreach (var address in addresses)
            if (!Hex(address, 40)) throw new ArgumentException("Canonical lowercase Ethereum addresses required.");

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
                    throw new InvalidDataException("EigenLayer RPC response budget exceeded.");
                bounded.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(bounded.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            var fields = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in root.EnumerateObject())
                if (!fields.Add(field.Name)) throw new InvalidDataException("Duplicate RPC envelope field.");
            if (root.GetProperty("jsonrpc").GetString() != "2.0" || root.GetProperty("id").GetInt32() != requestId)
                throw new InvalidDataException("EigenLayer RPC envelope mismatch.");
            if (root.TryGetProperty("error", out _))
                throw new InvalidDataException("EigenLayer RPC returned an error.");
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
        var records = new List<Observation>(addresses.Count);
        foreach (var address in addresses)
        {
            var data = "0x6d70f7ae" + address[2..].PadLeft(64, '0');
            var result = (await Rpc("eth_call", new object[] { new { to = DelegationManager, data }, selector })).GetString();
            if (!Hex(result, 64) || result!.AsSpan(2, 63).ContainsAnyExcept('0') || result[^1] is not ('0' or '1'))
                throw new InvalidDataException("Invalid EigenLayer operator registration result.");
            records.Add(new Observation(address, result[^1] == '1'));
        }
        return new Snapshot(hash!, number, records.AsReadOnly());
    }

    public void Dispose() => client.Dispose();

    private static bool Hex(string? value, int digits) => value != null && value.Length == digits + 2 &&
        value.StartsWith("0x", StringComparison.Ordinal) && !value.AsSpan(2).ContainsAnyExcept("0123456789abcdef");
}
