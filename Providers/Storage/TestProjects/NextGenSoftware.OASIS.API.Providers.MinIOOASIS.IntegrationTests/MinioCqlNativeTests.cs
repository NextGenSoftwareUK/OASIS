using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MinIOOASIS.IntegrationTests;

public sealed record MinioCqlSource(string Endpoint, string Bucket, Guid Actor, Guid[] LiveIds, Guid DeletedId, Guid MissingId);

[TestClass]
public class MinioCqlNativeTests
{
    [TestMethod]
    [TestCategory("MinioCqlNative")]
    public async Task ExistingNativeObjectsSupportThreeTwoExcludedPagingAndUnchangedEtags()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_MINIO_CQL_SOURCE") ?? throw new InvalidOperationException("Explicit retained source required.");
        var sourceBytes = await File.ReadAllBytesAsync(path);
        var source = JsonSerializer.Deserialize<MinioCqlSource>(sourceBytes)!;
        using var admitted = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(path)!, "bootstrap.ack.json")));
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(sourceBytes)), admitted.RootElement.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(4, admitted.RootElement.GetProperty("createdRecords").GetInt32());
        Assert.AreEqual("http://127.0.0.1:19700", source.Endpoint);
        Assert.AreEqual("cosmic-cql-minio-recovery", source.Bucket);
        Assert.AreNotEqual(Guid.Empty, source.Actor); Assert.AreEqual(3, source.LiveIds.Length);
        var ids = source.LiveIds.Concat(new[] { source.DeletedId, source.MissingId }).ToArray();
        Assert.AreEqual(5, ids.Distinct().Count()); Assert.IsFalse(ids.Contains(Guid.Empty));
        using var credentials = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(path)!, "credentials.json")));
        var username = credentials.RootElement.GetProperty("username").GetString()!;
        var password = credentials.RootElement.GetProperty("password").GetString()!;
        using var observer = new AmazonS3Client(username, password, new AmazonS3Config {
            ServiceURL = source.Endpoint, ForcePathStyle = true, UseHttp = true, AuthenticationRegion = "us-east-1", MaxErrorRetry = 0 });
        var provider = new MinIOOASIS(source.Endpoint, username, password, source.Bucket, false, source.LiveIds[0]);
        var activated = await provider.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString() ?? activated.Message);
        try
        {
            var before = await ObserveOriginalAsync(path, source, observer);
            CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MinIOOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(source.Actor, ProviderType.MinIOOASIS, ids).Result).Result;
            async Task<Guid[]> Read(CosmicQuery<Holon> query)
            {
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.MinIOOASIS, new[] { new CosmicQueryTarget(ProviderType.MinIOOASIS, provider.QueryCapabilities) });
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
            CollectionAssert.AreEqual(before, await ObserveOriginalAsync(path, source, observer));
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
    public static async Task<string[]> ObserveOriginalAsync(string path, MinioCqlSource source, IAmazonS3 observer)
    {
        Assert.AreEqual("http://127.0.0.1:19700", source.Endpoint);
        Assert.AreEqual("cosmic-cql-minio-recovery", source.Bucket);
        var ids = source.LiveIds.Concat(new[] { source.DeletedId, source.MissingId }).ToArray();
        Assert.AreEqual(3, source.LiveIds.Length); Assert.AreEqual(5, ids.Distinct().Count()); Assert.IsFalse(ids.Contains(Guid.Empty));
        using var admission = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(path)!, "bootstrap.ack.json")));
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))), admission.RootElement.GetProperty("sourceSha256").GetString());
        var observations = new List<string>();
        foreach (var id in ids)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            if (id == source.MissingId)
            {
                try { using var unexpected = await observer.GetObjectAsync(source.Bucket, $"oasis-holons/{id:D}.json", deadline.Token); Assert.Fail("Missing object control is present."); }
                catch (AmazonS3Exception error) { Assert.AreEqual(HttpStatusCode.NotFound, error.StatusCode); Assert.AreEqual("NoSuchKey", error.ErrorCode); }
                continue;
            }
            using var response = await observer.GetObjectAsync(source.Bucket, $"oasis-holons/{id:D}.json", deadline.Token);
            Assert.AreEqual(HttpStatusCode.OK, response.HttpStatusCode); Assert.IsFalse(string.IsNullOrWhiteSpace(response.ETag));
            using var body = new MemoryStream(); var buffer = new byte[8192]; int count;
            while ((count = await response.ResponseStream.ReadAsync(buffer, deadline.Token)) != 0)
            { Assert.IsTrue(body.Length + count <= 1024 * 1024); body.Write(buffer, 0, count); }
            Assert.AreEqual(response.ContentLength, body.Length);
            using var row = JsonDocument.Parse(body.ToArray());
            Assert.AreEqual(id, row.RootElement.GetProperty("Id").GetGuid());
            Assert.AreEqual(source.Actor, row.RootElement.GetProperty("CreatedByAvatarId").GetGuid());
            Assert.AreEqual(id == source.DeletedId, row.RootElement.GetProperty("IsDeleted").GetBoolean());
            var hash = Convert.ToHexString(SHA256.HashData(body.ToArray()));
            using var original = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Path.GetDirectoryName(path)!, id.ToString("N") + ".ack.json")));
            Assert.AreEqual(original.RootElement.GetProperty("etag").GetString(), response.ETag);
            Assert.AreEqual(original.RootElement.GetProperty("payloadSha256").GetString(), hash);
            observations.Add(response.ETag + ":" + hash);
        }
        return observations.ToArray();
    }
}
