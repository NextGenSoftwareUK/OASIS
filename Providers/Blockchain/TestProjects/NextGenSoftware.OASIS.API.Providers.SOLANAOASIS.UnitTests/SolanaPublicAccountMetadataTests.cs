using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Source = NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.SolanaPublicAccountMetadataSource;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaPublicAccountMetadataTests
{
    private const string Genesis = "5eykt4UsFv8P8NJdTREpY1vzqKqZKvdpKuc147dw2N9d";
    private static readonly string[] Keys = { "SysvarC1ock11111111111111111111111111111111",
        "SysvarRent111111111111111111111111111111111", "SysvarEpochSchedu1e111111111111111111111111" };

    private sealed class Rpc : HttpMessageHandler
    {
        public string Fault = "";
        public readonly List<string> Methods = new();
        public bool CancelObserved;
        public Action? CancelDuringRpc;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var method = body.RootElement.GetProperty("method").GetString()!;
            Methods.Add(method);
            object result;
            if (method == "getGenesisHash") result = Fault == "genesis" ? "11111111111111111111111111111111" : Genesis;
            else
            {
                Assert.AreEqual("getMultipleAccounts", method, "No signing, writes, history scans or other RPC methods permitted.");
                var parameters = body.RootElement.GetProperty("params");
                var options = parameters[1];
                Assert.AreEqual("finalized", options.GetProperty("commitment").GetString());
                Assert.AreEqual("base64", options.GetProperty("encoding").GetString());
                Assert.AreEqual(0, options.GetProperty("dataSlice").GetProperty("offset").GetInt32());
                Assert.AreEqual(0, options.GetProperty("dataSlice").GetProperty("length").GetInt32());
                if (Fault == "cancel")
                {
                    CancelDuringRpc?.Invoke();
                    try { await Task.Delay(Timeout.Infinite, token); }
                    catch (OperationCanceledException) { CancelObserved = token.IsCancellationRequested; throw; }
                }
                var count = parameters[0].GetArrayLength();
                var values = Enumerable.Range(0, Fault == "partial" ? count - 1 : count).Select(index => (object?)new {
                    data = new[] { Fault == "invalid-last" && index == count - 1 ? "AQ==" : "", "base64" },
                    owner = "11111111111111111111111111111111", executable = false,
                    lamports = 9007199254740993UL, rentEpoch = ulong.MaxValue, space = 40UL
                }).ToArray();
                if (Fault == "missing") values[0] = null;
                result = new { context = new { slot = 9007199254740993UL }, value = values };
                if (Fault == "rpc") return Json(new { jsonrpc = "2.0", id = 1, error = new { code = -32005, message = "busy" } });
                if (Fault == "oversize") return new(HttpStatusCode.OK) { Content = new StringContent(new string(' ', 65537)) };
            }
            return Json(new { jsonrpc = "2.0", id = 1, result });
        }
        private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("missing")]
    [DataRow("partial")] [DataRow("invalid-last")] [DataRow("empty-window-error")]
    [DataRow("rpc")] [DataRow("genesis")] [DataRow("oversize")]
    [DataRow("inactive")] [DataRow("cancel")] [DataRow("pre-cancel")] [DataRow("unknown")]
    public async Task NativeSelectionIsBoundedReadOnlyPreciseAndNeverPublishesPartialRows(string scenario)
    {
        var transport = new Rpc(); using var client = new HttpClient(transport);
        using var source = new Source(client, new Uri("https://rpc.example.invalid/"), Genesis, Keys, Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        transport.Methods.Clear();
        transport.Fault = scenario == "empty-window-error" ? "invalid-last" : scenario;
        var selection = scenario == "unknown" ? new[] { "11111111111111111111111111111111" } : Keys;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SolanaOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
            .Where(row => selection.Contains(CosmicQueryMetadata.Value<string>(row, Source.AddressMetadata))).Result;
        if (scenario == "point")
        {
            var point = Keys[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SolanaOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, Source.AddressMetadata) == point).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.SolanaOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SolanaOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancellation = new CancellationTokenSource();
        if (scenario == "pre-cancel") cancellation.Cancel();
        if (scenario == "cancel") { transport.CancelDuringRpc = cancellation.Cancel; cancellation.CancelAfter(2000); }
        var response = await source.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancellation.Token);
        if (scenario is "normal" or "point" or "window" or "missing")
        {
            Assert.IsFalse(response.IsError, response.Exception?.ToString());
            Assert.AreEqual(scenario == "normal" ? 3 : scenario == "missing" ? 2 : 1, response.Result.Count);
            Assert.IsTrue(response.IsLoaded);
            foreach (var row in response.Result)
            {
                Assert.IsTrue(row.IsPublic);
                Assert.AreEqual("18446744073709551615", row.MetaData["SolanaRentEpoch"]);
                Assert.AreEqual("9007199254740993", row.MetaData["SolanaLamports"]);
                Assert.AreEqual("9007199254740993", row.MetaData["SolanaSlot"]);
            }
            if (scenario == "window") Assert.AreEqual(Keys[1], response.Result[0].MetaData[Source.AddressMetadata]);
        }
        else
        {
            Assert.IsTrue(response.IsError);
            Assert.IsNull(response.Result);
            Assert.IsFalse(response.IsLoaded);
            Assert.AreEqual(scenario switch { "inactive" => "CQL_PROVIDER_INACTIVE", "unknown" => "CQL_INVALID_SOURCE_KEY",
                "cancel" or "pre-cancel" => "CQL_QUERY_CANCELLED", _ => "CQL_QUERY_FAILED" }, response.ErrorCode);
        }
        if (scenario == "cancel") Assert.IsTrue(transport.CancelObserved);
        Assert.AreEqual(scenario is "inactive" or "unknown" or "pre-cancel" ? 0 : scenario == "genesis" ? 1 : 2, transport.Methods.Count);
    }

    [TestMethod]
    public async Task UnsupportedContentPredicatesRejectBeforeRpcAndConfigurationAcceptsNoSignerFields()
    {
        var transport = new Rpc(); using var client = new HttpClient(transport);
        using var source = new Source(client, new Uri("https://rpc.example.invalid/"), Genesis, Keys, Guid.NewGuid());
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SolanaOASIS).Result)
            .Where(row => row.Name.Contains("coffee")).Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(query, ProviderType.SolanaOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SolanaOASIS, source.QueryCapabilities) }).IsError);
        Assert.AreEqual(0, transport.Methods.Count);
        var config = new Dictionary<string, object> { ["schema"] = 1, ["rpcUrl"] = "https://rpc.example.invalid/",
            ["genesisHash"] = Genesis, ["addresses"] = Keys, ["sourceOwner"] = Guid.NewGuid(), ["privateKey"] = "not-used" };
        Assert.ThrowsException<ArgumentException>(() => Source.FromJson(JsonSerializer.Serialize(config)));
        var saved = await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false);
        Assert.IsTrue(saved.IsError);
        Assert.AreEqual(0, transport.Methods.Count);
    }

    [TestMethod, TestCategory("SolanaPublicAccountsNative")]
    public async Task ActualFinalizedPublicAccountSourceReturnsThreeAndTwoWithoutWalletOrAccountData()
    {
        var path = Environment.GetEnvironmentVariable("OASIS_SOLANA_PUBLIC_SOURCE")
            ?? throw new InvalidOperationException("Explicit retained source catalogue path required.");
        var json = await System.IO.File.ReadAllTextAsync(path);
        using var config = JsonDocument.Parse(json);
        var addresses = config.RootElement.GetProperty("addresses").EnumerateArray().Select(value => value.GetString()!).ToArray();
        using var source = Source.FromJson(json);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var activated = await source.ActivateQuerySourceAsync(deadline.Token);
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());
        foreach (var count in new[] { 3, 2 })
        {
            var selected = addresses.Take(count).ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SolanaOASIS).Result)
                .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, Source.AddressMetadata))).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.SolanaOASIS,
                new[] { new CosmicQueryTarget(ProviderType.SolanaOASIS, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: deadline.Token);
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(count, result.Result.Count);
            CollectionAssert.AreEqual(selected, result.Result.Select(row => (string)row.MetaData[Source.AddressMetadata]).ToArray());
        }
    }
}
