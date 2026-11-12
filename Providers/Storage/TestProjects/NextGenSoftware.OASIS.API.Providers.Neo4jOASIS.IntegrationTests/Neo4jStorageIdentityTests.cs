using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public partial class Neo4jStorageIdentityTests
{
    private const string Uri = "bolt://127.0.0.1:17687";

    [TestMethod]
    public async Task NeoStorageIdentityUsesNativeOpaqueIdAndCapturedDriverWithoutWrites()
    {
        Assert.AreEqual(Uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var first = new Provider(Uri, "neo4j", "oasis-local-test-only");
        var second = new Provider(Uri, "neo4j", "oasis-local-test-only");
        using var driver = GraphDatabase.Driver(Uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspect = driver.AsyncSession();
        async Task<long[]> Counts() => new[]
        {
            (await (await inspect.RunAsync("MATCH (n) RETURN count(n) AS count")).SingleAsync())["count"].As<long>(),
            (await (await inspect.RunAsync("MATCH ()-[r]->() RETURN count(r) AS count")).SingleAsync())["count"].As<long>(),
            (long)(await (await inspect.RunAsync("SHOW INDEXES YIELD name RETURN name")).ToListAsync()).Count,
            (long)(await (await inspect.RunAsync("SHOW CONSTRAINTS YIELD name RETURN name")).ToListAsync()).Count
        };
        try
        {
            Assert.IsFalse((await first.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await second.ActivateProviderAsync()).IsError);
            var before = await Counts();
            var native = await (await inspect.RunAsync("CALL db.info() YIELD id,name RETURN id,name")).SingleAsync();
            var captures = await Task.WhenAll(first.CaptureStorageIdentityAsync(), second.CaptureStorageIdentityAsync());
            foreach (var capture in captures) Assert.IsFalse(capture.IsError, capture.Message + capture.Exception);
            Assert.AreEqual(native["id"].As<string>(), captures[0].Result.StoreId);
            Assert.AreEqual(captures[0].Result.BindingFingerprint, captures[1].Result.BindingFingerprint);
            CollectionAssert.AreEqual(before, await Counts());

            // Mutable descriptors are not the address/authentication of the
            // driver already serving storage operations.
            first.Host = "bolt://127.0.0.1:1";
            first.Username = "unused-after-driver-capture";
            first.Password = "unused-test-value";
            var stillPinned = await first.CaptureStorageIdentityAsync();
            Assert.IsFalse(stillPinned.IsError, stillPinned.Message + stillPinned.Exception);
            Assert.AreEqual(captures[0].Result.BindingFingerprint, stillPinned.Result.BindingFingerprint);
            Assert.IsFalse((await second.DeActivateProviderAsync()).IsError);
            second = new Provider(Uri, "neo4j", "oasis-local-test-only");
            Assert.IsFalse((await second.ActivateProviderAsync()).IsError);
            var reopened = await second.CaptureStorageIdentityAsync();
            Assert.IsFalse(reopened.IsError, reopened.Message);
            Assert.AreEqual(captures[0].Result.BindingFingerprint, reopened.Result.BindingFingerprint);
            var alias = new Provider("bolt://localhost:17687", "neo4j", "oasis-local-test-only");
            try
            {
                Assert.IsFalse((await alias.ActivateProviderAsync()).IsError);
                var otherEndpoint = await alias.CaptureStorageIdentityAsync();
                Assert.IsFalse(otherEndpoint.IsError, otherEndpoint.Message + otherEndpoint.Exception);
                Assert.AreEqual(captures[0].Result.StoreId, otherEndpoint.Result.StoreId);
                Assert.AreNotEqual(captures[0].Result.EndpointFingerprint, otherEndpoint.Result.EndpointFingerprint);
                Assert.AreNotEqual(captures[0].Result.BindingFingerprint, otherEndpoint.Result.BindingFingerprint);
            }
            finally { Assert.IsFalse((await alias.DeActivateProviderAsync()).IsError); }

        }
        finally
        {
            Assert.IsFalse((await second.DeActivateProviderAsync()).IsError);
            Assert.IsFalse((await first.DeActivateProviderAsync()).IsError);
        }
    }

    [TestMethod]
    public async Task NeoStorageIdentityDoesNotGuessTheEndpointOfAnInjectedDriver()
    {
        Assert.AreEqual(Uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var native = GraphDatabase.Driver(Uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        var provider = new Provider(Uri, "neo4j", "oasis-local-test-only", () => native);
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var refused = await provider.CaptureStorageIdentityAsync();
            Assert.AreEqual("COSMIC_STORAGE_IDENTITY_UNSUPPORTED", refused.ErrorCode);
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
    }

    [TestMethod]
    public async Task NeoStorageIdentityInactiveAndPreCancelledCaptureDoesNotConnect()
    {
        var provider = new Provider("bolt://127.0.0.1:1", "unused", "unused-test-value");
        try
        {
            Assert.AreEqual("COSMIC_STORAGE_IDENTITY_NOT_READY", (await provider.CaptureStorageIdentityAsync()).ErrorCode);
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            try
            {
                await provider.CaptureStorageIdentityAsync(cancelled.Token);
                Assert.Fail("Pre-cancelled capture must observe caller cancellation.");
            }
            catch (OperationCanceledException exception) { Assert.AreEqual(cancelled.Token, exception.CancellationToken); }
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
    }
}
