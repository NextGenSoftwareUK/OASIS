using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class WalletTokenSendRequestTests
{
    [Fact]
    public void ProviderRequestPreservesAssetIdentityAndSigningCredentials()
    {
        var request = new SendWeb4TokenRequest
        {
            Web3TokenId = Guid.NewGuid(), FromTokenAddress = "0x2::sui::SUI",
            FromWalletAddress = "sender", ToWalletAddress = "recipient", Amount = 0.001m,
            MemoText = "must reach provider validation", OwnerPrivateKey = "private-key",
            OwnerPublicKey = "public-key", OwnerSeedPhrase = "seed-phrase"
        };
        var projected = WalletManager.CreateProviderTokenSendRequest(request);
        Assert.Equal(request.Web3TokenId, projected.Web3TokenId);
        Assert.Equal(request.FromTokenAddress, projected.FromTokenAddress);
        Assert.Equal(request.FromWalletAddress, projected.FromWalletAddress);
        Assert.Equal(request.ToWalletAddress, projected.ToWalletAddress);
        Assert.Equal(request.Amount, projected.Amount);
        Assert.Equal(request.MemoText, projected.MemoText);
        Assert.Equal(request.OwnerPrivateKey, projected.OwnerPrivateKey);
        Assert.Equal(request.OwnerPublicKey, projected.OwnerPublicKey);
        Assert.Equal(request.OwnerSeedPhrase, projected.OwnerSeedPhrase);
        Assert.NotSame(request, projected);
    }

    [Fact]
    public void NullRequestIsNotConvertedToAValidEmptyTransfer()
    {
        Assert.Throws<ArgumentNullException>(() => WalletManager.CreateProviderTokenSendRequest(null!));
    }
}
