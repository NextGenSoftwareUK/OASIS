using System.Net;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.WakuOASIS.UnitTests;

[TestClass]
public sealed class WakuStoreCqlTests
{
    private const ProviderType Provider = ProviderType.WakuOASIS;
    private const string Pubsub = "/waku/2/rs/123/1";
    private const string Content = "/oasis-cql/1/store-fixture/proto";
    private const string Missing = "0x0101010101010101010101010101010101010101010101010101010101010101";
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");

    [TestMethod]
    public void TeachingDescriptorOnlyAdmitsThreeDistinctCanonicalHashes()
    {
        var hashes = new[] { Missing, "0x" + new string('a', 64), "0x" + new string('b', 64) };
        var descriptor = System.Text.Json.JsonSerializer.Serialize(new { sourceOwner = Owner, messageHashes = hashes });
        using var source = WakuStoreQueryConfiguration.CreateSource(descriptor);
        Assert.AreEqual(Provider, source.QueryProviderType);
        Assert.IsFalse(source.IsProviderActivated);

        foreach (var invalid in new[]
        {
            descriptor.Replace(hashes[1], hashes[0]),
            descriptor.Replace(hashes[1], hashes[1].ToUpperInvariant()),
            descriptor.Insert(1, "\"endpoint\":\"http://example.test\","),
            descriptor.Insert(1, $"\"sourceOwner\":\"{Owner:D}\","),
            System.Text.Json.JsonSerializer.Serialize(new { sourceOwner = Owner, messageHashes = hashes[..2] })
        })
            Assert.ThrowsException<ArgumentException>(() => WakuStoreQueryConfiguration.CreateSource(invalid));
    }

    [TestMethod]
    public async Task ReturnedHashCannotAuthenticateModifiedPayload()
    {
        using var source = new WakuStoreQuerySource(Owner, new[] { Missing }, new ForgedStore());
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        StringAssert.Contains(result.Exception?.Message, "does not match the requested hash");
    }

    [TestMethod, TestCategory("WakuStoreCqlNative")]
    public async Task IsolatedStoreReturnsExactThreeTwoAndZero()
    {
        var configured = Environment.GetEnvironmentVariable("COSMIC_WAKU_STORE_HASHES");
        if (string.IsNullOrEmpty(configured))
        {
            Assert.Inconclusive("An owned Waku Store fixture is required.");
            return;
        }

        var reviewed = configured.Split(',');
        Assert.AreEqual(3, reviewed.Length);
        using var source = new WakuStoreQuerySource(Owner, reviewed.Append(Missing).ToArray());
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.Message);
        foreach (var selection in new[] { reviewed, reviewed[..2], new[] { Missing } })
        {
            var ids = selection.Select(hash => WakuStoreQuerySource.LogicalId(Owner, hash)).ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
                .Where(row => ids.Contains(row.Id));
            Assert.IsFalse(query.IsError, query.Message);
            var plan = CosmicQueryPlanner.Plan(query.Result, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Exception?.Message);
            var expected = selection.Where(hash => hash != Missing).ToArray();
            CollectionAssert.AreEquivalent(expected,
                result.Result.Select(row => row.ProviderUniqueStorageKey[Provider]).ToArray());
            CollectionAssert.AreEquivalent(expected.Select(hash => WakuStoreQuerySource.LogicalId(Owner, hash)).ToArray(),
                result.Result.Select(row => row.Id).ToArray());
            foreach (var row in result.Result)
            {
                Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner);
                Assert.AreEqual(Pubsub, row.MetaData["WakuPubsubTopic"].ToString());
                Assert.AreEqual(Content, row.MetaData["WakuContentTopic"].ToString());
                Assert.AreEqual(15, Convert.ToInt32(row.MetaData["WakuPayloadBytes"]));
                Assert.IsFalse(row.MetaData.ContainsKey("payload"));
            }
        }
    }

    private sealed class ForgedStore : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("http://127.0.0.1:28645/store/v3/messages?includeData=true&pageSize=4&hashes=" + Missing,
                request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    statusCode = 200,
                    messages = new[]
                    {
                        new
                        {
                            messageHash = Missing, pubsubTopic = Pubsub,
                            message = new { contentTopic = Content, timestamp = 42, ephemeral = false, payload = "Zm9yZ2Vk" }
                        }
                    }
                })
            });
        }
    }
}
