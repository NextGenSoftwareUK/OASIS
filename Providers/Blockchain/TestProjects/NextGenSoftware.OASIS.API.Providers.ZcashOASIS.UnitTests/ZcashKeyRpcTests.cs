using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Providers.ZcashOASIS.Infrastructure.Services.Zcash;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.UnitTests
{
    [TestClass]
    public class ZcashKeyRpcTests
    {
        private sealed class Node : HttpMessageHandler
        {
            public readonly List<JObject> Requests = new();
            public Func<JObject, (HttpStatusCode Status, string Body)> Reply = _ => (HttpStatusCode.OK, "{\"result\":\"ok\",\"error\":null}");

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var body = JObject.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                Requests.Add(body);
                var (status, text) = Reply(body);
                return new HttpResponseMessage(status) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
            }
        }

        private static (ZcashRPCClient Client, Node Node) Client()
        {
            var node = new Node();
            return (new ZcashRPCClient("http://127.0.0.1:8232", "user", "pass", node), node);
        }

        [TestMethod]
        public async Task SaplingAddressAndItsKeysUseTheZcashdMethodsWithPositionalParameters()
        {
            var (client, node) = Client();
            node.Reply = r => r["method"]!.ToString() switch
            {
                "z_getnewaddress" => (HttpStatusCode.OK, "{\"result\":\"zs1address\",\"error\":null}"),
                "z_exportkey" => (HttpStatusCode.OK, "{\"result\":\"secret-extended-key-main1abc\",\"error\":null}"),
                "z_exportviewingkey" => (HttpStatusCode.OK, "{\"result\":\"zxviews1abc\",\"error\":null}"),
                _ => (HttpStatusCode.OK, "{\"result\":null,\"error\":{\"code\":-32601,\"message\":\"Method not found\"}}")
            };
            Assert.AreEqual("zs1address", (await client.GetNewShieldedAddressAsync()).Result);
            Assert.AreEqual("secret-extended-key-main1abc", (await client.ExportSpendingKeyAsync("zs1address")).Result);
            Assert.AreEqual("zxviews1abc", (await client.ExportShieldedViewingKeyAsync("zs1address")).Result);

            CollectionAssert.AreEqual(new[] { "z_getnewaddress", "z_exportkey", "z_exportviewingkey" }, node.Requests.ConvertAll(r => r["method"]!.ToString()));
            Assert.AreEqual(JTokenType.Array, node.Requests[0]["params"]!.Type);
            Assert.AreEqual("sapling", node.Requests[0]["params"]![0]!.ToString());
            Assert.AreEqual("zs1address", node.Requests[1]["params"]![0]!.ToString());
        }

        [DataTestMethod]
        [DataRow(200, "{\"result\":null,\"error\":{\"code\":-4,\"message\":\"Wallet does not hold private zkey for this zaddr\"}}", "does not hold")]
        [DataRow(200, "{\"result\":\"\",\"error\":null}", "empty result")]
        [DataRow(500, "{\"result\":null,\"error\":{\"code\":-28,\"message\":\"Loading wallet...\"}}", "failed")]
        public async Task NodeErrorsAreErrorsNotEmptySuccesses(int status, string body, string expected)
        {
            var (client, node) = Client();
            node.Reply = _ => ((HttpStatusCode)status, body);
            var key = await client.ExportSpendingKeyAsync("zs1address");
            Assert.IsTrue(key.IsError);
            Assert.IsNull(key.Result);
            StringAssert.Contains(key.Message, expected);
        }
    }
}
