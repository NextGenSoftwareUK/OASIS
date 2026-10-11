using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests
{
    /// <summary>Uses a real FastBridge transfer (HyperEVM -> Optimism) captured from the Synapse API.</summary>
    [TestClass]
    public class SynapseProtocolTests
    {
        private const string OriginTx = "0xb11b9d84f9fba9961d46dcbc9397aa5b9b84eb4af19c805686261b55604fd374";
        private FakeApi _api;
        private SynapseOASIS.SynapseOASIS _provider;

        [TestInitialize]
        public void Init()
        {
            _api = new FakeApi((req, _) =>
            {
                Assert.AreEqual("api.synapseprotocol.com", req.RequestUri.Host);
                return req.RequestUri.AbsolutePath == "/transaction-id/" + OriginTx
                    ? FakeApi.Json(FakeApi.Fixture("synapse-transaction-id.json"))
                    : FakeApi.Json("{\"message\":\"Transaction not found\"}");
            });
            _provider = new SynapseOASIS.SynapseOASIS("https://api.synapseprotocol.com", _api);
        }

        [TestMethod]
        public async Task Relayed_fastbridge_transfer_is_completed()
        {
            var result = await _provider.GetTransfersBySourceTxAsync(OriginTx);
            Assert.IsFalse(result.IsError, result.Message);
            var t = result.Result.Single();
            Assert.AreEqual(CrossChainTransferStatus.Completed, t.Status);
            Assert.AreEqual("hyperevm", t.SourceChain);
            Assert.AreEqual("optimism", t.DestinationChain);
            Assert.AreEqual(OriginTx, t.SourceTxHash);
            Assert.AreEqual("0x6e606e03c257a11b0806a892073917fe35a74c25d42e232303305b009a9beb3a", t.DestinationTxHash);
            Assert.AreEqual("0.01", t.Amount);
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
