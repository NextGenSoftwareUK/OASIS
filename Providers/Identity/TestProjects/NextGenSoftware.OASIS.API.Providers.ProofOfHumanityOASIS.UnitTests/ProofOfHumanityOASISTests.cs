using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.ProofOfHumanityOASIS.UnitTests
{
    [TestClass]
    public class ProofOfHumanityOASISTests
    {
        [TestMethod]
        public async Task Invalid_addresses_are_rejected_without_rpc_calls()
        {
            var provider = new ProofOfHumanityOASIS(new[] { new PohDeployment("test", "http://127.0.0.1:1", "0x0000000000000000000000000000000000000001") });
            var result = await provider.GetStatusAsync("not-an-address");
            Assert.IsTrue(result.IsError);
            StringAssert.Contains(result.Message, "not a valid EVM address");
        }

        [TestMethod]
        public async Task Unreachable_rpc_is_an_error_not_a_negative_answer()
        {
            var provider = new ProofOfHumanityOASIS(new[] { new PohDeployment("test", "http://127.0.0.1:1", "0x0000000000000000000000000000000000000001") });
            var result = await provider.IsHumanAsync("0x1111111111111111111111111111111111111111");
            Assert.IsTrue(result.IsError, "a network failure must not be reported as 'not human'");
        }

        /// <summary>Calls the real CrossChainProofOfHumanity contracts on Gnosis and Ethereum.</summary>
        [TestMethod, TestCategory("Live")]
        public async Task Live_contracts_answer_isHuman_on_both_chains()
        {
            var provider = new ProofOfHumanityOASIS();
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);

            var status = await provider.GetStatusAsync("0x1111111111111111111111111111111111111111");
            Assert.IsFalse(status.IsError, status.Message);
            Assert.IsFalse(status.Result.IsHuman);
        }
    }
}
