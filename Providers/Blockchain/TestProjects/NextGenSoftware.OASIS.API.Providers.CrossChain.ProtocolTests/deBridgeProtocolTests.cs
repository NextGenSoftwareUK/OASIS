using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests
{
    /// <summary>Uses a real DLN order (Solana USDC -> chain 4663) captured from the DLN stats API.</summary>
    [TestClass]
    public class deBridgeProtocolTests
    {
        private const string CreationTx = "4fjuJYizxrw6XoVy3KVehfVAevngQHUp3zfFhmxurC7LzokNfJ58BZsY7pSRvEicVKxNUXcs9d2RywjUwvnKD8av";
        private const string OrderId = "0xc16a897e5b7380b775ead9419937b6e5ccfc55e01b2044a03424d855f48f6912";
        private FakeApi _api;
        private deBridgeOASIS.deBridgeOASIS _provider;

        [TestInitialize]
        public void Init()
        {
            _api = new FakeApi((req, body) =>
            {
                Assert.AreEqual("stats-api.dln.trade", req.RequestUri.Host);
                var path = req.RequestUri.AbsolutePath;
                if (path == $"/api/Transaction/{CreationTx}/orderIds") return FakeApi.Json(FakeApi.Fixture("debridge-tx-orderids.json"));
                if (path == $"/api/Orders/{OrderId}") return FakeApi.Json(FakeApi.Fixture("debridge-order.json"));
                if (path == "/api/Orders/filteredList")
                {
                    Assert.AreEqual("makerAddr", JsonDocument.Parse(body).RootElement.GetProperty("maker").GetString());
                    return FakeApi.Json($"{{\"orders\":[{{\"orderId\":{{\"stringValue\":\"{OrderId}\"}},\"state\":\"Fulfilled\"}}]}}");
                }
                return FakeApi.Status(HttpStatusCode.NotFound);
            });
            _provider = new deBridgeOASIS.deBridgeOASIS("https://stats-api.dln.trade", _api);
        }

        [TestMethod]
        public async Task Orders_created_by_a_tx_are_loaded_and_normalised()
        {
            var result = await _provider.GetTransfersBySourceTxAsync(CreationTx);
            Assert.IsFalse(result.IsError, result.Message);
            var order = result.Result.Single();
            Assert.AreEqual(OrderId, order.Id);
            Assert.AreEqual(CrossChainTransferStatus.Completed, order.Status);
            Assert.AreEqual("solana", order.SourceChain);
            Assert.AreEqual("debridge-chain-4663", order.DestinationChain);
            Assert.AreEqual(CreationTx, order.SourceTxHash);
            Assert.AreEqual("0xc9c9f86a152572f853cdb107570d26a2cd298f9eda8c999fc59e14232fea740e", order.DestinationTxHash);
            Assert.AreEqual("76zf8MzCYi2u2ZKWRRfV2KPySBDHrveUuasV1PwPgL7s", order.Sender);
            Assert.AreEqual("USDC", order.Token);
            Assert.AreEqual("99663607", order.Amount);
        }

        [TestMethod]
        public async Task Address_lookup_filters_by_maker()
        {
            var result = await _provider.GetTransfersByAddressAsync("makerAddr");
            Assert.IsFalse(result.IsError, result.Message);
            Assert.AreEqual(OrderId, result.Result.Single().Id);
        }

        [TestMethod]
        public void Cancelled_orders_are_refunds()
            => Assert.AreEqual(CrossChainTransferStatus.Refunded, deBridgeOASIS.deBridgeOASIS.MapState("ClaimedOrderCancel"));
    }
}
