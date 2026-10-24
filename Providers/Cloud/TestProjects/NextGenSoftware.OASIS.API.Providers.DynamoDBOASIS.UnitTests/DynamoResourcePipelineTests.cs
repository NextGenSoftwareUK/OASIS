using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoResourcePipelineTests
{
    private sealed class Service(string body, bool failure = false) : HttpMessageHandler
    {
        internal int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Interlocked.Increment(ref Calls);
            Assert.AreEqual("dynamodb.eu-west-1.amazonaws.com", request.RequestUri!.Host);
            StringAssert.StartsWith(request.Headers.GetValues("Authorization").Single(), "AWS4-HMAC-SHA256 ");
            using var wire = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.AreEqual("INDEXES", wire.RootElement.GetProperty("ReturnConsumedCapacity").GetString());
            var response = new HttpResponseMessage(failure ? HttpStatusCode.BadRequest : HttpStatusCode.OK)
            { Content = new StringContent(body, Encoding.UTF8, "application/x-amz-json-1.0") };
            response.Headers.TryAddWithoutValidation("x-amzn-RequestId", "native-original"); return response;
        }
    }
    private static (DynamoDBOASIS Provider, AmazonDynamoDBClient Client) Create(Service service)
    {
        var provider = new DynamoDBOASIS("synthetic-key", "synthetic-secret", "eu-west-1");
        var clientField = typeof(DynamoDBOASIS).GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var original = (AmazonDynamoDBClient)clientField.GetValue(provider)!;
        var resourceClientType = typeof(DynamoDBOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoResourceClient")!;
        Assert.AreEqual(resourceClientType, original.GetType(), "Production factory must use this exact pipeline adapter."); original.Dispose();
        var capture = typeof(DynamoDBOASIS).GetField("_resourceCapture", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;
        var config = new AmazonDynamoDBConfig { RegionEndpoint = RegionEndpoint.EUWest1, MaxErrorRetry = 0, HttpClientFactory = new HandlerHttpClientFactory(service) };
        var client = (AmazonDynamoDBClient)Activator.CreateInstance(resourceClientType, BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { new BasicAWSCredentials("synthetic-key", "synthetic-secret"), config, capture }, null)!;
        clientField.SetValue(provider, client); return (provider, client);
    }

    [DataTestMethod, DataRow(0), DataRow(1), DataRow(2), DataRow(3)]
    public async Task PublicReadReportsMissingVersusExplicitZeroCapacity(int mode)
    {
        var capacity = mode switch { 0 => "", 1 => ",\"ConsumedCapacity\":{}", 2 => ",\"ConsumedCapacity\":{\"TableName\":\"OasisHolons\",\"CapacityUnits\":0}",
            _ => ",\"ConsumedCapacity\":{\"TableName\":\"OasisHolons\",\"ReadCapacityUnits\":0.5}" };
        using var service = new Service("{\"Item\":{}" + capacity + "}"); var (provider, client) = Create(service); using var owned = client;
        var rows = new List<NativeResourceUsageObservation>();
        var capability = (NextGenSoftware.OASIS.API.Core.Interfaces.INativeResourceUsageProvider)provider;
        var result = await capability.ObserveNativeResourceUsageAsync("attempt-original", () => provider.LoadHolonAsync(Guid.NewGuid()),
            (nativeOperation, evidence) => { Assert.AreEqual("GetItem", nativeOperation); rows.AddRange(evidence); return Task.CompletedTask; });
        Assert.IsTrue(result.IsError, "Fixture deliberately returns no holon; native read still consumed resources.");
        Assert.AreEqual(1, service.Calls); var row = rows.Single();
        Assert.AreEqual(mode < 2 ? (decimal?)null : mode == 2 ? 0m : 0.5m, row.Quantity);
        Assert.AreEqual("attempt-original", row.ProviderRequestId); Assert.AreEqual("native-original", row.NativeRequestId);
        Assert.AreEqual("DynamoDBOASIS", row.Provider); Assert.AreEqual("eu-west-1/table/OasisHolons", row.ResourceScope);
        Assert.AreEqual(DateTimeKind.Utc, row.ObservedAtUtc.Kind);
    }

    [DataTestMethod, DataRow("scan"), DataRow("update"), DataRow("delete")]
    public async Task EveryOtherProductionDataRequestUsesSamePipeline(string operation)
    {
        using var service = new Service("{\"ConsumedCapacity\":{\"TableName\":\"OasisHolons\",\"CapacityUnits\":1.5}}");
        var (provider, client) = Create(service); using var owned = client; var rows = new List<NativeResourceUsageObservation>();
        await provider.ObserveNativeResourceUsageAsync("attempt-original", async () =>
        {
            var key = new Dictionary<string, AttributeValue> { ["id"] = new() { S = "synthetic" } };
            if (operation == "scan") await client.ScanAsync(new ScanRequest { TableName = "OasisHolons" });
            if (operation == "update") await client.UpdateItemAsync(new UpdateItemRequest { TableName = "OasisHolons", Key = key, UpdateExpression = "SET #v = :v",
                ExpressionAttributeNames = new() { ["#v"] = "value" }, ExpressionAttributeValues = new() { [":v"] = new() { S = "test" } } });
            if (operation == "delete") await client.DeleteItemAsync(new DeleteItemRequest { TableName = "OasisHolons", Key = key });
            return true;
        }, (nativeOperation, evidence) => { Assert.AreEqual(operation == "scan" ? "Scan" : operation == "update" ? "UpdateItem" : "DeleteItem", nativeOperation); rows.AddRange(evidence); return Task.CompletedTask; });
        Assert.AreEqual(1, service.Calls); Assert.AreEqual(1.5m, rows.Single().Quantity);
    }

    [TestMethod]
    public async Task TransactionsRetainEveryRequestedTableIncludingMissingEvidence()
    {
        using var service = new Service("{\"ConsumedCapacity\":[{\"TableName\":\"OasisHolons\",\"WriteCapacityUnits\":1.5}]}");
        var (provider, client) = Create(service); using var owned = client; var rows = new List<NativeResourceUsageObservation>();
        await provider.ObserveNativeResourceUsageAsync("attempt-original", () => client.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = new() {
            new() { Put = new() { TableName = "OasisHolons", Item = new() { ["id"] = new() { S = "one" } } } },
            new() { Put = new() { TableName = "OasisAvatars", Item = new() { ["id"] = new() { S = "two" } } } } } }),
            (nativeOperation, evidence) => { Assert.AreEqual("TransactWriteItems", nativeOperation); rows.AddRange(evidence); return Task.CompletedTask; });
        Assert.AreEqual(1, service.Calls); Assert.AreEqual(2, rows.Count);
        Assert.AreEqual(1.5m, rows.Single(row => row.ResourceScope.EndsWith("/OasisHolons")).Quantity);
        Assert.IsNull(rows.Single(row => row.ResourceScope.EndsWith("/OasisAvatars")).Quantity);
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task NativeFailuresAndObserverFailuresCannotBecomeSuccess(bool observerFails)
    {
        using var service = new Service("{\"__type\":\"ValidationException\",\"message\":\"controlled rejection\"}", true);
        var (provider, client) = Create(service); using var owned = client; var rows = new List<NativeResourceUsageObservation>(); var cause = new IOException("owned observer failure");
        var result = await provider.ObserveNativeResourceUsageAsync("attempt-original", () => provider.LoadHolonAsync(Guid.NewGuid()),
            (nativeOperation, evidence) => { rows.AddRange(evidence); if (observerFails) throw cause; return Task.CompletedTask; });
        Assert.IsTrue(result.IsError); Assert.AreEqual(1, service.Calls); Assert.IsNull(rows.Single().Quantity);
        Assert.AreEqual("native-original", rows.Single().NativeRequestId);
        if (observerFails) { Assert.IsInstanceOfType(result.Exception, typeof(AggregateException)); var terminal = ((AggregateException)result.Exception).InnerExceptions[1];
            Assert.IsInstanceOfType(terminal, typeof(InvalidDataException)); Assert.AreSame(cause, terminal.InnerException); }
        else Assert.IsInstanceOfType(result.Exception, typeof(AmazonDynamoDBException));
    }

    [TestMethod]
    public async Task SignedSdkTransactionPreservesRepeatedReturnedTableEntries()
    {
        using var service = new Service("{\"ConsumedCapacity\":[{\"TableName\":\"OasisHolons\",\"WriteCapacityUnits\":0.5},{\"TableName\":\"OasisHolons\",\"WriteCapacityUnits\":1.5}]}");
        var (provider, client) = Create(service); using var owned = client; var rows = new List<NativeResourceUsageObservation>();
        await provider.ObserveNativeResourceUsageAsync("attempt-original", () => client.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = new() {
            new() { Put = new() { TableName = "OasisHolons", Item = new() { ["id"] = new() { S = "one" } } } },
            new() { Put = new() { TableName = "OasisHolons", Item = new() { ["id"] = new() { S = "two" } } } } } }),
            (nativeOperation,evidence)=>{Assert.AreEqual("TransactWriteItems",nativeOperation);rows.AddRange(evidence);return Task.CompletedTask;});
        Assert.AreEqual(1,service.Calls);Assert.AreEqual(2,rows.Count);
        CollectionAssert.AreEquivalent(new decimal?[]{0.5m,1.5m},rows.Select(row=>row.Quantity).ToArray());
        Assert.AreEqual(2,rows.Select(row=>row.ResourceScope).Distinct().Count());
    }

    [TestMethod]
    public async Task NestedAndDetachedScopesCannotReuseEndedObservationAuthority()
    {
        using var service = new Service("{\"Item\":{}}"); var (provider, client) = Create(service); using var owned = client;
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => provider.ObserveNativeResourceUsageAsync("outer",
            () => provider.ObserveNativeResourceUsageAsync("inner", () => Task.FromResult(true), (_, _) => Task.CompletedTask), (_, _) => Task.CompletedTask));
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>>? detached = null;
        await provider.ObserveNativeResourceUsageAsync("outer", () =>
        {
            detached = Task.Run(async () => { await release.Task; return await provider.LoadHolonAsync(Guid.NewGuid()); });
            return Task.FromResult(true);
        }, (_, _) => Task.CompletedTask);
        release.SetResult(); var result = await detached!.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.IsTrue(result.IsError); StringAssert.Contains(result.Message, "scope has ended"); Assert.AreEqual(0, service.Calls);
    }

    [TestMethod]
    public async Task SdkCannotCompleteBeforeItsObserverAndConcurrentScopesStayIndependent()
    {
        using var service = new Service("{\"Item\":{},\"ConsumedCapacity\":{\"TableName\":\"OasisHolons\",\"CapacityUnits\":1}}");
        var (provider, client) = Create(service); using var owned = client;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new System.Collections.Concurrent.ConcurrentBag<string>();
        Task<GetItemResponse> Read() => client.GetItemAsync(new GetItemRequest { TableName = "OasisHolons", Key = new() { ["id"] = new() { S = "synthetic" } } });
        var left = provider.ObserveNativeResourceUsageAsync("left", Read, async (nativeOperation, rows) =>
        { observed.Add(rows.Single().ProviderRequestId); entered.SetResult(); await release.Task; });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5)); Assert.IsFalse(left.IsCompleted, "Observation must be awaited before the SDK call returns.");
        var right = provider.ObserveNativeResourceUsageAsync("right", Read, (nativeOperation, rows) => { observed.Add(rows.Single().ProviderRequestId); return Task.CompletedTask; });
        await right.WaitAsync(TimeSpan.FromSeconds(5)); Assert.IsFalse(left.IsCompleted);
        release.SetResult(); await left.WaitAsync(TimeSpan.FromSeconds(5));
        CollectionAssert.AreEquivalent(new[] { "left", "right" }, observed.ToArray()); Assert.AreEqual(2, service.Calls);
    }

    [DataTestMethod, DataRow(0), DataRow(1), DataRow(2), DataRow(3), DataRow(4)]
    public async Task InvalidObservationIdentityFailsBeforeAnySdkWork(int mode)
    {
        using var service = new Service("{\"Item\":{}}"); var (provider, client) = Create(service); using var owned = client;
        var identity = mode switch { 0 => "", 1 => "has space", 2 => "has\ncontrol", 3 => new string('a',257), _ => new string('é',200) };
        var invoked = false;
        await Assert.ThrowsExceptionAsync<UsageProtocolException>(() => provider.ObserveNativeResourceUsageAsync(identity,
            () => { invoked = true; return provider.LoadHolonAsync(Guid.NewGuid()); }, (_, _) => Task.CompletedTask));
        Assert.IsFalse(invoked); Assert.AreEqual(0, service.Calls);
    }

    [TestMethod]
    public async Task ObserverDeadlineCannotBeMisclassifiedAsProviderTransportFailure()
    {
        using var service = new Service("{\"Item\":{}}"); var (provider, client) = Create(service); using var owned = client;
        var cause = new TimeoutException("owned observation persistence deadline");
        var result = await provider.ObserveNativeResourceUsageAsync("attempt-original", () => provider.LoadHolonAsync(Guid.NewGuid()), (_, _) => throw cause);
        Assert.IsTrue(result.IsError); Assert.IsInstanceOfType(result.Exception, typeof(InvalidDataException));
        Assert.AreSame(cause, result.Exception.InnerException);
        Assert.IsFalse(((NextGenSoftware.OASIS.API.Core.Interfaces.IProviderTransportFailureClassifier)provider).IsProviderTransportFailure(result.Exception));
        Assert.AreEqual(1, service.Calls);
    }
}
