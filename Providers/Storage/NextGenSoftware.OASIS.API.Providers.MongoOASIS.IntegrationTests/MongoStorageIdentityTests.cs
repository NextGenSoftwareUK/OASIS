using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public class MongoStorageIdentityTests
{
    private const string Uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
    private const string First = "cosmic_storage_identity_fixture_a";
    private const string Second = "cosmic_storage_identity_fixture_b";
    private const string Ghost = "cosmic_storage_identity_uncreated";

    [Fact]
    public async Task MongoStorageIdentityUsesNativeUuidAndRepositoryHandleWithoutWrites()
    {
        await RunOwned(async (client, source, other) =>
        {
            async Task<string[]> Snapshot(string name)
            {
                var db = client.GetDatabase(name);
                using var cursor = await db.ListCollectionsAsync();
                var rows = await cursor.ToListAsync();
                var result = new System.Collections.Generic.List<string>();
                foreach (var row in rows.OrderBy(row => row["name"].AsString))
                    result.Add(row.ToJson() + ":" + await db.GetCollection<BsonDocument>(row["name"].AsString).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
                return result.ToArray();
            }
            var before = await Snapshot(First);
            var captures = await Task.WhenAll(source.CaptureStorageIdentityAsync(), other.CaptureStorageIdentityAsync());
            Assert.All(captures, result => Assert.False(result.IsError, result.Message + result.Exception));
            Assert.Equal(captures[0].Result.StoreId, captures[1].Result.StoreId);
            Assert.Equal(captures[0].Result.BindingFingerprint, captures[1].Result.BindingFingerprint);
            Assert.NotEqual(Guid.Empty, captures[0].Result.StoreId);
            Assert.Equal(64, captures[0].Result.EndpointFingerprint.Length);
            Assert.Equal(before, await Snapshot(First));

            // The live repository still owns First. Mutable descriptors or a
            // replacement public context cannot redefine that native handle.
            source.ConnectionString = "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=50";
            source.DBName = Ghost;
            source.Database = new MongoDbContext(Uri, Ghost);
            var unchanged = await source.CaptureStorageIdentityAsync();
            Assert.False(unchanged.IsError, unchanged.Message + unchanged.Exception);
            Assert.Equal(captures[0].Result.BindingFingerprint, unchanged.Result.BindingFingerprint);
            using (var names = await client.ListDatabaseNamesAsync()) Assert.DoesNotContain(Ghost, await names.ToListAsync());

            var alternate = new Provider(Uri, Second);
            try
            {
                Assert.False((await alternate.ActivateProviderAsync()).IsError);
                var different = await alternate.CaptureStorageIdentityAsync();
                Assert.False(different.IsError, different.Message);
                Assert.NotEqual(captures[0].Result.StoreId, different.Result.StoreId);
                Assert.NotEqual(captures[0].Result.EndpointFingerprint, different.Result.EndpointFingerprint);
                Assert.NotEqual(captures[0].Result.BindingFingerprint, different.Result.BindingFingerprint);
            }
            finally { Assert.False((await alternate.DeActivateProviderAsync()).IsError); }
            var reopened = new Provider(Uri, First);
            try
            {
                Assert.False((await reopened.ActivateProviderAsync()).IsError);
                var identity = await reopened.CaptureStorageIdentityAsync();
                Assert.False(identity.IsError, identity.Message);
                Assert.Equal(captures[0].Result.BindingFingerprint, identity.Result.BindingFingerprint);
            }
            finally { Assert.False((await reopened.DeActivateProviderAsync()).IsError); }
        });
    }

    [Fact]
    public async Task MongoStorageIdentityMissingNativeCollectionIsRejectedWithoutRepair()
    {
        await RunOwned(async (client, source, _) =>
        {
            var db = client.GetDatabase(First);
            Assert.Equal(0, await db.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            // Exact empty, test-owned collection fault injection; no provider
            // operation is active while the native namespace is removed.
            await db.DropCollectionAsync("Holon");
            var rejected = await source.CaptureStorageIdentityAsync();
            Assert.True(rejected.IsError);
            Assert.Equal("COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED", rejected.ErrorCode);
            using var names = await db.ListCollectionNamesAsync();
            Assert.DoesNotContain("Holon", await names.ToListAsync());
        });
    }

    [Fact]
    public async Task MongoStorageIdentityDetectsExplicitCollectionRecreationAtSameEndpoint()
    {
        await RunOwned(async (client, source, other) =>
        {
            var original = await source.CaptureStorageIdentityAsync();
            Assert.False(original.IsError, original.Message);
            var db = client.GetDatabase(First);
            Assert.Equal(0, await db.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            await db.DropCollectionAsync("Holon");
            Assert.True((await source.CaptureStorageIdentityAsync()).IsError);
            // Explicit disposable-fixture recreation by the test owner; capture
            // itself never repairs the namespace or invents a replacement UUID.
            await db.CreateCollectionAsync("Holon");
            var recreated = await other.CaptureStorageIdentityAsync();
            Assert.False(recreated.IsError, recreated.Message);
            Assert.Equal(original.Result.EndpointFingerprint, recreated.Result.EndpointFingerprint);
            Assert.NotEqual(original.Result.StoreId, recreated.Result.StoreId);
            Assert.NotEqual(original.Result.BindingFingerprint, recreated.Result.BindingFingerprint);
        });
    }

    [Fact]
    public async Task MongoStorageIdentityInactiveAndPreCancelledCapturePerformsNoCreation()
    {
        var source = new Provider(Uri, Ghost);
        Assert.Equal("COSMIC_STORAGE_IDENTITY_NOT_READY", (await source.CaptureStorageIdentityAsync()).ErrorCode);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => source.CaptureStorageIdentityAsync(cancelled.Token));
    }

    private static async Task RunOwned(Func<MongoClient, Provider, Provider, Task> verify)
    {
        Assert.Equal(Uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
        Assert.NotNull(root);
        var state = Path.Combine(root.FullName, "LocalState/COSMIC/OwnedRoutingCurrent");
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(state, "mongo-storage-identity.pending.json");
        Assert.False(File.Exists(journal), "Preserve original pending outcome.");
        var client = new MongoClient(Uri);
        using (var names = await client.ListDatabaseNamesAsync())
            Assert.Empty((await names.ToListAsync()).Intersect(new[] { First, Second, Ghost }));
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file, new { ownedDatabases = new[] { First, Second }, ghost = Ghost });
            file.Flush(true);
        }
        var source = new Provider(Uri, First);
        var other = new Provider(Uri, First);
        var complete = false;
        try
        {
            Assert.False((await source.ActivateProviderAsync()).IsError);
            Assert.False((await other.ActivateProviderAsync()).IsError);
            await verify(client, source, other);
            using (var names = await client.ListDatabaseNamesAsync()) Assert.DoesNotContain(Ghost, await names.ToListAsync());
            complete = true;
        }
        finally
        {
            Assert.False((await other.DeActivateProviderAsync()).IsError);
            Assert.False((await source.DeActivateProviderAsync()).IsError);
            if (complete)
            {
                await client.DropDatabaseAsync(First);
                await client.DropDatabaseAsync(Second);
                using var names = await client.ListDatabaseNamesAsync();
                Assert.Empty((await names.ToListAsync()).Intersect(new[] { First, Second, Ghost }));
                File.Delete(journal);
            }
        }
    }
}
