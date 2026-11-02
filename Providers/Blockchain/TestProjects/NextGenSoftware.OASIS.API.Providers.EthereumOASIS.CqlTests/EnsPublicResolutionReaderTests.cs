using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.ENSOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.CqlTests;

[TestClass]
public class EnsPublicResolutionReaderTests
{
    private static readonly string[] Names = { "nick.eth", "vitalik.eth", "ur.integration-tests.eth" };
    private const string RpcUrl = "https://ethereum-rpc.publicnode.com/";

    [TestMethod]
    public async Task PinsEveryContractReadToSameCanonicalFinalizedBlock()
    {
        using var handler = new RpcHandler();
        using var client = new HttpClient(handler);
        var result = await new EnsPublicResolutionReader(client, new Uri(RpcUrl)).ReadAsync(Names, default);
        Assert.AreEqual(3, result.Records.Count);
        Assert.AreEqual(6, handler.Calls);
        Assert.AreEqual("0x" + new string('a', 64), result.BlockHash);
    }

    [DataTestMethod]
    [DataRow("chain")]
    [DataRow("block")]
    [DataRow("code")]
    [DataRow("rpc-error")]
    [DataRow("sentinel")]
    [DataRow("id")]
    [DataRow("duplicate")]
    [DataRow("oversize")]
    public async Task ContractErrorsFailWithoutFallbackOrRetry(string fault)
    {
        using var handler = new RpcHandler { Fault = fault };
        using var client = new HttpClient(handler);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() =>
            new EnsPublicResolutionReader(client, new Uri(RpcUrl)).ReadAsync(Names, default));
        Assert.IsTrue(handler.Calls <= 6);
    }

    [TestMethod]
    public async Task InvalidCatalogueIsRejectedBeforeAnyRequest()
    {
        using var handler = new RpcHandler();
        using var client = new HttpClient(handler);
        var reader = new EnsPublicResolutionReader(client, new Uri(RpcUrl));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => reader.ReadAsync(new[] { "Nick.eth" }, default));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => reader.ReadAsync(new[] { "nick.eth", "nick.eth" }, default));
        Assert.AreEqual(0, handler.Calls);
    }

    [TestMethod, TestCategory("EnsPublicNativeQualification")]
    public async Task QualifyOriginalThreeNamesOnce()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_ENS_NATIVE") != "original-three-name-qualification")
        {
            Assert.Inconclusive("Explicit native qualification opt-in required.");
            return;
        }
        var workspace = Environment.GetEnvironmentVariable("COSMIC_ENS_WORKSPACE")
            ?? throw new InvalidOperationException("Explicit worktree required.");
        Assert.AreEqual(Path.GetFullPath("C:/Source/OASIS-cosmic"), Path.GetFullPath(workspace));
        var state = Path.Combine(workspace, "LocalState/COSMIC/EnsPublicQualificationCurrent");
        var evidence = Path.Combine(workspace, "TestResults/EnsPublicSourceCurrent");
        Directory.CreateDirectory(state);
        Directory.CreateDirectory(evidence);
        // Exclusive durable intent precedes every native request. Existing intent
        // refuses replay; failure cannot silently create a replacement cohort.
        using (var intent = new FileStream(Path.Combine(state, "qualification.intent.json"), FileMode.CreateNew))
        {
            JsonSerializer.Serialize(intent, new { schema = 1, rpcUrl = RpcUrl, chainId = "0x1",
                names = Names, resolver = EnsUniversalResolverCodec.MainnetProxy,
                maxRequests = 6, aggregateSeconds = 45, responseBytes = 262144, aggregateBytes = 1048576,
                blockPolicy = "finalized-EIP1898-requireCanonical", writes = false,
                startedUtc = DateTimeOffset.UtcNow });
            intent.Flush(true);
        }
        using var httpHandler = NativeConnectionGuard.CreateGuardedHttpHandler();
        if (httpHandler is SocketsHttpHandler sockets) sockets.UseCookies = false;
        using var client = new HttpClient(httpHandler) { Timeout = TimeSpan.FromSeconds(15) };
        try
        {
            var snapshot = await new EnsPublicResolutionReader(client, new Uri(RpcUrl)).ReadAsync(Names, default);
            File.WriteAllText(Path.Combine(evidence, "native-resolution.json"), JsonSerializer.Serialize(snapshot));
            using var ack = new FileStream(Path.Combine(state, "qualification.acknowledged.json"), FileMode.CreateNew);
            JsonSerializer.Serialize(ack, snapshot);
            ack.Flush(true);
            Assert.AreEqual(3, snapshot.Records.Count);
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(evidence, "native-failure.json"), JsonSerializer.Serialize(new {
                type = error.GetType().FullName, message = error.Message, writes = false }));
            throw;
        }
    }

    private sealed class RpcHandler : HttpMessageHandler
    {
        public string Fault = "";
        public int Calls;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var root = json.RootElement;
            var method = root.GetProperty("method").GetString();
            if (method is "eth_getCode" or "eth_call")
            {
                var selector = root.GetProperty("params")[1];
                Assert.AreEqual("0x" + new string('a', 64), selector.GetProperty("blockHash").GetString());
                Assert.IsTrue(selector.GetProperty("requireCanonical").GetBoolean());
            }
            object result = method switch {
                "eth_chainId" => Fault == "chain" ? "0x2" : "0x1",
                "eth_getBlockByNumber" => new { hash = Fault == "block" ? "0x123" : "0x" + new string('a', 64), number = "0x123" },
                "eth_getCode" => Fault == "code" ? "0x" : "0x6000",
                "eth_call" => "0x" + new string('0', 62) + "40" + new string('0', 24) + new string('3', 40) +
                    new string('0', 62) + "20" + new string('0', 24) + new string(Fault == "sentinel" ? '1' : '2', 40),
                _ => throw new AssertFailedException("Unexpected native method.")
            };
            var id = root.GetProperty("id").GetInt32();
            var body = Fault == "rpc-error" ? JsonSerializer.Serialize(new { jsonrpc = "2.0", id, error = new { code = 3, message = "OffchainLookup" } }) :
                JsonSerializer.Serialize(new { jsonrpc = "2.0", id = Fault == "id" ? id + 1 : id, result });
            if (Fault == "duplicate") body = body.Insert(1, "\"id\":1,");
            if (Fault == "oversize") body = new string(' ', 262145);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }
}
