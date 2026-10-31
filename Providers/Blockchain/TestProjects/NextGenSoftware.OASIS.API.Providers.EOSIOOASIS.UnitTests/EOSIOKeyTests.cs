using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.UnitTests
{
    [TestClass]
    public class EOSIOKeyTests
    {
        // The well-known EOSIO development key pair (eosio genesis/dev key), an independent vector.
        private const string DevPrivateKey = "5KQwrPbwdL6PhXujxW37FSSQZ1JiwsST4cqQzDeyXtP79zkvFD3";
        private const string DevPublicKey = "EOS6MRyAjQq8ud7hVNYcfnVPJqcVpscN5So8BhtHuGYqET5GDW5CV";

        private static EOSIOOASIS Provider() => new("http://127.0.0.1:1", "oasis", "aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906", DevPrivateKey);

        [TestMethod]
        public async Task RestoringTheEosioDevKeyYieldsItsKnownPublicKey()
        {
            var restored = await Provider().RestoreAccountAsync(DevPrivateKey);
            Assert.IsFalse(restored.IsError, restored.Message);
            Assert.AreEqual((DevPublicKey, DevPrivateKey), restored.Result);
        }

        [TestMethod]
        public async Task CreatedKeysAreRealEosioKeysThatRestore()
        {
            var created = await Provider().CreateAccountAsync();
            Assert.IsFalse(created.IsError, created.Message);
            StringAssert.StartsWith(created.Result.PublicKey, "EOS");
            StringAssert.StartsWith(created.Result.PrivateKey, "5");
            Assert.AreEqual(51, created.Result.PrivateKey.Length, "WIF");
            Assert.AreEqual(created.Result.PrivateKey, created.Result.SeedPhrase, "the WIF key is the recovery secret");
            var restored = await Provider().RestoreAccountAsync(created.Result.SeedPhrase);
            Assert.AreEqual((created.Result.PublicKey, created.Result.PrivateKey), restored.Result);
            Assert.AreNotEqual(created.Result.PrivateKey, (await Provider().CreateAccountAsync()).Result.PrivateKey);
        }

        [DataTestMethod]
        [DataRow("abandon ability able about above absent absorb abstract absurd abuse access accident")]
        [DataRow("")]
        [DataRow("5KQwrPbwdL6PhXujxW37FSSQZ1JiwsST4cqQzDeyXtP79zkvFD4")]
        public async Task OnlyRealEosioPrivateKeysRestore(string input)
        {
            var restored = await Provider().RestoreAccountAsync(input);
            Assert.IsTrue(restored.IsError, "phrases and corrupted keys are never hashed into a key");
        }
    }
}
