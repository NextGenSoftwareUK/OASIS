using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.CrossChain;

namespace NextGenSoftware.OASIS.API.Providers.CrossChain.ProtocolTests
{
    /// <summary>Uses real Axelarscan GMP responses captured as fixtures.</summary>
    [TestClass]
    public class AxelarProtocolTests
    {
        private FakeApi _api;
        private AxelarOASIS.AxelarOASIS _provider;

        [TestInitialize]
        public void Init()
        {
            _api = new FakeApi((req, body) =>
            {
                Assert.AreEqual("/gmp/searchGMP", req.RequestUri.AbsolutePath);
                using var q = JsonDocument.Parse(body);
                if (q.RootElement.TryGetProperty("size", out var size) && size.GetInt32() > 25)
                    return FakeApi.Json("{\"error\":true,\"code\":400,\"message\":\"\\\"size\\\" cannot be more than 25.\"}");
                if (q.RootElement.TryGetProperty("txHash", out var tx) && tx.GetString() == "0xnone") return FakeApi.Json("{\"data\":[],\"total\":0}");
                return FakeApi.Json(FakeApi.Fixture("axelar-gmp.json"));
            });
            _provider = new AxelarOASIS.AxelarOASIS("https://api.axelarscan.io", _api);
        }

        [TestMethod]
        public async Task Executed_and_called_messages_are_normalised()
        {
            var result = await _provider.GetTransfersBySourceTxAsync("0xabc");
            Assert.IsFalse(result.IsError, result.Message);

            var executed = result.Result[0];
            Assert.AreEqual(CrossChainTransferStatus.Completed, executed.Status);
            Assert.AreEqual("osmosis", executed.SourceChain);
            Assert.AreEqual("arbitrum", executed.DestinationChain);
            Assert.AreEqual("0x2eeb8581d13c612054aaeb25d5a49fe7987824fd408b9a1c847d4c04339e967a", executed.DestinationTxHash);
            Assert.AreEqual("axlUSDC", executed.Token);
            Assert.AreEqual("29786904", executed.Amount);

            Assert.AreEqual(CrossChainTransferStatus.Pending, result.Result[1].Status);
            Assert.AreEqual("called", result.Result[1].RawStatus);
        }

        [TestMethod]
        public async Task Address_listing_pages_within_the_25_item_limit()
        {
            var result = await _provider.GetTransfersByAddressAsync("osmo1...", limit: 60);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsTrue(_api.Requests.All(r => JsonDocument.Parse(r.Body).RootElement.GetProperty("size").GetInt32() <= 25));
        }

        [TestMethod]
        public async Task No_match_is_an_empty_list()
        {
            var result = await _provider.GetTransfersBySourceTxAsync("0xnone");
            Assert.IsFalse(result.IsError);
            Assert.AreEqual(0, result.Result.Count);
        }
    }
}
