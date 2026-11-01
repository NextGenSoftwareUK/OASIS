using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Buffers.Binary;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS.UnitTests;

[TestClass]
public sealed class BitcoinPublicHeaderMetadataTests
{
    // Published mainnet headers, independently qualified by the Python preflight.
    private static readonly string[] Hashes = { "000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f","00000000839a8e6886ab5951d76f411475428afc90947ee320161bbf18eb6048","000000006a625f06636b8bb6ac7b960a8d03705d1ace08b1a19da3fdcc99ddbd" };
    private static readonly string[] Headers = { "0100000000000000000000000000000000000000000000000000000000000000000000003ba3edfd7a7b12b27ac72c3e67768f617fc81bc3888a51323a9fb8aa4b1e5e4a29ab5f49ffff001d1dac2b7c","010000006fe28c0ab6f1b372c1a6a246ae63f74f931e8365e15a089c68d6190000000000982051fd1e4ba744bbbe680e1fee14677ba1a3c3540bf7b1cdb606e857233e0e61bc6649ffff001d01e36299","010000004860eb18bf1b1620e37e9490fc8a427514416fd75159ab86688e9a8300000000d5fdcc541e25de1c7a5addedf24858b8bb665c9f36ef744ee42c316022c90f9bb0bc6649ffff001d08d2bd61" };

