using System.Net;
using System.Net.Http.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.CloudinaryOASIS.UnitTests;

[TestClass]
public class CloudinaryPublicImageCqlTests
{
    private const ProviderType Provider = ProviderType.CloudinaryOASIS;
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Assets = { "sample.jpg", "lady.jpg", "woman.jpg" };
    private static readonly Guid[] Ids = Assets.Select(asset => CloudinaryPublicImageQuerySource.LogicalId(Owner, asset)).ToArray();

    [TestMethod]
    public async Task ExactMetadataReadsUseTheCatalogueWithoutDownloadingImages()
    {
        using var transport = new Reply("jpg", 576);
        using var source = new CloudinaryPublicImageQuerySource(Owner, Assets, Assets[0], transport);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        await CheckAsync(source, Ids, Ids);
        await CheckAsync(source, Ids[..2], Ids[..2]);
        await CheckAsync(source, new[] { Guid.NewGuid() }, null);
        Assert.AreEqual(6, transport.Requests);
    }

    [DataTestMethod]
    [DataRow("png", 576)]
    [DataRow("jpg", 577)]
    [DataRow("jpg", 0)]
    public async Task ActivationRejectsUnreviewedMetadataShape(string format, int height)
    {
        using var transport = new Reply(format, height);
        using var source = new CloudinaryPublicImageQuerySource(Owner, Assets, Assets[0], transport);
        Assert.IsTrue((await source.ActivateQuerySourceAsync()).IsError);
        Assert.IsFalse(source.IsProviderActivated);
    }

    [DataTestMethod]
    [DataRow("../sample.jpg")]
    [DataRow("sample.jpg?test=1")]
    [DataRow("SAMPLE.jpg")]
    public void RejectsNoncanonicalAssetPaths(string asset)
    {
        Assert.ThrowsException<ArgumentException>(() => new CloudinaryPublicImageQuerySource(Owner, new[] { asset }, asset));
    }

    [TestMethod]
    [TestCategory("CloudinaryPublicCqlNative")]
    public async Task PublicCloudinaryDemoReturnsThreeAndTwoReviewedImages()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_CLOUDINARY_VERIFY_PUBLIC") != "1")
        {
            Assert.Inconclusive("Enable the reviewed public Cloudinary probe explicitly.");
            return;
        }
        using var source = new CloudinaryPublicImageQuerySource(Owner, Assets, Assets[0]);
        var active = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(active.IsError, active.Exception?.ToString());
        await CheckAsync(source, Ids, Ids);
        await CheckAsync(source, Ids[..2], Ids[..2]);
    }

    private static async Task CheckAsync(CloudinaryPublicImageQuerySource source, Guid[] selected, Guid[]? expected)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Owner, Provider, selected).Result).Result
            .Where(row => selected.Contains(row.Id)).Result;
        var plan = CosmicQueryPlanner.Plan(query, Provider, new[] { new CosmicQueryTarget(Provider, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var result = await source.ExecuteCosmicQueryAsync(plan.Result);
        if (expected == null)
        {
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result);
            return;
        }
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEquivalent(expected, result.Result.Select(row => row.Id).ToArray());
        foreach (var row in result.Result)
        {
            Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner);
            Assert.AreEqual(row.Name, row.ProviderUniqueStorageKey[Provider]);
            Assert.AreEqual(row.Name, row.MetaData["CloudinaryAsset"].ToString());
            Assert.AreEqual("jpg", row.MetaData["CloudinaryDeliveryFormat"].ToString());
            Assert.IsTrue(int.TryParse(row.MetaData["CloudinaryOriginalWidth"].ToString(), out var width) && width > 0);
            Assert.IsTrue(int.TryParse(row.MetaData["CloudinaryOriginalHeight"].ToString(), out var height) && height > 0);
        }
    }

    private sealed class Reply(string format, int outputHeight) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("res.cloudinary.com", request.RequestUri?.Host);
            Assert.IsNull(request.Headers.Authorization);
            Assert.IsNull(request.Content);
            var asset = request.RequestUri!.Segments[^1];
            Assert.AreEqual($"/demo/image/upload/fl_getinfo/{asset}", request.RequestUri.AbsolutePath);
            Assert.IsTrue(Assets.Contains(asset));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    input = new { width = 864, height = 576, bytes = 120253 },
                    output = new { format, width = 864, height = outputHeight, bytes = 87162 }
                })
            });
        }
    }
}
