using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.GelatoNetworkOASIS.QueryTests;

[TestClass]
public class GaslessCapabilitiesClientTests
{
    private const string Key = "owned-control-token";
    private const string Body = "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"1\":{\"feeCollector\":\"0x1111111111111111111111111111111111111111\",\"tokens\":[{\"address\":\"0x2222222222222222222222222222222222222222\",\"decimals\":18}],\"nativeField\":{\"n\":18446744073709551615}}}}";

    [DataTestMethod]
    [DataRow("http")]
    [DataRow("media")]
    [DataRow("utf8")]
    [DataRow("oversize")]
    [DataRow("length")]
    [DataRow("duplicate")]
    [DataRow("deep")]
    [DataRow("envelope")]
    [DataRow("id")]
    [DataRow("error")]
    [DataRow("array")]
    [DataRow("empty")]
    [DataRow("chain")]
    [DataRow("collector")]
    [DataRow("tokens")]
    [DataRow("token-address")]
    [DataRow("decimals")]
    [DataRow("duplicate-token")]
    [DataRow("transport")]
    [DataRow("chain-overflow")]
    [DataRow("too-many-chains")]
    [DataRow("too-many-tokens")]
    [DataRow("decimal-range")]
    [DataRow("nested-duplicate")]
    [DataRow("charset")]
    public async Task InvalidNativeResponseFailsOnceWithoutPartialCapabilities(string fault)
    {
        using var handler = new Replies { Fault = fault };
        using var client = new GelatoGaslessCapabilitiesClient(Key, handler);
        var result = await client.GetCapabilitiesAsync();
        Assert.IsTrue(result.IsError, fault);
        Assert.IsNull(result.Result);
        Assert.AreEqual(1, handler.Calls);
        Assert.IsFalse((result.Message ?? "").Contains(Key), "Native diagnostics must not expose the configured credential.");
    }

    [TestMethod]
    public async Task ActualRequestContractAndCompleteNativeValuesArePreserved()
    {
        using var handler = new Replies();
        using var client = new GelatoGaslessCapabilitiesClient(Key, handler);
        var result = await client.GetCapabilitiesAsync();
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(Body, result.Result.RawJson);
        Assert.AreEqual(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Body))), result.Result.RawSha256);
        Assert.AreEqual("18446744073709551615", result.Result.Capabilities.GetProperty("1").GetProperty("nativeField").GetProperty("n").GetRawText());
    }

    [TestMethod]
    public async Task ChainIdentityRemainsExactBeyondFloatingPointAndInt64()
    {
        var chain = "115792089237316195423570985008687907853269984665640564039457584007913129639935";
        using var handler = new Replies { Override = Body.Replace("\"1\":{", "\"" + chain + "\":{") };
        using var client = new GelatoGaslessCapabilitiesClient(Key, handler);
        var result = await client.GetCapabilitiesAsync();
        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.Result.Capabilities.TryGetProperty(chain, out _));
    }

    [TestMethod]
    public async Task CancelledOperationDoesNotDispatch()
    {
        using var handler = new Replies();
        using var client = new GelatoGaslessCapabilitiesClient(Key, handler);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => client.GetCapabilitiesAsync(cancellation.Token));
        Assert.AreEqual(0, handler.Calls);
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("bad\r\nkey")]
    [DataRow("=")]
    public void MissingOrMalformedCredentialsFailBeforeTransport(string? key) =>
        Assert.ThrowsException<ArgumentException>(() => new GelatoGaslessCapabilitiesClient(key!));

    [TestMethod]
    public void InjectedTransportLifetimeRemainsCallerOwned()
    {
        using var handler = new Replies();
        new GelatoGaslessCapabilitiesClient(Key, handler).Dispose();
        Assert.IsFalse(handler.Disposed);
    }

    private sealed class Replies : HttpMessageHandler
    {
        public string Fault = "";
        public string? Override;
        public int Calls;
        public bool Disposed;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Assert.AreEqual(GelatoGaslessCapabilitiesClient.Endpoint, request.RequestUri!.AbsoluteUri);
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("Bearer", request.Headers.Authorization!.Scheme);
            Assert.AreEqual(Key, request.Headers.Authorization.Parameter);
            using var sent = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            Assert.AreEqual("relayer_getCapabilities", sent.RootElement.GetProperty("method").GetString());
            Assert.AreEqual(0, sent.RootElement.GetProperty("params").GetArrayLength());
            if (Fault == "transport") throw new HttpRequestException("Private transport diagnostic " + Key);
            var json = JsonNode.Parse(Override ?? Body)!;
            var chain = json["result"]!["1"];
            switch (Fault)
            {
                case "envelope": json["jsonrpc"] = "1.0"; break;
                case "id": json["id"] = 2; break;
                case "error": json["error"] = new JsonObject { ["message"] = Key }; break;
                case "array": json["result"] = new JsonArray(); break;
                case "empty": json["result"] = new JsonObject(); break;
                case "chain": json["result"] = new JsonObject { ["01"] = chain!.DeepClone() }; break;
                case "collector": chain!["feeCollector"] = "invalid"; break;
                case "tokens": chain!["tokens"] = "invalid"; break;
                case "token-address": chain!["tokens"]![0]!["address"] = "invalid"; break;
                case "decimals": chain!["tokens"]![0]!["decimals"] = 1.5; break;
                case "duplicate-token": chain!["tokens"]!.AsArray().Add(chain["tokens"]![0]!.DeepClone()); break;
                case "chain-overflow": json["result"] = new JsonObject { ["115792089237316195423570985008687907853269984665640564039457584007913129639936"] = chain!.DeepClone() }; break;
                case "too-many-chains":
                    var chains = new JsonObject();
                    for (var i = 1; i <= 129; i++) chains[i.ToString(System.Globalization.CultureInfo.InvariantCulture)] = chain!.DeepClone();
                    json["result"] = chains; break;
                case "too-many-tokens":
                    while (chain!["tokens"]!.AsArray().Count <= 64) chain["tokens"]!.AsArray().Add(chain["tokens"]![0]!.DeepClone());
                    break;
                case "decimal-range": chain!["tokens"]![0]!["decimals"] = 256; break;
            }
            var body = Fault == "" ? Override ?? Body : json.ToJsonString();
            if (Fault == "duplicate") body = body.Replace("\"id\":1", "\"id\":1,\"id\":1");
            if (Fault == "nested-duplicate") body = body.Replace("\"decimals\":18", "\"decimals\":18,\"decimals\":18");
            if (Fault == "deep") body = new string('[', 40) + "0" + new string(']', 40);
            if (Fault == "oversize") body = new string('x', 131073);
            var response = new HttpResponseMessage(Fault == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)
            { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            if (Fault == "utf8")
            {
                response.Content.Dispose(); response.Content = new ByteArrayContent(new byte[] { 0xff });
                response.Content.Headers.ContentType = new("application/json");
            }
            if (Fault == "length") response.Content.Headers.ContentLength = 131073;
            if (Fault == "media") response.Content.Headers.ContentType = new("text/plain");
            if (Fault == "charset") response.Content.Headers.ContentType!.CharSet = "iso-8859-1";
            return response;
        }
        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
    }
}
