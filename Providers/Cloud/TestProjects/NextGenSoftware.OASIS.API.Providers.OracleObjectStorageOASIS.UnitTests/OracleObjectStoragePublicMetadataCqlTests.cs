#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.OracleObjectStorageOASIS.UnitTests;

[TestClass]
public class OracleObjectStoragePublicMetadataCqlTests
{
    private const ProviderType Provider = ProviderType.OracleObjectStorageOASIS;
    private static readonly Guid Owner = Guid.Parse("a5808c3e-7031-47e7-92de-48a659568f1b");
    private static readonly string[] Keys =
    {
        "bds-livelabs/env.sh", "bds-livelabs/download-all-hdfs-data.sh", "bds-livelabs/download-all-objstore.sh",
    };
    private static readonly Guid[] Ids = Keys.Select(key => OracleObjectStoragePublicMetadataQuerySource.LogicalId(Owner, key)).ToArray();

    [TestMethod]
    public async Task ExactHeadsReadOnlyTheSelectedMetadata()
    {
        using var transport = new Reply();
        using var source = new OracleObjectStoragePublicMetadataQuerySource(Owner, Keys, Keys[0], transport);
        Assert.IsFalse((await source.ActivateQuerySourceAsync()).IsError);
        await CheckAsync(source, Ids, Ids);
        await CheckAsync(source, Ids[..2], Ids[..2]);
        await CheckAsync(source, new[] { Guid.NewGuid() }, null);
        Assert.AreEqual(6, transport.Requests);
    }

    [DataTestMethod]
    [DataRow("etag")]
    [DataRow("size")]
    [DataRow("modified")]
    [DataRow("media")]
    public async Task ActivationRejectsIncompleteNativeMetadata(string field)
    {
        using var transport = new Reply(field);
        using var source = new OracleObjectStoragePublicMetadataQuerySource(Owner, Keys, Keys[0], transport);
        Assert.IsTrue((await source.ActivateQuerySourceAsync()).IsError);
        Assert.IsFalse(source.IsProviderActivated);
    }

    [DataTestMethod]
    [DataRow("bds-livelabs/../env.sh")]
    [DataRow("bds-livelabs/env.sh?list=1")]
    [DataRow("other/env.sh")]
    public void RejectsNoncanonicalWorkshopKeys(string key)
    {
        Assert.ThrowsException<ArgumentException>(() => new OracleObjectStoragePublicMetadataQuerySource(Owner, new[] { key }, key));
    }

    [TestMethod]
    [TestCategory("OracleObjectStoragePublicCqlNative")]
    public async Task OfficialOracleWorkshopReturnsThreeAndTwoReviewedObjects()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_OCI_OBJECT_VERIFY_PUBLIC") != "1")
        {
            Assert.Inconclusive("Enable the reviewed Oracle Object Storage workshop probe explicitly.");
            return;
        }
        using var source = new OracleObjectStoragePublicMetadataQuerySource(Owner, Keys, Keys[0]);
        var active = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(active.IsError, active.Exception?.ToString());
        await CheckAsync(source, Ids, Ids);
        await CheckAsync(source, Ids[..2], Ids[..2]);
    }

    private static async Task CheckAsync(OracleObjectStoragePublicMetadataQuerySource source, Guid[] selected, Guid[]? expected)
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
            Assert.IsTrue(row.IsPublic && row.CreatedByAvatarId == Owner && !row.Children.Any());
            Assert.AreEqual(row.Name, row.ProviderUniqueStorageKey[Provider]);
            Assert.AreEqual(row.Name, row.MetaData["OciWorkshopObjectKey"].ToString());
            Assert.AreEqual("text/x-sh", row.MetaData["OciObjectMediaType"].ToString());
            Assert.IsTrue(long.TryParse(row.MetaData["OciObjectBytes"].ToString(), out var bytes) && bytes > 0);
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.MetaData["OciObjectETag"].ToString()));
        }
    }

    private sealed class Reply(string? missing = null) : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.AreEqual(HttpMethod.Head, request.Method);
            Assert.AreEqual("objectstorage.us-phoenix-1.oraclecloud.com", request.RequestUri?.Host);
            Assert.IsNull(request.Headers.Authorization);
            Assert.IsNull(request.Content);
            Assert.AreEqual(string.Empty, request.RequestUri!.Query);
            const string prefix = "/n/oraclebigdatadb/b/workshop-data/o/";
            StringAssert.StartsWith(request.RequestUri.AbsolutePath, prefix);
            Assert.IsTrue(Keys.Contains(request.RequestUri.AbsolutePath[prefix.Length..]));
            var content = new UnreadableBody();
            if (missing != "size") content.Headers.ContentLength = 534;
            if (missing != "modified") content.Headers.LastModified = new DateTimeOffset(2020, 12, 1, 16, 19, 57, TimeSpan.Zero);
            if (missing != "media") content.Headers.ContentType = new MediaTypeHeaderValue("text/x-sh");
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
            if (missing != "etag") response.Headers.TryAddWithoutValidation("ETag", "ee15eb88-80e0-45a1-b8a1-a3121e1477fe");
            return Task.FromResult(response);
        }
    }

    private sealed class UnreadableBody : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            Task.FromException(new AssertFailedException("The native HEAD response body must not be read."));

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
