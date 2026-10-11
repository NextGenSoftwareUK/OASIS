using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests
{
    /// <summary>Uses a real Hyperlane Explorer response (Arbitrum -> Base) captured as a fixture.</summary>
    [TestClass]
    public class HyperlaneProtocolTests
    {
        private const string Tx = "0xc5587950324d33e6c39602493fbd23e8e1400387f049f87f40408aeed3adeed6";
        private FakeApi _api;
        private HyperlaneOASIS.HyperlaneOASIS _provider;

        [TestInitialize]
        public void Init()
        {
            _api = new FakeApi((req, body) =>
            {
                Assert.AreEqual("explorer4.hasura.app", req.RequestUri.Host);
                Assert.AreEqual("/v1/graphql", req.RequestUri.AbsolutePath);
                var v = JsonDocument.Parse(body).RootElement.GetProperty("variables").GetProperty("v").GetString();
                return v == "\\x" + Tx[2..]
                    ? FakeApi.Json(FakeApi.Fixture("hyperlane-messages.json"))
                    : FakeApi.Json("{\"data\":{\"message_view\":[]}}");
            });
            _provider = new HyperlaneOASIS.HyperlaneOASIS("https://explorer4.hasura.app/v1/graphql", _api);
        }

        [TestMethod]
        public async Task Delivered_message_is_normalised_with_hex_hashes()
        {
            var result = await _provider.GetTransfersBySourceTxAsync(Tx);
            Assert.IsFalse(result.IsError, result.Message);
            var message = result.Result.Single();
            Assert.AreEqual(CrossChainTransferStatus.Completed, message.Status);
            Assert.AreEqual("arbitrum", message.SourceChain);
            Assert.AreEqual("base", message.DestinationChain);
            Assert.AreEqual(Tx, message.SourceTxHash);
            Assert.IsTrue(message.DestinationTxHash.StartsWith("0xa542958d"));
            Assert.IsTrue(message.Id.StartsWith("0xff7a71ac"));
            Assert.AreEqual(2024, message.Timestamp.Value.Year);
        }

        [TestMethod]
        public void Base58_hashes_are_decoded_for_non_evm_chains()
        {
            Assert.AreEqual("\\x0000", HyperlaneOASIS.HyperlaneOASIS.ToBytea("11"));
            Assert.AreEqual("\\x0001", HyperlaneOASIS.HyperlaneOASIS.ToBytea("12"));
            Assert.AreEqual("\\x" + "ab", HyperlaneOASIS.HyperlaneOASIS.ToBytea("0xAB"));
        }

        [TestMethod]
        public async Task Invalid_input_is_rejected_without_calling_the_api()
        {
            Assert.IsTrue((await _provider.GetTransfersByAddressAsync("0xZZ")).IsError);
            Assert.AreEqual(0, _api.Requests.Count);
        }
    }
}
