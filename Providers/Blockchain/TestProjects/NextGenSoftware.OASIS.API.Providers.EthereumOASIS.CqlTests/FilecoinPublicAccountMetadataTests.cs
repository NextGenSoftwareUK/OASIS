using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Source = NextGenSoftware.OASIS.API.Providers.FilecoinOASIS.FilecoinPublicAccountMetadataSource;

namespace NextGenSoftware.OASIS.API.Providers.FilecoinOASIS.CqlTests;

[TestClass]
public sealed class FilecoinPublicAccountMetadataTests
{
    private const string Genesis = "bafy2bzacecnamqgqmifpluoeldx7zzglxcljo6oja4vrmtj7432rphldpdmm2";
    private static readonly string Block = "0x" + new string('a', 64);
    private static readonly string[] Keys = Enumerable.Range(1, 3).Select(value => "0x" + value.ToString("x40")).ToArray();

    private sealed class Rpc : HttpMessageHandler
    {
        public string Fault = "";
        public readonly List<string> Methods = new();
        public Action? Cancel;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var root = json.RootElement;
            var method = root.GetProperty("method").GetString()!;
            Methods.Add(method);
            var parameters = root.GetProperty("params");
            object result;
            switch (method)
            {
                case "eth_chainId": result = Fault == "chain" ? "0x2" : "0x13a"; break;
                case "Filecoin.ChainGetGenesis":
                    result = Fault switch {
                        "height" => new { Height = 1, Cids = new[] { new Dictionary<string,string>{{"/",Genesis}} } },
                        "empty-cids" => new { Height = 0, Cids = Array.Empty<Dictionary<string,string>>() },
                        "extra-cids" => new { Height = 0, Cids = new[] { new Dictionary<string,string>{{"/",Genesis}}, new Dictionary<string,string>{{"/",Genesis}} } },
                        "cid-field" => new { Height = 0, Cids = new[] { new Dictionary<string,string>{{"/",Genesis},{"other","forbidden"}} } },
                        _ => new { Height = 0, Cids = new[] { new Dictionary<string,string>{{"/",Fault == "genesis" ? "wrong" : Genesis}} } }
                    };
                    break;
                case "eth_getBlockByNumber":
                    Assert.IsFalse(parameters[1].GetBoolean());
                    Assert.AreEqual("finalized", parameters[0].GetString());
                    result = new { hash = Block, number = Fault == "block" ? "0x00" : "0x20000000000001" };
                    break;
                case "eth_getBalance":
                    Assert.AreEqual(Block, parameters[1].GetProperty("blockHash").GetString());
                    Assert.IsTrue(parameters[1].GetProperty("requireCanonical").GetBoolean());
                    var address = parameters[0].GetString()!;
                    Assert.IsTrue(Keys.Contains(address));
                    if (Fault == "cancel") { Cancel!(); token.ThrowIfCancellationRequested(); }
                    if (Fault == "rpc") return Json(new { jsonrpc="2.0", id=1, error=new { code=-32000, message="not canonical" } });
                    if (Fault == "oversize") return new(HttpStatusCode.OK) { Content=new StringContent(new string(' ',262145)) };
                    if (Fault == "duplicate") return new(HttpStatusCode.OK) { Content=new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"id\":1,\"result\":\"0x0\"}") };
                    result = Fault == "last" && address == Keys[2] ? "0x" + new string('f',65) : "0x" + new string('f',64);
                    break;
                default: throw new AssertFailedException("No signing, contract execution, scans or source writes allowed.");
            }
            return Json(new { jsonrpc="2.0", id=Fault=="id"?2:1, result });
        }
        private static HttpResponseMessage Json(object value) => new(HttpStatusCode.OK) {
            Content=new StringContent(JsonSerializer.Serialize(value),Encoding.UTF8,"application/json") };
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")]
    [DataRow("height")] [DataRow("empty-cids")] [DataRow("extra-cids")] [DataRow("cid-field")]
    [DataRow("chain")] [DataRow("genesis")] [DataRow("block")] [DataRow("last")]
    [DataRow("empty-window-error")] [DataRow("rpc")] [DataRow("oversize")] [DataRow("duplicate")]
    [DataRow("id")] [DataRow("inactive")] [DataRow("unknown")] [DataRow("cancel")] [DataRow("pre-cancel")]
    public async Task SelectionIsNativeHashBoundedPreciseAndNeverPartial(string scenario)
    {
        var rpc=new Rpc(); using var client=new HttpClient(rpc);
        using var source=new Source(client,new Uri("https://rpc.example.invalid/rpc/v1"),"0x13a",Keys,Guid.NewGuid());
        if(scenario!="inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        rpc.Methods.Clear(); rpc.Fault=scenario=="empty-window-error"?"last":scenario;
        var selected=scenario=="unknown"?new[]{"0x"+new string('9',40)}:Keys;
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.FilecoinOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result)
            .Where(row=>selected.Contains(CosmicQueryMetadata.Value<string>(row,Source.AddressMetadata))).Result;
        var point=Keys[0];
        if(scenario=="point") query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.FilecoinOASIS).Result)
            .Where(row=>CosmicQueryMetadata.Value<string>(row,Source.AddressMetadata)==point).Result;
        if(scenario=="window") query=query.Skip(1).Result.Take(1).Result;
        if(scenario=="empty-window-error") query=query.Take(0).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.FilecoinOASIS,
            new[]{new CosmicQueryTarget(ProviderType.FilecoinOASIS,source.QueryCapabilities)});
        Assert.IsFalse(plan.IsError,plan.Message);
        using var cancellation=new CancellationTokenSource(); rpc.Cancel=cancellation.Cancel;
        if(scenario=="pre-cancel") cancellation.Cancel();
        var response=await source.ExecuteCosmicQueryAsync(plan.Result,scenario=="budget"?2:100,cancellation.Token);
        if(scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(response.IsError,response.Exception?.ToString());
            Assert.AreEqual(scenario=="normal"?3:1,response.Result.Count);
            foreach(var row in response.Result)
            {
                Assert.IsTrue(row.IsPublic);
                Assert.AreEqual(Block,row.MetaData["FilecoinBlockHash"]);
                Assert.AreEqual("9007199254740993",row.MetaData["FilecoinBlockNumber"]);
                Assert.AreEqual("115792089237316195423570985008687907853269984665640564039457584007913129639935",row.MetaData["FilecoinBalanceWei"]);
            }
            if(scenario=="window") Assert.AreEqual(Keys[1],response.Result[0].MetaData[Source.AddressMetadata]);
        }
        else
        {
            Assert.IsTrue(response.IsError); Assert.IsNull(response.Result); Assert.IsFalse(response.IsLoaded);
            Assert.AreEqual(scenario switch {"inactive"=>"CQL_PROVIDER_INACTIVE","unknown"=>"CQL_INVALID_SOURCE_KEY",
                "budget"=>"CQL_QUERY_LIMIT","cancel" or "pre-cancel"=>"CQL_QUERY_CANCELLED",_=>"CQL_QUERY_FAILED"},response.ErrorCode);
        }
        if(scenario is "inactive" or "unknown" or "pre-cancel") Assert.AreEqual(0,rpc.Methods.Count);
        if(scenario=="chain") Assert.AreEqual(1,rpc.Methods.Count);
        if(scenario=="normal") Assert.AreEqual(6,rpc.Methods.Count);
    }

    [TestMethod]
    public async Task ContentScansAndWritesAreNotAdmittedAndConfigAcceptsNoCredentials()
    {
        var rpc=new Rpc(); using var client=new HttpClient(rpc);
        using var source=new Source(client,new Uri("https://rpc.example.invalid/rpc/v1"),"0x13a",Keys,Guid.NewGuid());
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.FilecoinOASIS).Result).Where(row=>row.Name.Contains("coffee")).Result;
        Assert.IsTrue(CosmicQueryPlanner.Plan(query,ProviderType.FilecoinOASIS,
            new[]{new CosmicQueryTarget(ProviderType.FilecoinOASIS,source.QueryCapabilities)}).IsError);
        Assert.IsTrue((await source.SaveHolonAsync(new Holon{Id=Guid.NewGuid()},saveChildren:false)).IsError);
        Assert.AreEqual(0,rpc.Methods.Count);
        foreach(var endpoint in new[]{"https://user:secret@rpc.example.invalid/","https://rpc.example.invalid/rpc/v1?apiKey=bad","https://rpc.example.invalid/rpc/v1key-path","http://rpc.example.invalid/"})
            Assert.ThrowsException<ArgumentException>(()=>new Source(client,new Uri(endpoint),"0x13a",Keys,Guid.NewGuid()));
        Assert.ThrowsException<ArgumentException>(()=>Source.FromJson(JsonSerializer.Serialize(new {schema=1,rpcUrl="https://rpc.example.invalid/rpc/v1",chainId="0x13a",addresses=Keys,sourceOwner=Guid.NewGuid(),privateKey="forbidden-test"})));
        Assert.ThrowsException<ArgumentException>(()=>new Source(client,new Uri("https://rpc.example.invalid/rpc/v1"),"0x2",Keys,Guid.NewGuid()));
    }

    [TestMethod, TestCategory("FilecoinPublicAccountsNative")]
    public async Task ActualNativeGenesisBoundBalancesReturnThreeAndTwoViaPublishedCql()
    {
        var path = Environment.GetEnvironmentVariable("OASIS_FILECOIN_PUBLIC_SOURCE")
            ?? throw new InvalidOperationException("Retained explicit source required.");
        using var output = new StringWriter();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        var result = await CosmicCqlDemo.FilecoinPublicWalkthrough.RunAsync(path, output, deadline.Token);
        Assert.AreEqual((3, 2), result);
        StringAssert.Contains(output.ToString(), "Native balance (attoFIL):");
        StringAssert.Contains(output.ToString(), "RPC finalized block (EC policy):");
        StringAssert.Contains(output.ToString(), "Read-only Filecoin demo complete:");
    }
}
