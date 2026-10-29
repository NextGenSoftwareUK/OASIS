using System.Net;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider = NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.AWSS3OASIS;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

// Requires an already running, explicitly configured compatible service.
// Never starts a service, provisions hosted AWS, or replaces a source/key.
[TestClass, DoNotParallelize]
public class AwsS3CqlNativeTests
{
    private sealed record Seed(Guid Actor, Guid[] Ids);

    [TestMethod, TestCategory("LocalNative")]
    public async Task EncryptedNativeSourceFiltersThreeToTwoAndPreservesObjects()
    {
        if (Environment.GetEnvironmentVariable("OASIS_S3_CQL_NATIVE") != "1")
            Assert.Inconclusive("Explicit existing-service S3 CQL opt-in required.");
        static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException($"Missing {name}.");
        var endpoint = new Uri(Required("OASIS_S3_CQL_ENDPOINT"));
        Assert.IsTrue(endpoint.Scheme == "http" && endpoint.Host == "127.0.0.1"
            && endpoint.AbsolutePath == "/" && endpoint.UserInfo == "" && endpoint.Query == "" && endpoint.Fragment == "",
            "Native compatible-service acceptance is loopback only.");
        var state = Path.GetFullPath(Required("OASIS_S3_CQL_STATE"));
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var manifest = Path.Combine(state, "demo-seed.json");
        var pending = Path.Combine(state, "demo-seed.pending.json");
        Assert.IsFalse(File.Exists(pending), "Preserve unfinished setup; never replay or reseed.");
        var bucket = Required("OASIS_S3_CQL_BUCKET");
        using var client = new AmazonS3Client(new BasicAWSCredentials(Required("MINIO_ACCESS_KEY"), Required("MINIO_SECRET_KEY")),
            new AmazonS3Config { ServiceURL = endpoint.ToString(), ForcePathStyle = true,
                AuthenticationRegion = "us-east-1", MaxErrorRetry = 0, Timeout = TimeSpan.FromSeconds(15) });
        Provider Open() => new(client, bucket,
            () => PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_S3_CQL_STORAGE_KEY"), "cosmic-cql/")
            { IsVersionControlEnabled = true };
        // Validate the separately retained key before any source mutation.
        var key = PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_S3_CQL_STORAGE_KEY");
        System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
        var buckets = (await client.ListBucketsAsync()).Buckets;
        Seed seed;
        if (!File.Exists(manifest))
        {
            Assert.IsFalse(buckets.Any(b => b.BucketName == bucket), "Never adopt an existing unowned bucket.");
            seed = new Seed(Guid.NewGuid(), Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray());
            using (var intent = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            { JsonSerializer.Serialize(intent, seed); intent.Flush(true); }
            var created = await client.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
            Assert.AreEqual(HttpStatusCode.OK, created.HttpStatusCode);
            using var writer = Open();
            var active = await writer.ActivateProviderAsync();
            Assert.IsFalse(active.IsError, active.Message + active.Exception);
            var names = new[] { "Espresso", "Latte", "Tea" };
            for (var i = 0; i < 3; i++)
            {
                var saved = await writer.SaveHolonAsync(new Holon { Id = seed.Ids[i], Name = names[i],
                    CreatedByAvatarId = seed.Actor, IsNewHolon = true }, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
                Assert.IsTrue(saved.IsSaved);
            }
            File.Move(pending, manifest);
        }
        else
        {
            seed = JsonSerializer.Deserialize<Seed>(await File.ReadAllTextAsync(manifest))!;
            Assert.IsTrue(buckets.Any(b => b.BucketName == bucket), "Original bucket missing; never recreate it.");
        }
        Assert.AreEqual(3, seed.Ids.Distinct().Count());
        Assert.AreNotEqual(Guid.Empty, seed.Actor);
        async Task<string[]> Snapshot()
        {
            // Test-only bounded integrity inspection; production CQL does not LIST.
            var listing = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, MaxKeys = 201 });
            Assert.IsFalse(listing.IsTruncated);
            Assert.IsTrue(listing.S3Objects.Count is > 0 and <= 200);
            var values = new List<string>();
            foreach (var item in listing.S3Objects.OrderBy(o => o.Key, StringComparer.Ordinal))
            {
                using var response = await client.GetObjectAsync(bucket, item.Key);
                using var reader = new StreamReader(response.ResponseStream);
                var body = await reader.ReadToEndAsync();
                foreach (var name in new[] { "Espresso", "Latte", "Tea" })
                { Assert.IsFalse(item.Key.Contains(name)); Assert.IsFalse(body.Contains(name)); }
                values.Add(item.Key + "|" + response.ETag + "|" + body);
            }
            return values.ToArray();
        }
        var before = await Snapshot();
        using var provider = Open();
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        foreach (var ids in new[] { seed.Ids, seed.Ids.Take(2).ToArray(), new[] { Guid.NewGuid() } })
        {
            var catalogue = ids.Length == 1 ? ids : seed.Ids;
            var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.AWSS3OASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(seed.Actor, ProviderType.AWSS3OASIS, catalogue).Result).Result
                .Where(h => ids.Contains(h.Id)).Result;
            var plan = CosmicQueryPlanner.Plan(query, ProviderType.AWSS3OASIS,
                new[] { new CosmicQueryTarget(ProviderType.AWSS3OASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            CollectionAssert.AreEquivalent(ids.Length == 1 ? Array.Empty<Guid>() : ids, result.Result.Select(h => h.Id).ToArray());
        }
        CollectionAssert.AreEqual(before, await Snapshot(), "CQL must preserve exact encrypted objects and ETags.");
    }
}
