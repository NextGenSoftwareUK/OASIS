using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers.Crypto;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS.UnitTests
{
    [TestClass]
    public class CardanoKeyTests
    {
        // CIP-19 test vectors: the payment verification key and its type-6 (enterprise) addresses.
        private const string PaymentVerificationKey = "addr_vk1w0l2sr2zgfm26ztc6nl9xy8ghsk5sh6ldwemlpmp9xylzy4dtf7st80zhd";

        [DataTestMethod]
        [DataRow(true, "addr1vx2fxv2umyhttkxyxp8x0dlpdt3k6cwng5pxj3jhsydzers66hrl8")]
        [DataRow(false, "addr_test1vz2fxv2umyhttkxyxp8x0dlpdt3k6cwng5pxj3jhsydzerspjrlsz")]
        public void EnterpriseAddressesMatchCip19Vectors(bool mainnet, string expected)
        {
            var (hrp, key) = Bech32.Decode(PaymentVerificationKey);
            Assert.AreEqual("addr_vk", hrp);
            Assert.AreEqual(expected, CardanoAddress.Enterprise(key, mainnet));
        }

        [TestMethod]
        public async Task CreatedAccountsRestoreFromTheirSeedAndNothingElse()
        {
            var provider = new CardanoOASIS("https://cardano-preprod.blockfrost.io/api/v0", "preprod");
            var created = await provider.CreateAccountAsync();
            Assert.IsFalse(created.IsError, created.Message);
            var restored = await provider.RestoreAccountAsync(created.Result.SeedPhrase);
            Assert.AreEqual((created.Result.PublicKey, created.Result.PrivateKey), restored.Result);
            Assert.IsTrue((await provider.RestoreAccountAsync("correct horse battery staple")).IsError, "text is never hashed into a key");
            Assert.IsTrue((await provider.RestoreAccountAsync(Convert.ToBase64String(new byte[16]))).IsError);
        }
    }
}
