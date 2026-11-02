using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.AkashOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class AkashPublicNativeQualificationTests
{
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
