using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class BlockscoutNativeQualificationFactAttribute : FactAttribute
{
    public BlockscoutNativeQualificationFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_BLOCKSCOUT_NATIVE") != "original-address-qualification")
            Skip = "Explicit original native qualification required; never count a controlled run as native proof.";
    }
}

/// <summary>Bounded anonymous explorer reads; not native storage or consensus proof.</summary>
public class BlockscoutPublicNativeQualificationTests
{
    private static readonly string[] Addresses =
    {
        "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2",
        "0x6b175474e89094c44da98b954eedeac495271d0f",
        "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48"
    };

    private static void Validate(JsonElement row, string expected)
    {
        var hash = row.GetProperty("hash").GetString();
        Assert.NotNull(hash);
        Assert.Matches("^0x[0-9a-fA-F]{40}$", hash);
        Assert.Equal(expected, hash.ToLowerInvariant());
        Assert.True(row.GetProperty("is_contract").ValueKind is JsonValueKind.True or JsonValueKind.False);
        var balance = row.GetProperty("coin_balance");
        if (balance.ValueKind != JsonValueKind.Null)
        {
            string? text = balance.GetString();
            Assert.NotNull(text);
            Assert.Matches("^(0|[1-9][0-9]{0,77})$", text);
        }
        // An explorer can report an unindexed address with null balance. That
        // absence is not a zero balance or evidence of an indexed account.
    }

    [Theory]
    [InlineData("exact", true)] [InlineData("unindexed", true)]
    [InlineData("wrong-address", false)] [InlineData("numeric-balance", false)]
    [InlineData("noncanonical-balance", false)] [InlineData("missing-hash", false)]
    public void ExplorerObservationRequiresIdentityAndPreservesNullableExactBalance(string scenario, bool accepted)
    {
        var row = new Dictionary<string, object?>
        {
            ["hash"] = scenario == "wrong-address" ? Addresses[1] : Addresses[0],
            ["is_contract"] = true,
            ["coin_balance"] = scenario == "unindexed" ? null : scenario == "numeric-balance" ? 123 :
                scenario == "noncanonical-balance" ? "01" : "9007199254740993"
        };
        if (scenario == "missing-hash") row.Remove("hash");
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(row));
        if (accepted) Validate(document.RootElement, Addresses[0]);
        else Assert.NotNull(Record.Exception(() => Validate(document.RootElement, Addresses[0])));
    }

    [BlockscoutNativeQualificationFact]
    public async Task ExplicitOriginalAnonymousExplorerAddressesNeverCreateAccountsOrTransactions()
    {
        DirectoryInfo? parent = new(AppContext.BaseDirectory);
        while (parent != null && !File.Exists(Path.Combine(parent.FullName, "Scripts/Sandbox/Verify-HostedQuerySource.py")))
            parent = parent.Parent;
        Assert.NotNull(parent);
        string root = parent.FullName;
        string state = Path.Combine(root, "LocalState/COSMIC/BlockscoutPublicQualificationCurrent");
        string results = Path.Combine(root, "TestResults/BlockscoutPublicSourceCurrent");
        Directory.CreateDirectory(state); Directory.CreateDirectory(results);
        async Task WriteNew(string path, object value)
        {
            await using var file = new FileStream(path, FileMode.CreateNew);
            await JsonSerializer.SerializeAsync(file, value);
            await file.FlushAsync(); file.Flush(true);
        }
        await WriteNew(Path.Combine(state, "qualification.intent.json"), new
        {
            apiUrl = "https://eth.blockscout.com/api/v2/", addresses = Addresses,
            maximumCalls = 3, maximumResponseBytes = 1048576, sourceWrites = 0,
            retryAllowed = false, credentialsUsed = false, userAgentOverride = false
        });
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var observations = new List<object>(); bool qualified = false;
        try
        {
            for (int index = 0; index < Addresses.Length; index++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    "https://eth.blockscout.com/api/v2/addresses/" + Addresses[index]);
                Assert.Null(request.Headers.Authorization); Assert.Empty(request.Headers);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                await response.Content.LoadIntoBufferAsync(1048576, deadline.Token);
                byte[] raw = await response.Content.ReadAsByteArrayAsync(deadline.Token);
                await using (var file = new FileStream(Path.Combine(results, $"address-{index}.json"), FileMode.CreateNew))
                    await file.WriteAsync(raw, deadline.Token);
                observations.Add(new { address = Addresses[index], status = (int)response.StatusCode,
                    bytes = raw.Length, sha256 = Convert.ToHexString(SHA256.HashData(raw)).ToLowerInvariant() });
                response.EnsureSuccessStatusCode();
                using var json = JsonDocument.Parse(raw, new JsonDocumentOptions { MaxDepth = 32 });
                Validate(json.RootElement, Addresses[index]);
            }
            qualified = true;
        }
        finally
        {
            await WriteNew(Path.Combine(state, "qualification.observed.json"), new
            {
                qualified, observations, sourceWrites = 0, retryAllowed = false,
                independentConsensus = false, explorerReportedAddressDataOnly = true
            });
        }
    }
}
