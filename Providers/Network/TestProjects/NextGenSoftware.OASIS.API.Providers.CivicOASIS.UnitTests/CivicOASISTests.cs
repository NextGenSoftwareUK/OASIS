using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.CivicOASIS.UnitTests
{
    [TestClass]
    public class CivicOASISTests
    {
        private static CivicOASIS Unreachable() =>
            new CivicOASIS(new[] { new CivicDeployment("test", "http://127.0.0.1:1", "0x0000000000000000000000000000000000000001") });

        [TestMethod]
        public async Task Invalid_addresses_are_rejected_without_rpc_calls()
        {
            var result = await Unreachable().VerifyPassAsync("not-an-address", CivicOASIS.UniquenessNetwork);
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "not a valid EVM address");
        }

        [TestMethod]
        public async Task Unreachable_rpc_is_an_error_not_a_negative_answer()
        {
            var result = await Unreachable().VerifyPassAsync("0x1111111111111111111111111111111111111111", CivicOASIS.UniquenessNetwork);
            Assert.IsTrue(result.IsError, "a network failure must not be reported as 'no pass'");
        }

        /// <summary>Calls the real GatewayToken contract on Polygon and Ethereum.</summary>
        [TestMethod, TestCategory("Live")]
        public async Task Live_contracts_answer_verifyToken_and_getTokenIds()
        {
            var provider = new CivicOASIS();
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);

            var verify = await provider.VerifyPassAsync("0x1111111111111111111111111111111111111111", CivicOASIS.UniquenessNetwork);
            Assert.IsFalse(verify.IsError, verify.Message);
            Assert.IsFalse(verify.Result);

            var passes = await provider.GetPassesAsync("0x1111111111111111111111111111111111111111", CivicOASIS.UniquenessNetwork);
            Assert.IsFalse(passes.IsError, passes.Message);
            Assert.AreEqual(0, passes.Result.Count);
        }
    }
}
