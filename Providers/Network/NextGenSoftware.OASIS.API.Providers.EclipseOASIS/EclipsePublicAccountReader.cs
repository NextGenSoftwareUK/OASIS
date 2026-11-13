using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.EclipseOASIS;

/// <summary>Finalized metadata-only SVM reads. Balances are ETH with9decimals, not SOL or wei.</summary>
public sealed class EclipsePublicAccountReader
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static long lastStart;
    private readonly HttpClient client;
    private readonly Uri endpoint;
    public sealed record Account(string Address, string Owner, string BalanceUnits, string RentEpoch, string Space, bool Executable);
    public sealed record Snapshot(string GenesisHash, string Slot, IReadOnlyList<Account> Accounts);

    public EclipsePublicAccountReader(HttpClient client, Uri endpoint)
    {
        if (endpoint.Scheme != "https" || endpoint.AbsolutePath != "/" || endpoint.AbsoluteUri.Length > 256 ||
            !string.IsNullOrEmpty(endpoint.UserInfo) || !string.IsNullOrEmpty(endpoint.Query) || !string.IsNullOrEmpty(endpoint.Fragment) ||
            client.DefaultRequestHeaders.Authorization != null || client.DefaultRequestHeaders.Contains("Cookie") ||
            client.DefaultRequestHeaders.Contains("x-api-key"))
            throw new ArgumentException("Explicit anonymous HTTPS RPC root required.");
        this.client = client;
        this.endpoint = endpoint;
    }

    public static void ValidateKey(string value)
    {
        if (value == null || value.Length is < 32 or > 44) throw new ArgumentException("Canonical32byte base58 key required.");
        var key = new PublicKey(value);
        if (key.KeyBytes.Length != 32 || new PublicKey(key.KeyBytes).Key != value)
            throw new ArgumentException("Canonical32byte base58 key required.");
    }

    /// <param name="expectedGenesis">Null only for explicit initial qualification; configured query profiles must supply their retained pin.</param>
    public async Task<Snapshot> ReadAsync(IReadOnlyList<string> addresses, string? expectedGenesis, CancellationToken cancellationToken)
    {
        if (addresses.Count is < 1 or > 8 || addresses.Distinct(StringComparer.Ordinal).Count() != addresses.Count)
            throw new ArgumentException("One to eight unique configured keys required.");
        foreach (var key in addresses) ValidateKey(key);
        if (expectedGenesis != null) ValidateKey(expectedGenesis);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        var token = deadline.Token;
        var id = 0;
        var total = 0;
        async Task<JsonElement> Rpc(string method, object[] parameters)
        {
            // Shared process-wide dispatch admission: at most one request/second,
            // below the documented public endpoint's3req/s ceiling. No retries.
            await Gate.WaitAsync(token);
            try
            {
                var remaining = TimeSpan.FromSeconds(1) - Stopwatch.GetElapsedTime(lastStart);
                if (lastStart != 0 && remaining > TimeSpan.Zero) await Task.Delay(remaining, token);
                lastStart = Stopwatch.GetTimestamp();
                var current = ++id;
                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) {
                    Content = JsonContent.Create(new { jsonrpc = "2.0", id = current, method, @params = parameters }) };
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();
                await using var body = await response.Content.ReadAsStreamAsync(token);
                using var bytes = new MemoryStream();
                var buffer = new byte[4096]; int count;
                while ((count = await body.ReadAsync(buffer, token)) != 0)
                {
                    total += count;
                    if (bytes.Length + count > 262144 || total > 524288) throw new InvalidDataException("Eclipse response budget exceeded.");
                    bytes.Write(buffer, 0, count);
                }
                using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 16 });
                var root = document.RootElement;
                Unique(root);
                if (root.GetProperty("jsonrpc").GetString() != "2.0" || root.GetProperty("id").GetInt32() != current || root.TryGetProperty("error", out _))
                    throw new InvalidDataException("Eclipse RPC envelope failed; no fallback or partial result.");
                return root.GetProperty("result").Clone();
            }
            finally { Gate.Release(); }
        }
        var genesis = (await Rpc("getGenesisHash", Array.Empty<object>())).GetString()!;
        ValidateKey(genesis);
        if (expectedGenesis != null && expectedGenesis != genesis) throw new InvalidDataException("Eclipse genesis differs from retained pin.");
        var result = await Rpc("getMultipleAccounts", new object[] { addresses.ToArray(), new {
            commitment = "finalized", encoding = "base64", dataSlice = new { offset = 0, length = 0 } } });
        var slot = Number(result.GetProperty("context"), "slot");
        var values = result.GetProperty("value");
        if (values.GetArrayLength() != addresses.Count) throw new InvalidDataException("Incomplete Eclipse account selection.");
        var rows = new List<Account>();
        for (var index = 0; index < addresses.Count; index++)
        {
            var row = values[index];
            if (row.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Configured Eclipse account is absent.");
            var data = row.GetProperty("data");
            if (data.GetArrayLength() != 2 || data[0].GetString() != "" || data[1].GetString() != "base64")
                throw new InvalidDataException("Metadata-only response must exclude account data.");
            var owner = row.GetProperty("owner").GetString()!; ValidateKey(owner);
            rows.Add(new Account(addresses[index], owner, Number(row, "lamports"), Number(row, "rentEpoch"),
                Number(row, "space"), row.GetProperty("executable").GetBoolean()));
        }
        token.ThrowIfCancellationRequested();
        return new Snapshot(genesis, slot, rows.AsReadOnly());
    }

    private static string Number(JsonElement row, string property)
    {
        var value = row.GetProperty(property);
        var text = value.GetUInt64().ToString(CultureInfo.InvariantCulture);
        if (value.GetRawText() != text) throw new InvalidDataException("Canonical unsigned integer required.");
        return text;
    }

    private static void Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in value.EnumerateObject())
            {
                if (!names.Add(field.Name)) throw new InvalidDataException("Duplicate native property.");
                Unique(field.Value);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array) foreach (var row in value.EnumerateArray()) Unique(row);
    }
}
