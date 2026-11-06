using System.Net;
using System.Net.Http.Headers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PinataOASIS.UnitTests;

[TestClass]
public sealed class PinataPublicBlockCqlTests
{
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly (string Cid, int Size, string Sha256)[] Blocks =
    {
        ("QmP4eZj61tDxCmtb6aqSwQsrxRQAN2gwxTEXkTed9TujHh", 344, "0ac23b0944aa602cc40e2ad0d6b59f5404b5b691e12753b82a6932215c1c59f0"),
        ("QmeBt6f2Dem8QnZuZ8PDRT1iuWpphmza3gP17en2ZoReYR", 262158, "eb7be6fc8a1a0b50cf51355eaadb9c54abc43a45ad8717b4b3165a5ae6fc4db2"),
        ("Qmb733sded7TRm8U8bM4FHif89jUVKd8yQiurKCKgCLyQk", 262158, "bdab0f3ede2ff4d7a4e55c102dd42e308126bc47a0b4d1f6fac391aa7488a949")
    };

    [DataTestMethod]
    [DataRow("application/vnd.ipld.raw", 3)]
    [DataRow("application/vnd.ipld.raw", 524289)]
    [DataRow("text/html", 3)]
    public async Task WrongDigestOversizedBlockAndWrongMediaTypeFailActivation(string mediaType, int bytes)
    {
        using var transport = new InvalidBlock(mediaType, bytes);
        using var source = new PinataPublicBlockQuerySource(Owner, transport);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_SOURCE_UNAVAILABLE", result.ErrorCode);
        Assert.IsFalse(source.IsProviderActivated);
        Assert.AreEqual(1, transport.Requests);
    }

    [TestMethod, TestCategory("PinataPublicBlockNative")]
    public async Task PublicGatewayReturnsExactVerifiedThreeAndTwoBlockRows()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_PINATA_PUBLIC_BLOCK_VERIFY") != "1")
        {
            Assert.Inconclusive("Enable the reviewed public Pinata block reads explicitly.");
            return;
        }

        const ProviderType provider = ProviderType.PinataOASIS;
        using var source = new PinataPublicBlockQuerySource(Owner);
        var activated = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.Message);
        foreach (var count in new[] { 3, 2 })
        {
            var selected = Blocks.Take(count).ToDictionary(item => PinataPublicBlockQuerySource.LogicalId(Owner, item.Cid));
            var ids = selected.Keys.ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, provider, ids).Result).Result
                .Where(row => ids.Contains(row.Id));
            Assert.IsFalse(query.IsError, query.Message);
            var plan = CosmicQueryPlanner.Plan(query.Result, provider, new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Exception?.Message);
            CollectionAssert.AreEquivalent(ids, result.Result.Select(row => row.Id).ToArray());
            foreach (var row in result.Result)
            {
                var block = selected[row.Id];
                Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner);
                Assert.AreEqual(block.Cid, row.ProviderUniqueStorageKey[provider]);
                Assert.AreEqual(block.Size, Convert.ToInt32(row.MetaData["PinataBlockBytes"]));
                Assert.AreEqual(block.Sha256, row.MetaData["PinataBlockSha256"].ToString());
                Assert.AreEqual("dag-pb", row.MetaData["PinataBlockCodec"].ToString());
            }
        }
    }

    [TestMethod]
    public async Task IndependentReviewedBlocksStartWithinOneBoundedReadBatch()
    {
        const ProviderType provider = ProviderType.PinataOASIS;
        var ids = Blocks.Select(block => PinataPublicBlockQuerySource.LogicalId(Owner, block.Cid)).ToArray();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int observed = 0;
        var reader = new BoundedPublicKeyValueQuery(provider, async (id, token) =>
        {
            if (Interlocked.Increment(ref observed) == ids.Length) started.TrySetResult();
            await release.Task.WaitAsync(token);
            return StoredRecordJson.Serialize(new Holon { Id = id, IsPublic = true, CreatedByAvatarId = Owner });
        }, maximumConcurrentReads: ids.Length);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id));
        Assert.IsFalse(query.IsError, query.Message);
        var plan = CosmicQueryPlanner.Plan(query.Result, provider,
            new[] { new CosmicQueryTarget(provider, BoundedIdentityCqlCompiler.Capabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);

        var resultTask = reader.ExecuteAsync(plan.Result, ids.Length, active: true, versioned: false, CancellationToken.None);
        var reachedBatch = await Task.WhenAny(started.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        release.TrySetResult();
        var result = await resultTask;
        Assert.AreSame(started.Task, reachedBatch);
        Assert.IsFalse(result.IsError, result.Exception?.Message);
        CollectionAssert.AreEquivalent(ids, result.Result.Select(row => row.Id).ToArray());
    }

    private sealed class InvalidBlock(string mediaType, int bytes) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Requests++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual($"https://gateway.pinata.cloud/ipfs/{Blocks[0].Cid}?format=raw", request.RequestUri!.AbsoluteUri);
            Assert.AreEqual("application/vnd.ipld.raw", request.Headers.Accept.Single().MediaType);
            Assert.IsNull(request.Headers.Authorization);
            var content = new ByteArrayContent(new byte[bytes]);
            content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        }
    }
}
