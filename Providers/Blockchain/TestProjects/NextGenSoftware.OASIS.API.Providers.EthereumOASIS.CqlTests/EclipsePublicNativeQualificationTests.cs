using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.EclipseOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class EclipsePublicNativeQualificationTests
{
    [TestMethod, TestCategory("EclipsePublicNativeQualification")]
    public async Task QualifyOriginalMetadataOnlyAccountCatalogueOnce()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_ECLIPSE_NATIVE") != "original-account-qualification")
        {
            Assert.Inconclusive("Explicit native opt-in required.");
            return;
        }
        const string root = "C:/Source/OASIS-cosmic";
        const string endpoint = "https://mainnetbeta-rpc.eclipse.xyz/";
        var state = Path.Combine(root, "LocalState/COSMIC/EclipsePublicQualificationCurrent");
        var evidence = Path.Combine(root, "TestResults/EclipsePublicSourceCurrent");
        Directory.CreateDirectory(state);
        Directory.CreateDirectory(evidence);
        var keys = EclipsePublicAccountReaderTests.Keys;
        using (var intent = new FileStream(Path.Combine(state, "qualification.intent.json"), FileMode.CreateNew))
        {
            JsonSerializer.Serialize(intent, new { schema = 1, endpoint, addresses = keys,
                methods = new[] { "getGenesisHash", "getMultipleAccounts" }, maximumRequests = 2,
                commitment = "finalized", encoding = "base64", dataSlice = new { offset = 0, length = 0 },
                aggregateSeconds = 30, clientSeconds = 15, responseBytes = 262144, aggregateBytes = 524288,
                maximumProcessRequestsPerSecond = 1, writes = false, sourceOwnerAllocated = false,
                policy = "official-endpoint-reported-genesis-and-finalized-context", consensusVerified = false,
                readerSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(EclipsePublicAccountReader).Assembly.Location))).ToLowerInvariant(),
                startedUtc = DateTimeOffset.UtcNow });
            intent.Flush(true);
        }
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            var snapshot = await new EclipsePublicAccountReader(client, new Uri(endpoint)).ReadAsync(keys, null, default);
            Assert.AreEqual(3, snapshot.Accounts.Count);
            var proof = new { schema = 1, snapshot, currency = "ETH", decimals = 9,
                independentNetworkVerification = false, writes = false };
            File.WriteAllText(Path.Combine(evidence, "native-qualification.json"), JsonSerializer.Serialize(proof));
            using var ack = new FileStream(Path.Combine(state, "qualification.acknowledged.json"), FileMode.CreateNew);
            JsonSerializer.Serialize(ack, proof);
            ack.Flush(true);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(evidence, "native-failure.json"), JsonSerializer.Serialize(new {
                type = error.GetType().FullName, message = error.Message, writes = false, replayAllowed = false }));
            throw;
        }
    }
}
