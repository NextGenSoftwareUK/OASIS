using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;

namespace NextGenSoftware.OASIS.API.Providers.CelestiaOASIS.UnitTests;

/// <summary>
/// One explicit read-only protocol qualification, before CQL admission.
/// It uses the production HTTP guard, not the legacy localhost blob adapter.
/// Returned hashes are node-reported identities, not independently verified consensus.
/// </summary>
[TestClass]
public class CelestiaPublicNativeQualificationTests
{
    private const string Endpoint = "https://public-endpoint.celestia-mainnet.quiknode.pro/";

    private static string Root()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName,
                   "Docs/WIP/COSMIC ORM/CELESTIA_PUBLIC_SOURCE_STATUS.md")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Worktree root is missing.");
    }

    private static async Task WriteNew(string path, object value)
    {
        await using var file = new FileStream(path, FileMode.CreateNew);
        await JsonSerializer.SerializeAsync(file, value);
        await file.FlushAsync();
        file.Flush(true);
    }

    // Keep native integers as text: never parse height through floating point.
    private static ulong Height(JsonElement result, ulong? expected = null)
    {
        var header = result.GetProperty("header");
        Assert.AreEqual("celestia", header.GetProperty("chain_id").GetString());
        var text = header.GetProperty("height").GetString();
        Assert.IsTrue(ulong.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var height));
        Assert.IsTrue(height > 0 && height.ToString(CultureInfo.InvariantCulture) == text);
        if (expected.HasValue) Assert.AreEqual(expected.Value, height);
        Assert.AreEqual(text, result.GetProperty("commit").GetProperty("height").GetString());
        var hash = result.GetProperty("commit").GetProperty("block_id").GetProperty("hash").GetString();
        Assert.IsNotNull(hash);
        Assert.IsTrue(hash.Length == 64 && hash.All(Uri.IsHexDigit));
        Assert.IsTrue(header.GetProperty("data_hash").GetString() is { Length: 64 } data && data.All(Uri.IsHexDigit));
        Assert.IsTrue(DateTimeOffset.TryParse(header.GetProperty("time").GetString(),
            CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _));
        return height;
    }

    [DataTestMethod]
    [DataRow("celestia", "123", "123", true)]
    [DataRow("mocha-4", "123", "123", false)]
    [DataRow("celestia", "0123", "0123", false)]
    [DataRow("celestia", "123", "124", false)]
    [DataRow("celestia", "0", "0", false)]
    public void QualificationRefusesWrongNetworkNoncanonicalAndMismatchedHeights(
        string chain, string height, string commitHeight, bool valid)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            header = new { chain_id = chain, height, time = "2026-11-02T00:00:00Z", data_hash = new string('A', 64) },
            commit = new { height = commitHeight, block_id = new { hash = new string('B', 64) } }
        }));
        if (valid) Assert.AreEqual(123UL, Height(document.RootElement, 123));
        else Assert.ThrowsException<AssertFailedException>(() => Height(document.RootElement, 123));
    }

    [TestMethod]
    public async Task OriginalAnonymousMainnetExtendedHeadersUseGuardedTransportWithoutWrites()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_CELESTIA_NATIVE") != "original-header-qualification")
            Assert.Inconclusive("Explicit native qualification only; consumed intent must never be replayed.");

        var root = Root();
        var state = Path.Combine(root, "LocalState/COSMIC/CelestiaPublicQualificationCurrent");
        var evidence = Path.Combine(root, "TestResults/CelestiaPublicSourceCurrent");
        Directory.CreateDirectory(state);
        Directory.CreateDirectory(evidence);
        await WriteNew(Path.Combine(state, "qualification.intent.json"), new
        {
            endpoint = Endpoint, methods = new[] { "header.NetworkHead", "header.GetByHeight" },
            maximumCalls = 4, sourceWrites = 0, retries = 0, maximumResponseBytes = 1048576,
            transport = "standard-native-guard", credentials = false, userAgentOverride = false
        });

        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var client = new HttpClient(NativeConnectionGuard.CreateGuardedHttpHandler())
        {
            BaseAddress = new Uri(Endpoint), Timeout = TimeSpan.FromSeconds(15)
        };
        var observations = new List<object>();
        var selected = new List<string>();
        bool qualified = false;
        try
        {
            async Task<JsonDocument> Read(string method, object[] parameters, int id)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/")
                {
                    Content = JsonContent.Create(new { jsonrpc = "2.0", id, method, @params = parameters })
                };
                Assert.IsNull(request.Headers.Authorization);
                Assert.IsFalse(request.Headers.Contains("Cookie"));
                Assert.IsFalse(request.Headers.Contains("User-Agent"));
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                await response.Content.LoadIntoBufferAsync(1048576, deadline.Token);
                var bytes = await response.Content.ReadAsByteArrayAsync(deadline.Token);
                await using (var file = new FileStream(Path.Combine(evidence, $"rpc-{id}.json"), FileMode.CreateNew))
                    await file.WriteAsync(bytes, deadline.Token);
                observations.Add(new { id, method, httpStatus = (int)response.StatusCode, bytes = bytes.Length,
                    sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant() });
                response.EnsureSuccessStatusCode();
                var json = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 });
                try
                {
                    Assert.AreEqual("2.0", json.RootElement.GetProperty("jsonrpc").GetString());
                    Assert.AreEqual(id, json.RootElement.GetProperty("id").GetInt32());
                    Assert.IsFalse(json.RootElement.TryGetProperty("error", out _), "RPC error retained; no retry.");
                    return json;
                }
                catch { json.Dispose(); throw; }
            }

            using var head = await Read("header.NetworkHead", Array.Empty<object>(), 1);
            var latest = Height(head.RootElement.GetProperty("result"));
            Assert.IsTrue(latest > 3);
            // Freeze these exact heights before reading any member of the set.
            var heights = new[] { latest - 3, latest - 2, latest - 1 };
            selected.AddRange(heights.Select(h => h.ToString(CultureInfo.InvariantCulture)));
            await WriteNew(Path.Combine(state, "selected-heights.json"), new { chainId = "celestia", heights = selected });
            for (int index = 0; index < heights.Length; index++)
            {
                using var item = await Read("header.GetByHeight", new object[] { heights[index] }, index + 2);
                Height(item.RootElement.GetProperty("result"), heights[index]);
            }
            qualified = true;
        }
        finally
        {
            await WriteNew(Path.Combine(state, "qualification.observed.json"), new
            {
                qualified, selectedHeights = selected, observations, sourceWrites = 0, retryAllowed = false,
                independentlyVerifiedConsensus = false, dataAvailabilitySampling = false
            });
        }
    }
}
