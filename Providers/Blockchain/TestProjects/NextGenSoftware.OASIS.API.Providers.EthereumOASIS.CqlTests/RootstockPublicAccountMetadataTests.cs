using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Source = NextGenSoftware.OASIS.API.Providers.RootstockOASIS.RootstockPublicAccountMetadataSource;

namespace NextGenSoftware.OASIS.API.Providers.RootstockOASIS.CqlTests;

/// <summary>Rootstock nodes reject the finalized tag and EIP-1898 requireCanonical (-32603), so the
/// profile reads 100 confirmations below the head by hash and fences that hash after the read.</summary>
[TestClass]
public sealed class RootstockPublicAccountMetadataTests
{
    private const string Genesis = "0xf88529d4ab262c0f4d042e9d8d3f2472848eaafe1a9b7213f57617eb40a9f9e0";
    private const string Head = "0x900dcd";      // 9440717
    private const string Target = "0x900d69";    // Head - 100
    private static readonly string Block = "0x" + new string('a', 64);
    private static readonly string[] Keys = Enumerable.Range(1, 3).Select(value => "0x" + value.ToString("x40")).ToArray();

    private sealed class Rpc : HttpMessageHandler
    {
        public string Fault = "";
        public readonly List<string> Methods = new();
        private int targetReads;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var method = json.RootElement.GetProperty("method").GetString()!;
            var parameters = json.RootElement.GetProperty("params");
            Methods.Add(method);
            object result;
            switch (method)
            {
                case "eth_chainId": result = "0x1e"; break;
                case "eth_blockNumber": result = Fault == "young" ? "0x63" : Head; break;
                case "eth_getBlockByNumber":
                    var tag = parameters[0].GetString();
                    Assert.AreNotEqual("finalized", tag, "Rootstock has no finalized tag.");
                    Assert.AreNotEqual("latest", tag, "Confirmation depth never reads the moving head.");
                    if (tag == "0x0") { result = new { hash = Genesis, number = "0x0" }; break; }
                    Assert.AreEqual(Target, tag);
                    targetReads++;
                    result = new { hash = Fault == "reorg" && targetReads > 1 ? "0x" + new string('b', 64) : Block, number = Target };
                    break;
                case "eth_getBalance":
                    var selector = parameters[1];
                    Assert.AreEqual(Block, selector.GetProperty("blockHash").GetString());
                    Assert.IsFalse(selector.TryGetProperty("requireCanonical", out _), "Rootstock rejects requireCanonical.");
                    result = "0x2a";
                    break;
                default: throw new AssertFailedException("No signing, contract execution, scans or source writes allowed.");
            }
            return new(HttpStatusCode.OK) { Content = new StringContent(
                JsonSerializer.Serialize(new { jsonrpc = "2.0", id = 1, result }), Encoding.UTF8, "application/json") };
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("reorg")] [DataRow("young")]
    public async Task ReadsAreConfirmationDepthBoundAndHashFenced(string scenario)
    {
        var rpc = new Rpc(); using var client = new HttpClient(rpc);
        using var source = new Source(client, new Uri("https://public-node.rsk.co/"), "0x1e", Keys, Guid.NewGuid());
        Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        rpc.Methods.Clear(); rpc.Fault = scenario;
        var selected = Keys;
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.RootstockOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row, Source.AddressMetadata))).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.RootstockOASIS,
            new[] { new CosmicQueryTarget(ProviderType.RootstockOASIS, source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var response = await source.ExecuteCosmicQueryAsync(plan.Result, 100, default);
        if (scenario != "normal")
        {
            Assert.IsTrue(response.IsError); Assert.IsNull(response.Result);
            Assert.AreEqual("CQL_QUERY_FAILED", response.ErrorCode);
            return;
        }
        Assert.IsFalse(response.IsError, response.Exception?.ToString());
        Assert.AreEqual(3, response.Result.Count);
        foreach (var row in response.Result)
        {
            Assert.IsTrue(row.IsPublic);
            Assert.AreEqual(Block, row.MetaData["RootstockBlockHash"]);
            Assert.AreEqual("9440617", row.MetaData["RootstockBlockNumber"]);
            Assert.AreEqual("confirmed-100", row.MetaData["RootstockCommitment"]);
            Assert.AreEqual("42", row.MetaData["RootstockBalanceWei"]);
        }
        CollectionAssert.AreEqual(new[] { "eth_chainId", "eth_getBlockByNumber", "eth_blockNumber", "eth_getBlockByNumber",
            "eth_getBalance", "eth_getBalance", "eth_getBalance", "eth_getBlockByNumber" }, rpc.Methods);
    }

    [TestMethod]
    public void ConfigurationIsPinnedToRootstockMainnetWithoutCredentials()
    {
        using var client = new HttpClient(new Rpc());
        Assert.ThrowsException<ArgumentException>(() => new Source(client, new Uri("https://public-node.rsk.co/"), "0x1f", Keys, Guid.NewGuid()));
        Assert.ThrowsException<ArgumentException>(() => Source.FromJson(JsonSerializer.Serialize(new {
            schema = 1, rpcUrl = "https://public-node.rsk.co/", chainId = "0x1e", addresses = Keys,
            sourceOwner = Guid.NewGuid(), privateKey = "forbidden-test" })));
    }
}