    private sealed class Native : HttpMessageHandler
    {
        public string Fault = "";
        public int Requests;
        public string? ExtraHash, ExtraHeader;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Requests++;
            Assert.AreEqual(HttpMethod.Get, request.Method, "Public profile must never send writes.");
            var path = request.RequestUri!.AbsolutePath;
            string text;
            if (path == "/api/block-height/0") text = Fault == "genesis" ? Hashes[1] : Hashes[0];
            else
            {
                var segments = path.Split('/');
                int index = Array.IndexOf(Hashes,segments[3]);
                bool extra = segments[3] == ExtraHash;
                if(extra) index=0;
                Assert.IsTrue(index >= 0,"No discovery scans or unconfigured keys.");
                text = segments[4] == "header" ? (extra ? ExtraHeader! : Headers[index]) : "{\"in_best_chain\":true}";
                if (Fault == "header" || (Fault == "last" && index == 2)) text = Headers[index][..^2] + "00";
                if (segments[4] == "status" && Fault == "orphan") text = "{\"in_best_chain\":false}";
                if (segments[4] == "status" && Fault == "duplicate") text = "{\"in_best_chain\":true,\"in_best_chain\":true}";
            }
            if (Fault == "oversize") text = new string(' ',4097);
            return Task.FromResult(new HttpResponseMessage(Fault == "http" ? HttpStatusCode.BadGateway : HttpStatusCode.OK)
                { Content = new StringContent(text,Encoding.UTF8,"text/plain") });
        }
    }

    [DataTestMethod]
    [DataRow("normal")] [DataRow("point")] [DataRow("window")] [DataRow("budget")]
    [DataRow("genesis")] [DataRow("header")] [DataRow("last")] [DataRow("orphan")]
    [DataRow("duplicate")] [DataRow("oversize")] [DataRow("http")] [DataRow("cancel")]
    [DataRow("unknown")] [DataRow("inactive")] [DataRow("empty-window-error")]
    public async Task OnlyCompleteVerifiedSelectionsArePublished(string scenario)
    {
        var native = new Native();
        using var client = new HttpClient(native);
        using var source = new BitcoinPublicHeaderMetadataSource(client,new Uri("https://example.invalid/api/"),
            BitcoinPublicHeaderMetadataSource.Genesis,Hashes,Guid.NewGuid());
        if (scenario != "inactive") Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        native.Requests=0;
        native.Fault=scenario == "empty-window-error" ? "last" : scenario;
        var selected = scenario == "unknown" ? new[] { new string('9',64) } : Hashes;
        var query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.BitcoinOASIS).Result)
            .Where(row => selected.Contains(CosmicQueryMetadata.Value<string>(row,BitcoinPublicHeaderMetadataSource.AddressMetadata))).Result;
        if (scenario == "point")
        {
            var key=Hashes[0];
            query=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.BitcoinOASIS).Result)
                .Where(row => CosmicQueryMetadata.Value<string>(row,BitcoinPublicHeaderMetadataSource.AddressMetadata)==key).Result;
        }
        if (scenario == "window") query=query.Skip(1).Result.Take(1).Result;
        if (scenario == "empty-window-error") query=query.Take(0).Result;
        var plan=CosmicQueryPlanner.Plan(query,ProviderType.BitcoinOASIS,
            new[] {new CosmicQueryTarget(ProviderType.BitcoinOASIS,source.QueryCapabilities)});
        Assert.IsFalse(plan.IsError,plan.Message);
        using var cancel=new CancellationTokenSource();
        if(scenario=="cancel") cancel.Cancel();
        var result=await source.ExecuteCosmicQueryAsync(plan.Result,scenario=="budget" ? 2 : 100,cancel.Token);
        if(scenario is "normal" or "point" or "window")
        {
            Assert.IsFalse(result.IsError,result.Exception?.ToString());
            Assert.AreEqual(scenario=="normal" ? 3 : 1,result.Result.Count);
            Assert.IsTrue(result.Result.All(row=>row.IsPublic));
            Assert.AreEqual(scenario=="window" ? Hashes[1] : Hashes[0],result.Result[0].MetaData[BitcoinPublicHeaderMetadataSource.AddressMetadata]);
        }
        else Assert.IsTrue(result.IsError,"No partial native result may escape.");
        if(scenario is "inactive" or "cancel" or "unknown") Assert.AreEqual(0,native.Requests);
    }

    [TestMethod]
    public void RejectsSecretsAndNonCanonicalKeys()
    {
        Assert.ThrowsException<ArgumentException>(() => BitcoinPublicHeaderMetadataSource.FromJson("{\"privateKey\":\"forbidden\"}"));
        using var client=new HttpClient(new Native());
        Assert.ThrowsException<InvalidDataException>(()=>new BitcoinPublicHeaderMetadataSource(client,
            new Uri("https://example.invalid/api/"),BitcoinPublicHeaderMetadataSource.Genesis,new[]{"0xAB"},Guid.NewGuid()));
    }
    [DataTestMethod]
    [DataRow("negative")] [DataRow("zero")] [DataRow("over-limit")] [DataRow("insufficient-work")]
    public async Task MatchingIdentityDoesNotBypassProofOfWork(string scenario)
    {
        var header=Convert.FromHexString(Headers[0]);
        uint bits=scenario switch { "negative"=>0x1d80ffffu, "zero"=>0u, "over-limit"=>0x1e00ffffu, _=>0x1d00ffffu };
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(72),bits);
        // Destroy the known valid nonce while retaining a self-consistent hash.
        header.AsSpan(76,4).Clear();
        var key=Convert.ToHexString(SHA256.HashData(SHA256.HashData(header)).Reverse().ToArray()).ToLowerInvariant();
        var native=new Native { ExtraHash=key, ExtraHeader=Convert.ToHexString(header).ToLowerInvariant() };
        using var client=new HttpClient(native);
        using var source=new BitcoinPublicHeaderMetadataSource(client,new Uri("https://example.invalid/api/"),
            BitcoinPublicHeaderMetadataSource.Genesis,new[]{key},Guid.NewGuid());
        var result=await source.ActivateQuerySourceAsync(default);
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(source.IsProviderActivated);
        StringAssert.Contains(result.Exception!.Message,"proof-of-work");
    }

    [TestMethod]
    [TestCategory("NativeBitcoinPublic")]
    public async Task NativeCatalogueRunsSharedManagerDemoThreeAndTwo()
    {
        var path=Environment.GetEnvironmentVariable("COSMIC_BITCOIN_PUBLIC_SOURCE");
        if(string.IsNullOrEmpty(path)) Assert.Inconclusive("Explicit retained native catalogue required.");
        using var output=new StringWriter();
        var result=await CosmicCqlDemo.BitcoinPublicWalkthrough.RunAsync(path!,output);
        Assert.AreEqual((3,2),result);
        StringAssert.Contains(output.ToString(),"Verified content hash:");
    }

}
