using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.FhenixOASIS;

/// <summary>Bounded public CoFHE AddressBook reads at a finalized block on each supported network.</summary>
internal sealed class FhenixPublicTaskManagerReader : IDisposable
{
    private readonly HttpClient client;
    private const string AddressBook = "0xc0f4e00e531a2b086492ae3dcc1515038307196b";
    private static readonly string LookupTaskManager = "0x25adb56b" + "1".PadLeft(64, '0');

    internal sealed record Observation(string ChainId, string BlockHash, string BlockNumber, string TaskManager);

    public FhenixPublicTaskManagerReader()
    {
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        handler.UseProxy = false;
        client = new HttpClient(handler);
    }

    public async Task<Observation> ReadAsync(string chainId, CancellationToken cancellationToken)
    {
        var (endpoint, expectedChainId) = chainId switch
        {
            "1" => (new Uri("https://eth.drpc.org"), "0x1"),
            "11155111" => (new Uri("https://ethereum-sepolia-rpc.publicnode.com"), "0xaa36a7"),
            "84532" => (new Uri("https://base-sepolia-rpc.publicnode.com"), "0x14a34"),
            _ => throw new ArgumentException("An explicitly supported CoFHE chain ID is required.", nameof(chainId))
        };

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        var token = deadline.Token;
        var aggregate = 0;

        async Task<JsonElement> Rpc(string method, object[] parameters)
        {
            const int requestId = 1;
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new { jsonrpc = "2.0", id = requestId, method, @params = parameters })
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
                    throw new InvalidDataException("CoFHE RPC response budget exceeded.");
                bounded.Write(buffer, 0, count);
            }
            using var document = JsonDocument.Parse(bounded.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
            var root = document.RootElement;
            var fields = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in root.EnumerateObject())
                if (!fields.Add(field.Name)) throw new InvalidDataException("Duplicate RPC envelope field.");
            if (root.GetProperty("jsonrpc").GetString() != "2.0" || root.GetProperty("id").GetInt32() != requestId)
                throw new InvalidDataException("CoFHE RPC envelope mismatch.");
            if (root.TryGetProperty("error", out _))
                throw new InvalidDataException("CoFHE RPC returned an error.");
            return root.GetProperty("result").Clone();
        }

        if ((await Rpc("eth_chainId", Array.Empty<object>())).GetString() != expectedChainId)
            throw new InvalidDataException("CoFHE RPC returned the wrong chain ID.");
        var block = await Rpc("eth_getBlockByNumber", new object[] { "finalized", false });
        var hash = block.GetProperty("hash").GetString();
        var number = block.GetProperty("number").GetString();
        if (!Hex(hash, 64) || number == null || number.Length < 3 || number.Length > 18 ||
            !number.StartsWith("0x", StringComparison.Ordinal) ||
            number.AsSpan(2).ContainsAnyExcept("0123456789abcdef") ||
            (number.Length > 3 && number[2] == '0'))
            throw new InvalidDataException("Canonical finalized block identity required.");
        var selector = new { blockHash = hash, requireCanonical = true };
        var result = (await Rpc("eth_call", new object[]
        {
            new { to = AddressBook, data = LookupTaskManager }, selector
        })).GetString();
        if (!Hex(result, 64) || result!.AsSpan(2, 24).ContainsAnyExcept('0') ||
            !result.AsSpan(26).ContainsAnyExcept('0'))
            throw new InvalidDataException("Invalid CoFHE Task Manager address.");

        return new Observation(chainId, hash!, number, "0x" + result[26..]);
    }

    public void Dispose() => client.Dispose();

    private static bool Hex(string? value, int digits) => value != null && value.Length == digits + 2 &&
        value.StartsWith("0x", StringComparison.Ordinal) && !value.AsSpan(2).ContainsAnyExcept("0123456789abcdef");
}
