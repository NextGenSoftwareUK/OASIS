using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using System.Net.Http;
using Amazon.Runtime;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using MinioProvider = NextGenSoftware.OASIS.API.Providers.MinIOOASIS.MinIOOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MinIOOASIS.IntegrationTests;

// Explicit native test filter only: an unavailable admitted source fails, never skips.
[TestClass]
public class MinioHostedNativeSourceTests
{
    private sealed record Original(Guid Id, string Sha256, string ETag);
    private sealed record Source(string Endpoint, string Bucket, string Region, Guid Actor, Guid[] LiveIds,
        Guid DeletedId, Guid MissingId, Original[] Originals);

    [TestMethod]
    [TestCategory("MinioHostedCqlNative")]
    public async Task ExistingOwnedNativeSourceSupportsThreeTwoExcludedPagingAndUnchangedOriginals()
    {
        string Required(string name) => Environment.GetEnvironmentVariable(name) is string value && !string.IsNullOrWhiteSpace(value)
            ? value : throw new InvalidOperationException("Required native source configuration is absent: " + name);
        var sourcePath = Required("COSMIC_MINIO_HOSTED_SOURCE");
        var size = new FileInfo(sourcePath).Length;
        Assert.IsTrue(size > 0 && size <= 16384, "Bounded teaching descriptor required.");
        var sourceBytes = await File.ReadAllBytesAsync(sourcePath);
        using (var document = JsonDocument.Parse(sourceBytes)) ValidateUniqueProperties(document.RootElement);
        var source = JsonSerializer.Deserialize<Source>(sourceBytes, new JsonSerializerOptions {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow })!;
        Assert.IsNotNull(source);
        Assert.IsTrue(Uri.TryCreate(source.Endpoint, UriKind.Absolute, out var origin) && origin.Scheme == "https" && !origin.IsLoopback &&
            origin.AbsolutePath == "/" && origin.UserInfo.Length == 0 && origin.Query.Length == 0 && origin.Fragment.Length == 0,
            "Explicit credential-free HTTPS MinIO origin required.");
        Assert.IsTrue(Regex.IsMatch(source.Region ?? "", "^[a-z0-9][a-z0-9-]{0,62}$"), "Explicit native signing region required.");
        Assert.IsTrue(Regex.IsMatch(source.Bucket ?? "", "^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$"), "DNS-safe non-dotted teaching bucket required.");
        Assert.AreNotEqual(Guid.Empty, source.Actor);
        Assert.IsNotNull(source.LiveIds); Assert.AreEqual(3, source.LiveIds.Length);
        var ids = source.LiveIds.Concat(new[] { source.DeletedId, source.MissingId }).ToArray();
        Assert.AreEqual(5, ids.Distinct().Count()); Assert.IsFalse(ids.Contains(Guid.Empty));
        Assert.IsNotNull(source.Originals); Assert.AreEqual(4, source.Originals.Length);
        CollectionAssert.AreEquivalent(ids.Where(id => id != source.MissingId).ToArray(), source.Originals.Select(row => row.Id).ToArray());
        foreach (var original in source.Originals)
        {
            Assert.IsTrue(Regex.IsMatch(original.Sha256 ?? "", "^[a-fA-F0-9]{64}$"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(original.ETag));
        }
        var access = Required("COSMIC_MINIO_HOSTED_ACCESS_KEY");
        var secret = Required("COSMIC_MINIO_HOSTED_SECRET_KEY");
        IAmazonS3 CreateClient() => new AmazonS3Client(access, secret, new AmazonS3Config {
            ServiceURL = source.Endpoint, ForcePathStyle = true, AuthenticationRegion = source.Region, MaxErrorRetry = 0,
            HttpClientFactory = new GuardedFactory() });
        using var observer = CreateClient();
        var before = await Observe(source, observer);
        var provider = new MinioProvider(source.Endpoint, access, secret, source.Bucket, true, source.LiveIds[0], clientFactory: CreateClient);
        try
        {
            var activated = await provider.ActivateQuerySourceAsync(default);
            Assert.IsFalse(activated.IsError, activated.Exception?.ToString() ?? activated.Message);
            CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MinIOOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(source.Actor, ProviderType.MinIOOASIS, ids).Result).Result;
            async Task<Guid[]> Read(CosmicQuery<Holon> query)
            {
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.MinIOOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.MinIOOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Exception?.ToString() ?? result.Message);
                Assert.IsTrue(result.Result.All(row => !row.Children.Any()));
                return result.Result.Select(row => row.Id).ToArray();
            }
            var ordered = source.LiveIds.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(ordered, await Read(Query()));
            var subset = new[] { ordered[0], ordered[2] };
            CollectionAssert.AreEqual(subset, await Read(Query().Where(row => subset.Contains(row.Id)).Result));
            var excluded = new[] { source.DeletedId, source.MissingId };
            Assert.AreEqual(0, (await Read(Query().Where(row => excluded.Contains(row.Id)).Result)).Length);
            CollectionAssert.AreEqual(new[] { ordered[1] }, await Read(Query().Skip(1).Result.Take(1).Result));
            CollectionAssert.AreEqual(before, await Observe(source, observer));
            CollectionAssert.AreEqual(sourceBytes, await File.ReadAllBytesAsync(sourcePath));
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    private sealed class GuardedFactory : Amazon.Runtime.HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig) => new(NativeConnectionGuard.CreateGuardedHttpHandler());
    }

    private static void ValidateUniqueProperties(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            { Assert.IsTrue(names.Add(property.Name), "Duplicate descriptor or record property."); ValidateUniqueProperties(property.Value); }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) ValidateUniqueProperties(item);
    }

    private static async Task<string[]> Observe(Source source, IAmazonS3 observer)
    {
        var observations = new List<string>();
        foreach (var original in source.Originals)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            using var response = await observer.GetObjectAsync(source.Bucket, "oasis-holons/" + original.Id.ToString("D") + ".json", deadline.Token);
            Assert.AreEqual(HttpStatusCode.OK, response.HttpStatusCode);
            Assert.AreEqual(original.ETag, response.ETag);
            Assert.IsTrue(response.ContentLength >= 0 && response.ContentLength <= 1024 * 1024);
            using var bytes = new MemoryStream(); var buffer = new byte[8192]; int count;
            while ((count = await response.ResponseStream.ReadAsync(buffer, deadline.Token)) != 0)
            { Assert.IsTrue(bytes.Length + count <= 1024 * 1024); bytes.Write(buffer, 0, count); }
            Assert.AreEqual(response.ContentLength, bytes.Length);
            var hash = Convert.ToHexString(SHA256.HashData(bytes.ToArray()));
            Assert.AreEqual(original.Sha256.ToUpperInvariant(), hash);
            using var record = JsonDocument.Parse(bytes.ToArray()); ValidateUniqueProperties(record.RootElement);
            Assert.AreEqual(original.Id, record.RootElement.GetProperty("Id").GetGuid());
            Assert.AreEqual(source.Actor, record.RootElement.GetProperty("CreatedByAvatarId").GetGuid());
            Assert.AreEqual(original.Id == source.DeletedId, record.RootElement.GetProperty("IsDeleted").GetBoolean());
            observations.Add(original.Id.ToString("N") + ":" + hash + ":" + response.ETag);
        }
        using var missingDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            using var unexpected = await observer.GetObjectAsync(source.Bucket, "oasis-holons/" + source.MissingId.ToString("D") + ".json", missingDeadline.Token);
            Assert.Fail("Admitted absent teaching object exists.");
        }
        catch (AmazonS3Exception error)
        { Assert.AreEqual(HttpStatusCode.NotFound, error.StatusCode); Assert.AreEqual("NoSuchKey", error.ErrorCode); }
        return observations.ToArray();
    }
}
