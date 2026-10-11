using System.Reflection;
using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaWalletHistoryTests
{
    private static WalletTransaction Map(string json)
    {
        using var document = JsonDocument.Parse(json);
        return (WalletTransaction)typeof(HashgraphOASIS).GetMethod("MapMirrorWalletTransaction", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { document.RootElement, "0.0.1001" })!;
    }

    private const string Record = """
        {"transaction_id":"0.0.1001-1700000000-000000001","consensus_timestamp":"1700000000.123456789",
         "result":"SUCCESS","name":"CRYPTOTRANSFER","transfers":[
          {"account":"0.0.1001","amount":-9007199254740993},{"account":"0.0.1001","amount":-7},
          {"account":"0.0.1002","amount":9007199254740993}]}
        """;

    [TestMethod]
    public void ExactWalletDeltaIncludesFeesWithoutInventingCounterparty()
    {
        var result = Map(Record);
        Assert.AreEqual("-9007199254741000", result.RawAmount);
        Assert.AreEqual("tinybar", result.AmountUnit);
        Assert.AreEqual("0.0.1001-1700000000-000000001", result.NativeTransactionId);
        Assert.AreEqual(Guid.Empty, result.TransactionId);
        Assert.IsNull(result.FromWalletAddress); Assert.IsNull(result.ToWalletAddress);
        Assert.AreEqual(DateTime.UnixEpoch.AddSeconds(1700000000).AddTicks(1234567), result.CreatedDate);
    }

    [TestMethod]
    public void FailedRecordRetainsActualExecutionResultAndAppliedFeeDelta()
    {
        var result = Map(Record.Replace("SUCCESS", "CONTRACT_REVERT_EXECUTED"));
        Assert.AreEqual("CONTRACT_REVERT_EXECUTED", result.NativeExecutionResult);
        Assert.AreEqual("CONTRACT_REVERT_EXECUTED", result.NativeError);
        Assert.AreEqual("-9007199254741000", result.RawAmount);
    }

    [DataTestMethod]
    [DataRow("transaction_id", "")]
    [DataRow("consensus_timestamp", "bad")]
    [DataRow("result", "")]
    [DataRow("name", "")]
    public void IncompleteRecordsCannotBecomeSuccessfulHistory(string field, string value)
    {
        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Record)!;
        data[field] = JsonSerializer.SerializeToElement(value);
        Assert.ThrowsException<TargetInvocationException>(() => Map(JsonSerializer.Serialize(data)));
    }

    private static string Page(IEnumerable<string> records, string? next) =>
        "{\"transactions\":[" + string.Join(",", records) + "],\"links\":{\"next\":" + JsonSerializer.Serialize(next) + "}}";

    private static HashgraphOASIS Provider(HistoryHandler handler)
    {
        var provider = new HashgraphOASIS("http://127.0.0.1:12345/api/v1", "testnet") { IsProviderActivated = true };
        var field = typeof(HashgraphOASIS).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)!;
        ((HttpClient)field.GetValue(provider)!).Dispose();
        field.SetValue(provider, new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:12345/api/v1/") });
        return provider;
    }

    [TestMethod]
    public async Task PublicHistoryReadsBeyondOnePageAtConfiguredEndpoint()
    {
        var first = Enumerable.Range(1, 100).Select(i => Record.Replace("1700000000.123456789", $"1700000000.{i:D9}"));
        using var handler = new HistoryHandler(new[] {
            Page(first, "/api/v1/transactions?account.id=0.0.1001&timestamp=gt:1700000000.000000100"),
            Page(new[] { Record }, null) });
        var result = await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest { WalletAddress = "0.0.1001" });
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(101, result.Result.Count);
        Assert.AreEqual(2, handler.Requests.Count);
        StringAssert.StartsWith(handler.Requests[0], "http://127.0.0.1:12345/api/v1/transactions?");
        StringAssert.Contains(handler.Requests[0], "timestamp=gte:0");
        var bound = handler.Requests[0].Split('&').Single(value => value.StartsWith("timestamp=lte:"));
        StringAssert.Contains(handler.Requests[1], bound);
        Assert.AreEqual("-9007199254741000", result.Result[100].RawAmount);
    }

    [DataTestMethod]
    [DataRow("https://foreign.example/api/v1/transactions")]
    [DataRow("/api/v1/accounts")]
    [DataRow("/api/v1/transactions?account.id=0.0.2002")]
    [DataRow("/api/v1/transactions?account.id=0.0.1001&timestamp=lte:9999999999.000000000")]
    [DataRow("/api/v1/transactions?account.id=0.0.1001&limit=100&order=asc&timestamp=gte:0")]
    public async Task UnsafeOrCyclicNextLinkReturnsErrorWithoutPartialHistory(string next)
    {
        using var handler = new HistoryHandler(new[] { Page(new[] { Record }, next) });
        var result = await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest { WalletAddress = "0.0.1001" });
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        Assert.AreEqual(1, handler.Requests.Count, "Unsafe next links must be rejected before transmission.");
    }

    [TestMethod]
    public async Task SecondPageHttpFailureCannotReturnSuccessfulPartialHistory()
    {
        using var handler = new HistoryHandler(new[] { Page(new[] { Record }, "/api/v1/transactions?account.id=0.0.1001&page=2") }, HttpStatusCode.ServiceUnavailable);
        var result = await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest { WalletAddress = "0.0.1001" });
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.AreEqual(2, handler.Requests.Count);
    }

    [TestMethod]
    public async Task RepeatedConsensusRecordCannotInflateHistory()
    {
        using var handler = new HistoryHandler(new[] { Page(new[] { Record, Record }, null) });
        var result = await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest { WalletAddress = "0.0.1001" });
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task OutOfOrderNanosecondHistoryCannotReturnPartialSuccess(bool acrossPages)
    {
        // Both timestamps map to the same DateTime tick; compare native nanoseconds instead.
        var later = Record.Replace("1700000000.123456789", "1700000000.123456788");
        var earlier = Record.Replace("1700000000.123456789", "1700000000.123456787")
            .Replace("0.0.1001-1700000000-000000001", "0.0.1001-1700000000-000000002");
        var pages = acrossPages
            ? new[] { Page(new[] { later }, "/api/v1/transactions?account.id=0.0.1001&page=2"), Page(new[] { earlier }, null) }
            : new[] { Page(new[] { later, earlier }, null) };
        using var handler = new HistoryHandler(pages);
        var result = await Provider(handler).GetTransactionsAsync(new GetWeb3TransactionsRequest { WalletAddress = "0.0.1001" });
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message, "ascending consensus timestamps");
    }

    private sealed class HistoryHandler(IEnumerable<string> pages, HttpStatusCode exhausted = HttpStatusCode.BadRequest) : HttpMessageHandler
    {
        private readonly Queue<string> _pages = new(pages);
        public List<string> Requests { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.AbsoluteUri);
            return Task.FromResult(_pages.Count == 0 ? new HttpResponseMessage(exhausted)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_pages.Dequeue()) });
        }
    }
}
