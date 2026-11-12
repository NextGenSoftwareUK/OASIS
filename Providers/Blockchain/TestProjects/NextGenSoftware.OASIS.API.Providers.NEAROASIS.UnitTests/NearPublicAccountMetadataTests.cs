using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public sealed class NearPublicAccountMetadataTests
{
    private static readonly string[] Keys = { "near", "wrap.near", "aurora" };
    private static readonly string Block = new('1',32);
    private const string MaximumAmount = "340282366920938463463374607431768211455";

    private sealed class Rpc : HttpMessageHandler
    {
        public string Fault = "";
        public int Requests;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Requests++;
            Assert.AreEqual(HttpMethod.Post,request.Method);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var root = body.RootElement; var parameters = root.GetProperty("params");
            object result;
            if (root.GetProperty("method").GetString() == "status")
                result = new { chain_id = Fault == "chain" ? "testnet" : "mainnet", sync_info = new { syncing = Fault == "syncing" } };
            else
            {
                Assert.AreEqual("query",root.GetProperty("method").GetString());
                Assert.AreEqual("view_account",parameters.GetProperty("request_type").GetString());
                var account = parameters.GetProperty("account_id").GetString()!;
                Assert.IsTrue(Keys.Contains(account),"No account scans or contract calls.");
                if (parameters.TryGetProperty("finality",out var finality))
                {
                    Assert.AreEqual("final",finality.GetString());
                    Assert.IsFalse(parameters.TryGetProperty("block_id",out _));
                }
                else Assert.AreEqual(Block,parameters.GetProperty("block_id").GetString());
                result = new Dictionary<string,object> {
                    ["amount"] = Fault == "numeric-amount" ? 1 : Fault == "last" && account == Keys[2] ? "01" : MaximumAmount,
                    ["locked"] = "0", ["code_hash"] = Fault == "hash" ? new string('1',33) : Block,
                    ["storage_usage"] = ulong.MaxValue,
                    ["block_hash"] = Fault == "block" && account == Keys[1] ? new string('1',31)+"2" : Block,
                    ["block_height"] = Fault == "height" && account == Keys[1] ? 9007199254740994UL : 9007199254740993UL
                };
            }
            string json = JsonSerializer.Serialize(new { jsonrpc = "2.0", id = Fault == "id" ? 2 : 1, result });
            if (Fault == "duplicate") json = "{\"id\":1,\"id\":1}";
            if (Fault == "oversize") json = new string(' ',262145);
            return new HttpResponseMessage(Fault == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK) {
                Content = new StringContent(json,Encoding.UTF8,"application/json") };
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")]
    [DataRow("chain")] [DataRow("syncing")] [DataRow("block")] [DataRow("height")]
    [DataRow("numeric-amount")] [DataRow("last")] [DataRow("hash")] [DataRow("id")]
    [DataRow("duplicate")] [DataRow("oversize")] [DataRow("http")]
    [DataRow("empty-window-error")] [DataRow("inactive")] [DataRow("cancel")] [DataRow("unknown")]
    public async Task SelectionIsFinalHashPinnedPreciseAndNeverPartial(string scenario)
    {
        var rpc = new Rpc(); using var client = new HttpClient(rpc);
        using var source = new NearPublicAccountMetadataSource(client,new Uri("https://example.invalid/"),"mainnet",Keys,Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        rpc.Requests = 0; rpc.Fault = scenario == "empty-window-error" ? "last" : scenario;
        var selected = scenario == "unknown" ? new[] { "unknown.near" } : Keys;
        var point = Keys[0];
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.NEAROASIS).Result)
            .Where(row=>selected.Contains(CosmicQueryMetadata.Value<string>(row,NearPublicAccountMetadataSource.AddressMetadata))).Result;
        if (scenario == "point") query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.NEAROASIS).Result)
            .Where(row=>CosmicQueryMetadata.Value<string>(row,NearPublicAccountMetadataSource.AddressMetadata)==point).Result;
        if (scenario == "window") query = query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query = query.Take(0).Result;
        var plan = CosmicQueryPlanner.Plan(query,ProviderType.NEAROASIS,
            new[] { new CosmicQueryTarget(ProviderType.NEAROASIS,source.QueryCapabilities) });
        Assert.IsFalse(plan.IsError,plan.Message);
        using var cancel = new CancellationTokenSource(); if (scenario == "cancel") cancel.Cancel();
        var result = await source.ExecuteCosmicQueryAsync(plan.Result,scenario == "budget" ? 2 : 100,cancel.Token);
        if (scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError,result.Exception?.ToString());
            Assert.AreEqual(scenario == "normal" ? 3 : 1,result.Result.Count);
            foreach (var row in result.Result)
            {
                Assert.IsTrue(row.IsPublic);
                Assert.AreEqual(MaximumAmount,row.MetaData["NearAmountYocto"]);
                Assert.AreEqual("9007199254740993",row.MetaData["NearBlockHeight"]);
                Assert.AreEqual(ulong.MaxValue.ToString(),row.MetaData["NearStorageUsageBytes"]);
            }
            if (scenario == "window") Assert.AreEqual(Keys[1],result.Result[0].MetaData[NearPublicAccountMetadataSource.AddressMetadata]);
        }
        else Assert.IsTrue(result.IsError,"No partial/malformed account output may escape.");
        if (scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0,rpc.Requests);
    }

    [TestMethod]
    public void InvalidAccountOrSecretConfigurationFailsBeforeNetwork()
    {
        Assert.ThrowsException<ArgumentException>(()=>NearPublicAccountMetadataSource.FromJson("{\"privateKey\":\"forbidden\"}"));
        using var client = new HttpClient(new Rpc());
        Assert.ThrowsException<ArgumentException>(()=>new NearPublicAccountMetadataSource(client,
            new Uri("https://example.invalid/"),"mainnet",new[] { "Upper.Near" },Guid.NewGuid()));
    }

    [TestMethod]
    [TestCategory("NativeNearPublic")]
    public async Task ExistingConfiguredNativeSourceReturnsThreeAndFilteredTwo()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_NEAR_PUBLIC_SOURCE");
        if (string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained native catalogue required.");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        using var output = new StringWriter();
        var result = await CosmicCqlDemo.NearPublicWalkthrough.RunAsync(path!,output,deadline.Token);
        Assert.AreEqual((3,2),result);
        StringAssert.Contains(output.ToString(),"Amount (yoctoNEAR):");
        StringAssert.Contains(output.ToString(),"Pinned block height:");
    }
}
