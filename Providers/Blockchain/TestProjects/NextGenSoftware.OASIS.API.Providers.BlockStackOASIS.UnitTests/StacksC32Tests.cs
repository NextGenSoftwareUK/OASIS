using System;
using NextGenSoftware.OASIS.API.Providers.BlockStackOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.BlockStackOASIS.UnitTests
{
    public class StacksC32Tests
    {
        // Vectors from the c32check reference implementation (stacks-network/c32check).
        [Theory]
        [InlineData((byte)22, "a46ff88886c2ef9762d970b4d2c63678835bd39d", "SP2J6ZY48GV1EZ5V2V5RB9MP66SW86PYKKNRV9EJ7")]
        [InlineData((byte)0, "0000000000000000000000000000000000000000", "S0000000000000000000002AA028H")]
        public void AddressesMatchC32checkVectors(byte version, string hash160, string expected)
        {
            Assert.Equal(expected, StacksC32.Address(version, Convert.FromHexString(hash160)));
            var (decodedVersion, decodedHash) = StacksC32.Decode(expected);
            Assert.Equal(version, decodedVersion);
            Assert.Equal(hash160, Convert.ToHexString(decodedHash).ToLowerInvariant());
        }

        [Theory]
        [InlineData("SP2J6ZY48GV1EZ5V2V5RB9MP66SW86PYKKNRV9EJ8")] // checksum altered
        [InlineData("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa")]         // a Bitcoin address
        [InlineData("SP")]
        public void InvalidAddressesAreRejected(string address) => Assert.ThrowsAny<FormatException>(() => StacksC32.Decode(address));

        [Fact]
        public void GeneratedAddressesAreMainnetSingleSigAndRoundTrip()
        {
            var key = new NBitcoin.Key();
            var address = StacksC32.Address(StacksC32.MainnetSingleSig, key.PubKey.Hash.ToBytes());
            Assert.StartsWith("SP", address);
            Assert.Equal(key.PubKey.Hash.ToBytes(), StacksC32.Decode(address).Hash160);
        }
    }
}
