using System.Net;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaNftReadContractTests
{
    [DataTestMethod]
    [DataRow("aXBmczovL21ldGFkYXRh", "ipfs://metadata")]
    [DataRow("/w==", null)]
    public async Task ReadsConfiguredSerialEndpointAndPreservesBinaryMetadata(string metadata, string? text)
    {
        using var handler = new FixtureHandler(Response(metadata));
        var provider = Provider(handler);
        var result = await provider.LoadOnChainNFTDataAsync("0.0.1234/9007199254740993");
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual("http://127.0.0.1:12345/api/v1/tokens/0.0.1234/nfts/9007199254740993", handler.RequestUri);
        Assert.AreEqual(metadata, result.Result.MetaData["HederaMetadataBase64"]);
        Assert.AreEqual("0.0.1001", result.Result.MetaData["HederaOwnerAccount"]);
        Assert.AreEqual("0.0.1001", result.Result.SendToAddressAfterMinting);
        Assert.AreEqual(text != null, result.Result.MetaData.ContainsKey("HederaMetadataText"));
        if (text != null) Assert.AreEqual(text, result.Result.JSONMetaDataURL);
    }

    [DataTestMethod]
    [DataRow("wrongIdentity")]
    [DataRow("deleted")]
    [DataRow("badMetadata")]
    [DataRow("missingOwner")]
    [DataRow("zeroOwner")]
    [DataRow("oversizedMetadata")]
    public async Task InvalidMirrorResponseCannotBecomeSuccessfulNft(string failure)
    {
        var json = Response("aXBmczovL21ldGFkYXRh");
        json = failure switch
        {
            "wrongIdentity" => json.Replace("0.0.1234", "0.0.9999"),
            "deleted" => json.Replace("false", "true"),
            "badMetadata" => json.Replace("aXBmczovL21ldGFkYXRh", "not base64"),
            "zeroOwner" => json.Replace("0.0.1001", "0.0.0"),
            "oversizedMetadata" => Response(Convert.ToBase64String(new byte[101])),
            _ => json.Replace("0.0.1001", "")
        };
        using var handler = new FixtureHandler(json);
        var result = await Provider(handler).LoadOnChainNFTDataAsync("0.0.1234/9007199254740993");
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    private static string Response(string metadata) => "{\"token_id\":\"0.0.1234\",\"serial_number\":9007199254740993,\"deleted\":false,\"account_id\":\"0.0.1001\",\"metadata\":\"" + metadata + "\"}";

    private static HashgraphOASIS Provider(FixtureHandler handler)
    {
        var provider = new HashgraphOASIS("http://127.0.0.1:12345/api/v1", "testnet");
        var field = typeof(HashgraphOASIS).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)!;
        ((HttpClient)field.GetValue(provider)!).Dispose();
        field.SetValue(provider, new HttpClient(handler) { BaseAddress = new Uri("http://127.0.0.1:12345/api/v1/") });
        return provider;
    }

    private sealed class FixtureHandler(string json) : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri!.AbsoluteUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
