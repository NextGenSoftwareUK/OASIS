using System.Net;
using System.Reflection;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;
using Source = NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.AwsS3PublicMetadataSource;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

[TestClass]
public class AwsS3PublicMetadataTests
{
    public class Native : DispatchProxy
    {
        public List<string> Reads = new();
        public string? Missing;
        public string? Failed;
        public int Disposals;
        protected override object? Invoke(MethodInfo? method, object?[]? arguments)
        {
            if (method!.Name == "Dispose") { Disposals++; return null; }
            Assert.AreEqual("GetObjectMetadataAsync", method.Name, "No object body, list, credential or mutation operation is allowed.");
            var request = (GetObjectMetadataRequest)arguments![0]!;
            ((CancellationToken)arguments[1]!).ThrowIfCancellationRequested();
            Assert.AreEqual("public-test-bucket", request.BucketName);
            Reads.Add(request.Key);
            if (request.Key == Missing) throw new AmazonS3Exception("Missing controlled object") { StatusCode = HttpStatusCode.NotFound };
            if (request.Key == Failed) throw new AmazonS3Exception("Denied controlled object") { StatusCode = HttpStatusCode.Forbidden };
            var response = new GetObjectMetadataResponse
            {
                ContentLength = 1234, ETag = "\"native-etag\"", LastModified = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };
            response.Headers.ContentType = "application/octet-stream";
            return Task.FromResult(response);
        }
    }

