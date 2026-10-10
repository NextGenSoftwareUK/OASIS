using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests
{
    /// <summary>Uses real Wormholescan responses captured as fixtures.</summary>
    [TestClass]
    public class WormholeProtocolTests
    {
        private const string SourceTx = "0x8aa1ee216977e7f2aa965d7114bf506ae1d9c709aa433f80638447c2f2e57aaa";
        private FakeApi _api;
        private WormholeOASIS.WormholeOASIS _provider;

        [TestInitialize]
        public void Init()
        {
            _api = new FakeApi((req, _) =>
            {
                Assert.AreEqual("api.wormholescan.io", req.RequestUri.Host);
                return req.RequestUri.AbsolutePath == "/api/v1/operations"
                    ? FakeApi.Json(FakeApi.Fixture("wormhole-operations.json"))
                    : FakeApi.Status(HttpStatusCode.NotFound, "{\"code\":5,\"message\":\"NOT FOUND\"}");
            });
            _provider = new WormholeOASIS.WormholeOASIS("https://api.wormholescan.io", _api);
        }

        [TestMethod]
        public async Task Completed_and_pending_operations_are_normalised()
        {
            var result = await _provider.GetTransfersBySourceTxAsync(SourceTx);
            Assert.IsFalse(result.IsError, result.Message);
            StringAssert.Contains(_api.Requests.Single().Request.RequestUri.Query, "txHash=" + SourceTx);

            var completed = result.Result[0];
            Assert.AreEqual(CrossChainTransferStatus.Completed, completed.Status);
            Assert.AreEqual("bsc", completed.SourceChain);
            Assert.AreEqual("solana", completed.DestinationChain);
            Assert.AreEqual(SourceTx, completed.SourceTxHash);
            Assert.IsNotNull(completed.DestinationTxHash);

            var pending = result.Result[1];
            Assert.AreEqual(CrossChainTransferStatus.Pending, pending.Status);
            Assert.AreEqual("near", pending.SourceChain, "app-level route from standardized properties wins over the emitter chain");
            Assert.AreEqual("11632519", pending.Amount);
            Assert.IsNull(pending.DestinationTxHash);
        }

        [TestMethod]
        public async Task Api_errors_are_OASIS_errors()
        {
            var result = await _provider.GetTransferAsync(2, "0xabc", "1");
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "404");
        }

        [TestMethod]
        public async Task Missing_arguments_are_rejected_without_calling_the_api()
        {
            Assert.IsTrue((await _provider.GetTransfersByAddressAsync(" ")).IsError);
            Assert.AreEqual(0, _api.Requests.Count);
        }
    }
}
