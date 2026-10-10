using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests
{
    /// <summary>Uses real LayerZero Scan responses captured as fixtures.</summary>
    [TestClass]
    public class LayerZeroProtocolTests
    {
        private FakeApi _api;
        private LayerZeroV2OASIS.LayerZeroV2OASIS _provider;

        [TestInitialize]
        public void Init()
        {
            _api = new FakeApi((req, _) =>
            {
                Assert.AreEqual("scan.layerzero-api.com", req.RequestUri.Host);
                return req.RequestUri.AbsolutePath.StartsWith("/v1/messages/tx/0xknown")
                    ? FakeApi.Json(FakeApi.Fixture("layerzero-messages.json"))
                    : FakeApi.Status(HttpStatusCode.NotFound, "{\"message\":\"Message not found\",\"code\":4040}");
            });
            _provider = new LayerZeroV2OASIS.LayerZeroV2OASIS("https://scan.layerzero-api.com/v1", _api);
        }

        [TestMethod]
        public async Task Delivered_and_inflight_messages_are_normalised()
        {
            var result = await _provider.GetTransfersBySourceTxAsync("0xknown");
            Assert.IsFalse(result.IsError, result.Message);

            var delivered = result.Result[0];
            Assert.AreEqual(CrossChainTransferStatus.Completed, delivered.Status);
            Assert.AreEqual("DELIVERED", delivered.RawStatus);
            Assert.IsNotNull(delivered.DestinationTxHash);
            Assert.IsNotNull(delivered.SourceChain);
            Assert.IsTrue(delivered.Id.StartsWith("0x"), "id is the LayerZero GUID");

            Assert.AreEqual(CrossChainTransferStatus.Pending, result.Result[1].Status);
            Assert.IsNull(result.Result[1].DestinationTxHash);
        }

        [TestMethod]
        public async Task Unknown_transactions_return_an_empty_list_not_an_error()
        {
            var result = await _provider.GetTransfersBySourceTxAsync("0xunknown");
            Assert.IsFalse(result.IsError, result.Message);
            Assert.AreEqual(0, result.Result.Count);
        }

        [TestMethod]
        public void Every_documented_status_is_mapped()
        {
            foreach (var s in new[] { "INFLIGHT", "CONFIRMING", "DELIVERED", "FAILED", "BLOCKED", "PAYLOAD_STORED", "APPLICATION_BURNED", "APPLICATION_SKIPPED", "UNRESOLVABLE_COMMAND", "MALFORMED_COMMAND" })
                Assert.AreNotEqual(CrossChainTransferStatus.Unknown, LayerZeroV2OASIS.LayerZeroV2OASIS.MapStatus(s), s);
        }
    }
}
