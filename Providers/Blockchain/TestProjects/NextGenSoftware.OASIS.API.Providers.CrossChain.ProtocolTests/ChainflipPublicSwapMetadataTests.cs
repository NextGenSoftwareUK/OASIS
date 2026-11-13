using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.ChainflipOASIS;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests;

[TestClass]
public class ChainflipPublicSwapMetadataTests
{
    private static readonly string[] Ids = { "500000", "500001", "500002" };
    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public List<string> Calls = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get, request.Method); Assert.IsNull(request.Content);
            Assert.IsNull(request.Headers.Authorization); Assert.IsFalse(request.Headers.Contains("Cookie"));
            Assert.AreEqual("", request.RequestUri.Query); Assert.AreEqual("", request.RequestUri.Fragment);
            const string prefix = "/v2/swaps/"; Assert.IsTrue(request.RequestUri.AbsolutePath.StartsWith(prefix));
            string id = request.RequestUri.AbsolutePath.Substring(prefix.Length);
            Assert.IsTrue(Ids.Contains(id)); Calls.Add(id);
            var amounts = new Dictionary<string, object> { ["originalInputAmount"] = new string('9',78),
                ["remainingInputAmount"] = "0", ["swappedInputAmount"] = "99", ["swappedIntermediateAmount"] = "123", ["swappedOutputAmount"] = "100" };
            if (Fault == "number") amounts["originalInputAmount"] = 123;
            if (Fault == "quantity-width") amounts["originalInputAmount"] = new string('9',79);
            if (Fault == "quantity-canonical") amounts["originalInputAmount"] = "01";
            if (Fault == "quantity-negative") amounts["originalInputAmount"] = "-1";
            if (Fault == "missing-quantity") amounts.Remove("swappedOutputAmount");
            var record = new Dictionary<string, object> { ["swapId"] = Fault == "id" ? "123" : id,
                ["state"] = Fault == "state" ? "WAITING" : "COMPLETED", ["srcChain"] = "Bitcoin", ["srcAsset"] = "BTC",
                ["destChain"] = "Ethereum", ["destAsset"] = Fault == "asset" ? "" : "ETH", ["swap"] = amounts,
                ["lastStatechainUpdateAt"] = long.MaxValue };
            if (id != "500001") record["deposit"] = new Dictionary<string, object> {
                ["amount"] = "530649", ["txRef"] = "native-deposit-tx", ["witnessedAt"] = 1746793380000L };
            if (Fault == "deposit-null") record["deposit"] = null;
            if (Fault == "deposit-incomplete") record["deposit"] = new { amount = "1" };
            if (Fault == "no-deposit") record.Remove("deposit");
            if (Fault == "refund") record["refundEgress"] = new { amount = "100" };
            if (Fault == "refund-null") record["refundEgress"] = null;
            if (Fault == "refund-quantity") record["refundEgress"] = new { amount = 100 };
            if (Fault == "timestamp-negative") record["lastStatechainUpdateAt"] = -1;
            if (Fault == "timestamp-string") record["lastStatechainUpdateAt"] = "123";
            if (Fault == "timestamp-fraction") record["lastStatechainUpdateAt"] = 1.5;
            if (Fault == "timestamp-overflow") record["lastStatechainUpdateAt"] = JsonSerializer.Deserialize<JsonElement>("9223372036854775808");
            if (Fault == "error") record["error"] = false;
            if (Fault == "missing") record.Remove("swap");
            string text = JsonSerializer.Serialize(record);
            if (Fault == "duplicate-json") text = "{\"swapId\":\"1\",\"swapId\":\"2\"}";
            if (Fault == "oversize") text = new string(' ', 262145);
            bool denied = Fault == "http" || Fault == "last" && id == Ids.Last();
            return Task.FromResult(new HttpResponseMessage(denied ? HttpStatusCode.NotFound : HttpStatusCode.OK) {
                Content = new StringContent(text, Encoding.UTF8, "application/json") });
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("subset")] [DataRow("window")] [DataRow("no-deposit")] [DataRow("refund")]
    [DataRow("id")] [DataRow("state")] [DataRow("asset")] [DataRow("number")] [DataRow("quantity-width")]
    [DataRow("quantity-canonical")] [DataRow("quantity-negative")] [DataRow("missing-quantity")]
    [DataRow("deposit-null")] [DataRow("deposit-incomplete")] [DataRow("refund-null")] [DataRow("refund-quantity")]
    [DataRow("timestamp-negative")] [DataRow("timestamp-string")] [DataRow("timestamp-fraction")] [DataRow("timestamp-overflow")]
    [DataRow("error")] [DataRow("missing")] [DataRow("duplicate-json")] [DataRow("oversize")]
    [DataRow("http")] [DataRow("last")] [DataRow("empty-window-error")] [DataRow("budget")]
    [DataRow("unknown")] [DataRow("cancel")] [DataRow("inactive")]
    public async Task CompleteHistoricalSelectionPreservesPrecisionAndOptionalDepositOrFails(string scenario)
    {
        var native = new Native(); using var client = new HttpClient(native); var owner = Guid.NewGuid();
        using var source = new ChainflipPublicSwapMetadataSource(client, new Uri("https://example.invalid/"), Ids, owner);
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Calls.Clear(); native.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var keys = scenario == "subset" ? Ids.Take(2).ToArray() : scenario == "unknown" ? new[] { "599999" } : Ids;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ChainflipOASIS).Result)
            .Where(row => keys.Contains(CosmicQueryMetadata.Value<string>(row, ChainflipPublicSwapMetadataSource.AddressMetadata))).Result;
        if (scenario == "point") {
            string key = Ids[0];
            query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ChainflipOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row, ChainflipPublicSwapMetadataSource.AddressMetadata) == key).Result;
        }
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.ChainflipOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ChainflipOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancel = new CancellationTokenSource(); if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 2 : 100, cancel.Token);
        if (scenario is "normal" or "point" or "subset" or "window" or "no-deposit" or "refund") {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario is "point" or "window" ? 1 : scenario == "subset" ? 2 : 3, result.Result.Count);
            CollectionAssert.AreEqual(scenario == "point" ? Ids.Take(1).ToArray() : keys, native.Calls.ToArray());
            foreach (var row in result.Result) {
                Assert.IsTrue(row.IsPublic); Assert.AreEqual(owner, row.CreatedByAvatarId);
                Assert.AreEqual("indexer-reported-swap-state", row.MetaData["ChainflipObservationPolicy"]);
                Assert.AreEqual(new string('9',78), row.MetaData["ChainflipOriginalInputAmount"]);
                Assert.AreEqual("9223372036854775807", row.MetaData["ChainflipLastStatechainUpdateAtMilliseconds"]);
                bool deposit = scenario != "no-deposit" && row.Name != "500001";
                Assert.AreEqual(deposit ? "true" : "false", row.MetaData["ChainflipExternalDepositObserved"]);
                Assert.AreEqual(deposit, row.MetaData.ContainsKey("ChainflipDepositAmount"));
                Assert.AreEqual(scenario == "refund" ? "true" : "false", row.MetaData["ChainflipRefundObserved"]);
            }
        } else { Assert.IsTrue(result.IsError); Assert.IsTrue(result.Result == null || result.Result.Count == 0); }
        if (scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0, native.Calls.Count);
        if (scenario is "last" or "empty-window-error") Assert.AreEqual(3, native.Calls.Count);
    }

    [TestMethod]
    public async Task UnsupportedQueriesAndNativeWritesNeverDispatch()
    {
        var native = new Native(); using var client = new HttpClient(native);
        using var source = new ChainflipPublicSwapMetadataSource(client, new Uri("https://example.invalid/"), Ids, Guid.NewGuid());
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ChainflipOASIS).Result).Where(row => row.Name == "any").Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(query, ProviderType.ChainflipOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ChainflipOASIS, source.QueryCapabilities) }).IsError);
        Assert.IsTrue((await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), IsNewHolon = true }, saveChildren: false)).IsError);
        Assert.IsTrue((await source.DeleteHolonAsync(Guid.NewGuid())).IsError);
        Assert.AreEqual(0, native.Calls.Count);
    }

    [DataTestMethod]
    [DataRow("scheme")] [DataRow("owner")] [DataRow("duplicate")] [DataRow("id")] [DataRow("overflow")]
    [DataRow("canonical")] [DataRow("count")]
    public void InvalidCatalogueFailsBeforeAnyRequest(string fault)
    {
        var keys = Ids.ToList();
        if (fault == "duplicate") keys.Add(keys[0]);
        if (fault == "id") keys[0] = "invalid";
        if (fault == "overflow") keys[0] = "18446744073709551616";
        if (fault == "canonical") keys[0] = "0500000";
        if (fault == "count") keys.Clear();
        using var client = new HttpClient(new Native());
        Assert.ThrowsException<ArgumentException>(() => new ChainflipPublicSwapMetadataSource(client,
            new Uri(fault == "scheme" ? "http://example.invalid/" : "https://example.invalid/"), keys,
            fault == "owner" ? Guid.Empty : Guid.NewGuid()));
    }
}
