using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.XRPLOASIS.UnitTests;

[TestClass]
public sealed class XrplPublicHeaderMetadataTests
{
    // Native public golden headers, qualified independently by the Python probe.
    private static readonly System.Text.Json.Nodes.JsonArray Headers = System.Text.Json.Nodes.JsonNode.Parse("""[{"account_hash":"C3B52124CDFCA43B88130B9D446E49F4D8AA618C7C885A60186C3B3DA2B85ED4","close_flags":0,"close_time":846839092,"close_time_human":"2026-Nov-01 09:04:52.000000000 UTC","close_time_iso":"2026-11-01T09:04:52Z","close_time_resolution":10,"closed":true,"ledger_hash":"3AA1A57F3E24012170CD2922194620B795D9CBA8698E79A4EEFA10A26A8F6592","ledger_index":"108042083","parent_close_time":846839091,"parent_hash":"EDDBA0BAB31F6EEC15E0C3913B2CB0207D11F7F8020DC77C76CFED364AF3520C","total_coins":"99985574693823492","transaction_hash":"D425FA2B1EA2FC170A4EF426F6A3F17F548D76AEB51D56C7F3D9D915D5FF73C2"},{"account_hash":"6542085C58AE158D597F67121E803F0ED74EEC3B3D17A374306BB0818E70ACC5","close_flags":0,"close_time":846839091,"close_time_human":"2026-Nov-01 09:04:51.000000000 UTC","close_time_iso":"2026-11-01T09:04:51Z","close_time_resolution":10,"closed":true,"ledger_hash":"EDDBA0BAB31F6EEC15E0C3913B2CB0207D11F7F8020DC77C76CFED364AF3520C","ledger_index":"108042082","parent_close_time":846839090,"parent_hash":"9F7C6331BE65A669963F42357757C6E0C47B7B2A52E67793E86AA87A877BC87A","total_coins":"99985574693829711","transaction_hash":"F461969EF7B412835AE7951528052CAD3802DC017580D8928320D1D0AEF14473"},{"account_hash":"43BF031D44CB5EB3DC211DDBF8C9C230922A484CEE018A0A5B075235FAD48F65","close_flags":0,"close_time":846839090,"close_time_human":"2026-Nov-01 09:04:50.000000000 UTC","close_time_iso":"2026-11-01T09:04:50Z","close_time_resolution":10,"closed":true,"ledger_hash":"9F7C6331BE65A669963F42357757C6E0C47B7B2A52E67793E86AA87A877BC87A","ledger_index":"108042081","parent_close_time":846839081,"parent_hash":"4AFCC3D62B578BE690BF06212780691DA970986EA103367A6AE49709AD21AA6D","total_coins":"99985574693830235","transaction_hash":"A835993CBA0108250FB08BA9C6674B02EF9F05B72BCECDF44FEBAC1333024C6B"}]""")!.AsArray();
    private static readonly string[] Hashes = Headers.Select(row => (string)row!["ledger_hash"]!).ToArray();

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public int Requests;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Requests++;
            Assert.AreEqual(HttpMethod.Post,request.Method);
            using var input=System.Text.Json.JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var root=input.RootElement;
            string method=root.GetProperty("method").GetString()!;
            string text;
            if(method=="server_info") text="{\"result\":{\"status\":\"success\",\"info\":{\"network_id\":"+(Fault=="network" ? "1" : "0")+"}}}";
            else
            {
                Assert.AreEqual("ledger",method,"No signing, submission or source scans.");
                string hash=root.GetProperty("params")[0].GetProperty("ledger_hash").GetString()!;
                int index=Array.IndexOf(Hashes,hash);
                Assert.IsTrue(index>=0,"Only explicit configured hashes may be requested.");
                var header=Headers[index]!.DeepClone();
                if(Fault=="header" || (Fault=="last" && index==2)) header["close_time"]=(uint)header["close_time"]!+1;
                if(Fault=="closed") header["closed"]=false;
                if(Fault=="width") header["close_flags"]=-1;
                var result=new System.Text.Json.Nodes.JsonObject {
                    ["status"]="success", ["validated"]=Fault!="unvalidated", ["ledger_hash"]=hash,
                    ["ledger_index"]=Fault=="sequence" ? uint.Parse(header["ledger_index"]!.ToString(),System.Globalization.CultureInfo.InvariantCulture)+1 : uint.Parse(header["ledger_index"]!.ToString(),System.Globalization.CultureInfo.InvariantCulture),
                    ["ledger"]=header };
                if(Fault=="identity") result["ledger_hash"]=Hashes[(index+1)%3];
                text=new System.Text.Json.Nodes.JsonObject { ["result"]=result }.ToJsonString();
            }
            if(Fault=="oversize") text=new string(' ',262145);
            if(Fault=="duplicate") text="{\"result\":{},\"result\":{}}";
            if(Fault=="rpc") text="{\"result\":{\"status\":\"error\",\"error\":\"lgrNotFound\"}}";
            return new HttpResponseMessage(Fault=="http" ? HttpStatusCode.BadGateway : HttpStatusCode.OK)
                { Content=new StringContent(text,Encoding.UTF8,"application/json") };
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")]
    [DataRow("network")] [DataRow("header")] [DataRow("last")] [DataRow("unvalidated")] [DataRow("closed")] [DataRow("width")] [DataRow("identity")] [DataRow("sequence")] [DataRow("rpc")]
    [DataRow("duplicate")] [DataRow("oversize")] [DataRow("http")] [DataRow("cancel")]
    [DataRow("unknown")] [DataRow("inactive")] [DataRow("empty-window-error")]
    public async Task OnlyCompleteVerifiedSelectionsArePublished(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new XrplPublicHeaderMetadataSource(client,new Uri("https://example.invalid/"),
            0,Hashes,Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Requests=0;
        native.Fault=scenario == "empty-window-error" ? "last" : scenario;
        var selected = scenario == "unknown" ? new[] { new string('9',64) } : Hashes;
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.XRPLOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row,XrplPublicHeaderMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            var key=Hashes[0];
            query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.XRPLOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row,XrplPublicHeaderMetadataSource.AddressMetadata)==key).Result;
        }
        if (scenario == "window") query=query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query=query.Take(0).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.XRPLOASIS,
            new[] {new CosmicQueryTarget(ProviderType.XRPLOASIS,source.QueryCapabilities)});
        Assert.IsFalse(plan.IsError,plan.Message);
        using var cancel=new CancellationTokenSource();
        if(scenario=="cancel") cancel.Cancel();
        var result=await source.ExecuteCosmicQueryAsync(plan.Result,scenario=="budget" ? 2 : 100,cancel.Token);
        if(scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError,result.Exception?.ToString());
            Assert.AreEqual(scenario=="normal" ? 3 : 1,result.Result.Count);
            Assert.IsTrue(result.Result.All(row=>row.IsPublic));
            Assert.AreEqual(scenario=="window" ? Hashes[1] : Hashes[0],result.Result[0].MetaData[XrplPublicHeaderMetadataSource.AddressMetadata]);
        }
        else Assert.IsTrue(result.IsError,"No partial native result may escape.");
        if(scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0,native.Requests);
    }

    [TestMethod]
    public void RejectsSecretsAndNonCanonicalKeys()
    {
        Assert.ThrowsException<ArgumentException>(() => XrplPublicHeaderMetadataSource.FromJson("{\"privateKey\":\"forbidden\"}"));
        using var client=new HttpClient(new Native());
        Assert.ThrowsException<InvalidDataException>(()=>new XrplPublicHeaderMetadataSource(client,
            new Uri("https://example.invalid/"),0,new[]{"0xAB"},Guid.NewGuid()));
    }
    [TestMethod]
    [TestCategory("NativeXrplPublic")]
    public async Task NativeCatalogueRunsSharedManagerDemoThreeAndTwo()
    {
        var path=Environment.GetEnvironmentVariable("COSMIC_XRPL_PUBLIC_SOURCE");
        if(string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained native catalogue required.");
        using var output=new StringWriter();
        var result=await CosmicCqlDemo.XrplPublicWalkthrough.RunAsync(path!,output);
        Assert.AreEqual((3,2),result);
        StringAssert.Contains(output.ToString(),"Verified content hash:");
    }

}