    [DataTestMethod]
    [DataRow("selection")] [DataRow("point")] [DataRow("window")] [DataRow("missing")]
    [DataRow("failed")] [DataRow("empty-window-failure")] [DataRow("budget")]
    [DataRow("cancelled")] [DataRow("unknown")] [DataRow("inactive")]
    public async Task NativeMetadataSelectionIsBoundedReadOnlyAndPublishesNoPartialFailure(string scenario)
    {
        var client = DispatchProxy.Create<IAmazonS3, Native>();
        var native = (Native)(object)client;
        var keys = new[] { "observations/one.nc", "observations/two.nc", "observations/three.nc" };
        var owner = Guid.NewGuid();
        using var source = new Source(client, "public-test-bucket", keys, owner);
        keys[2] = "mutated-after-construction";

        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Reads.Clear();
        var selected = new[] { keys[0], scenario == "unknown" ? "outside-catalogue" : keys[1] };
        native.Missing = scenario == "missing" ? selected[1] : null;
        native.Failed = scenario is "failed" or "empty-window-failure" ? selected[1] : null;

        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AWSS3OASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var pointKey = selected[0];
        query = scenario == "point"
            ? query.Where(row => CosmicQueryMetadata.Value<string>(row, Source.ObjectKeyMetadata) == pointKey).Result
            : query.Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, Source.ObjectKeyMetadata))).Result;
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-failure") query = query.Take(0).Result;
        var planned = CosmicQueryPlanner.Plan(query, ProviderType.AWSS3OASIS,
            new[] { new CosmicQueryTarget(ProviderType.AWSS3OASIS, source.QueryCapabilities) });
        Assert.IsFalse(planned.IsError, planned.Message);
        using var cancellation = new CancellationTokenSource();
        if (scenario == "cancelled") cancellation.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(planned.Result,
            maximumResults: scenario == "budget" ? 1 : 8, cancellationToken: cancellation.Token);

        if (scenario is "failed" or "empty-window-failure" or "budget" or "cancelled" or "unknown" or "inactive")
        {
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result);
            Assert.IsFalse(result.IsLoaded);
            Assert.AreEqual(scenario switch
            {
                "budget" => "CQL_QUERY_LIMIT", "cancelled" => "CQL_QUERY_CANCELLED",
                "unknown" => "CQL_INVALID_SOURCE_KEY", "inactive" => "CQL_PROVIDER_INACTIVE", _ => "CQL_QUERY_FAILED"
            }, result.ErrorCode);
        }
        else
        {
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(scenario is "point" or "window" or "missing" ? 1 : 2, result.Result.Count);
            foreach (var row in result.Result)
            {
                Assert.IsTrue(row.IsPublic);
                Assert.AreEqual(owner, row.CreatedByAvatarId);
                Assert.AreEqual(1234L, row.MetaData["S3ContentLength"]);
                StringAssert.Contains(row.Description, "Object body not downloaded");
            }
        }
        Assert.AreEqual(scenario is "cancelled" or "unknown" or "inactive" ? 0 : scenario == "point" ? 1 : 2, native.Reads.Count);
        Assert.AreEqual(0, native.Disposals); // Borrowed SDK client lifetime remains with its owner.
    }

    [TestMethod]
    public async Task GeneralContentFiltersRejectBeforeAnyNativeRequestAndOwnedStorageIsNeverSelectedImplicitly()
    {
        var client = DispatchProxy.Create<IAmazonS3, Native>();
        var owner = Guid.NewGuid();
        using var source = new Source(client, "public-test-bucket", new[] { "one.nc" }, owner);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AWSS3OASIS).Result)
            .Where(row => row.Name.Contains("one")).Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(query, ProviderType.AWSS3OASIS,
            new[] { new CosmicQueryTarget(ProviderType.AWSS3OASIS, source.QueryCapabilities) }).IsError);
        Assert.AreEqual(0, ((Native)(object)client).Reads.Count);

        Assert.ThrowsException<ArgumentException>(() => AwsS3ProviderFactory.CreatePublicMetadata(new AWSS3OASISSettings
        {
            Region = "us-east-1", BucketName = "public-test-bucket", PublicObjectMetadataKeys = new[] { "one.nc" },
            PublicObjectMetadataOwner = Guid.NewGuid(), PublicObjectMetadataOnly = false
        }));
        Assert.ThrowsException<ArgumentException>(() => AwsS3ProviderFactory.CreatePublicMetadata(new AWSS3OASISSettings
        {
            Region = "us-east-1", BucketName = "public-test-bucket", PublicObjectMetadataKeys = new[] { "one.nc" },
            PublicObjectMetadataOwner = Guid.NewGuid(), PublicObjectMetadataOnly = true, UseDefaultCredentials = true
        }));
        // Ordinary mutations cannot change external objects, even when called directly.
        var id = NativeRecordHolonMapper.StableHolonId(owner, ProviderType.AWSS3OASIS, "s3-public-metadata:public-test-bucket", "one.nc");
        var deleted = await source.DeleteHolonAsync(id);
        Assert.IsTrue(deleted.IsError);
        Assert.IsFalse(deleted.IsDeleted);
        Assert.AreEqual(1, ((Native)(object)client).Reads.Count);
    }

    [TestMethod, TestCategory("AwsS3PublicNative")]
    public async Task ActualAnonymousS3SdkReturnsThreeMetadataRecordsAndTwoRecordSubset()
    {
        var path = Environment.GetEnvironmentVariable("OASIS_AWS_S3_PUBLIC_SOURCE")
            ?? throw new InvalidOperationException("Explicit native source catalogue path required.");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var config = document.RootElement;
        var keys = config.GetProperty("keys").EnumerateArray().Select(value => value.GetString()!).ToArray();
        using var source = AwsS3ProviderFactory.CreatePublicMetadata(new AWSS3OASISSettings
        {
            PublicObjectMetadataOnly = true,
            Region = config.GetProperty("region").GetString(), BucketName = config.GetProperty("bucket").GetString(),
            PublicObjectMetadataKeys = keys, PublicObjectMetadataOwner = config.GetProperty("owner").GetGuid()
        });
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var activation = await source.ActivateQuerySourceAsync(cancellation.Token);
        Assert.IsFalse(activation.IsError, activation.Exception?.ToString());
        foreach (var count in new[] { 3, 2 })
        {
            var selected = keys.Take(count).ToArray();
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AWSS3OASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
                .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, Source.ObjectKeyMetadata))).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.AWSS3OASIS,
                new[] { new CosmicQueryTarget(ProviderType.AWSS3OASIS, source.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await source.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancellation.Token);
            Assert.IsFalse(result.IsError, result.Exception?.ToString());
            Assert.AreEqual(count, result.Result.Count);
            CollectionAssert.AreEqual(selected, result.Result.Select(row => (string)row.MetaData[Source.ObjectKeyMetadata]).ToArray());
            Assert.IsTrue(result.Result.All(row => row.IsPublic && (long)row.MetaData["S3ContentLength"] > 0));
        }
    }
}
