using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests
{
    /// <summary>Uses a real swap (BTC -> USDT on Ethereum, swap 500000) captured from the Chainflip swap API.</summary>
    [TestClass]
    public class ChainflipProtocolTests
    {
        private const string DepositTx = "0884595f1e677846a140a3446fbb24bf1b7b4943861fa140acedfcf3ec75074f";
        private FakeApi _api;
        private ChainflipOASIS.ChainflipOASIS _provider;

        [TestInitialize]
        public void Init()
        {
            _api = new FakeApi((req, _) =>
            {
                Assert.AreEqual("chainflip-swap.chainflip.io", req.RequestUri.Host);
                return req.RequestUri.AbsolutePath == "/v2/swaps/" + DepositTx
                    ? FakeApi.Json(FakeApi.Fixture("chainflip-swap.json"))
                    : FakeApi.Status(HttpStatusCode.NotFound, "{\"message\":\"resource not found\"}");
            });
            _provider = new ChainflipOASIS.ChainflipOASIS("https://chainflip-swap.chainflip.io", _api);
        }

        [TestMethod]
        public async Task Completed_swap_maps_deposit_and_egress()
        {
            var result = await _provider.GetTransfersBySourceTxAsync(DepositTx);
            Assert.IsFalse(result.IsError, result.Message);
            var t = result.Result.Single();
            Assert.AreEqual("500000", t.Id);
            Assert.AreEqual(CrossChainTransferStatus.Completed, t.Status);
            Assert.AreEqual("Bitcoin", t.SourceChain);
            Assert.AreEqual("Ethereum", t.DestinationChain);
            Assert.AreEqual(DepositTx, t.SourceTxHash);
            Assert.AreEqual("0x1fccfde3e33a4b7e363215c40e51684eff8ee812711ac5fcf39944ca174eab1b", t.DestinationTxHash);
            Assert.AreEqual("0xbacf01118527e6865427bb65fa62ab2d8bed5bf6", t.Recipient);
            Assert.AreEqual("530649", t.Amount);
        }

        [TestMethod]
        public async Task Not_found_is_an_empty_list()
        {
            var result = await _provider.GetTransfersBySourceTxAsync("0x01");
            Assert.IsFalse(result.IsError, result.Message);
            Assert.AreEqual(0, result.Result.Count);
        }

        [TestMethod]
        public async Task Address_lookup_is_an_explicit_error_because_the_api_has_none()
        {
            var result = await _provider.GetTransfersByAddressAsync("0xabc");
            Assert.IsTrue(result.IsError);
            Assert.AreEqual(0, _api.Requests.Count);
        }
    }
}
