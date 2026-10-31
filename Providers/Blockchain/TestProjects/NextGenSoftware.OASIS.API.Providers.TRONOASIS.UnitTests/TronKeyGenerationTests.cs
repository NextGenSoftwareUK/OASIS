using Nethereum.Signer;
using TronNet;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronKeyGenerationTests
{
    private sealed class OfflineOnly : TRONOASIS
    {
        public override Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> ActivateProviderAsync()
            => throw new InvalidOperationException("Offline key generation must never activate storage or contact a node.");
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task GeneratedKeyHasRealPublicKeyAndChecksummedTronAddressWithoutActivation(bool synchronous)
    {
        var provider = new OfflineOnly();
        var generated = synchronous ? provider.GenerateKeyPair() : await provider.GenerateKeyPairAsync();
        Assert.False(generated.IsError, generated.Message); Assert.NotNull(generated.Result);
        var result = generated.Result;
        Assert.True(result.PrivateKey.Length == 64 && result.PrivateKey.All(Uri.IsHexDigit));
        var ec = new EthECKey(result.PrivateKey);
        Assert.Equal(Convert.ToHexString(ec.GetPubKey()).ToLowerInvariant(), result.PublicKey);
        var restored = new TronECKey(result.PrivateKey, TronNetwork.MainNet);
        Assert.Equal(restored.GetPublicAddress(), result.WalletAddressLegacy);
        var decoded = NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(result.WalletAddressLegacy);
        Assert.Equal(21, decoded.Length); Assert.Equal(0x41, decoded[0]);
        Assert.Equal(ec.GetPublicAddressAsBytes(), decoded[1..]);
        Assert.True(string.IsNullOrEmpty(result.WalletAddressSegwitP2SH), "TRON has no Segwit address.");
        Assert.False(provider.IsProviderActivated);
        var second = await provider.GenerateKeyPairAsync();
        Assert.False(second.IsError, second.Message);
        Assert.True(result.PrivateKey != second.Result.PrivateKey);
        Assert.NotEqual(result.WalletAddressLegacy, second.Result.WalletAddressLegacy);
    }
}
