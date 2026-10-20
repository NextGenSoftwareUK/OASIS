using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Providers.ElrondOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS.UnitTests
{
    public class ElrondKeyTests
    {
        // MultiversX's published test wallets (mx-sdk-testwallets): one mnemonic, alice at index 0, bob at index 1.
        private const string TestMnemonic = "moral volcano peasant pass circle pen over picture flat shop clap goat never lyrics gather prepare woman film husband gravity behind test tiger improve";

        [Fact]
        public void Rfc8032Ed25519VectorProducesItsPublicKey()
        {
            var keys = ElrondKeys.FromSecretKey(Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60"));
            Assert.Equal("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a", keys.PublicKeyHex);
            Assert.StartsWith("erd1", keys.Address);
        }

        [Theory]
        [InlineData(0u, "erd1qyu5wthldzr8wx5c9ucg8kjagg0jfs53s8nr3zpz3hypefsdd8ssycr6th")]
        [InlineData(1u, "erd1spyavw0956vq68xj8y4tenjpq2wd5a9p2c6j8gsz7ztyrnpxrruqzu66jx")]
        public void MultiversXTestWalletsDeriveTheirKnownAddresses(uint index, string address)
            => Assert.Equal(address, ElrondKeys.FromMnemonic(TestMnemonic, index).Address);

        [Fact]
        public async Task CreatedAccountsRestoreToTheSameKeys()
        {
            var provider = new ElrondOASIS();
            var created = await provider.CreateAccountAsync();
            Assert.False(created.IsError, created.Message);
            Assert.Equal(24, created.Result.SeedPhrase.Split(' ').Length);
            var restored = await provider.RestoreAccountAsync(created.Result.SeedPhrase);
            Assert.Equal((created.Result.PublicKey, created.Result.PrivateKey), restored.Result);
        }

        [Theory]
        [InlineData("hello world this is not a mnemonic")]
        [InlineData("")]
        [InlineData("abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon")]
        public async Task OnlyValidMnemonicsRestore(string phrase)
            => Assert.True((await new ElrondOASIS().RestoreAccountAsync(phrase)).IsError);
    }
}
