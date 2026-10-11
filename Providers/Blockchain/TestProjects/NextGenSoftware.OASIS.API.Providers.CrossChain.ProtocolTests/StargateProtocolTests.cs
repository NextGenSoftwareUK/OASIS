using System.Linq;
using System.Net;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.StargateOASIS;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests
{
    [TestClass]
    public class StargateProtocolTests
    {
        private const string QuoteResponse = "{\"error\":null,\"quotes\":[{\"id\":\"q-1\",\"routeSteps\":[{\"type\":\"STARGATE_V2_TAXI\",\"srcChainKey\":\"base\",\"fees\":[]}],\"fees\":[],\"duration\":{\"estimated\":\"60\"},\"feeUsd\":\"0.12\",\"expiresAt\":\"2030-01-01T00:00:00Z\",\"srcAmount\":1000000000000000000000000,\"dstAmount\":999000000000000000000000,\"dstAmountMin\":998000000000000000000000,\"userSteps\":[]}]}";

        private static (StargateOASIS.StargateOASIS Provider, FakeApi Api) Create(string apiKey)
        {
            var api = new FakeApi((req, body) =>
            {
                var path = req.RequestUri.AbsolutePath;
                if (req.RequestUri.Host == "scan.layerzero-api.com") return FakeApi.Json(FakeApi.Fixture("layerzero-messages.json"));
                Assert.AreEqual("transfer.layerzero-api.com", req.RequestUri.Host);
                if (path == "/v1/chains") return FakeApi.Json("{\"chains\":[{\"chainKey\":\"base\",\"chainId\":8453,\"chainType\":\"EVM\"}]}");
                if (!req.Headers.Contains("x-api-key")) return FakeApi.Status(HttpStatusCode.Unauthorized, "{\"error\":\"Unauthorized\"}");
                if (path == "/v1/quotes") return FakeApi.Json(QuoteResponse);
                if (path == "/v1/status/q-1") return FakeApi.Json("{\"status\":\"SUCCEEDED\",\"explorerUrl\":\"https://layerzeroscan.com/tx/0xabc\",\"executionHistory\":[{\"event\":\"SRC_TX\",\"transaction\":{\"chainKey\":\"base\",\"hash\":\"0xabc\"}}]}");
                return FakeApi.Status(HttpStatusCode.NotFound);
            });
            var scan = new LayerZeroV2OASIS.LayerZeroV2OASIS("https://scan.layerzero-api.com/v1", api);
            return (new StargateOASIS.StargateOASIS(apiKey, "https://transfer.layerzero-api.com/v1", scan, api), api);
        }

        [TestMethod]
        public async Task Quotes_send_integer_amounts_and_parse_big_values()
        {
            var (provider, api) = Create("key-1");
            var result = await provider.GetQuotesAsync(new StargateQuoteRequest
            {
                SrcChainKey = "base", SrcTokenAddress = "0xsrc", SrcWalletAddress = "0xme",
                DstChainKey = "arbitrum", DstTokenAddress = "0xdst", DstWalletAddress = "0xme",
                Amount = BigInteger.Parse("1000000000000000000000000")
            });
            Assert.IsFalse(result.IsError, result.Message);
            var quote = result.Result.Single();
            Assert.AreEqual(BigInteger.Parse("999000000000000000000000"), quote.DstAmount);
            Assert.AreEqual("STARGATE_V2_TAXI", quote.RouteTypes.Single());

            using var sent = JsonDocument.Parse(api.Requests.Last().Body);
            Assert.AreEqual(JsonValueKind.Number, sent.RootElement.GetProperty("amount").ValueKind);
            Assert.AreEqual("1000000000000000000000000", sent.RootElement.GetProperty("amount").GetRawText());
            Assert.AreEqual("key-1", api.Requests.Last().Request.Headers.GetValues("x-api-key").Single());
        }

        [TestMethod]
        public async Task Quotes_without_an_api_key_fail_before_calling_the_api()
        {
            var (provider, api) = Create(null);
            Assert.IsTrue((await provider.GetQuotesAsync(new StargateQuoteRequest { Amount = 1 })).IsError);
            Assert.AreEqual(0, api.Requests.Count);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError, "chains are public, so activation works without a key");
        }

        [TestMethod]
        public async Task Status_maps_succeeded_to_completed()
        {
            var (provider, _) = Create("key-1");
            var status = await provider.GetTransferStatusAsync("q-1", "0xabc");
            Assert.AreEqual(CrossChainTransferStatus.Completed, status.Result.Status);
            Assert.AreEqual("0xabc", status.Result.History.Single().TxHash);
        }

        [TestMethod]
        public async Task Tracking_keeps_only_stargate_sent_layerzero_messages()
        {
            var (provider, _) = Create("key-1");
            var transfers = await provider.GetTransfersBySourceTxAsync("0xknown");
            Assert.IsFalse(transfers.IsError, transfers.Message);
            Assert.AreEqual(1, transfers.Result.Count, "the fixture holds one Stargate and one USDT0 message");
            Assert.IsTrue(transfers.Result.All(t => t.Protocol == "stargate" && t.SenderApplication == "stargate"));
        }
    }
}
