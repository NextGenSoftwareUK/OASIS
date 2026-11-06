using System.Net;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.DecentralandOASIS.UnitTests;

[TestClass]
public sealed class DecentralandSceneCqlTests
{
    private const ProviderType Provider = ProviderType.DecentralandOASIS;
    private const string Entity = "bafkreifjj7y4dqzyu5topcw3omj774dxa2obms6nmnccxb2fwp4pl25gxy";
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Parcels = { "0,0", "-53,71", "-100,-50" };

    [TestMethod]
    [DataRow("-3,-2", "-3,-2")]
    [DataRow("0,0", "-3,-2")]
    public async Task MissingRequestedOrBaseParcelRejectsTheScene(string pointer, string baseParcel)
    {
        using var source = new DecentralandSceneQuerySource(Owner, new SceneTransport(pointer, baseParcel));
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(activation.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        StringAssert.Contains(activation.Exception?.Message, "Catalyst");
    }

    [TestMethod, TestCategory("DecentralandSceneCqlNative")]
    public async Task CatalystReturnsThreeAndTwoCurrentParcelScenes()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_DECENTRALAND_SCENE_VERIFY") != "1")
        {
            Assert.Inconclusive("Enable the reviewed public Catalyst reads explicitly.");
            return;
        }

        using var source = new DecentralandSceneQuerySource(Owner);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.Message);
        foreach (var count in new[] { 3, 2 })
        {
            var selected = Parcels.Take(count).ToArray();
            var ids = selected.Select(parcel => DecentralandSceneQuerySource.LogicalId(Owner, parcel)).ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, ids).Result).Result
                .Where(row => ids.Contains(row.Id));
            Assert.IsFalse(query.IsError, query.Message);
            var plan = CosmicQueryPlanner.Plan(query.Result, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Exception?.Message);
            CollectionAssert.AreEquivalent(ids, result.Result.Select(row => row.Id).ToArray());
            CollectionAssert.AreEquivalent(selected, result.Result.Select(row => row.ProviderUniqueStorageKey[Provider]).ToArray());
            foreach (var row in result.Result)
            {
                Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner);
                Assert.AreEqual(row.ProviderUniqueStorageKey[Provider], row.MetaData["DecentralandParcel"].ToString());
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.MetaData["DecentralandSceneEntityId"].ToString()));
                Assert.IsTrue(Convert.ToInt64(row.MetaData["DecentralandSceneTimestamp"]) > 0);
            }
        }
    }

    private sealed class SceneTransport(string pointer, string baseParcel) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("https://peer.decentraland.org/content/entities/scene?pointer=0%2C0", request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new[]
                {
                    new
                    {
                        type = "scene", id = Entity, timestamp = 42, pointers = new[] { pointer },
                        metadata = new { scene = new { @base = baseParcel }, display = new { title = "Synthetic plaza" } }
                    }
                })
            });
        }
    }
}
