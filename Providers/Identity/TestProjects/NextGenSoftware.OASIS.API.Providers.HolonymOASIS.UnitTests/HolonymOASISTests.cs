using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HolonymOASIS.UnitTests
{
    [TestClass]
    public class HolonymOASISTests
    {
        private static HolonymOASIS Unreachable() => new HolonymOASIS("http://127.0.0.1:1");

        [TestMethod]
        public async Task Invalid_inputs_are_rejected_without_rpc_calls()
        {
            var badAddress = await Unreachable().GetSbtAsync("not-an-address", HolonymOASIS.GovIdCircuitId);
            StringAssert.Contains(badAddress.Message, "not a valid EVM address");
            var badCircuit = await Unreachable().GetSbtAsync("0x1111111111111111111111111111111111111111", "0x1234");
            StringAssert.Contains(badCircuit.Message, "32-byte circuit id");
        }

        [TestMethod]
        public async Task Unreachable_rpc_is_an_error_not_a_negative_answer()
        {
            var result = await Unreachable().IsUniqueAsync("0x1111111111111111111111111111111111111111");
            Assert.IsTrue(result.IsError, "a network failure must not be reported as 'not unique'");
        }

        /// <summary>Reads the real Hub V3 on Optimism: a holder that verified government ID and phone in 2027-dated SBTs, and an empty address.</summary>
        [TestMethod, TestCategory("Live")]
        public async Task Live_hub_reports_real_sbts_and_absence()
        {
            var provider = new HolonymOASIS();
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);

            var holder = await provider.GetSbtAsync("0x34Fb5A3eD7d94979f6262DF28DD2a480432F55d3", HolonymOASIS.GovIdCircuitId);
            Assert.IsFalse(holder.IsError, holder.Message);
            Assert.AreEqual(1821393469, holder.Result.Expiry?.ToUnixTimeSeconds());

            var none = await provider.GetSbtAsync("0x1111111111111111111111111111111111111111", HolonymOASIS.PhoneCircuitId);
            Assert.IsFalse(none.IsError, none.Message);
            Assert.AreEqual(HolonymSbtState.None, none.Result.State);
        }
    }
}
