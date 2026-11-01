using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.StellarOASIS.UnitTests;

[TestClass]
public sealed class StellarPublicHeaderMetadataTests
{
    private static readonly System.Text.Json.Nodes.JsonArray Headers = System.Text.Json.Nodes.JsonNode.Parse("""[{"hash":"c8deae88416e676c30358bef81cfbb7aa78546a7713d3c252653591e3c3340ce","prev_hash":"4cc75f196c333bcc325fc90d1a42ea39209513e7ad1696c1866573d90a74d768","sequence":65247993,"header_xdr":"AAAAHUzHXxlsMzvMMl/JDRpC6jkglRPnrRaWwYZlc9kKdNdoTjCNVOqHMrsnmJcg+6EgreyCuLzPXHltEH+IHs+MjOcAAAAAaucH9QAAAAAAAAABAAAAAF60jCXLtH/CB/4KI+zln4jBc1nQ2p1AsXCUSYH47yvBAAAAQPxzrzJSy0nzXXUEARyz7KU3gQ0FB4fXpFo1bqlKhl2aD4Ky2/j0ZQ+GYJFO+u5+70JMLLmNoE3cZFqJLASI4QrB4X9kxbbO4mML52LWT5bR/MJRhcrswSZJ39UEEgG+NnU7Qirq8CWbNLxKij5BKHr4mR9ZGlFjsuyiPtSqWwIYA+Oa+Q6iHrPseVthAABjr1Pzg0MAAAEWAAAAAG8bf/IAAABkAExLQAAAA+iBCC8MsOtZLjU5HN1q6iFT5QcllPG/wOgBXBU4AGA+Xtr7PVWPOF0ni85riuBo+cQjwG9WX4OxeFTqunFDjGefrB3T50jbEIwtgiwPv0zF/m7rZ8gSe+zSmUC5KzCTkfAy/CmIHD6wNBmWE0N/i2Yg76Dtg7RN1j5wnDbOBCfdhAAAAAA=","protocolVersion":29},{"hash":"4cc75f196c333bcc325fc90d1a42ea39209513e7ad1696c1866573d90a74d768","prev_hash":"d898b8941c4cb2d0c03d5cf5fac6fb18b52888102fdb3283c26b779a039054d9","sequence":65247992,"header_xdr":"AAAAHdiYuJQcTLLQwD1c9frG+xi1KIgQL9syg8Jrd5oDkFTZhE89mVOK81qOlgWKO587jbtvRoUpNqyd8R+g0/r0My4AAAAAaucH8AAAAAAAAAABAAAAAGLK4AK2p9ZUqsvAkvnk6pa4sXh8nzinkjgQOMq3wyDTAAAAQFu53TpZ/OQugUzuDeF45U5EPShWx8MrMoueLtKXRiwu666B/kaaQ8heBlJFsm53Fd46/M73CmmKUzEKm4lbfAZjJYliEGkk1W4ZXHqQ3A5muLH3U3eB+O4sLNuQdU0qyImKRNztpYy/KL+HxBqAoXFsoZLoiEQLjlROT4iEV2lBA+Oa+A6iHrPseVthAABjr1Pu/U4AAAEWAAAAAG8bf+gAAABkAExLQAAAA+iBCC8MsOtZLjU5HN1q6iFT5QcllPG/wOgBXBU4AGA+Xtr7PVWPOF0ni85riuBo+cQjwG9WX4OxeFTqunFDjGefrB3T50jbEIwtgiwPv0zF/m7rZ8gSe+zSmUC5KzCTkfAy/CmIHD6wNBmWE0N/i2Yg76Dtg7RN1j5wnDbOBCfdhAAAAAA=","protocolVersion":29},{"hash":"d898b8941c4cb2d0c03d5cf5fac6fb18b52888102fdb3283c26b779a039054d9","prev_hash":"f261d3d88e7c45caf1c62e54b260a26dc83132c3e95137a589888ab7c557c5b5","sequence":65247991,"header_xdr":"AAAAHfJh09iOfEXK8cYuVLJgom3IMTLD6VE3pYmIirfFV8W1VcDGHXGP+uJnqNLEr83n+15eRKhwoQ7KJGJu+MHfkqIAAAAAaucH6wAAAAAAAAABAAAAAFaWt2CucvNouAYniCiCbacMYpTGlVK3TqKyoNF3RcOeAAAAQEhkphzTWe6ftm0BB4jfgsQG9bj9DximOhRmJdMzaDOJP/spgwRHFj6hHv3YpyykcM4pnHt6qk8XlROOBV2I4Au3W8bOo5MYRp08PFyzQrj281wQNm23idaNkIhcUOnUl1iBVoGJpvKWiRx9T8dqPapC77lzpPapv1JQGpriesGTA+Oa9w6iHrPseVthAABjr1PldnkAAAEWAAAAAG8bf+IAAABkAExLQAAAA+iBCC8MsOtZLjU5HN1q6iFT5QcllPG/wOgBXBU4AGA+Xtr7PVWPOF0ni85riuBo+cQjwG9WX4OxeFTqunFDjGefrB3T50jbEIwtgiwPv0zF/m7rZ8gSe+zSmUC5KzCTkfAy/CmIHD6wNBmWE0N/i2Yg76Dtg7RN1j5wnDbOBCfdhAAAAAA=","protocolVersion":29}]""")!.AsArray();
    private static readonly string[] Hashes = Headers.Select(row => row!["sequence"]!.ToString()).ToArray();
    private static readonly StellarPublicHeaderMetadataSource.LedgerIdentity[] Ledgers = Headers.Select(row =>
        new StellarPublicHeaderMetadataSource.LedgerIdentity((uint)row!["sequence"]!, (string)row["hash"]!)).ToArray();

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Requests++;
            Assert.AreEqual(HttpMethod.Get,request.Method,"No source writes.");
            string text;
            string path=request.RequestUri!.AbsolutePath;
            if(path=="/") text=System.Text.Json.JsonSerializer.Serialize(new {network_passphrase=Fault=="network" ? "test-network" : StellarPublicHeaderMetadataSource.PublicNetwork});
            else
            {
                Assert.IsTrue(path.StartsWith("/ledgers/",StringComparison.Ordinal));
                int index=Array.IndexOf(Hashes,path[9..]);
                Assert.IsTrue(index>=0,"No discovery scans or unconfigured keys.");
                var header=Headers[index]!;
                var result=new System.Text.Json.Nodes.JsonObject {
                    ["header_xdr"]=(string)header["header_xdr"]!, ["hash"]=(string)header["hash"]!,
                    ["prev_hash"]=(string)header["prev_hash"]!, ["sequence"]=(uint)header["sequence"]!,
                    ["protocol_version"]=(uint)header["protocolVersion"]! };
                if(Fault=="header" || (Fault=="last" && index==2) || Fault=="width") result["header_xdr"]="invalid";
                if(Fault=="identity") result["hash"]=new string('0',64);
                if(Fault=="sequence") result["sequence"]=(uint)header["sequence"]!+1;
                if(Fault=="parent") result["prev_hash"]=new string('0',64);
                if(Fault=="protocol") result["protocol_version"]=(uint)header["protocolVersion"]!+1;
                text=result.ToJsonString();
            }
            if(Fault=="oversize") text=new string(' ',262145);
            if(Fault=="duplicate") text="{\"sequence\":1,\"sequence\":1}";
            if(Fault=="rpc") text="{\"error\":\"not_found\"}";
            return Task.FromResult(new HttpResponseMessage(Fault=="http" ? HttpStatusCode.BadGateway : HttpStatusCode.OK)
                { Content=new StringContent(text,Encoding.UTF8,"application/json") });
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")]
    [DataRow("network")] [DataRow("header")] [DataRow("last")] [DataRow("parent")] [DataRow("protocol")] [DataRow("width")] [DataRow("identity")] [DataRow("sequence")] [DataRow("rpc")]
    [DataRow("duplicate")] [DataRow("oversize")] [DataRow("http")] [DataRow("cancel")]
    [DataRow("unknown")] [DataRow("inactive")] [DataRow("empty-window-error")]
    public async Task OnlyCompleteVerifiedSelectionsArePublished(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new StellarPublicHeaderMetadataSource(client,new Uri("https://example.invalid/"),
            StellarPublicHeaderMetadataSource.PublicNetwork,Ledgers,Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Requests=0;
        native.Fault=scenario == "empty-window-error" ? "last" : scenario;
        var selected = scenario == "unknown" ? new[] { "4294967295" } : Hashes;
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.StellarOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row,StellarPublicHeaderMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            var key=Hashes[0];
            query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.StellarOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row,StellarPublicHeaderMetadataSource.AddressMetadata)==key).Result;
        }
        if (scenario == "window") query=query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query=query.Take(0).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.StellarOASIS,
            new[] {new CosmicQueryTarget(ProviderType.StellarOASIS,source.QueryCapabilities)});
        Assert.IsFalse(plan.IsError,plan.Message);
        using var cancel=new CancellationTokenSource();
        if(scenario=="cancel") cancel.Cancel();
        var result=await source.ExecuteCosmicQueryAsync(plan.Result,scenario=="budget" ? 2 : 100,cancel.Token);
        if(scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError,result.Exception?.ToString());
            Assert.AreEqual(scenario=="normal" ? 3 : 1,result.Result.Count);
            Assert.IsTrue(result.Result.All(row=>row.IsPublic));
            Assert.AreEqual(scenario=="window" ? Hashes[1] : Hashes[0],result.Result[0].MetaData[StellarPublicHeaderMetadataSource.AddressMetadata]);
        }
        else Assert.IsTrue(result.IsError,"No partial native result may escape.");
        if(scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0,native.Requests);
    }

    [TestMethod]
    public void RejectsSecretsAndNonCanonicalKeys()
    {
        Assert.ThrowsException<ArgumentException>(() => StellarPublicHeaderMetadataSource.FromJson("{\"privateKey\":\"forbidden\"}"));
        using var client=new HttpClient(new Native());
        Assert.ThrowsException<ArgumentException>(()=>new StellarPublicHeaderMetadataSource(client,
            new Uri("https://example.invalid/"),StellarPublicHeaderMetadataSource.PublicNetwork,new[]{new StellarPublicHeaderMetadataSource.LedgerIdentity(0,"invalid")},Guid.NewGuid()));
    }
    [TestMethod]
    [TestCategory("NativeStellarPublic")]
    public async Task NativeCatalogueRunsSharedManagerDemoThreeAndTwo()
    {
        var path=Environment.GetEnvironmentVariable("COSMIC_STELLAR_PUBLIC_SOURCE");
        if(string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained native catalogue required.");
        using var output=new StringWriter();
        var result=await CosmicCqlDemo.StellarPublicWalkthrough.RunAsync(path!,output);
        Assert.AreEqual((3,2),result);
        StringAssert.Contains(output.ToString(),"Verified content hash:");
    }

}
