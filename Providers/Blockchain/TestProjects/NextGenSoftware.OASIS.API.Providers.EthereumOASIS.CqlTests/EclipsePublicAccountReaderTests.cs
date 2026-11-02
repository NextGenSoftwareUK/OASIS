using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.EclipseOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class EclipsePublicAccountReaderTests
{
    internal static readonly string[] Keys = { "11111111111111111111111111111111",
        "SysvarC1ock11111111111111111111111111111111", "SysvarRent111111111111111111111111111111111" };

    [TestMethod]
    public async Task ExactIntegerMetadataUsesOneFinalizedContextWithoutDownloadingData()
    {
        using var handler = new Native(); using var client = new HttpClient(handler);
        var result = await new EclipsePublicAccountReader(client, new Uri("https://example.invalid/"))
            .ReadAsync(Keys, Keys[0], default);
        Assert.AreEqual(3, result.Accounts.Count);
        Assert.AreEqual(ulong.MaxValue.ToString(), result.Accounts[0].BalanceUnits);
        Assert.AreEqual(ulong.MaxValue.ToString(), result.Slot);
        Assert.AreEqual(Keys[0], result.GenesisHash);
        Assert.AreEqual(2, handler.Starts.Count);
        Assert.IsTrue(Stopwatch.GetElapsedTime(handler.Starts[0], handler.Starts[1]) >= TimeSpan.FromMilliseconds(950));
    }

    [DataTestMethod]
    [DataRow("absent")]
    [DataRow("length")]
    [DataRow("data")]
    [DataRow("owner")]
    [DataRow("fraction")]
    [DataRow("overflow")]
    [DataRow("duplicate")]
    [DataRow("error")]
    [DataRow("id")]
    [DataRow("genesis")]
    public async Task NativeFailureNeverProducesPartialOrSubstitutedRows(string fault)
    {
        using var handler = new Native { Fault = fault }; using var client = new HttpClient(handler);
        try
        {
            await new EclipsePublicAccountReader(client, new Uri("https://example.invalid/"))
                .ReadAsync(Keys, Keys[0], default);
            Assert.Fail("Malformed native response accepted.");
        }
        catch (Exception error) when (error is InvalidDataException or ArgumentException or FormatException or InvalidOperationException) { }
        Assert.IsTrue(handler.Starts.Count <= 2);
    }

    [TestMethod]
    public async Task InvalidCatalogueAndCancellationDoNotDispatch()
    {
        using var handler = new Native(); using var client = new HttpClient(handler);
        var reader = new EclipsePublicAccountReader(client, new Uri("https://example.invalid/"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => reader.ReadAsync(new[] { "not-base58" }, null, default));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => reader.ReadAsync(new[] { Keys[0], Keys[0] }, null, default));
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() => reader.ReadAsync(Keys, null, cancellation.Token));
        Assert.AreEqual(0, handler.Starts.Count);
    }

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public List<long> Starts = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Starts.Add(Stopwatch.GetTimestamp());
            using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var root = document.RootElement; var method = root.GetProperty("method").GetString();
            object result;
            if (method == "getGenesisHash") result = Fault == "genesis" ? Keys[1] : Keys[0];
            else
            {
                Assert.AreEqual("getMultipleAccounts", method);
                var config = root.GetProperty("params")[1];
                Assert.AreEqual("finalized", config.GetProperty("commitment").GetString());
                Assert.AreEqual(0, config.GetProperty("dataSlice").GetProperty("length").GetInt32());
                object?[] values = Keys.Select(_ => (object?)new Dictionary<string, object> {
                    ["data"] = new[] { Fault == "data" ? "AA==" : "", "base64" },
                    ["owner"] = Fault == "owner" ? "bad" : Keys[0], ["executable"] = false,
                    ["lamports"] = ulong.MaxValue, ["rentEpoch"] = ulong.MaxValue, ["space"] = 0 }).ToArray();
                if (Fault == "absent") values[2] = null;
                if (Fault == "length") values = values[..2];
                if (Fault == "fraction") ((Dictionary<string, object>)values[2]!)["lamports"] = 1.5;
                if (Fault == "overflow") ((Dictionary<string, object>)values[2]!)["lamports"] = JsonSerializer.Deserialize<JsonElement>("18446744073709551616");
                result = new { context = new { slot = ulong.MaxValue }, value = values };
            }
            var id = root.GetProperty("id").GetInt32();
            var text = Fault == "error" ? JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code = -1 } }) :
                JsonSerializer.Serialize(new { jsonrpc = "2.0", id = Fault == "id" ? 999 : id, result });
            if (Fault == "duplicate") text = text.Insert(1, "\"id\":1,");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }
}
