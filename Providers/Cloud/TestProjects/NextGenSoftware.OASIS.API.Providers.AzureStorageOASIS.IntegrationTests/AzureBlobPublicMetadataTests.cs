using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Source = NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.AzureBlobPublicMetadataSource;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

[TestClass]
public class AzureBlobPublicMetadataTests
{
    private sealed class Native : HttpMessageHandler
    {
        public List<string> Reads { get; } = new();
        public string? Missing, Failed, Malformed;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Head, request.Method, "Only native object properties may be read.");
            Assert.AreEqual("publictest.blob.core.windows.net", request.RequestUri!.Host);
            Assert.AreEqual("", request.RequestUri.Query, "No SAS or listing query is permitted.");
            Assert.IsNull(request.Headers.Authorization, "Anonymous reads must not obtain credentials.");
            var key = Uri.UnescapeDataString(request.RequestUri.AbsolutePath["/public-data/".Length..]);
            Reads.Add(key);

            var status = key == Missing ? HttpStatusCode.NotFound
                : key == Failed ? HttpStatusCode.Forbidden : HttpStatusCode.OK;
            var response = new HttpResponseMessage(status) { Content = new ByteArrayContent(Array.Empty<byte>()) };
            if (status != HttpStatusCode.OK)
                response.Headers.Add("x-ms-error-code", status == HttpStatusCode.NotFound ? "BlobNotFound" : "AuthorizationFailure");
            else
            {
                response.Content.Headers.ContentLength = 9007199254740993L;
                response.Content.Headers.LastModified = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
                response.Headers.ETag = new EntityTagHeaderValue("\"native-etag\"");
                response.Headers.Add("x-ms-blob-type", key == Malformed ? "PageBlob" : "BlockBlob");
            }
            return Task.FromResult(response);
        }
    }

    [DataTestMethod]
    [DataRow("selection")][DataRow("point")][DataRow("paging")][DataRow("missing")]
    [DataRow("failure")][DataRow("malformed")][DataRow("empty-page-failure")]
    [DataRow("budget")][DataRow("cancelled")][DataRow("unknown")][DataRow("inactive")]
    public async Task PropertiesQueriesUseOnlyAnonymousHeadAndPublishCompleteResults(string scenario)
    {
        using var native = new Native();
        using var http = new HttpClient(native);
        var options = new BlobClientOptions { Transport = new HttpClientTransport(http) };
        options.Retry.MaxRetries = 0;
        var client = new BlobContainerClient(new Uri("https://publictest.blob.core.windows.net/public-data"), options);
        var keys = new[] { "data/one.parquet", "data/two.parquet", "data/three.parquet" };
        var owner = Guid.NewGuid();
        using var source = new Source("publictest", "public-data", keys, owner, client);

        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Reads.Clear();
        var selected = new[] { keys[0], scenario == "unknown" ? "outside.parquet" : keys[1] };
        native.Missing = scenario == "missing" ? keys[1] : null;
        native.Failed = scenario is "failure" or "empty-page-failure" ? keys[1] : null;
        native.Malformed = scenario == "malformed" ? keys[1] : null;
        keys[2] = "mutated-after-capture";

        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AzureStorageOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var point = selected[0];
        query = scenario == "point"
            ? query.Where(row => CosmicQueryMetadata.Value<string>(row, Source.ObjectKeyMetadata) == point).Result
            : query.Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, Source.ObjectKeyMetadata))).Result;
        if (scenario == "paging") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-page-failure") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.AzureStorageOASIS,
            new[] { new CosmicQueryTarget(ProviderType.AzureStorageOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        using var cancellation = new CancellationTokenSource();
        if (scenario == "cancelled") cancellation.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result, scenario == "budget" ? 1 : 8, cancellation.Token);

        if (scenario is "failure" or "malformed" or "empty-page-failure" or "budget" or "cancelled" or "unknown" or "inactive")
        {
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result, "Never publish a prefix of a failed selection.");
            Assert.AreEqual(scenario switch {
                "budget" => "CQL_QUERY_LIMIT", "cancelled" => "CQL_QUERY_CANCELLED",
                "unknown" => "CQL_INVALID_SOURCE_KEY", "inactive" => "CQL_PROVIDER_INACTIVE",
                _ => "CQL_QUERY_FAILED"
            }, result.ErrorCode);
        }
        else
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario is "point" or "paging" or "missing" ? 1 : 2, result.Result.Count);
            foreach (var row in result.Result)
            {
                Assert.IsTrue(row.IsPublic);
                Assert.AreEqual(owner, row.CreatedByAvatarId);
                Assert.AreEqual("9007199254740993", row.MetaData["AzureBlobContentLength"], "Preserve integers above JavaScript precision.");
                StringAssert.Contains(row.Description, "Object body not downloaded");
            }
        }
        Assert.AreEqual(scenario is "cancelled" or "unknown" or "inactive" ? 0 : scenario == "point" ? 1 : 2, native.Reads.Count);
    }

    [DataTestMethod]
    [DataRow("credential")][DataRow("endpoint")][DataRow("sas")][DataRow("duplicate")]
    [DataRow("escaped-duplicate")][DataRow("schema-type")][DataRow("owner")]
    [DataRow("path")][DataRow("account")][DataRow("container")][DataRow("duplicate-key")]
    public void InvalidConfigurationCannotCreateAPublicSource(string scenario)
    {
        var json = JsonSerializer.Serialize(new { schema = 1, account = "publictest", container = "public-data",
            keys = new[] { "data/one.parquet" }, sourceOwner = Guid.NewGuid() });
        json = scenario switch {
            "credential" => json[..^1] + ",\"credential\":\"forbidden\"}",
            "endpoint" => json[..^1] + ",\"endpoint\":\"http://localhost\"}",
            "sas" => json[..^1] + ",\"sas\":\"forbidden\"}",
            "duplicate" => json[..^1] + ",\"schema\":1}",
            "escaped-duplicate" => json[..^1] + ",\"\\u0073chema\":1}",
            "schema-type" => json.Replace("\"schema\":1", "\"schema\":\"1\""),
            "owner" => json.Replace("\"sourceOwner\":\"", "\"sourceOwner\":\"invalid-"),
            "path" => json.Replace("data/one.parquet", "../one.parquet"),
            "account" => json.Replace("publictest", "public.test"),
            "container" => json.Replace("public-data", "public--data"),
            _ => json.Replace("[\"data/one.parquet\"]", "[\"data/one.parquet\",\"data/one.parquet\"]")
        };
        Assert.ThrowsException<ArgumentException>(() => Source.FromJson(json));
    }

    [TestMethod]
    public async Task PublicSourceCannotSaveOrDeleteExternalObjects()
    {
        using var native = new Native();
        using var http = new HttpClient(native);
        var options = new BlobClientOptions { Transport = new HttpClientTransport(http) };
        options.Retry.MaxRetries = 0;
        var client = new BlobContainerClient(new Uri("https://publictest.blob.core.windows.net/public-data"), options);
        var owner = Guid.NewGuid();
        using var source = new Source("publictest", "public-data", new[] { "one.parquet" }, owner, client);
        Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        var saved = await source.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "Must not be written" }, saveChildren: false);
        Assert.IsTrue(saved.IsError);
        Assert.IsFalse(saved.IsSaved);
        var id = NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeRecordHolonMapper.StableHolonId(
            owner, ProviderType.AzureStorageOASIS, "azure-public-metadata:publictest/public-data", "one.parquet");
        var deleted = await source.DeleteHolonAsync(id);
        Assert.IsTrue(deleted.IsError);
        Assert.IsFalse(deleted.IsDeleted);
        // Native's handler asserts that every dispatched operation was HEAD.
    }

    [TestMethod]
    public void ContentScansRejectBeforeActivation()
    {
        using var source = new Source("publictest", "public-data", new[] { "one.parquet" }, Guid.NewGuid());
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AzureStorageOASIS).Result)
            .Where(row => row.Name.Contains("one")).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.AzureStorageOASIS,
            new[] { new CosmicQueryTarget(ProviderType.AzureStorageOASIS, source.QueryCapabilities) });
        Assert.IsTrue(plan.IsError);
        Assert.IsFalse(source.IsProviderActivated);
    }

    [TestMethod, TestCategory("AzureBlobPublicNative")]
    public async Task RealAzureManagerLinqWalkthroughReturnsThreeAndTwo()
    {
        var path = Environment.GetEnvironmentVariable("OASIS_AZURE_BLOB_PUBLIC_SOURCE")
            ?? throw new InvalidOperationException("Explicit retained Azure public catalogue required.");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var counts = await CosmicCqlDemo.AzureBlobPublicWalkthrough.RunAsync(path, TextWriter.Null, deadline.Token);
        Assert.AreEqual((3, 2), counts);
    }

    [TestMethod, TestCategory("AzureBlobPublicNative")]
    public async Task RealAzureSdkReadsThreePropertyRecordsAndTwoRecordSubset()
    {
        var path = Environment.GetEnvironmentVariable("OASIS_AZURE_BLOB_PUBLIC_SOURCE")
            ?? throw new InvalidOperationException("Explicit retained Azure public catalogue required.");
        var json = await File.ReadAllTextAsync(path);
        using var document = JsonDocument.Parse(json);
        var keys = document.RootElement.GetProperty("keys").EnumerateArray().Select(k => k.GetString()!).ToArray();
        using var source = Source.FromJson(json);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var activated = await source.ActivateQuerySourceAsync(deadline.Token);
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());

        foreach (var count in new[] { 3, 2 })
        {
            var selected = keys.Take(count).ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AzureStorageOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
                .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, Source.ObjectKeyMetadata))).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.AzureStorageOASIS,
                new[] { new CosmicQueryTarget(ProviderType.AzureStorageOASIS, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: deadline.Token);
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(count, result.Result.Count);
            CollectionAssert.AreEqual(selected, result.Result.Select(r => (string)r.MetaData[Source.ObjectKeyMetadata]).ToArray());
            Assert.IsTrue(result.Result.All(r => r.IsPublic));
            Assert.IsTrue(long.Parse((string)result.Result[0].MetaData["AzureBlobContentLength"]) > 0);
        }
    }
}
