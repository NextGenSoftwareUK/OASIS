using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class WalletTokenBurnRequestTests
{
    [Fact]
    public void BurnProjectionPreservesAmountAvatarAssetAndCredentials()
    {
        var request = new BurnWeb4TokenRequest
        {
            Amount = 0.123m, BurntByAvatarId = Guid.NewGuid(), Web3TokenId = Guid.NewGuid(),
            TokenAddress = "0.0.1001", OwnerPrivateKey = "test-private-key",
            OwnerPublicKey = "test-public-key", OwnerSeedPhrase = "test-seed"
        };
        var projected = WalletManager.CreateProviderTokenBurnRequest(request);
        Assert.Equal(request.Amount, projected.Amount);
        Assert.Equal(request.BurntByAvatarId, projected.BurntByAvatarId);
        Assert.Equal(request.Web3TokenId, projected.Web3TokenId);
        Assert.Equal(request.TokenAddress, projected.TokenAddress);
        Assert.Equal(request.OwnerPrivateKey, projected.OwnerPrivateKey);
        Assert.Equal(request.OwnerPublicKey, projected.OwnerPublicKey);
        Assert.Equal(request.OwnerSeedPhrase, projected.OwnerSeedPhrase);
        Assert.NotSame(request, projected);
    }

    [Fact]
    public void MissingBurnRequestIsRejected()
        => Assert.Throws<ArgumentNullException>(() => WalletManager.CreateProviderTokenBurnRequest(null!));
}
