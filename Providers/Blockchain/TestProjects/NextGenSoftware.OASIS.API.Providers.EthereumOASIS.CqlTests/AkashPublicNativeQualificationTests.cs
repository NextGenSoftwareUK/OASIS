using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.AkashOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class AkashPublicNativeQualificationTests
{
    [TestMethod, TestCategory("AkashPublicNativeContinuation")]
    public async Task QualifyBoundedPrefixAfterOriginalByteBudgetFailureOnce()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_AKASH_NATIVE") != "bounded-prefix-continuation")
        {
            Assert.Inconclusive("Explicit evidence-bound continuation opt-in required.");
            return;
        }

        const string root = "C:/Source/OASIS-cosmic";
        const string endpoint = "https://console-api.akash.network/";
        var state = Path.Combine(root, "LocalState/COSMIC/AkashPublicQualificationCurrent");
        var evidence = Path.Combine(root, "TestResults/AkashPublicSourceCurrent");
        var originalIntent = File.ReadAllBytes(Path.Combine(state, "qualification.intent.json"));
        var originalFailure = File.ReadAllBytes(Path.Combine(evidence, "native-failure.json"));
        using var priorIntent = JsonDocument.Parse(originalIntent);
        using var priorFailure = JsonDocument.Parse(originalFailure);
        Assert.AreEqual(endpoint, priorIntent.RootElement.GetProperty("endpoint").GetString());
        Assert.AreEqual("Akash response byte budget exceeded.", priorFailure.RootElement.GetProperty("message").GetString());
        Assert.IsFalse(File.Exists(Path.Combine(state, "qualification.acknowledged.json")));

        // This is a new, explicitly bounded observation of a corrected algorithm,
        // linked to the immutable failure. The original probe is never rerun.
        var intentPath = Path.Combine(state, "bounded-prefix-continuation.intent.json");
        using (var intent = new FileStream(intentPath, FileMode.CreateNew))
        {
            JsonSerializer.Serialize(intent, new {
                schema = 1, endpoint,
                originalIntentSha256 = Convert.ToHexString(SHA256.HashData(originalIntent)).ToLowerInvariant(),
                originalFailureSha256 = Convert.ToHexString(SHA256.HashData(originalFailure)).ToLowerInvariant(),
                selection = "first-three-complete-unique-owner-records; sorted-within-sample; tail-not-certified",
                discoveryMaximumBytes = 2097152, discoveryMaximumBufferedRecordBytes = 262144,
                pointMaximumBytes = 262144, maximumRequests = 4,
                maximumProcessRequestsPerSecond = 1, aggregateSeconds = 45, clientSeconds = 15,
                writes = false, sourceOwnerAllocated = false, followProviderHostUri = false,
                readerSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(AkashPublicProviderReader).Assembly.Location))).ToLowerInvariant(),
                startedUtc = DateTimeOffset.UtcNow });
            intent.Flush(true);
        }
        using (var preserved = new FileStream(Path.Combine(state, "original-discovery-failure.json"), FileMode.CreateNew))
        {
            preserved.Write(originalFailure);
            preserved.Flush(true);
        }

        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            var reader = new AkashPublicProviderReader(client, new Uri(endpoint));
            var owners = await reader.DiscoverThreeAsync(deadline.Token);
            var rows = await reader.ReadAsync(owners, deadline.Token);
            Assert.AreEqual(3, rows.Count);
            CollectionAssert.AreEqual(owners.ToArray(), rows.Select(row => row.Owner).ToArray());
            Assert.IsTrue(originalIntent.AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(state, "qualification.intent.json"))));
            Assert.IsTrue(originalFailure.AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(evidence, "native-failure.json"))));
            var proof = new {
                schema = 1, endpoint, owners, records = rows,
                continuationIntentSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(intentPath))).ToLowerInvariant(),
                policy = "indexer-reported-provider-metadata; configured-sample-not-network-catalogue",
                independentConsensusVerification = false,
                writes = false, sourceOwnerAllocated = false, followProviderHostUri = false,
                observedUtc = DateTimeOffset.UtcNow };
            using var ack = new FileStream(Path.Combine(state, "bounded-prefix-continuation.acknowledged.json"), FileMode.CreateNew);
            JsonSerializer.Serialize(ack, proof);
            ack.Flush(true);
        }
        catch (Exception error)
        {
            using var failure = new FileStream(Path.Combine(state, "bounded-prefix-continuation.failure.json"), FileMode.CreateNew);
            JsonSerializer.Serialize(failure, new {
                type = error.GetType().FullName, message = error.Message,
                writes = false, replayAllowed = false, observedUtc = DateTimeOffset.UtcNow });
            failure.Flush(true);
            throw;
        }
    }

    [TestMethod, TestCategory("AkashPublicNativeQualification")]
    public async Task QualifyOriginalIndexedProviderCatalogueOnce()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_AKASH_NATIVE") != "original-provider-qualification")
        {
            Assert.Inconclusive("Explicit native opt-in required.");
            return;
        }
        const string root = "C:/Source/OASIS-cosmic";
        const string endpoint = "https://console-api.akash.network/";
        var state = Path.Combine(root, "LocalState/COSMIC/AkashPublicQualificationCurrent");
        var evidence = Path.Combine(root, "TestResults/AkashPublicSourceCurrent");
        Directory.CreateDirectory(state); Directory.CreateDirectory(evidence);
        using (var intent = new FileStream(Path.Combine(state, "qualification.intent.json"), FileMode.CreateNew))
        {
            JsonSerializer.Serialize(intent, new { schema = 1, endpoint, method = "GET",
                discoveryPath = "v1/providers", pointPath = "v1/providers/{owner}",
                discoveryMaximumBytes = 2097152, discoveryMaximumOwners = 1024, pointMaximumBytes = 262144,
                maximumRequests = 4, maximumProcessRequestsPerSecond = 1, aggregateSeconds = 45, clientSeconds = 15,
                writes = false, sourceOwnerAllocated = false, followProviderHostUri = false,
                policy = "indexer-reported-provider-metadata", independentConsensusVerification = false,
                readerSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(AkashPublicProviderReader).Assembly.Location))).ToLowerInvariant(),
                startedUtc = DateTimeOffset.UtcNow });
            intent.Flush(true);
        }
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            var reader = new AkashPublicProviderReader(client, new Uri(endpoint));
            var owners = await reader.DiscoverThreeAsync(deadline.Token);
            var rows = await reader.ReadAsync(owners, deadline.Token);
            Assert.AreEqual(3, rows.Count);
            CollectionAssert.AreEqual(owners.ToArray(), rows.Select(row => row.Owner).ToArray());
            var proof = new { schema = 1, endpoint, owners, records = rows, policy = "indexer-reported-provider-metadata",
                writes = false, sourceOwnerAllocated = false, followProviderHostUri = false,
                observedUtc = DateTimeOffset.UtcNow };
            File.WriteAllText(Path.Combine(evidence, "native-qualification.json"), JsonSerializer.Serialize(proof));
            using var ack = new FileStream(Path.Combine(state, "qualification.acknowledged.json"), FileMode.CreateNew);
            JsonSerializer.Serialize(ack, proof); ack.Flush(true);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(evidence, "native-failure.json"), JsonSerializer.Serialize(new {
                type = error.GetType().FullName, message = error.Message, writes = false, replayAllowed = false }));
            throw;
        }
    }
}
