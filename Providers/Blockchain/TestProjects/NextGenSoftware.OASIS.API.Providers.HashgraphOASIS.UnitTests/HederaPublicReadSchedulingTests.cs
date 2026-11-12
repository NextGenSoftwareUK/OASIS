using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests;

[TestClass]
public sealed class HederaPublicReadSchedulingTests
{
    private static readonly string[] Keys = { "0.0.2", "0.0.3", "0.0.98", "0.0.99", "0.0.100", "0.0.101" };

    private sealed class GatedMirror : HttpMessageHandler
    {
        public readonly TaskCompletionSource<bool> FirstWave = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Started, Active, Maximum;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("?limit=1", request.RequestUri!.Query);
            string key = request.RequestUri.AbsolutePath[17..];
            Assert.IsTrue(Keys.Contains(key));
            int active = Interlocked.Increment(ref Active);
            int observed;
            do { observed = Volatile.Read(ref Maximum); }
            while (active > observed && Interlocked.CompareExchange(ref Maximum, active, observed) != observed);
            if (Interlocked.Increment(ref Started) == 3) FirstWave.TrySetResult(true);
            try
            {
                await Release.Task.WaitAsync(token);
                return new HttpResponseMessage(HttpStatusCode.OK) {
                    Content = new StringContent(JsonSerializer.Serialize(new {
                        account = key, deleted = false, transactions = Array.Empty<object>(),
                        balance = new { balance = 123, timestamp = "1742896287.333154756" }
                    }), Encoding.UTF8, "application/json") };
            }
            finally { Interlocked.Decrement(ref Active); }
        }
    }

    [TestMethod]
    public async Task IndependentReadsOverlapWithinBoundAndPreserveSelectionOrder()
    {
        var mirror = new GatedMirror();
        using var client = new HttpClient(mirror);
        using var source = new HederaPublicAccountMetadataSource(client,
            new Uri(HederaPublicAccountMetadataSource.PublicEndpoint), Keys, Guid.NewGuid());
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var activation = source.ActivateQuerySourceAsync(deadline.Token);
        try
        {
            // A serial implementation cannot start the third request while
            // the first is blocked. No latency sleeps or timing assumptions.
            await mirror.FirstWave.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreEqual(3, Volatile.Read(ref mirror.Started));
            Assert.IsFalse(activation.IsCompleted);
            mirror.Release.TrySetResult(true);
            Assert.IsFalse((await activation).IsError);
            Assert.AreEqual(3, mirror.Maximum);
            Assert.AreEqual(0, mirror.Active);
            Assert.AreEqual(6, mirror.Started);

            var selected = Keys;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.HashgraphOASIS).Result)
                .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, HederaPublicAccountMetadataSource.AddressMetadata))).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.HashgraphOASIS,
                new[] { new CosmicQueryTarget(ProviderType.HashgraphOASIS, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result, 100, deadline.Token);
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            CollectionAssert.AreEqual(Keys, result.Result.Select(row => (string)row.MetaData[HederaPublicAccountMetadataSource.AddressMetadata]).ToArray());
            Assert.IsTrue(mirror.Maximum <= 3);
            Assert.AreEqual(0, mirror.Active);
        }
        finally
        {
            deadline.Cancel();
            mirror.Release.TrySetResult(true);
            await activation;
        }
    }

    [TestMethod]
    public async Task CancellationStopsInflightAndQueuedReadsWithoutPartialActivation()
    {
        var mirror = new GatedMirror();
        using var client = new HttpClient(mirror);
        using var source = new HederaPublicAccountMetadataSource(client,
            new Uri(HederaPublicAccountMetadataSource.PublicEndpoint), Keys, Guid.NewGuid());
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var activation = source.ActivateQuerySourceAsync(deadline.Token);
        try
        {
            await mirror.FirstWave.Task.WaitAsync(TimeSpan.FromSeconds(5));
            deadline.Cancel();
            var result = await activation;
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
            Assert.IsFalse(source.IsProviderActivated);
            Assert.AreEqual(3, mirror.Maximum);
            Assert.AreEqual(3, mirror.Started);
            Assert.AreEqual(0, mirror.Active);
        }
        finally
        {
            deadline.Cancel();
            mirror.Release.TrySetResult(true);
            await activation;
        }
    }
}
