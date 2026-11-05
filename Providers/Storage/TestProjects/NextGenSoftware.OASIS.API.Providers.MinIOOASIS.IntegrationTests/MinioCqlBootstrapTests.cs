using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.MinIOOASIS.IntegrationTests;

[TestClass]
public class MinioCqlBootstrapTests
{
    private static void Durable(string path, object value)
    {
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, value); stream.Flush(true);
    }

    [TestMethod]
    [TestCategory("MinioCqlBootstrap")]
    public async Task ExplicitNewOwnedBucketReceivesFourCreateOnlyCanonicalObjectsOnce()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_MINIO_CQL_SOURCE") ?? throw new InvalidOperationException("Explicit native runner required.");
        var root = Path.GetDirectoryName(path)!;
        var bytes = await File.ReadAllBytesAsync(path);
        var source = JsonSerializer.Deserialize<MinioCqlSource>(bytes)!;
        Assert.AreEqual("http://127.0.0.1:19700", source.Endpoint);
        Assert.AreEqual("cosmic-cql-minio-recovery", source.Bucket);
        Assert.AreEqual(3, source.LiveIds.Length);
        Assert.AreEqual(5, source.LiveIds.Concat(new[] { source.DeletedId, source.MissingId }).Distinct().Count());
        Assert.IsFalse(File.Exists(Path.Combine(root, "bootstrap.intent.json")), "Unknown original outcome cannot be replayed.");
        using var credentials = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "credentials.json")));
        using var client = new AmazonS3Client(credentials.RootElement.GetProperty("username").GetString(), credentials.RootElement.GetProperty("password").GetString(),
            new AmazonS3Config { ServiceURL = source.Endpoint, ForcePathStyle = true, UseHttp = true, AuthenticationRegion = "us-east-1", MaxErrorRetry = 0 });
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await client.GetBucketLocationAsync(source.Bucket, deadline.Token); Assert.Fail("Existing bucket cannot be adopted."); }
        catch (AmazonS3Exception error) { Assert.AreEqual(HttpStatusCode.NotFound, error.StatusCode); Assert.AreEqual("NoSuchBucket", error.ErrorCode); }
        var sourceHash = Convert.ToHexString(SHA256.HashData(bytes));
        Durable(Path.Combine(root, "bootstrap.intent.json"), new { sourceSha256 = sourceHash, bucket = source.Bucket, createdRecords = 4 });
        var bucket = await client.PutBucketAsync(new PutBucketRequest { BucketName = source.Bucket, UseClientRegion = true }, deadline.Token);
        Assert.AreEqual(HttpStatusCode.OK, bucket.HttpStatusCode);
        Durable(Path.Combine(root, "bucket.ack.json"), new { bucket = source.Bucket, created = true });
        foreach (var id in source.LiveIds.Append(source.DeletedId))
        {
            var row = new Holon { Id = id, Name = "MinIO teaching " + id.ToString("N"), CreatedByAvatarId = source.Actor,
                DeletedDate = id == source.DeletedId ? DateTime.UtcNow : DateTime.MinValue };
            if (id == source.LiveIds[0]) row.Children = new[] { new Holon { Id = Guid.NewGuid(), Name = "Embedded native child" } };
            var payload = JsonSerializer.SerializeToUtf8Bytes(row);
            var key = $"oasis-holons/{id:D}.json";
            var payloadHash = Convert.ToHexString(SHA256.HashData(payload));
            Durable(Path.Combine(root, id.ToString("N") + ".intent.json"), new { id, key, payloadSha256 = payloadHash });
            using var input = new MemoryStream(payload);
            var request = new PutObjectRequest { BucketName = source.Bucket, Key = key, InputStream = input, ContentType = "application/json" };
            request.Headers["If-None-Match"] = "*";
            var saved = await client.PutObjectAsync(request, deadline.Token);
            Assert.AreEqual(HttpStatusCode.OK, saved.HttpStatusCode);
            Assert.AreEqual(Convert.ToHexString(MD5.HashData(payload)), saved.ETag.Trim('"'), true);
            Durable(Path.Combine(root, id.ToString("N") + ".ack.json"), new { id, key, payloadSha256 = payloadHash, etag = saved.ETag });
        }
        Durable(Path.Combine(root, "bootstrap.ack.json"), new { sourceSha256 = sourceHash, createdRecords = 4 });
    }
}
